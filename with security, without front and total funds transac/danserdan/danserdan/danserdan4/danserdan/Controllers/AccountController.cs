using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using danserdan.Models;
using danserdan.Services;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;


namespace danserdan.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDBContext _context;

        public AccountController(ApplicationDBContext context)
        {
            _context = context;
        }

        public IActionResult GenerateCaptcha()
        {
            string captchaText = GenerateCaptchaText();
            HttpContext.Session.SetString("Captcha", captchaText);

            using (Bitmap bitmap = new Bitmap(120, 40))
            using (Graphics g = Graphics.FromImage(bitmap))
            using (MemoryStream ms = new MemoryStream())
            {
                g.Clear(Color.FromArgb(42, 45, 58));
                Font font = new Font("Arial", 24, FontStyle.Bold);
                Brush brush = new SolidBrush(Color.White);
                g.DrawString(captchaText, font, brush, 10, 5);

                // Add noise
                Random rand = new Random();
                for (int i = 0; i < 20; i++)
                {
                    g.DrawLine(new Pen(Color.Gray), rand.Next(120), rand.Next(40), rand.Next(120), rand.Next(40));
                }

                bitmap.Save(ms, ImageFormat.Png);
                return File(ms.ToArray(), "image/png");
            }
        }

        private string GenerateCaptchaText()
        {
            string characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            Random rand = new Random();
            char[] captchaText = new char[4];
            for (int i = 0; i < captchaText.Length; i++)
            {
                captchaText[i] = characters[rand.Next(characters.Length)];
            }
            return new string(captchaText);
        }

        private bool IsValidPassword(string password)
        {
            var passwordRegex = new Regex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$");
            return passwordRegex.IsMatch(password);
        }

        [HttpGet]
        public IActionResult Signup()
        {
            return View();
        }

        [HttpPost]

        public async Task<IActionResult> Signup(string firstName, string lastName, string username, string email, string password, string captcha, bool isModal = false)
        {
            try
            {
                if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                {
                    ViewBag.Error = "Please fill in all fields.";
                    return isModal ? Json(new { success = false, message = "Please fill in all fields." }) : View();
                }

                if (!IsValidPassword(password))
                {
                    ViewBag.Error = "Password must be at least 8 characters long, contain at least one uppercase letter, one lowercase letter, one number, and one special character.";
                    return isModal ? Json(new { success = false, message = "Password must meet the required criteria." }) : View();
                }

                string sessionCaptcha = HttpContext.Session.GetString("Captcha");
                if (string.IsNullOrEmpty(captcha) || captcha != sessionCaptcha)
                {
                    ViewBag.Error = "Incorrect CAPTCHA. Please try again.";
                    return isModal ? Json(new { success = false, message = "Incorrect CAPTCHA. Please try again." }) : View();
                }

                if (await _context.Users.AnyAsync(u => u.username == username))
                {
                    ViewBag.Error = "Username is already taken.";
                    return isModal ? Json(new { success = false, message = "Username is already taken." }) : View();
                }

                if (await _context.Users.AnyAsync(u => u.email == email))
                {
                    ViewBag.Error = "Email is already taken.";
                    return isModal ? Json(new { success = false, message = "Email is already taken." }) : View();
                }

                var newUser = new Users
                {
                    firstName = firstName,
                    lastName = lastName,
                    username = username,
                    email = email,
                    password_hash = HashPassword(password),
                    balance = null,
                    created_at = DateTime.Now
                };

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                HttpContext.Session.SetString("UserEmail", newUser.email);
                HttpContext.Session.SetString("Username", newUser.username);

                return isModal ? Json(new { success = true }) : RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Signup: {ex.Message}");
                ViewBag.Error = "An error occurred during signup. Please try again.";
                return isModal ? Json(new { success = false, message = "An error occurred during signup. Please try again." }) : View();
            }
        }

        // Dictionary to track failed login attempts
        private static Dictionary<string, (int attempts, DateTime lastAttempt)> _failedLoginAttempts = new Dictionary<string, (int, DateTime)>();
        private const int MAX_LOGIN_ATTEMPTS = 5;
        private const int LOCKOUT_SECONDS = 30;
        
        [HttpGet]
        public IActionResult Login(string message = null)
        {
            if (!string.IsNullOrEmpty(message))
            {
                ViewBag.Message = message;
            }
            return View();
        }
        
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }
        
        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ViewBag.Error = "Please enter your email address.";
                return View();
            }
            
            var user = await _context.Users.FirstOrDefaultAsync(u => u.email == email);
            if (user == null)
            {
                // For security reasons, don't reveal that the email doesn't exist
                ViewBag.Message = "If your email is registered, you will receive password reset instructions.";
                return View();
            }
            
            // Generate a temporary password
            string tempPassword = GenerateRandomPassword();
            string hashedTempPassword = HashPassword(tempPassword);
            
            // Update the user's password in the database
            user.password_hash = hashedTempPassword;
            await _context.SaveChangesAsync();
            
            // In a real application, you would send an email here
            // For this demo, we'll just display the temporary password
            ViewBag.TempPassword = tempPassword;
            ViewBag.Message = "A temporary password has been generated. Please use it to log in and then change your password.";
            
            return View("ForgotPasswordConfirmation");
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password, bool isModal = false)
        {
            try
            {
                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    ViewBag.Error = "Please enter both username and password.";
                    return isModal ? Json(new { success = false, message = "Please enter both username and password." }) : View();
                }
                
                // Check if the IP is currently locked out - track by IP address only
                string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                string clientKey = ipAddress; // Track by IP address only, regardless of username
                
                if (_failedLoginAttempts.TryGetValue(clientKey, out var attemptInfo))
                {
                    // Check if the lockout period is still active
                    if (attemptInfo.attempts >= MAX_LOGIN_ATTEMPTS)
                    {
                        TimeSpan timeElapsed = DateTime.Now - attemptInfo.lastAttempt;
                        int secondsRemaining = LOCKOUT_SECONDS - (int)timeElapsed.TotalSeconds;
                        
                        if (secondsRemaining > 0)
                        {
                            string errorMessage = $"Too many failed login attempts. Please try again in {secondsRemaining} seconds.";
                            ViewBag.Error = errorMessage;
                            return isModal ? Json(new { success = false, message = errorMessage }) : View();
                        }
                        else
                        {
                            // Lockout period has expired, reset the counter
                            _failedLoginAttempts.Remove(clientKey);
                        }
                    }
                }

                // For debugging - log the hashed password being checked
                string hashedPassword = HashPassword(password);
                Console.WriteLine($"Login attempt: Username={username}, HashedPassword={hashedPassword}");

                var user = await _context.Users.FirstOrDefaultAsync(u => (u.email == username || u.username == username));
                
                if (user != null)
                {
                    Console.WriteLine($"User found: Username={user.username}, StoredHash={user.password_hash}, IsAdmin={user.IsAdmin}");
                    
                    // Check if password matches
                    if (user.password_hash == hashedPassword)
                    {
                        // Successful login - reset failed attempts
                        if (_failedLoginAttempts.ContainsKey(clientKey))
                        {
                            _failedLoginAttempts.Remove(clientKey);
                        }
                        
                        if (string.IsNullOrEmpty(user.firstName)) user.firstName = "User";
                        if (string.IsNullOrEmpty(user.lastName)) user.lastName = "Account";
                        await _context.SaveChangesAsync();

                        HttpContext.Session.SetString("UserEmail", user.email);
                        HttpContext.Session.SetString("Username", user.username);
                        HttpContext.Session.SetString("IsAdmin", user.IsAdmin.ToString());

                        Console.WriteLine($"Login successful: Username={user.username}, IsAdmin={user.IsAdmin}");

                        // If the user is an admin, redirect to Admin dashboard
                        if (user.IsAdmin)
                        {
                            return isModal ? Json(new { success = true, redirectUrl = "/Admin/Index" }) : RedirectToAction("Index", "Admin");
                        }

                        return isModal ? Json(new { success = true }) : RedirectToAction("Index", "Home");
                    }
                    else
                    {
                        Console.WriteLine("Password mismatch");
                        // Increment failed login attempts
                        RecordFailedLoginAttempt(clientKey);
                    }
                }
                else
                {
                    Console.WriteLine($"User not found: {username}");
                    // Increment failed login attempts
                    RecordFailedLoginAttempt(clientKey);
                }

                // Check if we've reached the maximum attempts after this failure
                if (_failedLoginAttempts.TryGetValue(clientKey, out var currentAttempts) && 
                    currentAttempts.attempts >= MAX_LOGIN_ATTEMPTS)
                {
                    string errorMessage = $"Too many failed login attempts. Please try again in {LOCKOUT_SECONDS} seconds.";
                    ViewBag.Error = errorMessage;
                    return isModal ? Json(new { success = false, message = errorMessage }) : View();
                }
                else
                {
                    int attemptsLeft = MAX_LOGIN_ATTEMPTS - (currentAttempts.attempts);
                    string errorMessage = $"Invalid username or password. {attemptsLeft} attempts remaining before timeout.";
                    ViewBag.Error = errorMessage;
                    return isModal ? Json(new { success = false, message = errorMessage }) : View();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Login: {ex.Message}");
                ViewBag.Error = "An error occurred during login. Please try again.";
                return isModal ? Json(new { success = false, message = "An error occurred during login. Please try again." }) : View();
            }
        }

        public async Task<IActionResult> Profile(int page = 1, int pageSize = 5, string errorMessage = null)
        {
            string? userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail)) return RedirectToAction("Login");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.email == userEmail);
            if (user == null) return RedirectToAction("Login");

            if (string.IsNullOrEmpty(user.firstName)) user.firstName = "User";
            if (string.IsNullOrEmpty(user.lastName)) user.lastName = "Account";
            
            // Only save changes if we actually modified the user
            bool userModified = _context.Entry(user).State == EntityState.Modified;
            if (userModified)
            {
                await _context.SaveChangesAsync();
            }

            // Calculate true balance from all transactions
            var computedBalance = await CalculateUserBalanceAsync(user.user_id);
            ViewBag.ComputedBalance = computedBalance;

            // Get paginated transactions in a single query
            var totalTransactions = await _context.Transactions.CountAsync(t => t.user_id == user.user_id);
            var transactions = await _context.Transactions
                .Where(t => t.user_id == user.user_id)
                .OrderByDescending(t => t.TransactionTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Get summary data in a more efficient way
            var transactionSummary = await _context.Transactions
                .Where(t => t.user_id == user.user_id)
                .GroupBy(t => 1) // Group all together
                .Select(g => new
                {
                    TotalInvested = g.Where(t => t.Price > 0).Sum(t => t.Price * t.quantity),
                    TotalSold = g.Where(t => t.Price < 0).Sum(t => t.Price * t.quantity),
                    UniqueStocks = g.Select(t => t.StockId).Distinct().Count()
                })
                .FirstOrDefaultAsync();

            decimal totalInvested = 0;
            decimal totalSold = 0;
            int uniqueStocks = 0;
            int totalTrades = totalTransactions; // Add the missing totalTrades variable

            if (transactionSummary != null)
            {
                totalInvested = transactionSummary.TotalInvested;
                totalSold = transactionSummary.TotalSold;
                uniqueStocks = transactionSummary.UniqueStocks;
            }

            decimal totalReturn = totalSold - totalInvested;
            string returnPercentage = totalInvested > 0 ? $"{(totalReturn / totalInvested * 100):F2}%" : "0.00%";

            // Get stock holdings more efficiently
            var stockHoldings = new List<StockHolding>();
            
            // Get all relevant stock IDs and their transaction data in a single query
            var stockTransactions = await _context.Transactions
                .Where(t => t.user_id == user.user_id && t.StockId != null)
                .GroupBy(t => t.StockId)
                .Select(g => new
                {
                    StockId = g.Key,
                    SharesBought = g.Where(t => t.Price > 0).Sum(t => t.quantity),
                    SharesSold = g.Where(t => t.Price < 0).Sum(t => t.quantity),
                    TotalSpent = g.Where(t => t.Price > 0).Sum(t => t.Price * t.quantity)
                })
                .ToListAsync();

            // Get all relevant stock data in a single query
            var stockIds = stockTransactions.Select(st => st.StockId).ToList();
            var stocks = await _context.Stocks
                .Where(s => stockIds.Contains(s.stock_id))
                .ToDictionaryAsync(s => s.stock_id, s => s);

            // Calculate stock holdings
            foreach (var st in stockTransactions)
            {
                var sharesOwned = st.SharesBought - st.SharesSold;
                if (sharesOwned <= 0 || !st.StockId.HasValue || !stocks.TryGetValue(st.StockId.Value, out var stock))
                    continue;

                var avgPurchasePrice = st.SharesBought > 0 ? st.TotalSpent / st.SharesBought : 0;
                var profitLoss = (stock.market_price - avgPurchasePrice) * sharesOwned;
                var profitLossPercentage = avgPurchasePrice > 0 ? $"{(stock.market_price - avgPurchasePrice) / avgPurchasePrice * 100:F2}%" : "0.00%";

                stockHoldings.Add(new StockHolding
                {
                    StockId = st.StockId ?? 0,
                    Symbol = stock.symbol ?? "",
                    CompanyName = stock.company_name ?? "",
                    Quantity = sharesOwned,
                    PurchasePrice = avgPurchasePrice,
                    CurrentPrice = stock.market_price,
                    ProfitLoss = profitLoss,
                    ProfitLossPercentage = profitLossPercentage
                });
            }

            var viewModel = new ProfileViewModel
            {
                User = user,
                Transactions = transactions,
                StockHoldings = stockHoldings,
                TotalTrades = totalTrades,
                UniqueStocks = uniqueStocks,
                TotalReturn = totalReturn,
                ReturnPercentage = returnPercentage,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(totalTransactions / (double)pageSize),
                PageSize = pageSize
            };

            if (TempData.ContainsKey("SuccessMessage"))
                ViewBag.SuccessMessage = TempData["SuccessMessage"];
            if (TempData.ContainsKey("ErrorMessage"))
                ViewBag.ErrorMessage = TempData["ErrorMessage"];
            
            // Handle the error message passed as a parameter
            if (!string.IsNullOrEmpty(errorMessage))
                ViewBag.ErrorMessage = errorMessage;

            return View("~/Views/Account/Profile.cshtml", viewModel);
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(string username, string firstName, string lastName, string email, string newPassword, string jsDetection)
        {
            // Check if this is an AJAX request or if JS is enabled
            bool isAjaxRequest = Request.Headers["X-Requested-With"] == "XMLHttpRequest" || jsDetection == "true";
            
            try
            {
                var userEmail = HttpContext.Session.GetString("UserEmail");
                if (string.IsNullOrEmpty(userEmail))
                    return isAjaxRequest 
                        ? Json(new { success = false, message = "You must be logged in to update your profile." })
                        : RedirectToAction("Login");

                var user = await _context.Users.FirstOrDefaultAsync(u => u.email == userEmail);
                if (user == null)
                    return isAjaxRequest
                        ? Json(new { success = false, message = "User not found." })
                        : RedirectToAction("Login");

                if (username != user.username && await _context.Users.AnyAsync(u => u.username == username && u.user_id != user.user_id))
                    return isAjaxRequest
                        ? Json(new { success = false, message = "Username is already taken." })
                        : (IActionResult)RedirectToAction("Profile", new { errorMessage = "Username is already taken." });

                user.username = username;
                user.firstName = firstName;
                user.lastName = lastName;

                if (!string.IsNullOrEmpty(newPassword))
                {
                    if (!IsPasswordStrong(newPassword))
                    {
                        var errorMsg = "Password must be at least 8 characters long and include an uppercase letter, lowercase letter, number, and special character.";
                        return isAjaxRequest
                            ? Json(new { success = false, message = errorMsg })
                            : (IActionResult)RedirectToAction("Profile", new { errorMessage = errorMsg });
                    }

                    user.password_hash = HashPassword(newPassword);
                }

                await _context.SaveChangesAsync();
                HttpContext.Session.SetString("Username", username);

                if (isAjaxRequest)
                {
                    return Json(new
                    {
                        success = true,
                        message = "Profile updated successfully.",
                        username = user.username,
                        email = user.email
                    });
                }
                else
                {
                    TempData["SuccessMessage"] = "Profile updated successfully.";
                    return RedirectToAction("Profile");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateProfile: {ex.Message}");
                return isAjaxRequest
                    ? Json(new { success = false, message = "An error occurred while updating your profile. Please try again." })
                    : (IActionResult)RedirectToAction("Profile", new { errorMessage = "An error occurred while updating your profile. Please try again." });
            }
        }

        private bool IsPasswordStrong(string password)
        {
            return password.Length >= 8 &&
                   password.Any(char.IsUpper) &&
                   password.Any(char.IsLower) &&
                   password.Any(char.IsDigit) &&
                   password.Any(ch => "!@#$%^&*(),.?\":{}|<>".Contains(ch));
        }

        [HttpPost]
     
        private string HashPassword(string password)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hashBytes = sha.ComputeHash(bytes);
                return Convert.ToBase64String(hashBytes);
            }
        }
        
        // Helper method to record failed login attempts
        private void RecordFailedLoginAttempt(string clientKey)
        {
            if (_failedLoginAttempts.TryGetValue(clientKey, out var attemptInfo))
            {
                _failedLoginAttempts[clientKey] = (attemptInfo.attempts + 1, DateTime.Now);
            }
            else
            {
                _failedLoginAttempts[clientKey] = (1, DateTime.Now);
            }
        }
        
        // Helper method to generate a random password
        private string GenerateRandomPassword()
        {
            const string uppercaseChars = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string lowercaseChars = "abcdefghijkmnopqrstuvwxyz";
            const string digitChars = "23456789";
            const string specialChars = "!@#$%^&*()";
            
            var random = new Random();
            var password = new StringBuilder();
            
            // Ensure at least one of each character type
            password.Append(uppercaseChars[random.Next(uppercaseChars.Length)]);
            password.Append(lowercaseChars[random.Next(lowercaseChars.Length)]);
            password.Append(digitChars[random.Next(digitChars.Length)]);
            password.Append(specialChars[random.Next(specialChars.Length)]);
            
            // Add 4 more random characters for a total length of 8
            const string allChars = uppercaseChars + lowercaseChars + digitChars + specialChars;
            for (int i = 0; i < 4; i++)
            {
                password.Append(allChars[random.Next(allChars.Length)]);
            }
            
            // Shuffle the password characters
            return new string(password.ToString().OrderBy(c => random.Next()).ToArray());
        }

        // Helper to calculate true balance from all transactions
        private async Task<decimal> CalculateUserBalanceAsync(int userId)
        {
            // Add funds (transaction_type: addfunds or similar): Price > 0, StockId == null
            var addFunds = await _context.Transactions
                .Where(t => t.user_id == userId && t.StockId == null && t.Price > 0)
                .SumAsync(t => t.Price * t.quantity);

            // Payout (transaction_type: payout or similar): Price < 0, StockId == null
            var payouts = await _context.Transactions
                .Where(t => t.user_id == userId && t.StockId == null && t.Price < 0)
                .SumAsync(t => t.Price * t.quantity);

            // Buy stock: Price > 0, StockId != null
            var buys = await _context.Transactions
                .Where(t => t.user_id == userId && t.StockId != null && t.Price > 0)
                .SumAsync(t => t.Price * t.quantity);

            // Sell stock: Price < 0, StockId != null
            var sells = await _context.Transactions
                .Where(t => t.user_id == userId && t.StockId != null && t.Price < 0)
                .SumAsync(t => t.Price * t.quantity);

            // True balance: addFunds + sells + payouts (payouts are negative) - buys
            return addFunds + sells + payouts - buys;
        }
    }
}
