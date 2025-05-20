using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Checkout;

namespace danserdan.Services
{
    public class StripeService
    {
        private readonly string _secretKey;
        private readonly string _publicKey;
        private readonly ILogger<StripeService> _logger;
        
        public StripeService(IConfiguration configuration, ILogger<StripeService> logger)
        {
            _secretKey = configuration["Stripe:SecretKey"];
            _publicKey = configuration["Stripe:PublicKey"];
            _logger = logger;
            
            // Configure Stripe API key
            StripeConfiguration.ApiKey = _secretKey;
            
            _logger.LogInformation("Stripe Service initialized");
        }
        
        public async Task<Session> CreateCheckoutSession(decimal amount, string description, string successUrl, string cancelUrl)
        {
            try
            {
                // Convert amount to cents for Stripe
                long amountInCents = (long)(amount * 100);
                
                _logger.LogInformation("Creating Stripe checkout session with amount: ${Amount}", amount);
                _logger.LogInformation("Using Stripe API key: {ApiKey}", _secretKey.Substring(0, 10) + "...");
                
                var options = new SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string> { "card" },
                    LineItems = new List<SessionLineItemOptions>
                    {
                        new SessionLineItemOptions
                        {
                            PriceData = new SessionLineItemPriceDataOptions
                            {
                                UnitAmount = amountInCents,
                                Currency = "usd",
                                ProductData = new SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = "Account Deposit",
                                    Description = description
                                }
                            },
                            Quantity = 1
                        }
                    },
                    Mode = "payment",
                    SuccessUrl = successUrl,
                    CancelUrl = cancelUrl
                };
                
                var service = new SessionService();
                var session = await service.CreateAsync(options);
                
                _logger.LogInformation("Created Stripe checkout session: {SessionId}", session.Id);
                
                return session;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Stripe checkout session: {Message}", ex.Message);
                if (ex.InnerException != null)
                {
                    _logger.LogError(ex.InnerException, "Inner exception: {Message}", ex.InnerException.Message);
                }
                throw;
            }
        }
        
        public string GetPublicKey()
        {
            return _publicKey;
        }
    }
}
