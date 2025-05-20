using danserdan.Models;
using danserdan.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace danserdan.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDBContext _context;
        private readonly AlphaVantageService _alphaVantageService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(ApplicationDBContext context, AlphaVantageService alphaVantageService, ILogger<AdminController> logger)
        {
            _context = context;
            _alphaVantageService = alphaVantageService;
            _logger = logger;
        }

        // Helper method to check if user is admin
        private bool IsAdmin()
        {
            var isAdminString = HttpContext.Session.GetString("IsAdmin");
            return !string.IsNullOrEmpty(isAdminString) && bool.TryParse(isAdminString, out bool isAdmin) && isAdmin;
        }

        public async Task<IActionResult> Index()
        {
            // Check if user is admin
            if (!IsAdmin())
            {
                return RedirectToAction("Index", "Home");
            }

            try
            {
                // Get total number of stocks
                var totalStocks = await _context.Stocks.CountAsync();
                
                // Get total number of users
                var totalUsers = await _context.Users.CountAsync();
                
                // Get active users (users who have logged in within the last 30 days)
                // Since we don't have a last_login field, we'll use users who have made transactions in the last 30 days
                var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
                var activeUserIds = await _context.Transactions
                    .Where(t => t.TransactionTime >= thirtyDaysAgo)
                    .Select(t => t.user_id)
                    .Distinct()
                    .CountAsync();
                
                var activeUsers = activeUserIds > 0 ? activeUserIds : totalUsers / 2; // Fallback if no transactions

                // Get monthly revenue data for the chart (real data from transactions)
                var monthlyData = new Dictionary<string, decimal>();
                var currentYear = DateTime.UtcNow.Year;
                
                // Initialize all months with zero
                var months = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
                foreach (var month in months)
                {
                    monthlyData[month] = 0;
                }
                
                // Get transaction data for the current year AND 2024 (for our dummy data) grouped by month
                var transactionsByMonth = await _context.Transactions
                    .Where(t => (t.TransactionTime.Year == currentYear || t.TransactionTime.Year == 2024) && t.Price > 0)
                    .GroupBy(t => new { t.TransactionTime.Year, t.TransactionTime.Month })
                    .Select(g => new
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Revenue = g.Sum(t => t.Price * t.quantity)
                    })
                    .ToListAsync();
                
                // Fill in the real data
                foreach (var item in transactionsByMonth)
                {
                    var monthName = months[item.Month - 1]; // -1 because months are 1-indexed
                    
                    // If we have data from both years for the same month, sum them
                    if (monthlyData.ContainsKey(monthName))
                    {
                        monthlyData[monthName] += item.Revenue;
                    }
                    else
                    {
                        monthlyData[monthName] = item.Revenue;
                    }
                }
                
                // If no real data, use placeholder data
                if (!transactionsByMonth.Any())
                {
                    monthlyData = new Dictionary<string, decimal>
                    {
                        { "Jan", 5000 },
                        { "Feb", 3000 },
                        { "Mar", 3500 },
                        { "Apr", 3000 },
                        { "May", 3800 },
                        { "Jun", 3200 },
                        { "Jul", 4500 },
                        { "Aug", 4000 },
                        { "Sep", 3800 },
                        { "Oct", 4200 },
                        { "Nov", 2800 },
                        { "Dec", 4000 }
                    };
                }

                // Get recent transactions with user details
                var transactionData = await _context.Transactions
                    .Include(t => t.User)
                    .OrderByDescending(t => t.TransactionTime)
                    .Take(5)
                    .ToListAsync();
                
                List<object> recentTransactions = new List<object>();
                
                if (transactionData.Any())
                {
                    foreach (var t in transactionData)
                    {
                        var stock = _context.Stocks.FirstOrDefault(s => s.stock_id == t.StockId);
                        bool isFundsTransaction = stock == null;
                        string transactionType;
                        string stockSymbol;
                        
                        if (isFundsTransaction)
                        {
                            // This is a funds transaction (Add Funds or Payout)
                            transactionType = t.Price > 0 ? "addfunds" : "payout";
                            stockSymbol = t.Price > 0 ? "ADD FUNDS" : "PAYOUT";
                        }
                        else
                        {
                            // This is a stock transaction (Buy or Sell)
                            transactionType = t.Price > 0 ? "buy" : "sell";
                            stockSymbol = stock?.symbol ?? "Unknown";
                        }
                        
                        recentTransactions.Add(new {
                            UserName = $"{t.User?.firstName ?? ""} {t.User?.lastName ?? ""}".Trim(),
                            Email = t.User?.email ?? "unknown@email.com",
                            Amount = Math.Abs(t.Price * t.quantity),
                            StockSymbol = stockSymbol,
                            TransactionType = transactionType,
                            Date = t.TransactionTime
                        });
                    }
                }
                
                // Fallback if no transactions
                if (!recentTransactions.Any())
                {
                    recentTransactions.Add(new { UserName = "Olivia Martin", Email = "olivia.martin@email.com", Amount = 1999.00m, StockSymbol = "AAPL", TransactionType = "buy", Date = DateTime.UtcNow.AddDays(-1) });
                    recentTransactions.Add(new { UserName = "Jackson Lee", Email = "jackson.lee@email.com", Amount = 39.00m, StockSymbol = "MSFT", TransactionType = "buy", Date = DateTime.UtcNow.AddDays(-2) });
                    recentTransactions.Add(new { UserName = "Isabella Nguyen", Email = "isabella.nguyen@email.com", Amount = 299.00m, StockSymbol = "GOOG", TransactionType = "buy", Date = DateTime.UtcNow.AddDays(-3) });
                    recentTransactions.Add(new { UserName = "William Kim", Email = "will@email.com", Amount = 99.00m, StockSymbol = "AMZN", TransactionType = "buy", Date = DateTime.UtcNow.AddDays(-4) });
                    recentTransactions.Add(new { UserName = "Sofia Davis", Email = "sofia.davis@email.com", Amount = 39.00m, StockSymbol = "TSLA", TransactionType = "buy", Date = DateTime.UtcNow.AddDays(-5) });
                }

                // Get total number of transactions
                var totalTransactions = await _context.Transactions.CountAsync();

                // Create view model
                var viewModel = new AdminDashboardViewModel
                {
                    TotalStocks = totalStocks,
                    TotalUsers = totalUsers,
                    ActiveUsers = activeUsers,
                    TotalTransactions = totalTransactions,
                    MonthlyData = monthlyData,
                    RecentTransactions = recentTransactions
                };
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin dashboard");
                return View(new AdminDashboardViewModel
                {
                    TotalStocks = 0,
                    TotalUsers = 0,
                    ActiveUsers = 0,
                    MonthlyData = new Dictionary<string, decimal>(),
                    RecentTransactions = new List<object>()
                });
            }
        }

        public async Task<IActionResult> AllStocks()
        {
            // Check if user is admin
            if (!IsAdmin())
            {
                return RedirectToAction("Index", "Home");
            }

            try
            {
                // Check if we need to seed the database with more stocks
                var stockCount = await _context.Stocks.CountAsync();
                if (stockCount < 35)
                {
                    await SeedStocksAsync(35 - stockCount);
                }
                
                // Get all stocks ordered by symbol for consistent display
                var stocks = await _context.Stocks
                    .OrderBy(s => s.symbol)
                    .ToListAsync();
                
                // Set the total stocks count for the pagination
                ViewBag.TotalStocksCount = stocks.Count;
                
                return View(stocks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all stocks");
                return RedirectToAction("Index", "Admin");
            }
        }
        
        private async Task SeedStocksAsync(int countToAdd)
        {
            // List of stock symbols and company names to add
            var stocksToAdd = new List<(string symbol, string name, string sector)>
            {
                // Technology
                ("AAPL", "Apple Inc.", "Technology"),
                ("MSFT", "Microsoft Corporation", "Technology"),
                ("GOOGL", "Alphabet Inc.", "Technology"),
                ("META", "Meta Platforms Inc.", "Technology"),
                ("NVDA", "NVIDIA Corporation", "Technology"),
                ("INTC", "Intel Corporation", "Technology"),
                ("AMD", "Advanced Micro Devices, Inc.", "Technology"),
                ("CRM", "Salesforce, Inc.", "Technology"),
                ("ADBE", "Adobe Inc.", "Technology"),
                ("CSCO", "Cisco Systems, Inc.", "Technology"),
                
                // E-Commerce
                ("AMZN", "Amazon.com, Inc.", "E-Commerce"),
                ("BABA", "Alibaba Group Holding Limited", "E-Commerce"),
                ("SHOP", "Shopify Inc.", "E-Commerce"),
                ("ETSY", "Etsy, Inc.", "E-Commerce"),
                ("EBAY", "eBay Inc.", "E-Commerce"),
                
                // Automotive
                ("TSLA", "Tesla, Inc.", "Automotive"),
                ("F", "Ford Motor Company", "Automotive"),
                ("GM", "General Motors Company", "Automotive"),
                ("TM", "Toyota Motor Corporation", "Automotive"),
                ("RIVN", "Rivian Automotive, Inc.", "Automotive"),
                
                // Financial Services
                ("JPM", "JPMorgan Chase & Co.", "Financial Services"),
                ("V", "Visa Inc.", "Financial Services"),
                ("MA", "Mastercard Incorporated", "Financial Services"),
                ("BAC", "Bank of America Corporation", "Financial Services"),
                ("GS", "The Goldman Sachs Group, Inc.", "Financial Services"),
                
                // Healthcare
                ("JNJ", "Johnson & Johnson", "Healthcare"),
                ("PFE", "Pfizer Inc.", "Healthcare"),
                ("MRK", "Merck & Co., Inc.", "Healthcare"),
                ("UNH", "UnitedHealth Group Incorporated", "Healthcare"),
                ("ABBV", "AbbVie Inc.", "Healthcare"),
                
                // Conglomerate
                ("BRK.A", "Berkshire Hathaway Inc.", "Conglomerate"),
                ("GE", "General Electric Company", "Conglomerate"),
                
                // Telecommunications
                ("VZ", "Verizon Communications Inc.", "Telecommunications"),
                ("T", "AT&T Inc.", "Telecommunications"),
                ("TMUS", "T-Mobile US, Inc.", "Telecommunications")
            };
            
            // Get existing stock symbols
            var existingSymbols = await _context.Stocks.Select(s => s.symbol).ToListAsync();
            
            // Filter out stocks that already exist
            var newStocks = stocksToAdd
                .Where(s => !existingSymbols.Contains(s.symbol))
                .Take(countToAdd)
                .ToList();
            
            // Add new stocks to the database
            var random = new Random();
            foreach (var stock in newStocks)
            {
                // Generate random price between $50 and $500
                decimal marketPrice = Math.Round((decimal)(random.NextDouble() * 450 + 50), 2);
                
                // Generate random open price within 5% of market price
                decimal openPrice = Math.Round(marketPrice * (decimal)(0.95 + random.NextDouble() * 0.1), 2);
                
                _context.Stocks.Add(new Models.Stocks
                {
                    symbol = stock.symbol,
                    company_name = stock.name,
                    market_price = marketPrice,
                    open_price = openPrice,
                    open_price_time = DateTime.UtcNow.AddHours(-random.Next(1, 8)),
                    last_updated = DateTime.UtcNow,
                    IsAvailable = true
                });
            }
            
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Added {newStocks.Count} new stocks to the database");
        }
        
        public async Task<IActionResult> AllTransactions()
        {
            try
            {
                var transactions = await _context.Transactions
                    .Include(t => t.User)
                    .OrderByDescending(t => t.TransactionTime)
                    .Take(100) // Limit to 100 most recent transactions
                    .ToListAsync();
                
                return View(transactions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving transactions");
                return RedirectToAction("Index", "Admin");
            }
        }
        
        public async Task<IActionResult> AllUsers()
        {
            try
            {
                var users = await _context.Users.ToListAsync();
                return View(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all users");
                return RedirectToAction("Index", "Admin");
            }
        }
        
        [HttpPost]
        public async Task<IActionResult> UpdateStockAvailability(int stockId, bool isAvailable)
        {
            try
            {
                var stock = await _context.Stocks.FindAsync(stockId);
                if (stock == null)
                {
                    return Json(new { success = false, message = "Stock not found" });
                }
                
                // Update availability status
                stock.IsAvailable = isAvailable;
                await _context.SaveChangesAsync();
                
                string statusMessage = isAvailable ? "available" : "unavailable";
                return Json(new { 
                    success = true, 
                    message = $"Stock {stock.symbol} is now {statusMessage} for trading"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating stock availability for stock ID {stockId}");
                return Json(new { success = false, message = "An error occurred while updating stock availability" });
            }
        }
    }
}
