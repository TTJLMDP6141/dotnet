using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UrbanServicesMvc.Data;
using UrbanServicesMvc.Models;

var builder = WebApplication.CreateBuilder(args);
var sqlitePath = Path.Combine(builder.Environment.ContentRootPath, "urbanservices.db");
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? $"Data Source={sqlitePath}";

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Home/AccessDenied";
    options.Cookie.Name = "UrbanServices.Auth";
    options.SlidingExpiration = true;
});
builder.Services.AddControllersWithViews();

var port = Environment.GetEnvironmentVariable("PORT");
builder.WebHost.UseUrls($"http://0.0.0.0:{(string.IsNullOrWhiteSpace(port) ? "5080" : port)}");

var app = builder.Build();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
    var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var role in new[] { "CUSTOMER", "PROVIDER", "ADMIN" })
        if (!await roles.RoleExistsAsync(role))
            await roles.CreateAsync(new IdentityRole(role));

    if (!await db.Services.AnyAsync())
    {
        db.Services.AddRange(
            new ServiceOffering { Name = "Home Cleaning", Category = "Cleaning", Description = "A careful top-to-bottom clean for your living spaces, bedrooms and kitchen.", Price = 499, DurationMinutes = 120 },
            new ServiceOffering { Name = "Bathroom Cleaning", Category = "Cleaning", Description = "Deep scrubbing and sanitizing for tiles, fixtures and hard-to-reach corners.", Price = 299, DurationMinutes = 60 },
            new ServiceOffering { Name = "AC Repair", Category = "Repair", Description = "Diagnosis and repair for common air-conditioner cooling and electrical issues.", Price = 399, DurationMinutes = 90 },
            new ServiceOffering { Name = "Plumbing", Category = "Repair", Description = "Help with leaking taps, blocked drains and everyday plumbing repairs.", Price = 349, DurationMinutes = 60 },
            new ServiceOffering { Name = "Electrician", Category = "Repair", Description = "Safe help with switches, lights, fans and other household electrical needs.", Price = 299, DurationMinutes = 60 },
            new ServiceOffering { Name = "Salon at Home", Category = "Personal Care", Description = "Book a convenient grooming appointment in the comfort of your home.", Price = 599, DurationMinutes = 90 },
            new ServiceOffering { Name = "Pest Control", Category = "Home Care", Description = "A home inspection and targeted treatment to help keep common pests away.", Price = 699, DurationMinutes = 120 },
            new ServiceOffering { Name = "Car Cleaning", Category = "Cleaning", Description = "A thorough exterior wash and interior refresh at your doorstep.", Price = 399, DurationMinutes = 60 },
            new ServiceOffering { Name = "Appliance Repair", Category = "Repair", Description = "Troubleshooting for common problems with home appliances.", Price = 449, DurationMinutes = 90 },
            new ServiceOffering { Name = "Painting", Category = "Home Care", Description = "Get help with touch-ups and small interior painting jobs.", Price = 899, DurationMinutes = 180 });
        await db.SaveChangesAsync();
    }
}

await app.RunAsync();
