using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UrbanServicesMvc.Data;
using UrbanServicesMvc.Models;

namespace UrbanServicesMvc.Controllers;

[Authorize(Roles = "ADMIN")]
public sealed class AdminController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        return View(new AdminDashboardVm
        {
            CustomerCount = await db.UserRoles.Join(db.Roles, userRole => userRole.RoleId, role => role.Id,
                (userRole, role) => role).CountAsync(role => role.Name == "CUSTOMER"),
            ProviderCount = await db.ProviderProfiles.CountAsync(),
            ServiceCount = await db.Services.CountAsync(),
            BookingCount = await db.Bookings.CountAsync(),
            RecentBookings = await db.Bookings.AsNoTracking().Include(x => x.Customer).Include(x => x.Service)
                .OrderByDescending(x => x.CreatedAt).Take(8).ToListAsync()
        });
    }

    public async Task<IActionResult> Customers()
    {
        var customers = await db.Users.AsNoTracking()
            .Where(x => db.UserRoles.Any(ur => ur.UserId == x.Id &&
                db.Roles.Any(r => r.Id == ur.RoleId && r.Name == "CUSTOMER")))
            .OrderBy(x => x.FullName).ToListAsync();
        return View(customers);
    }

    public async Task<IActionResult> Providers()
    {
        var providers = await db.ProviderProfiles.AsNoTracking().Include(x => x.User)
            .OrderBy(x => x.User.FullName).ToListAsync();
        return View(providers);
    }

    public async Task<IActionResult> Bookings()
    {
        var model = new AdminBookingsVm
        {
            Bookings = await db.Bookings.AsNoTracking().Include(x => x.Customer).Include(x => x.Service)
                .Include(x => x.ProviderProfile).ThenInclude(x => x!.User)
                .OrderByDescending(x => x.CreatedAt).ToListAsync(),
            Providers = await db.ProviderProfiles.AsNoTracking().Include(x => x.User)
                .OrderBy(x => x.User.FullName).ToListAsync()
        };
        ViewBag.Statuses = Enum.GetValues<BookingStatus>();
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignProvider(int id, int? providerId)
    {
        var booking = await db.Bookings.FindAsync(id);
        if (booking is null) return RedirectToAction(nameof(Bookings));
        if (providerId.HasValue && !await db.ProviderProfiles.AnyAsync(x => x.Id == providerId.Value))
        {
            TempData["Error"] = "Please choose a valid provider.";
            return RedirectToAction(nameof(Bookings));
        }
        booking.ProviderProfileId = providerId;
        await db.SaveChangesAsync();
        TempData["Success"] = "Provider assignment updated.";
        return RedirectToAction(nameof(Bookings));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateBookingStatus(int id, string status)
    {
        var booking = await db.Bookings.FindAsync(id);
        if (booking is null) return RedirectToAction(nameof(Bookings));
        if (!Enum.TryParse<BookingStatus>(status, true, out var parsed))
            TempData["Error"] = "Please choose a valid booking status.";
        else
        {
            booking.Status = parsed;
            await db.SaveChangesAsync();
            TempData["Success"] = "Booking status updated.";
        }
        return RedirectToAction(nameof(Bookings));
    }

    public async Task<IActionResult> Services()
    {
        return View(await db.Services.AsNoTracking().OrderBy(x => x.Name).ToListAsync());
    }

    [HttpGet]
    public IActionResult CreateService() => View("ServiceForm", new ServiceVm { Active = true, DurationMinutes = 60 });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateService(ServiceVm form)
    {
        if (!ModelState.IsValid) return View("ServiceForm", form);
        db.Services.Add(ToService(form));
        await db.SaveChangesAsync();
        TempData["Success"] = "Service added successfully.";
        return RedirectToAction(nameof(Services));
    }

    [HttpGet]
    public async Task<IActionResult> EditService(int id)
    {
        var service = await db.Services.FindAsync(id);
        return service is null ? RedirectToAction(nameof(Services)) : View("ServiceForm", FromService(service));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditService(int id, ServiceVm form)
    {
        var service = await db.Services.FindAsync(id);
        if (service is null) return RedirectToAction(nameof(Services));
        if (!ModelState.IsValid) return View("ServiceForm", form);
        Copy(form, service);
        await db.SaveChangesAsync();
        TempData["Success"] = "Service updated successfully.";
        return RedirectToAction(nameof(Services));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteService(int id)
    {
        var service = await db.Services.FindAsync(id);
        if (service is null) return RedirectToAction(nameof(Services));
        if (await db.Bookings.AnyAsync(x => x.ServiceId == id))
            TempData["Error"] = "This service has bookings and cannot be deleted. Mark it inactive instead.";
        else
        {
            db.Services.Remove(service);
            await db.SaveChangesAsync();
            TempData["Success"] = "Service deleted successfully.";
        }
        return RedirectToAction(nameof(Services));
    }

    private static ServiceOffering ToService(ServiceVm form)
    {
        var service = new ServiceOffering();
        Copy(form, service);
        return service;
    }

    private static void Copy(ServiceVm form, ServiceOffering service)
    {
        service.Name = form.Name.Trim();
        service.Description = form.Description.Trim();
        service.Category = form.Category.Trim();
        service.Price = form.Price;
        service.DurationMinutes = form.DurationMinutes;
        service.ImageUrl = string.IsNullOrWhiteSpace(form.ImageUrl) ? null : form.ImageUrl.Trim();
        service.Active = form.Active;
    }

    private static ServiceVm FromService(ServiceOffering service) => new()
    {
        Id = service.Id,
        Name = service.Name,
        Description = service.Description,
        Category = service.Category,
        Price = service.Price,
        DurationMinutes = service.DurationMinutes,
        ImageUrl = service.ImageUrl,
        Active = service.Active
    };
}
