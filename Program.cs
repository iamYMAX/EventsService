using EventsService.Data;
using EventsService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddIdentity<User, Role>(options => {
    options.SignIn.RequireConfirmedAccount = false; // Adjust as needed
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6; // Adjust as needed
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure application cookie settings
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login"; // Set your login path
    options.AccessDeniedPath = "/Account/AccessDenied"; // Set your access denied path
});

builder.Services.AddControllersWithViews();
builder.Services.AddScoped<EventsService.Services.ICartService, EventsService.Services.CartService>();

var app = builder.Build();

// Seed roles and default admin user
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var roleManager = services.GetRequiredService<RoleManager<Role>>();
        var userManager = services.GetRequiredService<UserManager<User>>();
        var logger = services.GetRequiredService<ILogger<Program>>(); // Get logger instance

        string[] roleNames = { "Admin", "Manager", "SalesRepresentative", "Client" };
        foreach (var roleName in roleNames)
        {
            var roleExist = await roleManager.RoleExistsAsync(roleName);
            if (!roleExist)
            {
                await roleManager.CreateAsync(new Role { Name = roleName });
                logger.LogInformation($"Role '{roleName}' created.");
            }
        }

        var adminUser = await userManager.FindByEmailAsync("admin@example.com");
        if (adminUser == null)
        {
            // IMPORTANT: Use a strong password, preferably from configuration
            // For now, using a placeholder. This should be changed in a real application.
            var newAdminUser = new User { UserName = "admin", Email = "admin@example.com", EmailConfirmed = true }; // EmailConfirmed = true for simplicity
            var createUserResult = await userManager.CreateAsync(newAdminUser, "AdminPassword123!"); // CHANGE THIS PASSWORD
            if (createUserResult.Succeeded)
            {
                await userManager.AddToRoleAsync(newAdminUser, "Admin");
                logger.LogInformation("Default admin user created and assigned to 'Admin' role.");
            }
            else
            {
                foreach (var error in createUserResult.Errors)
                {
                    logger.LogError($"Error creating admin user: {error.Description}");
                }
            }
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
        // In a production environment, you might want to handle this more gracefully
        // For example, prevent application startup or log to a persistent store
    }
}

// Diagnostic check for Product.Quantity column in Development
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<Program>>();
        var dbContext = services.GetRequiredService<ApplicationDbContext>();
        try
        {
            // Try a benign query that would fail if 'Quantity' column is missing from Products table
            dbContext.Products.Select(p => p.Quantity).Take(1).ToList();
            logger.LogInformation("Product.Quantity column diagnostic check passed.");
        }
        catch (Exception ex) when (ex.InnerException is Microsoft.Data.Sqlite.SqliteException sqliteEx && sqliteEx.Message.ToLower().Contains("no such column"))
        {
            logger.LogWarning(sqliteEx,
                "DIAGNOSTIC WARNING: Could not access 'Product.Quantity' column. " +
                "This might indicate that database migrations have not been fully applied. " +
                "Please ensure your database schema is up-to-date. Try running 'dotnet ef database update'.");
        }
        catch (Exception ex)
        {
            // Catch other potential exceptions during the check, but don't make it fatal for app startup
            logger.LogWarning(ex, "An unexpected error occurred during the Product.Quantity diagnostic check.");
        }
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
