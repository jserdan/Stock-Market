using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore;
using danserdan.Models;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using danserdan.Services;

public class AlphaVantageService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly ApplicationDBContext _context;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<AlphaVantageService> _logger;
    private const string CACHE_KEY_PREFIX = "STOCK_";
    private readonly TimeSpan _cacheDuration = TimeSpan.FromSeconds(1);

    public AlphaVantageService(
        HttpClient httpClient,
        string apiKey,
        ApplicationDBContext context,
        IMemoryCache? memoryCache = null,
        ILogger<AlphaVantageService>? logger = null)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
        _context = context;
        _memoryCache = memoryCache ?? new MemoryCache(new MemoryCacheOptions());
        _logger = logger;
    }

    public async Task<StockData> GetStockDataAsync(string symbol)
    {
        try
        {
            string cacheKey = $"{CACHE_KEY_PREFIX}{symbol}";
            if (_memoryCache.TryGetValue(cacheKey, out StockData? cachedData) && cachedData != null)
            {
                _logger?.LogInformation($"Retrieved {symbol} from cache: {cachedData.market_price}");
                return cachedData;
            }

            var stockFromDb = await _context.Stocks
                .FirstOrDefaultAsync(s => s.symbol == symbol);

            if (stockFromDb != null)
            {
                _logger?.LogInformation($"Found {symbol} in database: {stockFromDb.market_price}");

                var stockData = new StockData
                {
                    symbol = stockFromDb.symbol,
                    market_price = stockFromDb.market_price != null ? stockFromDb.market_price.ToString() : "0.00",
                    open_price = stockFromDb.open_price != null ? stockFromDb.open_price.Value.ToString("0.00") : null,
                    open_price_time = stockFromDb.open_price_time?.ToString("o")
                };

                _memoryCache.Set(cacheKey, stockData, _cacheDuration);

                if (stockFromDb.last_updated > DateTime.UtcNow.AddMinutes(-15))
                {
                    return stockData;
                }

                var freshData = await FetchFromApiAsync(symbol, cacheKey);
                return freshData ?? stockData;
            }

            // Not in DB or cache: fetch from API
            var apiData = await FetchFromApiAsync(symbol, cacheKey);
            if (apiData != null)
            {
                return apiData;
            }
            else
            {
                // Return mock data if API fetch fails
                return GetMockStockData(symbol);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, $"Error in GetStockDataAsync for {symbol}");
            // Return mock data on error
            return GetMockStockData(symbol);
        }
    }
    
    private StockData GetMockStockData(string symbol)
    {
        // Generate random price based on symbol to ensure consistency
        var random = new Random(symbol.GetHashCode());
        decimal basePrice = 0;
        
        switch (symbol)
        {
            case "TSLA": basePrice = 193.75m; break;
            case "AAPL": basePrice = 175.45m; break;
            case "MSFT": basePrice = 402.15m; break;
            case "AMZN": basePrice = 178.25m; break;
            case "NVDA": basePrice = 879.90m; break;
            case "GOOGL": basePrice = 165.30m; break;
            case "META": basePrice = 474.85m; break;
            case "JPM": basePrice = 195.10m; break;
            case "BRK.A": basePrice = 608495.00m; break;
            case "V": basePrice = 275.65m; break;
            default: basePrice = 100.00m + (decimal)random.Next(1, 900); break;
        }
        
        // Add some randomness to the price
        decimal variation = (decimal)(random.NextDouble() * 0.05 - 0.025); // ±2.5%
        decimal price = basePrice * (1 + variation);
        
        // Create mock stock data as StockData
        var mockData = new StockData
        {
            symbol = symbol,
            market_price = price.ToString("0.00"),
            open_price = price.ToString("0.00"),
            open_price_time = DateTime.UtcNow.ToString("o")
        };
        
        return mockData;
    }


    private async Task<StockData> FetchFromApiAsync(string symbol, string cacheKey)
    {
        // Use GLOBAL_QUOTE for real-time data instead of TIME_SERIES_DAILY
        var url = $"https://www.alphavantage.co/query?function=GLOBAL_QUOTE&symbol={symbol}&apikey={_apiKey}";

        try
        {
            _logger?.LogInformation($"Fetching data from API for {symbol}");

            var response = await _httpClient.GetStringAsync(url);
            _logger?.LogDebug($"API Response for {symbol}: {response}");

            // Try parsing as GlobalQuote first (more reliable for current price)
            var quoteResponse = JsonConvert.DeserializeObject<GlobalQuoteResponse>(response);

            if (quoteResponse?.GlobalQuote != null && !string.IsNullOrEmpty(quoteResponse.GlobalQuote.Price))
            {
                var price = quoteResponse.GlobalQuote.Price;
                _logger?.LogInformation($"Successfully parsed GlobalQuote for {symbol}: {price}");

                if (decimal.TryParse(price, out decimal priceDecimal))
                {
                    UpdateDatabase(symbol, priceDecimal);

                    var result = new StockData
                    {
                        symbol = symbol,
                        market_price = price
                    };

                    _memoryCache.Set(cacheKey, result, _cacheDuration);
                    return result;
                }
            }

            // Fallback to TIME_SERIES_DAILY if GLOBAL_QUOTE didn't work
            _logger?.LogWarning($"GlobalQuote failed, trying TIME_SERIES_DAILY for {symbol}");

            url = $"https://www.alphavantage.co/query?function=TIME_SERIES_DAILY&symbol={symbol}&apikey={_apiKey}";
            response = await _httpClient.GetStringAsync(url);

            var stockData = JsonConvert.DeserializeObject<AlphaVantageResponse>(response);

            if (stockData?.TimeSeries == null || !stockData.TimeSeries.Any())
            {
                throw new Exception($"No time series data for {symbol}.");
            }

            var latestTimeSeries = stockData.TimeSeries.First();
            var marketPriceStr = latestTimeSeries.Value.Close;

            if (string.IsNullOrEmpty(marketPriceStr))
            {
                throw new Exception($"Market price not available for {symbol}.");
            }

            if (decimal.TryParse(marketPriceStr, out decimal marketPriceDecimal))
            {
                _logger?.LogInformation($"Successfully parsed TIME_SERIES_DAILY for {symbol}: {marketPriceStr}");

                UpdateDatabase(symbol, marketPriceDecimal);

                var stockFromDb = await _context.Stocks.FirstOrDefaultAsync(s => s.symbol == symbol);
                var result = new StockData
                {
                    symbol = symbol,
                    market_price = marketPriceStr,
                    open_price = stockFromDb?.open_price.HasValue == true ? stockFromDb.open_price.Value.ToString("0.00") : null,
                    open_price_time = stockFromDb?.open_price_time?.ToString("o") ?? string.Empty
                };

                _memoryCache.Set(cacheKey, result, _cacheDuration);
                return result;
            }
            else
            {
                throw new Exception($"Failed to parse price: {marketPriceStr}");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, $"Error fetching data from API for {symbol}");
            throw;
        }
    }

    public async Task<List<ChartDataset>> GetHistoricalDataAsync(string symbol)
    {
        try
        {
            string cacheKey = $"{CACHE_KEY_PREFIX}{symbol}_HISTORICAL";
            if (_memoryCache.TryGetValue(cacheKey, out List<ChartDataset>? cachedData) && cachedData != null)
            {
                _logger?.LogInformation($"Retrieved historical data for {symbol} from cache");
                return cachedData;
            }

            try
            {
                // Fetch historical data from Alpha Vantage
                var url = $"https://www.alphavantage.co/query?function=TIME_SERIES_DAILY&symbol={symbol}&outputsize=compact&apikey={_apiKey}";
                var response = await _httpClient.GetStringAsync(url);
                var stockData = JsonConvert.DeserializeObject<AlphaVantageResponse>(response);

                if (stockData?.TimeSeries != null && stockData.TimeSeries.Any())
                {
                    // Get the last 7 data points (or less if not available)
                    var historicalData = stockData.TimeSeries
                        .Take(7)
                        .Select(ts => new HistoricalDataPoint
                        {
                            Date = ts.Key,
                            Price = decimal.Parse(ts.Value.Close),
                            IsPositive = true // Will set this later
                        })
                        .OrderBy(dp => dp.Date)
                        .ToList();

                    // Determine if each point is positive or negative (compared to previous point)
                    for (int i = 1; i < historicalData.Count; i++)
                    {
                        historicalData[i].IsPositive = historicalData[i].Price >= historicalData[i - 1].Price;
                    }

                    // Create datasets for chart.js
                    var datasets = CreateChartDatasets(historicalData);
                    _memoryCache.Set(cacheKey, datasets, TimeSpan.FromMinutes(15));
                    return datasets;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error fetching historical data from API for {symbol}");
                // Continue to generate mock data
            }

            // If we get here, either the API call failed or returned no data
            _logger?.LogWarning($"Using mock historical data for {symbol}");
            var mockData = GenerateMockChartData(symbol);
            _memoryCache.Set(cacheKey, mockData, TimeSpan.FromMinutes(5));
            return mockData;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, $"Error in GetHistoricalDataAsync for {symbol}");
            return GenerateMockChartData(symbol);
        }
    }

    private List<ChartDataset> CreateChartDatasets(List<HistoricalDataPoint> historicalData)
    {
        var datasets = new List<ChartDataset>();
        var dataPoints = historicalData.Select(dp => dp.Price).ToList();
        var colorChangePoints = new List<int>();

        // Find points where the trend changes
        for (int i = 1; i < historicalData.Count; i++)
        {
            if (i == 1 || historicalData[i].IsPositive != historicalData[i - 1].IsPositive)
            {
                colorChangePoints.Add(i - 1);
            }
        }

        // Add the last point if it's not already included
        if (colorChangePoints.Count == 0 || colorChangePoints.Last() != historicalData.Count - 1)
        {
            colorChangePoints.Add(historicalData.Count - 1);
        }

        // Create datasets for each segment
        int startIndex = 0;
        for (int i = 0; i < colorChangePoints.Count; i++)
        {
            int endIndex = colorChangePoints[i];
            bool isGreen = i % 2 == 0; // Alternate between green and red

            // Create segment data (fill with null except for the segment range)
            var segmentData = Enumerable.Repeat<decimal?>(null, dataPoints.Count).ToList();
            for (int j = startIndex; j <= endIndex; j++)
            {
                segmentData[j] = dataPoints[j];
            }

            datasets.Add(new ChartDataset
            {
                Label = isGreen ? "Increase" : "Decrease",
                Data = segmentData,
                BorderColor = isGreen ? "#4ade80" : "#ef4444",
                BorderWidth = 3,
                Fill = false,
                Tension = 0.4
            });

            startIndex = endIndex;
        }

        return datasets;
    }

    private List<ChartDataset> GenerateMockChartData(string symbol = null)
    {
        // Generate base price based on symbol
        decimal basePrice = 100m;
        if (!string.IsNullOrEmpty(symbol))
        {
            var random = new Random(symbol.GetHashCode());
            switch (symbol)
            {
                case "TSLA": basePrice = 193.75m; break;
                case "AAPL": basePrice = 175.45m; break;
                case "MSFT": basePrice = 402.15m; break;
                case "AMZN": basePrice = 178.25m; break;
                case "NVDA": basePrice = 879.90m; break;
                case "GOOGL": basePrice = 165.30m; break;
                case "META": basePrice = 474.85m; break;
                case "JPM": basePrice = 195.10m; break;
                case "BRK.A": basePrice = 608495.00m; break;
                case "V": basePrice = 275.65m; break;
                default: basePrice = 100.00m + (decimal)random.Next(1, 900); break;
            }
        }

        // Generate 7 data points with realistic variations
        var data = new List<decimal>();
        var priceRandom = string.IsNullOrEmpty(symbol) ? new Random() : new Random(symbol.GetHashCode());
        
        // Start with base price and add variations
        decimal currentPrice = basePrice;
        for (int i = 0; i < 7; i++)
        {
            data.Add(currentPrice);
            // Add random variation for next price (-2% to +2%)
            decimal variation = (decimal)(priceRandom.NextDouble() * 0.04 - 0.02);
            currentPrice = currentPrice * (1 + variation);
        }

        // Determine color change points based on price movements
        var colorChangePoints = new List<int>();
        for (int i = 1; i < data.Count; i++)
        {
            if (i == 1 || (data[i] > data[i - 1] && data[i - 1] <= data[i - 2]) ||
                (data[i] < data[i - 1] && data[i - 1] >= data[i - 2]))
            {
                colorChangePoints.Add(i - 1);
            }
        }

        // Ensure we have at least one color change point
        if (colorChangePoints.Count == 0)
        {
            colorChangePoints.Add(3); // Add a change point in the middle
        }

        // Create datasets for each segment
        var datasets = new List<ChartDataset>();
        int startIndex = 0;

        for (int i = 0; i <= colorChangePoints.Count; i++)
        {
            int endIndex = i < colorChangePoints.Count ? colorChangePoints[i] : data.Count - 1;
            
            // Determine if segment is increasing or decreasing
            bool isGreen = false;
            if (endIndex > startIndex)
            {
                isGreen = data[endIndex] >= data[startIndex];
            }
            else
            {
                isGreen = i % 2 == 0; // Alternate if we can't determine
            }

            // Create segment data (fill with null except for the segment range)
            var segmentData = Enumerable.Repeat<decimal?>(null, data.Count).ToList();
            for (int j = startIndex; j <= endIndex; j++)
            {
                segmentData[j] = data[j];
            }

            datasets.Add(new ChartDataset
            {
                Label = isGreen ? "Increase" : "Decrease",
                Data = segmentData,
                BorderColor = isGreen ? "#4ade80" : "#ef4444",
                BorderWidth = 3,
                Fill = false,
                Tension = 0.4
            });

            startIndex = endIndex;
        }

        return datasets;
    }

    private async void UpdateDatabase(string symbol, decimal price)
    {
        try
        {
            // Get company name based on symbol (ideally from a proper API)
            string companyName = symbol == "TSLA" ? "Tesla" : symbol;

            // Update or create stock record in DB
            var stock = await _context.Stocks
                .FirstOrDefaultAsync(s => s.symbol == symbol);
            var now = DateTime.UtcNow;
            var today = now.Date;

            if (stock != null)
            {
                // Set open price if it's a new trading day
                if (stock.open_price_time == null || stock.open_price_time.Value.Date != today)
                {
                    stock.open_price = price;
                    stock.open_price_time = now;
                }
                stock.market_price = price;
                stock.last_updated = now;
                stock.company_name = companyName;
            }
            else
            {
                _context.Stocks.Add(new Stocks
                {
                    symbol = symbol,
                    market_price = price,
                    last_updated = now,
                    company_name = companyName,
                    open_price = price,
                    open_price_time = now
                });
            }

            await _context.SaveChangesAsync();
            _logger?.LogInformation($"Updated database for {symbol} with price {price}");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, $"Error updating database for {symbol}");
        }
    }

    public async Task<bool> UpdateStockPriceAsync(string symbol)
    {
        try
        {
            _logger?.LogInformation($"Updating stock price for {symbol}");
            
            var stock = await _context.Stocks.FirstOrDefaultAsync(s => s.symbol == symbol);
            if (stock == null)
            {
                _logger?.LogWarning($"Stock {symbol} not found in database");
                return false;
            }
            
            // For demonstration purposes, we'll use simulated price changes
            // In a real-world scenario, this would make an API call to get the latest price
            
            var random = new Random();
            var changePercent = (decimal)(random.NextDouble() * 0.02 - 0.01); // Random change between -1% and +1%
            
            var currentPrice = stock.market_price;
            var newPrice = currentPrice * (1 + changePercent);
            
            // Update stock price in the database
            stock.market_price = newPrice;
            stock.last_updated = DateTime.UtcNow;
            
            // If it's a new trading day and we don't have an open price yet, set it
            if (!stock.open_price_time.HasValue || stock.open_price_time.Value.Date < DateTime.UtcNow.Date)
            {
                stock.open_price = newPrice;
                stock.open_price_time = DateTime.UtcNow;
            }
            
            await _context.SaveChangesAsync();
            
            // Clear the cache for this stock
            string cacheKey = $"{CACHE_KEY_PREFIX}{symbol}";
            _memoryCache.Remove(cacheKey);
            
            _logger?.LogInformation($"Updated price for {symbol}: {newPrice:F2} (change: {changePercent:P2})");
            
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, $"Error updating price for {symbol}");
            return false;
        }
    }
}

