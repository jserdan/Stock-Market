using danserdan.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Net.Http;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add memory cache
builder.Services.AddMemoryCache();

// Add DbContext for SQL Server
builder.Services.AddDbContext<ApplicationDBContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseSqlServer(connectionString);
});

// Register HttpClient and AlphaVantageService with all dependencies
builder.Services.AddHttpClient<AlphaVantageService>()
    .ConfigureHttpClient(client =>
    {
        // You can configure the HttpClient here, e.g., set the base address or headers.
        client.BaseAddress = new Uri("https://www.alphavantage.co");
    });

// Register the AlphaVantageService with ApiKey and ApplicationDBContext injection
builder.Services.AddScoped<AlphaVantageService>(serviceProvider =>
{
    var apiKey = builder.Configuration.GetValue<string>("AlphaVantage:ApiKey");
    var context = serviceProvider.GetRequiredService<ApplicationDBContext>();
    var httpClient = serviceProvider.GetRequiredService<HttpClient>();
    var memoryCache = serviceProvider.GetRequiredService<IMemoryCache>();
    return new AlphaVantageService(httpClient, apiKey, context, memoryCache);
});

// Add session support with shorter timeout for security
builder.Services.AddDistributedMemoryCache(); // This adds in-memory cache for session storage
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(20); // Set session timeout to 20 minutes
    options.Cookie.HttpOnly = true; // Better security
    options.Cookie.IsEssential = true; // Make the session cookie essential
    options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict; // Enhance security
});

// Register the background service for random stock price updates
builder.Services.AddHostedService<StockPriceUpdateService>();
builder.Services.AddScoped<StripeService>();

// Add HttpContextAccessor for currency service
builder.Services.AddHttpContextAccessor();

// Register the currency service
builder.Services.AddScoped<CurrencyService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Enable session middleware
app.UseSession();  // Add this line to enable session

app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Create admin user if it doesn't exist
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var dbContext = services.GetRequiredService<danserdan.Services.ApplicationDBContext>();
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        // Check if admin user exists
        var adminUser = dbContext.Users.FirstOrDefault(u => u.username == "admin");
        
        string adminPassword = "Admin@123"; // Strong default password
        string hashedPassword;
        
        using (var sha256 = System.Security.Cryptography.SHA256.Create())
        {
            var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(adminPassword));
            hashedPassword = Convert.ToBase64String(hashedBytes); // Use Base64 encoding to match AccountController
        }
        
        if (adminUser == null)
        {
            // Create admin user with secure password
            var newAdmin = new danserdan.Models.Users
            {
                firstName = "Admin",
                lastName = "User",
                username = "admin",
                email = "admin@trades.com",
                password_hash = hashedPassword,
                balance = 0,
                created_at = DateTime.UtcNow,
                IsAdmin = true
            };
            
            dbContext.Users.Add(newAdmin);
            dbContext.SaveChanges();
            
            logger.LogInformation("Admin user created successfully");
        }
        else
        {
            // Update existing admin user's password to ensure it has the correct hash
            adminUser.password_hash = hashedPassword;
            adminUser.IsAdmin = true; // Ensure admin flag is set
            dbContext.SaveChanges();
            logger.LogInformation("Admin user credentials updated");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error creating/updating admin user");
    }
}

app.Run();