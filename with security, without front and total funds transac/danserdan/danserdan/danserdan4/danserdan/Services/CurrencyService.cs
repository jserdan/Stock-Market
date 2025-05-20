using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace danserdan.Services
{
    public class CurrencyService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<CurrencyService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private const string EXCHANGE_RATE_CACHE_KEY = "USD_TO_PHP_RATE";
        private const string PREFERRED_CURRENCY_COOKIE = "PreferredCurrency";
        private readonly TimeSpan _cacheDuration = TimeSpan.FromHours(1);
        
        // Fixed exchange rate for USD to PHP (this could be replaced with an API call to get real-time rates)
        private const decimal USD_TO_PHP_RATE = 56.5m;

        public CurrencyService(IMemoryCache memoryCache, ILogger<CurrencyService> logger, IHttpContextAccessor httpContextAccessor)
        {
            _memoryCache = memoryCache;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public Task<decimal> GetExchangeRateAsync(string fromCurrency, string toCurrency)
        {
            // For now, we only support USD to PHP conversion
            if (fromCurrency == "USD" && toCurrency == "PHP")
            {
                return Task.FromResult(GetUsdToPhpRateAsync());
            }
            else if (fromCurrency == "PHP" && toCurrency == "USD")
            {
                // Inverse of USD to PHP rate
                decimal usdToPhp = GetUsdToPhpRateAsync();
                return Task.FromResult(1 / usdToPhp);
            }
            
            // If same currency or unsupported conversion, return 1
            return Task.FromResult(1m);
        }

        private decimal GetUsdToPhpRateAsync()
        {
            // Check if exchange rate is cached
            if (_memoryCache.TryGetValue(EXCHANGE_RATE_CACHE_KEY, out decimal cachedRate))
            {
                _logger.LogInformation($"Retrieved USD to PHP exchange rate from cache: {cachedRate}");
                return cachedRate;
            }

            // In a real application, you would fetch the exchange rate from an API
            // For now, we'll use a fixed rate
            decimal rate = USD_TO_PHP_RATE;
            
            // Cache the exchange rate
            _memoryCache.Set(EXCHANGE_RATE_CACHE_KEY, rate, _cacheDuration);
            
            _logger.LogInformation($"Set USD to PHP exchange rate in cache: {rate}");
            return rate;
        }

        public async Task<decimal> ConvertCurrencyAsync(decimal amount, string fromCurrency, string toCurrency)
        {
            if (fromCurrency == toCurrency)
            {
                return amount;
            }

            decimal exchangeRate = await GetExchangeRateAsync(fromCurrency, toCurrency);
            return amount * exchangeRate;
        }

        public string FormatCurrency(decimal amount, string currency)
        {
            return currency switch
            {
                "PHP" => "₱" + amount.ToString("N2"),
                _ => "$" + amount.ToString("N2") // Default to USD
            };
        }
        
        /// <summary>
        /// Gets the user's preferred currency from cookie or defaults to USD
        /// </summary>
        public string GetUserPreferredCurrency()
        {
            if (_httpContextAccessor.HttpContext != null && 
                _httpContextAccessor.HttpContext.Request.Cookies.TryGetValue(PREFERRED_CURRENCY_COOKIE, out string currency))
            {
                return currency == "PHP" ? "PHP" : "USD";
            }
            
            return "USD"; // Default
        }
        
        /// <summary>
        /// Sets the user's preferred currency in a cookie
        /// </summary>
        public void SetUserPreferredCurrency(string currency)
        {
            if (_httpContextAccessor.HttpContext != null)
            {
                var cookieOptions = new CookieOptions
                {
                    Expires = DateTime.Now.AddYears(1),
                    Path = "/",
                    SameSite = SameSiteMode.Lax,
                    HttpOnly = false // Allow JavaScript access
                };
                
                _httpContextAccessor.HttpContext.Response.Cookies.Append(
                    PREFERRED_CURRENCY_COOKIE, 
                    currency == "PHP" ? "PHP" : "USD",
                    cookieOptions);
            }
        }
        
        /// <summary>
        /// Converts an amount to the user's preferred currency
        /// </summary>
        public async Task<(decimal Amount, string Symbol)> ConvertToPreferredCurrencyAsync(decimal amountInUsd)
        {
            string preferredCurrency = GetUserPreferredCurrency();
            
            if (preferredCurrency == "PHP")
            {
                decimal phpAmount = await ConvertCurrencyAsync(amountInUsd, "USD", "PHP");
                return (phpAmount, "₱");
            }
            
            return (amountInUsd, "$");
        }
    }
}