public class AlphaVantageResponse
{
    [JsonProperty("Time Series (Daily)")]
    public Dictionary<string, TimeSeries> TimeSeries { get; set; }
}

public class TimeSeries
{
    [JsonProperty("4. close")]
    public string Close { get; set; }
}

public class GlobalQuoteResponse
{
    [JsonProperty("Global Quote")]
    public GlobalQuote GlobalQuote { get; set; }
}

public class GlobalQuote
{
    [JsonProperty("01. symbol")]
    public string Symbol { get; set; }

    [JsonProperty("05. price")]
    public string Price { get; set; }

    [JsonProperty("10. change percent")]
    public string ChangePercent { get; set; }
}

public class StockData
{
    public string symbol { get; set; }
    public string market_price { get; set; }
    public string open_price { get; set; }
    public string open_price_time { get; set; }
}

public class GlobalQuoteData : StockData
{
    public string ChangePercent { get; set; }
}

public class HistoricalDataPoint
{
    public string Date { get; set; }
    public decimal Price { get; set; }
    public bool IsPositive { get; set; }
}

public class ChartDataset
{
    public string Label { get; set; }
    public List<decimal?> Data { get; set; }
    public string BorderColor { get; set; }
    public int BorderWidth { get; set; }
    public bool Fill { get; set; }
    public double Tension { get; set; }
}