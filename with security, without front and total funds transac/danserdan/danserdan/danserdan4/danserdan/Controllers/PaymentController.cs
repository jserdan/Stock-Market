using danserdan.Models;
using danserdan.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace danserdan.Controllers
{
    public class PaymentRequest
    {
        public decimal amount { get; set; }
        public string? paymentMethodId { get; set; }
        public string? currency { get; set; }
    }

    public class PaymentController : Controller
    {
        private readonly ApplicationDBContext _context;
        private readonly ILogger<PaymentController> _logger;
        private readonly StripeService _stripeService;

        public PaymentController(ApplicationDBContext context, ILogger<PaymentController> logger, StripeService stripeService)
        {
            _context = context;
            _logger = logger;
            _stripeService = stripeService;
        }

        [HttpGet]
        public IActionResult AddFunds()
        {
            ViewData["StripePublicKey"] = _stripeService.GetPublicKey();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateCheckoutSession([FromBody] PaymentRequest request)
        {
            try
            {
                if (request == null || request.amount <= 0)
                {
                    return BadRequest("Amount must be greater than zero");
                }
                
                // Get the amount and currency from the request
                decimal amount = request.amount;
                string currency = request.currency ?? "USD";
                
                // Store the original currency and amount for display purposes
                string displayCurrency = currency;
                decimal displayAmount = amount;
                
                // Convert amount to USD for Stripe if it's in PHP
                if (currency == "PHP")
                {
                    // The amount sent from client is in PHP, convert to USD for Stripe
                    // Use the exact same conversion rate as elsewhere and round to 2 decimal places
                    amount = decimal.Round(amount / 56.5m, 2);
                    
                    // Log the conversion for debugging
                    _logger.LogInformation($"Checkout: Converting {displayAmount} PHP to USD: {amount} USD");
                }
                
                // For Stripe, we always use USD
                string stripeCurrency = "usd";
                
                // Format the amount for display based on the currency
                string formattedAmount = currency == "PHP" ? "₱" + displayAmount.ToString("F2") : "$" + amount.ToString("F2");

                string? userEmail = HttpContext.Session.GetString("UserEmail");
                if (string.IsNullOrEmpty(userEmail))
                {
                    return RedirectToAction("Login", "Account");
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.email == userEmail);
                if (user == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                try
                {
                    // Create success and cancel URLs with currency info
                    // Pass the original display amount (not the converted amount) to the success URL
                    string successUrl = Url.Action("PaymentSuccess", "Payment", new { amount = displayAmount, userId = user.user_id, currency = displayCurrency }, Request.Scheme);
                    string cancelUrl = Url.Action("PaymentCancel", "Payment", null, Request.Scheme);
                    
                    // Create a Stripe checkout session
                    string description = $"Add {formattedAmount} to account balance for {user.email}";
                    var session = await _stripeService.CreateCheckoutSession(amount, description, successUrl, cancelUrl);
                    
                    // Return the session ID for the client to redirect to Stripe
                    return Json(new { success = true, sessionId = session.Id, currency = displayCurrency });
                }
                catch (Exception stripeEx)
                {
                    _logger.LogError(stripeEx, "Stripe API error: {Message}", stripeEx.Message);
                    Console.WriteLine($"Stripe API error: {stripeEx.Message}");
                    
                    // For demo purposes, provide a fallback option that directly processes the payment
                    _logger.LogInformation("Using fallback payment option for demo");
                    
                    // Add funds to user's balance (using the converted amount for PHP)
                    // The 'amount' variable already contains the converted USD value if currency is PHP
                    user.balance = (user.balance ?? 0) + amount;

                    // Create a transaction record for the deposit without creating a stock entry
                    // We'll use a special transaction type instead of creating a stock entry
                    var transaction = new Transaction
                    {
                        user_id = user.user_id,
                        StockId = null, // No stock associated with this transaction
                        quantity = 1,
                        Price = amount, // Positive for deposit (in USD)
                        TransactionTime = DateTime.UtcNow,
                        TransactionType = currency == "PHP" ? $"DEPOSIT (PHP ₱{displayAmount.ToString("F2")})" : "DEPOSIT" // Include original PHP amount
                    };

                    _context.Transactions.Add(transaction);
                    await _context.SaveChangesAsync();
                    
                    // Set success message - convert amount to string to avoid serialization issues
                    TempData["SuccessMessage"] = $"${amount.ToString("F2")} has been added to your account!";
                    
                    // Return success with redirect to profile page
                    return Json(new { 
                        success = true, 
                        directSuccess = true,
                        message = $"${amount} has been added to your account!",
                        redirectUrl = Url.Action("Profile", "Account")
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment: {Message}", ex.Message);
                Console.WriteLine($"Payment error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.LogError(ex.InnerException, "Inner exception: {Message}", ex.InnerException.Message);
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> PaymentSuccess(decimal amount, int userId, string currency = "USD")
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return RedirectToAction("Index", "Home");
                }
                
                // Convert amount to USD if it's in PHP
                decimal usdAmount = amount;
                if (currency == "PHP")
                {
                    // Convert PHP to USD for storage
                    // Use the exact same conversion rate as in the layout
                    usdAmount = decimal.Round(amount / 56.5m, 2);
                    
                    // Log the conversion for debugging
                    _logger.LogInformation($"Converting {amount} PHP to USD: {usdAmount} USD");
                }

                // Format the amount based on the currency for display
                string formattedAmount = currency == "PHP" ? "₱" + amount.ToString("F2") : "$" + amount.ToString("F2");
                
                // Store the original currency and amount in TempData for the profile page to use
                // Convert decimal values to strings to avoid serialization issues
                TempData["LastDepositCurrency"] = currency;
                TempData["LastDepositAmount"] = amount.ToString("F2");
                TempData["LastDepositUsdAmount"] = usdAmount.ToString("F2");
                
                // Add funds to user's balance (always stored in USD)
                user.balance = (user.balance ?? 0) + usdAmount;

                // Create a transaction record for the deposit without creating a stock entry
                // We'll use a special transaction type instead of creating a stock entry
                var transaction = new Transaction
                {
                    user_id = userId,
                    StockId = null, // No stock associated with this transaction
                    quantity = 1,
                    Price = usdAmount, // Positive for deposit, in USD
                    TransactionTime = DateTime.UtcNow,
                    TransactionType = currency == "PHP" ? $"DEPOSIT (PHP ₱{amount.ToString("F2")}, USD ${usdAmount.ToString("F2")})" : "DEPOSIT" // Include both currencies for clarity
                };

                _context.Transactions.Add(transaction);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"{formattedAmount} has been added to your account!";
                return RedirectToAction("Profile", "Account");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing successful payment: {Message}", ex.Message);
                Console.WriteLine($"Payment success error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.LogError(ex.InnerException, "Inner exception: {Message}", ex.InnerException.Message);
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                TempData["ErrorMessage"] = $"There was an error processing your payment: {ex.Message}";
                return RedirectToAction("Profile", "Account");
            }
        }

        [HttpGet]
        public IActionResult PaymentCancel()
        {
            TempData["ErrorMessage"] = "Payment was cancelled";
            return RedirectToAction("Profile", "Account");
        }
        
        [HttpGet]
        public async Task<IActionResult> CashOut()
        {
            string? userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.email == userEmail);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }
            
            // Pass the available balance to the view
            ViewBag.AvailableBalance = user.balance?.ToString("F2") ?? "0.00";
            ViewData["StripePublicKey"] = _stripeService.GetPublicKey();
            
            return View();
        }
        
        [HttpPost]
        public async Task<IActionResult> ProcessCashOut([FromBody] PaymentRequest request)
        {
            try
            {
                if (request == null || request.amount <= 0)
                {
                    return BadRequest(new { success = false, message = "Amount must be greater than zero" });
                }
                
                if (string.IsNullOrEmpty(request.paymentMethodId))
                {
                    return BadRequest(new { success = false, message = "Payment method information is required" });
                }
                
                // Get the amount and currency from the request
                decimal amount = request.amount;
                string currency = request.currency ?? "USD";
                
                // Store the original amount for display
                decimal displayAmount = amount;
                
                // Convert amount to USD if it's in PHP (for database storage)
                if (currency == "PHP")
                {
                    // Convert PHP to USD for internal storage
                    amount = decimal.Round(amount / 56.5m, 2);
                    _logger.LogInformation($"Converting {displayAmount} PHP to USD: {amount} USD for cashout");
                }
                
                // Format the amount for display based on the currency
                string formattedAmount = currency == "PHP" ? "₱" + displayAmount.ToString("F2") : "$" + amount.ToString("F2");

                string? userEmail = HttpContext.Session.GetString("UserEmail");
                if (string.IsNullOrEmpty(userEmail))
                {
                    return Json(new { success = false, message = "User not logged in" });
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.email == userEmail);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }
                
                // Check if user has enough balance (in USD)
                if (!user.balance.HasValue || user.balance.Value < amount)
                {
                    return Json(new { success = false, message = "Insufficient funds" });
                }
                
                try
                {
                    // For demo purposes, we'll simulate a Stripe payout to the payment method
                    // In a real application, you would use Stripe's Transfer or Payout API
                    if (currency == "PHP")
                    {
                        _logger.LogInformation($"Simulating payout of {displayAmount} PHP (${amount} USD) to payment method {request.paymentMethodId}");
                    }
                    else
                    {
                        _logger.LogInformation($"Simulating payout of ${amount} to payment method {request.paymentMethodId}");
                    }
                    
                    // Simulate processing time
                    await Task.Delay(1500);
                    
                    // Deduct funds from user's balance (in USD)
                    user.balance = user.balance.Value - amount;

                    // Get last 4 digits of the payment method ID for display purposes
                    string cardLastFour = request.paymentMethodId.Length >= 4 ? 
                        request.paymentMethodId.Substring(Math.Max(0, request.paymentMethodId.Length - 4)) : 
                        "****";
                    
                    // Create a transaction record for the withdrawal
                    var transaction = new Transaction
                    {
                        user_id = user.user_id,
                        StockId = null, // No stock associated with this transaction
                        quantity = 1,
                        Price = -amount, // Negative for withdrawal
                        TransactionTime = DateTime.UtcNow,
                        TransactionType = currency == "PHP" ? 
                            $"WITHDRAWAL TO CARD ****{cardLastFour} (PHP ₱{displayAmount.ToString("F2")})" : 
                            $"WITHDRAWAL TO CARD ****{cardLastFour}" // Include original PHP amount
                    };

                    _context.Transactions.Add(transaction);
                    await _context.SaveChangesAsync();
                    
                    // Store the original currency and amount in TempData for the profile page to use
                    // Convert decimal values to strings to avoid serialization issues
                    TempData["LastWithdrawalCurrency"] = currency;
                    TempData["LastWithdrawalAmount"] = displayAmount.ToString("F2");
                    TempData["LastWithdrawalUsdAmount"] = amount.ToString("F2");
                    
                    // Set success message for when user is redirected to profile
                    TempData["SuccessMessage"] = $"{formattedAmount} has been withdrawn to your card ending in {cardLastFour}. The funds should arrive within 1-3 business days.";
                    
                    // Return success response
                    return Json(new { 
                        success = true, 
                        message = $"{formattedAmount} has been withdrawn to your card ending in {cardLastFour}. The funds should arrive within 1-3 business days.",
                        redirectUrl = Url.Action("Profile", "Account")
                    });
                }
                catch (Exception stripeEx)
                {
                    _logger.LogError(stripeEx, "Stripe API error: {Message}", stripeEx.Message);
                    return Json(new { success = false, message = $"Payment processing error: {stripeEx.Message}" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing withdrawal: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }
    }
}
