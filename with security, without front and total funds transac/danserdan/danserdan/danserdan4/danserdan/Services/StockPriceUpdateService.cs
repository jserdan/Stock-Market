using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using danserdan.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace danserdan.Services
{
    public class StockPriceUpdateService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<StockPriceUpdateService> _logger;
        private readonly Random _random = new Random();
        private readonly TimeSpan _updateInterval = TimeSpan.FromSeconds(10); // Update every 10 seconds

        public StockPriceUpdateService(
            IServiceProvider serviceProvider,
            ILogger<StockPriceUpdateService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Stock Price Update Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await UpdateStockPrices();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating stock prices");
                }

                await Task.Delay(_updateInterval, stoppingToken);
            }

            _logger.LogInformation("Stock Price Update Service is stopping.");
        }

        private async Task UpdateStockPrices()
        {
            // Create a new scope to resolve dependencies
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

            // Get all stocks from the database
            var stocks = await dbContext.Stocks.ToListAsync();
            if (stocks.Count == 0)
            {
                _logger.LogWarning("No stocks found in database to update");
                return;
            }

            _logger.LogInformation($"Updating prices for {stocks.Count} stocks");

            var today = DateTime.UtcNow.Date;
            
            foreach (var stock in stocks)
            {
                // Generate a random price change between -30 and +30
                decimal priceChange = (decimal)(_random.NextDouble() * 60 - 30);
                
                // Ensure the price doesn't go below 1
                decimal newPrice = Math.Max(stock.market_price + priceChange, 1m);
                
                // Update the stock price in the database
                stock.market_price = newPrice;
                stock.last_updated = DateTime.UtcNow;
                
                // Make sure we have an open price for today
                // If open_price is null or from a different day, set it to the previous market price
                // This ensures we have a valid base for percentage calculations
                if (stock.open_price == null || stock.open_price_time == null || stock.open_price_time.Value.Date != today)
                {
                    // We're setting the open price to the previous market price (before our update)
                    // This ensures we have a valid reference point for percentage calculations
                    stock.open_price = stock.market_price - priceChange; // Use the price before our change
                    stock.open_price_time = DateTime.UtcNow;
                    _logger.LogInformation($"Set new open price for {stock.symbol}: ${stock.open_price:F2}");
                }
                
                // Calculate the percentage change for logging
                decimal percentChange = stock.open_price > 0 ? ((stock.market_price - stock.open_price.Value) / stock.open_price.Value) * 100 : 0;
                _logger.LogInformation($"Updated {stock.symbol} price: ${stock.market_price:F2} (change: ${priceChange:F2}, {percentChange:F2}%)");
            }

            // Save changes to the database
            await dbContext.SaveChangesAsync();
        }
    }
}
