using danserdan.Services;
using danserdan.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text;
using System.Security.Cryptography;
using System.Linq;

namespace danserdan.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDBContext _context;
        private readonly AlphaVantageService _alphaVantageService;

        public HomeController(ILogger<HomeController> logger, ApplicationDBContext context, AlphaVantageService alphaVantageService)
        {
            _logger = logger;
            _context = context;
            _alphaVantageService = alphaVantageService;
        }

        public async Task<IActionResult> Index()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (userEmail != null)
            {
                ViewBag.UserEmail = userEmail;
            }

            try
            {
                var stockData = await _alphaVantageService.GetStockDataAsync("TSLA");
                ViewBag.TeslaPrice = stockData != null ? stockData.market_price : "Unable to fetch price";

                _logger.LogInformation($"Initial Tesla price: {ViewBag.TeslaPrice}");
                
                // Get top gainers for Market Movers section
                var stocks = await _context.Stocks.ToListAsync();
                var topGainers = new List<StockViewModel>();
                
                foreach (var stock in stocks.Take(10)) // Process first 10 stocks for performance
                {
                    try
                    {
                        // Calculate change percent based on database values
                        var marketPrice = stock.market_price;
                        var openPrice = stock.open_price ?? marketPrice;
                        var changePercent = openPrice != 0 ? ((marketPrice - openPrice) / openPrice) * 100 : 0;
                        var changeValue = (marketPrice - openPrice >= 0 ? "+" : "") + changePercent.ToString("0.00") + "%";
                        var changeClass = (marketPrice - openPrice) >= 0 ? "text-success" : "text-danger";

                        // Determine the color based on sector
                        string color;
                        switch (stock.sector ?? "Other")
                        {
                            case "Automotive": color = "#f43f5e"; break;
                            case "Technology": color = "#3b82f6"; break;
                            case "E-Commerce": color = "#eab308"; break;
                            case "Financial Services": color = "#14b8a6"; break;
                            case "Conglomerate": color = "#a3e635"; break;
                            case "Healthcare": color = "#ec4899"; break;
                            case "Energy": color = "#f97316"; break;
                            case "Consumer Goods": color = "#10b981"; break;
                            case "Telecommunications": color = "#7c3aed"; break;
                            case "Manufacturing": color = "#2563eb"; break;
                            default: color = "#4ade80"; break;
                        }

                        // Create view model with all required properties
                        var viewModel = new StockViewModel
                        {
                            Id = stock.stock_id,
                            Symbol = stock.symbol,
                            Name = stock.company_name,
                            Sector = stock.sector ?? "Other",
                            Price = "$" + marketPrice.ToString("F2"),
                            Change = changeValue,
                            ChangeClass = changeClass,
                            Color = color,
                            Hour1 = GetRandomChange(changeClass == "text-success"),
                            Hour24 = GetRandomChange(changeClass == "text-success"),
                            Days7 = GetRandomChange(changeClass == "text-success"),
                            Hour1Class = changeClass,
                            Hour24Class = changeClass,
                            Days7Class = changeClass,
                            ChartData = new List<ChartDataset>()
                        };
                        
                        topGainers.Add(viewModel);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error processing stock {stock.symbol}");
                    }
                }
                
                // Sort by change percent (calculated from Change property)
                topGainers = topGainers
                    .OrderByDescending(s => {
                        // Extract numeric value from change string (e.g., "+2.5%" -> 2.5)
                        string changeStr = s.Change.TrimStart('+').TrimEnd('%');
                        if (decimal.TryParse(changeStr, out decimal changeVal))
                            return changeVal;
                        return 0m;
                    })
                    .Take(3)
                    .ToList();
                
                ViewBag.Gainers = topGainers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching initial Tesla price");
                ViewBag.TeslaPrice = "Unable to fetch price";
            }

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetTeslaPrice()
        {
            try
            {
                var stockData = await _alphaVantageService.GetStockDataAsync("TSLA");
                if (stockData != null && !string.IsNullOrEmpty(stockData.market_price))
                {
                    _logger.LogInformation($"Retrieved Tesla price via API: {stockData.market_price}");

                    return Json(new
                    {
                        success = true,
                        price = stockData.market_price,
                        symbol = stockData.symbol
                    });
                }

                _logger.LogWarning("Failed to get valid Tesla price");
                return Json(new { success = false, message = "No price data available" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Tesla price for real-time update");
                return Json(new { success = false, message = "Error fetching price" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetStockUpdates()
        {
            try
            {
                // Define the stock symbols we want to display
                var symbols = new[] { "TSLA", "AAPL", "MSFT", "AMZN", "NVDA", "GOOGL", "META", "JPM", "BRK.A", "V" };
                var stockDataList = new List<object>();

                // Fetch data for each stock
                foreach (var symbol in symbols)
                {
                    try
                    {
                        var stockData = await _alphaVantageService.GetStockDataAsync(symbol);
                        if (stockData != null)
                        {
                            // Get historical data for the chart
                            var historicalData = await _alphaVantageService.GetHistoricalDataAsync(symbol);

                            // Determine if the stock is up or down based on the change percent
                            string changeClass = "text-success";
                            string changeValue = "+0.00%";
                            
                            // Calculate change percent based on database values
                            if (decimal.TryParse(stockData.market_price, out decimal marketPrice) &&
                                decimal.TryParse(stockData.open_price, out decimal openPrice) && openPrice != 0)
                            {
                                decimal diff = marketPrice - openPrice;
                                decimal percent = (diff / openPrice) * 100;
                                changeValue = (diff >= 0 ? "+" : "") + percent.ToString("0.00") + "%";
                                changeClass = diff >= 0 ? "text-success" : "text-danger";
                            }

                            // Store the raw price value without currency symbol for conversion
                            string rawPrice = stockData.market_price;
                            if (rawPrice.StartsWith("$"))
                            {
                                rawPrice = rawPrice.Substring(1);
                            }

                            // Create response object
                            stockDataList.Add(new
                            {
                                symbol = symbol,
                                price = stockData.market_price.StartsWith("$") ? stockData.market_price : $"${stockData.market_price}",
                                priceUsd = rawPrice, // Add raw USD price for currency conversion
                                change = changeValue,
                                changeClass = changeClass,
                                open_price = stockData.open_price,
                                open_price_time = stockData.open_price_time,
                                chartData = historicalData
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error fetching data for {symbol}");
                    }
                }

                return Json(new { success = true, stocks = stockDataList });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetStockUpdates");
                return Json(new { success = false, message = "Error fetching stock updates" });
            }
        }

        public async Task<IActionResult> Stocks(int page = 1, int pageSize = 5, string? sector = null, string? market = null)
        {
            try
            {
                // Fetch all available stocks from the database
                var stocksQuery = _context.Stocks.AsQueryable();

                // Apply sector filter if provided
                if (!string.IsNullOrEmpty(sector))
                {
                    stocksQuery = stocksQuery.Where(s => s.sector != null && s.sector.ToLower() == sector.ToLower());
                    // Log the filter being applied
                    _logger.LogInformation($"Applying sector filter: {sector}");
                }

                var stocks = await stocksQuery.ToListAsync();
                var stockDataList = new List<StockViewModel>();

                foreach (var stock in stocks)
                {
                    try
                    {
                        // Use the latest market price and open price from the database
                        var marketPrice = stock.market_price;
                        var openPrice = stock.open_price ?? marketPrice;
                        var changePercent = openPrice != 0 ? ((marketPrice - openPrice) / openPrice) * 100 : 0;
                        var changeValue = (marketPrice - openPrice >= 0 ? "+" : "") + changePercent.ToString("0.00") + "%";
                        var changeClass = (marketPrice - openPrice) >= 0 ? "text-success" : "text-danger";

                        // Determine the color based on sector
                        string color;
                        switch (stock.sector)
                        {
                            case "Automotive": color = "#f43f5e"; break;
                            case "Technology": color = "#3b82f6"; break;
                            case "E-Commerce": color = "#eab308"; break;
                            case "Financial Services": color = "#14b8a6"; break;
                            case "Conglomerate": color = "#a3e635"; break;
                            case "Healthcare": color = "#ec4899"; break;
                            case "Energy": color = "#f97316"; break;
                            case "Consumer Goods": color = "#10b981"; break;
                            case "Telecommunications": color = "#7c3aed"; break;
                            case "Manufacturing": color = "#2563eb"; break;
                            default: color = "#4ade80"; break;
                        }

                        // Apply market filter (gainers/losers)
                        if (!string.IsNullOrEmpty(market))
                        {
                            if (market == "gainers" && changePercent <= 0) continue;
                            if (market == "losers" && changePercent >= 0) continue;
                        }

                        // Create view model
                        var viewModel = new StockViewModel
                        {
                            Id = stock.stock_id,
                            Symbol = stock.symbol,
                            Name = stock.company_name,
                            Sector = stock.sector ?? "Other",
                            Price = "$" + marketPrice.ToString("F2"),
                            Change = changeValue,
                            ChangeClass = changeClass,
                            Color = color,
                            Hour1 = GetRandomChange(changeClass == "text-success"),
                            Hour24 = GetRandomChange(changeClass == "text-success"),
                            Days7 = GetRandomChange(changeClass == "text-success"),
                            Hour1Class = changeClass,
                            Hour24Class = changeClass,
                            Days7Class = changeClass,
                            ChartData = new List<ChartDataset>() // You can fetch real chart data if needed
                        };
                        stockDataList.Add(viewModel);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error processing stock {stock.symbol}");
                    }
                }

                // Fallback to sample data if no stocks found
                if (!stockDataList.Any())
                {
                    stockDataList = GetSampleStockData();
                    
                    // Apply sector filter to sample data if needed
                    if (!string.IsNullOrEmpty(sector))
                    {
                        stockDataList = stockDataList.Where(s => s.Sector != null && 
                                                              s.Sector.ToLower() == sector.ToLower()).ToList();
                        _logger.LogInformation($"Applied sector filter to sample data: {sector}");
                    }
                }

                // Get all available sectors for the filter dropdown
                var allSectors = new List<string> {
                    "Automotive", "Technology", "E-Commerce", "Financial Services", 
                    "Conglomerate", "Healthcare", "Energy", "Consumer Goods", 
                    "Telecommunications", "Manufacturing"
                };
                
                // Add any additional sectors from the database that aren't in our predefined list
                var dbSectors = stocks.Select(s => s.sector).Distinct().Where(s => !string.IsNullOrEmpty(s)).ToList();
                foreach (var sectorName in dbSectors)
                {
                    if (!allSectors.Contains(sectorName))
                    {
                        allSectors.Add(sectorName);
                    }
                }
                
                ViewBag.Sectors = allSectors;
                ViewBag.SelectedSector = sector;
                ViewBag.SelectedMarket = market;

                // Pagination
                int totalItems = stockDataList.Count;
                int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
                if (page > totalPages && totalPages > 0) page = totalPages;
                var paginatedItems = stockDataList.Skip((page - 1) * pageSize).Take(pageSize).ToList();
                var model = new PaginatedList<StockViewModel>(paginatedItems, totalItems, page, pageSize);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Stocks action");
                var sampleData = GetSampleStockData();
                var paginatedSampleData = new PaginatedList<StockViewModel>(sampleData, sampleData.Count, 1, 6);
                return View(paginatedSampleData);
            }
        }

        private string GetRandomChange(bool isPositive)
        {
            var random = new Random();
            double change = random.NextDouble() * 5.0;
            return isPositive ? $"+{change:F1}%" : $"-{change:F1}%";
        }

        private string GetCompanyName(string symbol)
        {
            return symbol switch
            {
                // Original stocks
                "TSLA" => "Tesla Inc.",
                "AAPL" => "Apple Inc.",
                "MSFT" => "Microsoft Corp.",
                "AMZN" => "Amazon.com Inc.",
                "NVDA" => "NVIDIA Corp.",
                "GOOGL" => "Alphabet Inc.",
                "META" => "Meta Platforms Inc.",
                "JPM" => "JPMorgan Chase & Co.",
                "BRK.A" => "Berkshire Hathaway Inc.",
                "V" => "Visa Inc.",
                
                // Healthcare sector
                "JNJ" => "Johnson & Johnson",
                "PFE" => "Pfizer Inc.",
                "MRK" => "Merck & Co.",
                "UNH" => "UnitedHealth Group",
                "ABT" => "Abbott Laboratories",
                
                // Energy sector
                "XOM" => "Exxon Mobil Corp.",
                "CVX" => "Chevron Corp.",
                "COP" => "ConocoPhillips",
                "BP" => "BP p.l.c.",
                "SLB" => "Schlumberger Ltd.",
                
                // Consumer Goods
                "PG" => "Procter & Gamble Co.",
                "KO" => "Coca-Cola Co.",
                "PEP" => "PepsiCo Inc.",
                "WMT" => "Walmart Inc.",
                "COST" => "Costco Wholesale Corp.",
                
                // Telecommunications
                "VZ" => "Verizon Communications",
                "T" => "AT&T Inc.",
                "TMUS" => "T-Mobile US Inc.",
                "VOD" => "Vodafone Group Plc",
                "ERIC" => "Ericsson",
                
                // Manufacturing
                "GE" => "General Electric Co.",
                "MMM" => "3M Co.",
                "CAT" => "Caterpillar Inc.",
                "DE" => "Deere & Co.",
                "BA" => "Boeing Co.",
                
                _ => $"{symbol} Inc."
            };
        }

        private string GetCompanySector(string symbol)
        {
            return symbol switch
            {
                // Original stocks
                "TSLA" => "Automotive",
                "AAPL" => "Technology",
                "MSFT" => "Technology",
                "AMZN" => "E-Commerce",
                "NVDA" => "Technology",
                "GOOGL" => "Technology",
                "META" => "Technology",
                "JPM" => "Financial Services",
                "BRK.A" => "Conglomerate",
                "V" => "Financial Services",
                
                // Healthcare sector
                "JNJ" => "Healthcare",
                "PFE" => "Healthcare",
                "MRK" => "Healthcare",
                "UNH" => "Healthcare",
                "ABT" => "Healthcare",
                
                // Energy sector
                "XOM" => "Energy",
                "CVX" => "Energy",
                "COP" => "Energy",
                "BP" => "Energy",
                "SLB" => "Energy",
                
                // Consumer Goods
                "PG" => "Consumer Goods",
                "KO" => "Consumer Goods",
                "PEP" => "Consumer Goods",
                "WMT" => "Consumer Goods",
                "COST" => "Consumer Goods",
                
                // Telecommunications
                "VZ" => "Telecommunications",
                "T" => "Telecommunications",
                "TMUS" => "Telecommunications",
                "VOD" => "Telecommunications",
                "ERIC" => "Telecommunications",
                
                // Manufacturing
                "GE" => "Manufacturing",
                "MMM" => "Manufacturing",
                "CAT" => "Manufacturing",
                "DE" => "Manufacturing",
                "BA" => "Manufacturing",
                
                _ => "Other"
            };
        }

        private int GetStockId(string symbol)
        {
            return symbol switch
            {
                // Original stocks
                "TSLA" => 1,
                "AAPL" => 2,
                "MSFT" => 3,
                "AMZN" => 4,
                "NVDA" => 5,
                "GOOGL" => 6,
                "META" => 7,
                "JPM" => 8,
                "BRK.A" => 9,
                "V" => 10,
                
                // Healthcare sector
                "JNJ" => 11,
                "PFE" => 12,
                "MRK" => 13,
                "UNH" => 14,
                "ABT" => 15,
                
                // Energy sector
                "XOM" => 16,
                "CVX" => 17,
                "COP" => 18,
                "BP" => 19,
                "SLB" => 20,
                
                // Consumer Goods
                "PG" => 21,
                "KO" => 22,
                "PEP" => 23,
                "WMT" => 24,
                "COST" => 25,
                
                // Telecommunications
                "VZ" => 26,
                "T" => 27,
                "TMUS" => 28,
                "VOD" => 29,
                "ERIC" => 30,
                
                // Manufacturing
                "GE" => 31,
                "MMM" => 32,
                "CAT" => 33,
                "DE" => 34,
                "BA" => 35,
                
                _ => 0
            };
        }

        private List<StockViewModel> GetSampleStockData()
        {
            var result = new List<StockViewModel>
            {
                // Original stocks
                new StockViewModel { Id = 1, Symbol = "TSLA", Name = "Tesla Inc.", Sector = "Automotive", Price = "$193.75", Change = "+5.42%", ChangeClass = "text-success", Color = "#f43f5e", Hour1 = "+1.2%", Hour24 = "+3.8%", Days7 = "+7.5%", Hour1Class = "text-success", Hour24Class = "text-success", Days7Class = "text-success", ChartData = new List<ChartDataset>() },
                new StockViewModel { Id = 2, Symbol = "AAPL", Name = "Apple Inc.", Sector = "Technology", Price = "$175.45", Change = "-2.13%", ChangeClass = "text-danger", Color = "#3b82f6", Hour1 = "-0.5%", Hour24 = "-1.7%", Days7 = "-4.2%", Hour1Class = "text-danger", Hour24Class = "text-danger", Days7Class = "text-danger", ChartData = new List<ChartDataset>() },
                new StockViewModel { Id = 3, Symbol = "MSFT", Name = "Microsoft Corp.", Sector = "Technology", Price = "$402.15", Change = "+0.88%", ChangeClass = "text-success", Color = "#3b82f6", Hour1 = "+0.3%", Hour24 = "+0.9%", Days7 = "+2.1%", Hour1Class = "text-success", Hour24Class = "text-success", Days7Class = "text-success", ChartData = new List<ChartDataset>() },
                new StockViewModel { Id = 4, Symbol = "AMZN", Name = "Amazon.com Inc.", Sector = "E-Commerce", Price = "$178.25", Change = "+1.75%", ChangeClass = "text-success", Color = "#eab308", Hour1 = "+0.8%", Hour24 = "+1.2%", Days7 = "+3.5%", Hour1Class = "text-success", Hour24Class = "text-success", Days7Class = "text-success", ChartData = new List<ChartDataset>() },
                new StockViewModel { Id = 5, Symbol = "NVDA", Name = "NVIDIA Corp.", Sector = "Technology", Price = "$879.90", Change = "+3.21%", ChangeClass = "text-success", Color = "#3b82f6", Hour1 = "+1.5%", Hour24 = "+2.8%", Days7 = "+5.2%", Hour1Class = "text-success", Hour24Class = "text-success", Days7Class = "text-success", ChartData = new List<ChartDataset>() },
                new StockViewModel { Id = 6, Symbol = "GOOGL", Name = "Alphabet Inc.", Sector = "Technology", Price = "$165.30", Change = "-0.45%", ChangeClass = "text-danger", Color = "#3b82f6", Hour1 = "-0.2%", Hour24 = "-0.5%", Days7 = "-1.1%", Hour1Class = "text-danger", Hour24Class = "text-danger", Days7Class = "text-danger", ChartData = new List<ChartDataset>() },
                new StockViewModel { Id = 7, Symbol = "META", Name = "Meta Platforms Inc.", Sector = "Technology", Price = "$474.85", Change = "+2.10%", ChangeClass = "text-success", Color = "#3b82f6", Hour1 = "+0.9%", Hour24 = "+1.8%", Days7 = "+4.2%", Hour1Class = "text-success", Hour24Class = "text-success", Days7Class = "text-success", ChartData = new List<ChartDataset>() },
                new StockViewModel { Id = 8, Symbol = "JPM", Name = "JPMorgan Chase & Co.", Sector = "Financial Services", Price = "$195.10", Change = "-1.15%", ChangeClass = "text-danger", Color = "#14b8a6", Hour1 = "-0.4%", Hour24 = "-0.9%", Days7 = "-2.3%", Hour1Class = "text-danger", Hour24Class = "text-danger", Days7Class = "text-danger", ChartData = new List<ChartDataset>() },
                new StockViewModel { Id = 9, Symbol = "BRK.A", Name = "Berkshire Hathaway Inc.", Sector = "Conglomerate", Price = "$608,495.00", Change = "+0.65%", ChangeClass = "text-success", Color = "#a3e635", Hour1 = "+0.2%", Hour24 = "+0.5%", Days7 = "+1.2%", Hour1Class = "text-success", Hour24Class = "text-success", Days7Class = "text-success", ChartData = new List<ChartDataset>() },
                new StockViewModel { Id = 10, Symbol = "V", Name = "Visa Inc.", Sector = "Financial Services", Price = "$275.65", Change = "+1.25%", ChangeClass = "text-success", Color = "#14b8a6", Hour1 = "+0.6%", Hour24 = "+1.1%", Days7 = "+2.8%", Hour1Class = "text-success", Hour24Class = "text-success", Days7Class = "text-success", ChartData = new List<ChartDataset>() },
                
                // Healthcare sector
                new StockViewModel { Id = 11, Symbol = "JNJ", Name = "Johnson & Johnson", Sector = "Healthcare", Price = "$152.36", Change = "+1.28%", ChangeClass = "text-success", Color = "#ec4899", Hour1 = "+0.5%", Hour24 = "+1.3%", Days7 = "+2.7%", Hour1Class = "text-success", Hour24Class = "text-success", Days7Class = "text-success", ChartData = new List<ChartDataset>() },
                new StockViewModel { Id = 12, Symbol = "PFE", Name = "Pfizer Inc.", Sector = "Healthcare", Price = "$28.79", Change = "-0.89%", ChangeClass = "text-danger", Color = "#ec4899", Hour1 = "-0.3%", Hour24 = "-0.8%", Days7 = "-1.9%", Hour1Class = "text-danger", Hour24Class = "text-danger", Days7Class = "text-danger", ChartData = new List<ChartDataset>() },
                new StockViewModel { Id = 13, Symbol = "MRK", Name = "Merck & Co.", Sector = "Healthcare", Price = "$130.45", Change = "+2.15%", ChangeClass = "text-success", Color = "#ec4899", Hour1 = "+0.9%", Hour24 = "+1.8%", Days7 = "+3.5%", Hour1Class = "text-success", Hour24Class = "text-success", Days7Class = "text-success", ChartData = new List<ChartDataset>() },
                new StockViewModel { Id = 14, Symbol = "UNH", Name = "UnitedHealth Group", Sector = "Healthcare", Price = "$528.73", Change = "+0.75%", ChangeClass = "text-success", Color = "#ec4899", Hour1 = "+0.3%", Hour24 = "+0.6%", Days7 = "+1.5%", Hour1Class = "text-success", Hour24Class = "text-success", Days7Class = "text-success", ChartData = new List<ChartDataset>() },
                new StockViewModel { Id = 15, Symbol = "ABT", Name = "Abbott Laboratories", Sector = "Healthcare", Price = "$107.52", Change = "-1.32%", ChangeClass = "text-danger", Color = "#ec4899", Hour1 = "-0.6%", Hour24 = "-1.1%", Days7 = "-2.4%", Hour1Class = "text-danger", Hour24Class = "text-danger", Days7Class = "text-danger", ChartData = new List<ChartDataset>() },
                
                // Energy sector
                new StockViewModel { Id = 16, Symbol = "XOM", Name = "Exxon Mobil Corp.", Sector = "Energy", Price = "$119.88", Change = "+2.34%", ChangeClass = "text-success", Color = "#f97316", Hour1 = "+1.1%", Hour24 = "+2.0%", Days7 = "+4.2%", Hour1Class = "text-success", Hour24Class = "text-success", Days7Class = "text-success", ChartData = new List<ChartDataset>() },
                new StockViewModel { Id = 17, Symbol = "CVX", Name = "Chevron Corp.", Sector = "Energy", Price = "$156.30", Change = "-0.95%", ChangeClass = "text-danger", Color = "#f97316", Hour1 = "-0.4%", Hour24 = "-0.8%", Days7 = "-1.8%", Hour1Class = "text-danger", Hour24Class = "text-danger", Days7Class = "text-danger", ChartData = new List<ChartDataset>() },
                new StockViewModel { Id = 18, Symbol = "COP", Name = "ConocoPhillips", Sector = "Energy", Price = "$112.67", Change = "+1.85%", ChangeClass = "text-success", Color = "#f97316", Hour1 = "+0.8%", Hour24 = "+1.5%", Days7 = "+3.2%", Hour1Class = "text-success", Hour24Class = "text-success", Days7Class = "text-success", ChartData = new List<ChartDataset>() },
                new StockViewModel { Id = 19, Symbol = "BP", Name = "BP p.l.c.", Sector = "Energy", Price = "$35.42", Change = "-1.23%", ChangeClass = "text-danger", Color = "#f97316", Hour1 = "-0.5%", Hour24 = "-1.0%", Days7 = "-2.2%", Hour1Class = "text-danger", Hour24Class = "text-danger", Days7Class = "text-danger", ChartData = new List<ChartDataset>() },
                new StockViewModel { Id = 20, Symbol = "SLB", Name = "Schlumberger Ltd.", Sector = "Energy", Price = "$43.78", Change = "+0.92%", ChangeClass = "text-success", Color = "#f97316", Hour1 = "+0.4%", Hour24 = "+0.7%", Days7 = "+1.6%", Hour1Class = "text-success", Hour24Class = "text-success", Days7Class = "text-success", ChartData = new List<ChartDataset>() },
                
                // Consumer Goods
                new StockViewModel { Id = 21, Symbol = "PG", Name = "Procter & Gamble Co.", Sector = "Consumer Goods", Price = "$166.89", Change = "+0.78%", ChangeClass = "text-success", Color = "#10b981", Hour1 = "+0.3%", Hour24 = "+0.6%", Days7 = "+1.4%", Hour1Class = "text-success", Hour24Class = "text-success", Days7Class = "text-success", ChartData = new List<ChartDataset>() },
                new StockViewModel { Id = 22, Symbol = "KO", Name = "Coca-Cola Co.", Sector = "Consumer Goods", Price = "$63.15", Change = "-0.52%", ChangeClass = "text-danger", Color = "#10b981", Hour1 = "-0.2%", Hour24 = "-0.4%", Days7 = "-1.0%", Hour1Class = "text-danger", Hour24Class = "text-danger", Days7Class = "text-danger", ChartData = new List<ChartDataset>() },
                new StockViewModel { Id = 23, Symbol = "PEP", Name = "PepsiCo Inc.", Sector = "Consumer Goods", Price = "$172.73", Change = "+1.15%", ChangeClass = "text-success", Color = "#10b981", Hour1 = "+0.5%", Hour24 = "+0.9%", Days7 = "+2.1%", Hour1Class = "text-success", Hour24Class = "text-success", Days7Class = "text-success", ChartData = new List<ChartDataset>() },
                new StockViewModel { Id = 24, Symbol = "WMT", Name = "Walmart Inc.", Sector = "Consumer Goods", Price = "$60.35", Change = "+2.25%", ChangeClass = "text-success", Color = "#10b981", Hour1 = "+1.0%", Hour24 = "+1.9%", Days7 = "+4.0%", Hour1Class = "text-success", Hour24Class = "text-success", Days7Class = "text-success", ChartData = new List<ChartDataset>() },
                new StockViewModel { Id = 25, Symbol = "COST", Name = "Costco Wholesale Corp.", Sector = "Consumer Goods", Price = "$855.97", Change = "-0.68%", ChangeClass = "text-danger", Color = "#10b981", Hour1 = "-0.3%", Hour24 = "-0.6%", Days7 = "-1.3%", Hour1Class = "text-danger", Hour24Class = "text-danger", Days7Class = "text-danger", ChartData = new List<ChartDataset>() },
                
                // Telecommunications
                new StockViewModel { Id = 26, Symbol = "VZ", Name = "Verizon Communications", Sector = "Telecommunications", Price = "$40.78", Change = "-1.45%", ChangeClass = "text-danger", Color = "#7c3aed", Hour1 = "-0.6%", Hour24 = "-1.2%", Days7 = "-2.6%", Hour1Class = "text-danger", Hour24Class = "text-danger", Days7Class = "text-danger", ChartData = new List<ChartDataset>() },
                new StockViewModel { Id = 27, Symbol = "T", Name = "AT&T Inc.", Sector = "Telecommunications", Price = "$17.25", Change = "+0.88%", ChangeClass = "text-success", Color = "#7c3aed", Hour1 = "+0.4%", Hour24 = "+0.7%", Days7 = "+1.6%", Hour1Class = "text-success", Hour24Class = "text-success", Days7Class = "text-success", ChartData = new List<ChartDataset>() },
                new StockViewModel { Id = 28, Symbol = "TMUS", Name = "T-Mobile US Inc.", Sector = "Telecommunications", Price = "$162.35", Change = "+1.95%", ChangeClass = "text-success", Color = "#7c3aed", Hour1 = "+0.9%", Hour24 = "+1.6%", Days7 = "+3.5%", Hour1Class = "text-success", Hour24Class = "text-success", Days7Class = "text-success", ChartData = new List<ChartDataset>() },
                new StockViewModel { Id = 29, Symbol = "VOD", Name = "Vodafone Group Plc", Sector = "Telecommunications", Price = "$8.92", Change = "-0.78%", ChangeClass = "text-danger", Color = "#7c3aed", Hour1 = "-0.3%", Hour24 = "-0.6%", Days7 = "-1.4%", Hour1Class = "text-danger", Hour24Class = "text-danger", Days7Class = "text-danger", ChartData = new List<ChartDataset>() },
                new StockViewModel { Id = 30, Symbol = "ERIC", Name = "Ericsson", Sector = "Telecommunications", Price = "$5.47", Change = "+0.55%", ChangeClass = "text-success", Color = "#7c3aed", Hour1 = "+0.2%", Hour24 = "+0.4%", Days7 = "+1.0%", Hour1Class = "text-success", Hour24Class = "text-success", Days7Class = "text-success", ChartData = new List<ChartDataset>() },
                
                // Manufacturing
                new StockViewModel { Id = 31, Symbol = "GE", Name = "General Electric Co.", Sector = "Manufacturing", Price = "$160.23", Change = "+2.45%", ChangeClass = "text-success", Color = "#2563eb", Hour1 = "+1.1%", Hour24 = "+2.1%", Days7 = "+4.5%", Hour1Class = "text-success", Hour24Class = "text-success", Days7Class = "text-success", ChartData = new List<ChartDataset>() },
                new StockViewModel { Id = 32, Symbol = "MMM", Name = "3M Co.", Sector = "Manufacturing", Price = "$97.56", Change = "-1.25%", ChangeClass = "text-danger", Color = "#2563eb", Hour1 = "-0.5%", Hour24 = "-1.0%", Days7 = "-2.3%", Hour1Class = "text-danger", Hour24Class = "text-danger", Days7Class = "text-danger", ChartData = new List<ChartDataset>() },
                new StockViewModel { Id = 33, Symbol = "CAT", Name = "Caterpillar Inc.", Sector = "Manufacturing", Price = "$345.68", Change = "+1.65%", ChangeClass = "text-success", Color = "#2563eb", Hour1 = "+0.7%", Hour24 = "+1.4%", Days7 = "+3.0%", Hour1Class = "text-success", Hour24Class = "text-success", Days7Class = "text-success", ChartData = new List<ChartDataset>() },
                new StockViewModel { Id = 34, Symbol = "DE", Name = "Deere & Co.", Sector = "Manufacturing", Price = "$394.25", Change = "-0.85%", ChangeClass = "text-danger", Color = "#2563eb", Hour1 = "-0.4%", Hour24 = "-0.7%", Days7 = "-1.6%", Hour1Class = "text-danger", Hour24Class = "text-danger", Days7Class = "text-danger", ChartData = new List<ChartDataset>() },
                new StockViewModel { Id = 35, Symbol = "BA", Name = "Boeing Co.", Sector = "Manufacturing", Price = "$182.35", Change = "+0.95%", ChangeClass = "text-success", Color = "#2563eb", Hour1 = "+0.4%", Hour24 = "+0.8%", Days7 = "+1.8%", Hour1Class = "text-success", Hour24Class = "text-success", Days7Class = "text-success", ChartData = new List<ChartDataset>() }
            };
            
            return result;
        }

        public IActionResult Currency()
        {
            return View();
        }

        public IActionResult Aboutus()
        {
            return View();
        }

        public IActionResult TheTeam()
        {
            return View();
        }

        public IActionResult Login()
        {
            return View("AuthCard", "login");
        }

        public IActionResult Signup()
        {
            return View("AuthCard", "signup");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost]
        public async Task<IActionResult> ProcessSignup(string firstName, string lastName, string username, string email, string password)
        {
            try
            {
                // Check if any required field is missing
                if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName) || 
                    string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || 
                    string.IsNullOrEmpty(password))
                {
                    ViewBag.Error = "Please fill in all required fields.";
                    return View("AuthCard", "signup");
                }

                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.username == username || u.email == email);

                if (existingUser != null)
                {
                    ViewBag.Error = "Username or email already exists";
                    return View("AuthCard", "signup");
                }

                var user = new Users
                {
                    firstName = firstName,
                    lastName = lastName,
                    username = username,
                    email = email,
                    password_hash = HashPassword(password),
                    balance = null,
                    created_at = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                HttpContext.Session.SetString("UserEmail", email);
                HttpContext.Session.SetString("Username", username);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during signup process");
                ViewBag.Error = "An error occurred during registration. Please try again.";
                return View("AuthCard", "signup");
            }
        }

        [HttpPost]
        public async Task<IActionResult> ProcessLogin(string email, string password)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.email == email);

                if (user == null)
                {
                    ViewBag.Error = "Invalid email or password";
                    return View("AuthCard", "login");
                }

                string hashedPassword = HashPassword(password);
                if (user.password_hash != hashedPassword)
                {
                    ViewBag.Error = "Invalid email or password";
                    return View("AuthCard", "login");
                }

                HttpContext.Session.SetString("UserEmail", user.email);
                HttpContext.Session.SetString("Username", user.username);

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login process");
                ViewBag.Error = "An error occurred during login. Please try again.";
                return View("AuthCard", "login");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password, bool isModal = false)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.email == email);

                if (user == null || user.password_hash != HashPassword(password))
                {
                    if (isModal)
                    {
                        return Json(new { success = false, message = "Invalid email or password" });
                    }
                    else
                    {
                        ViewBag.Error = "Invalid email or password";
                        return View("AuthCard", "login");
                    }
                }

                HttpContext.Session.SetString("UserEmail", user.email);
                HttpContext.Session.SetString("Username", user.username);

                if (isModal)
                {
                    return Json(new { success = true });
                }
                else
                {
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login process");
                if (isModal)
                {
                    return Json(new { success = false, message = "An error occurred during login. Please try again." });
                }
                else
                {
                    ViewBag.Error = "An error occurred during login. Please try again.";
                    return View("AuthCard", "login");
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> Signup(string firstName, string lastName, string username, string email, string password, bool isModal = false)
        {
            try
            {
                // Check if any required field is missing
                if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName) || 
                    string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || 
                    string.IsNullOrEmpty(password))
                {
                    if (isModal)
                    {
                        return Json(new { success = false, message = "Please fill in all required fields." });
                    }
                    else
                    {
                        ViewBag.Error = "Please fill in all required fields.";
                        return View("AuthCard", "signup");
                    }
                }

                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.username == username || u.email == email);

                if (existingUser != null)
                {
                    if (isModal)
                    {
                        return Json(new { success = false, message = "Username or email already exists" });
                    }
                    else
                    {
                        ViewBag.Error = "Username or email already exists";
                        return View("AuthCard", "signup");
                    }
                }

                var user = new Users
                {
                    firstName = firstName,
                    lastName = lastName,
                    username = username,
                    email = email,
                    password_hash = HashPassword(password),
                    balance = null,
                    created_at = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                HttpContext.Session.SetString("UserEmail", email);
                HttpContext.Session.SetString("Username", username);

                if (isModal)
                {
                    return Json(new { success = true });
                }
                else
                {
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during signup process");
                if (isModal)
                {
                    return Json(new { success = false, message = "An error occurred during registration. Please try again." });
                }
                else
                {
                    ViewBag.Error = "An error occurred during registration. Please try again.";
                    return View("AuthCard", "signup");
                }
            }
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetMarketMovers()
        {
            try
            {
                // Define the stock symbols we want to display
                var symbols = new[] { "TSLA", "AAPL", "MSFT", "AMZN", "NVDA", "GOOGL", "META", "JPM", "BRK.A", "V" };
                var allStocks = new List<object>();
                
                // Fetch data for each stock
                foreach (var symbol in symbols)
                {
                    try
                    {
                        var stockData = await _alphaVantageService.GetStockDataAsync(symbol);
                        if (stockData != null)
                        {
                            // Calculate change percent based on database values
                            decimal changePercent = 0;
                            string changeValue = "+0.00%";
                            
                            if (decimal.TryParse(stockData.market_price, out decimal marketPrice) &&
                                decimal.TryParse(stockData.open_price, out decimal openPrice) && openPrice != 0)
                            {
                                decimal diff = marketPrice - openPrice;
                                changePercent = (diff / openPrice) * 100;
                                changeValue = (diff >= 0 ? "+" : "") + changePercent.ToString("0.00") + "%";
                            }
                            
                            // Get company name
                            string name = GetCompanyName(symbol);
                            
                            allStocks.Add(new
                            {
                                Symbol = symbol,
                                Name = name,
                                Price = stockData.market_price.StartsWith("$") ? stockData.market_price : $"${stockData.market_price}",
                                Change = changeValue,
                                ChangePercent = changePercent
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error fetching data for {symbol}");
                    }
                }
                
                // Sort stocks by change percent to find top gainers and losers
                var gainers = allStocks
                    .OrderByDescending(s => ((dynamic)s).ChangePercent)
                    .Take(5)
                    .ToList();
                    
                var losers = allStocks
                    .OrderBy(s => ((dynamic)s).ChangePercent)
                    .Take(5)
                    .ToList();
                
                return Json(new { success = true, gainers = gainers, losers = losers });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetMarketMovers");
                return Json(new { success = false, message = "Error fetching market movers" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> UpdatePrices()
        {
            try
            {
                // Define the stock symbols we want to update
                var symbols = new[] { "TSLA", "AAPL", "MSFT", "AMZN", "NVDA", "GOOGL", "META", "JPM", "BRK.A", "V" };
                var updatedCount = 0;
                
                // Update prices for each stock
                foreach (var symbol in symbols)
                {
                    try
                    {
                        // In a real application, this would call an external API to get the latest price
                        // For this demo, we'll use the AlphaVantage service with simulated price changes
                        var success = await _alphaVantageService.UpdateStockPriceAsync(symbol);
                        if (success)
                        {
                            updatedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error updating price for {symbol}");
                    }
                }
                
                return Json(new { success = true, updatedCount = updatedCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdatePrices");
                return Json(new { success = false, message = "Error updating prices" });
            }
        }

        [HttpPost]
        public IActionResult SetCurrency(string currency)
        {
            try
            {
                var currencyService = HttpContext.RequestServices.GetService<CurrencyService>();
                if (currencyService != null)
                {
                    currencyService.SetUserPreferredCurrency(currency);
                }
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting currency preference");
                return Json(new { success = false, message = "Error setting currency preference" });
            }
        }
    }
}