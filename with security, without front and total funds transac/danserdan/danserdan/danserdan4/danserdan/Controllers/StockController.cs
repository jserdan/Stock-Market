using danserdan.Models;
using danserdan.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace danserdan.Controllers
{
    public class StockController : Controller
    {
        private readonly AlphaVantageService _alphaVantageService;
        private readonly ApplicationDBContext _context;
        private readonly ILogger<StockController> _logger;

        public StockController(AlphaVantageService alphaVantageService, ApplicationDBContext context, ILogger<StockController> logger)
        {
            _alphaVantageService = alphaVantageService;
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            try
            {
                // Get paginated list of stocks
                var totalStocks = await _context.Stocks.CountAsync();
                var stocks = await _context.Stocks
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Create view model for pagination
                var viewModel = new PaginatedList<Stocks>(stocks, totalStocks, page, pageSize);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading stocks");
                return View(new PaginatedList<Stocks>(new List<Stocks>(), 0, 1, 10));
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetStockPrice(string symbol)
        {
            try
            {
                var stockData = await _alphaVantageService.GetStockDataAsync(symbol);
                if (stockData != null)
                {
                    return Json(new
                    {
                        success = true,
                        price = stockData.market_price,
                        symbol = stockData.symbol
                    });
                }
                return Json(new { success = false, message = "Unable to fetch price" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching price for {symbol}");
                return Json(new { success = false, message = "Error fetching stock price" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> TradeStock(string symbol, int quantity, string transactionType, int? stockId = null)
        {
            try
            {
                _logger.LogInformation($"TradeStock called with symbol={symbol}, quantity={quantity}, transactionType={transactionType}, stockId={stockId}");
                
                // Get user from session
                string? userEmail = HttpContext.Session.GetString("UserEmail");
                if (string.IsNullOrEmpty(userEmail))
                {
                    return Json(new { success = false, message = "You must be logged in to trade stocks" });
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.email == userEmail);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                // Get current stock price - try by stockId first if provided
                Stocks stock = null;
                if (stockId.HasValue && stockId.Value > 0)
                {
                    stock = await _context.Stocks.FindAsync(stockId.Value);
                }
                
                // If not found by ID or ID not provided, try by symbol
                if (stock == null && !string.IsNullOrEmpty(symbol))
                {
                    stock = await _context.Stocks.FirstOrDefaultAsync(s => s.symbol == symbol);
                }
                
                if (stock == null)
                {
                    return Json(new { success = false, message = $"Stock {symbol} not found. Please try again." });
                }
                
                // Make sure we have the symbol for later use
                symbol = stock.symbol;
                
                // Check if the stock is available for trading
                if (!stock.IsAvailable)
                {
                    return Json(new { success = false, message = $"Stock {symbol} is currently unavailable for trading" });
                }
                
                // Try to get the latest price from Alpha Vantage
                try
                {
                    var updatedStockData = await _alphaVantageService.GetStockDataAsync(symbol);
                    if (updatedStockData != null)
                    {
                        // Update the stock price in the database
                        stock.market_price = decimal.Parse(updatedStockData.market_price);
                        stock.last_updated = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                        _logger.LogInformation($"Updated stock price for {symbol} to {stock.market_price}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Failed to update stock price for {symbol}: {ex.Message}");
                    // Continue with the existing price if we can't get an update
                }

                decimal stockPrice = stock.market_price;
                decimal totalAmount = stockPrice * quantity;

                // Validate transaction
                if (transactionType.ToLower() == "buy")
                {
                    // Check if user has enough balance
                    if (user.balance < totalAmount)
                    {
                        return Json(new { success = false, message = "Insufficient funds" });
                    }

                    // Deduct from user balance
                    user.balance -= totalAmount;
                }
                else if (transactionType.ToLower() == "sell")
                {
                    // Check if user owns the stock by calculating their current holdings
                    var userOwnsStock = await _context.Transactions
                        .Where(t => t.user_id == user.user_id && t.StockId == stock.stock_id && t.Price > 0)
                        .SumAsync(t => t.quantity);

                    var userSoldStock = await _context.Transactions
                        .Where(t => t.user_id == user.user_id && t.StockId == stock.stock_id && t.Price < 0)
                        .SumAsync(t => t.quantity);

                    int userStockBalance = userOwnsStock - userSoldStock;

                    if (userStockBalance < quantity)
                    {
                        return Json(new { 
                            success = false, 
                            message = $"You don't own enough shares of {symbol}", 
                            currentShares = userStockBalance,
                            requestedShares = quantity
                        });
                    }

                    // Add to user balance
                    user.balance += totalAmount;
                }
                else
                {
                    return Json(new { success = false, message = "Invalid transaction type" });
                }

                // Create transaction record
                var transaction = new Transaction
                {
                    user_id = user.user_id,
                    StockId = stock.stock_id,
                    quantity = quantity,
                    Price = transactionType.ToLower() == "buy" ? stockPrice : -stockPrice,
                    TransactionTime = DateTime.UtcNow
                };

                // Save changes
                _context.Transactions.Add(transaction);
                await _context.SaveChangesAsync();

                // Calculate total shares owned after transaction
                int totalShares = 0;
                if (transactionType.ToLower() == "buy")
                {
                    var userOwnsStock = await _context.Transactions
                        .Where(t => t.user_id == user.user_id && t.StockId == stock.stock_id && t.Price > 0)
                        .SumAsync(t => t.quantity);

                    var userSoldStock = await _context.Transactions
                        .Where(t => t.user_id == user.user_id && t.StockId == stock.stock_id && t.Price < 0)
                        .SumAsync(t => t.quantity);

                    totalShares = userOwnsStock - userSoldStock;
                }
                
                string actionText = transactionType.ToLower() == "buy" ? "purchased" : "sold";
                decimal newBalance = user.balance ?? 0m;
                
                // Calculate PHP equivalents using the fixed exchange rate of 56.5 PHP to 1 USD
                decimal phpTotalAmount = decimal.Round(totalAmount * 56.5m, 2);
                decimal phpNewBalance = decimal.Round(newBalance * 56.5m, 2);
                
                return Json(new { 
                    success = true, 
                    message = $"You have {actionText} {quantity} shares of {symbol} for ${totalAmount:F2} (₱{phpTotalAmount:F2}). Your new balance is ${newBalance:F2} (₱{phpNewBalance:F2}).",
                    newBalance = newBalance,
                    transactionId = transaction.transaction_id,
                    totalShares = totalShares
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing {transactionType} transaction for {symbol}");
                return Json(new { success = false, message = "Error processing transaction" });
            }
        }
        
        [HttpGet]
        public async Task<IActionResult> RefreshPortfolioPrices()
        {
            try
            {
                // Get user from session
                string? userEmail = HttpContext.Session.GetString("UserEmail");
                if (string.IsNullOrEmpty(userEmail))
                {
                    return Json(new { success = false, message = "You must be logged in to refresh your portfolio" });
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.email == userEmail);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }
                
                // Get all stocks the user owns
                var stockIds = await _context.Transactions
                    .Where(t => t.user_id == user.user_id)
                    .Select(t => t.StockId)
                    .Distinct()
                    .ToListAsync();
                    
                var updatedStocks = new List<object>();
                
                foreach (var stockId in stockIds)
                {
                    var stock = await _context.Stocks.FindAsync(stockId);
                    if (stock == null) continue;
                    
                    // Calculate shares owned
                    var sharesBought = await _context.Transactions
                        .Where(t => t.user_id == user.user_id && t.StockId == stockId && t.Price > 0)
                        .SumAsync(t => t.quantity);
                        
                    var sharesSold = await _context.Transactions
                        .Where(t => t.user_id == user.user_id && t.StockId == stockId && t.Price < 0)
                        .SumAsync(t => t.quantity);
                        
                    var sharesOwned = sharesBought - sharesSold;
                    
                    // Only include stocks that the user still owns
                    if (sharesOwned <= 0) continue;
                    
                    // Try to update the stock price
                    try
                    {
                        var updatedStockData = await _alphaVantageService.GetStockDataAsync(stock.symbol);
                        if (updatedStockData != null)
                        {
                            // Update the stock price in the database
                            stock.market_price = decimal.Parse(updatedStockData.market_price);
                            stock.last_updated = DateTime.UtcNow;
                            await _context.SaveChangesAsync();
                            
                            updatedStocks.Add(new
                            {
                                stockId = stock.stock_id,
                                symbol = stock.symbol,
                                price = stock.market_price,
                                lastUpdated = stock.last_updated
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Failed to update stock price for {stock.symbol}: {ex.Message}");
                    }
                }
                
                return Json(new { success = true, updatedStocks });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing portfolio prices");
                return Json(new { success = false, message = "Error refreshing portfolio prices" });
            }
        }
    }
}