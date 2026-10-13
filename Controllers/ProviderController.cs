using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UrbanServicesMvc.Data;
using UrbanServicesMvc.Models;

namespace UrbanServicesMvc.Controllers;

[Authorize(Roles = "PROVIDER")]
public sealed class ProviderController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var provider = await GetProviderAsync();
        if (provider is null) return NotFound("Provider profile was not found.");
        var bookings = await db.Bookings.AsNoTracking().Include(x => x.Service).Include(x => x.Customer)
            .Where(x => x.ProviderProfileId == provider.Id)
            .OrderBy(x => x.BookingDate).ThenBy(x => x.BookingTime).ToListAsync();
        return View(new ProviderDashboardVm { Provider = provider, Bookings = bookings });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, string status)
    {
        var provider = await GetProviderAsync();
        if (provider is null) return NotFound();
        var booking = await db.Bookings.FirstOrDefaultAsync(x => x.Id == id && x.ProviderProfileId == provider.Id);
        if (booking is null) return RedirectToAction(nameof(Index));
        if (!Enum.TryParse<BookingStatus>(status, true, out var next))
        {
            TempData["Error"] = "That booking status change is not allowed.";
            return RedirectToAction(nameof(Index));
        }
        var allowed = booking.Status switch
        {
            BookingStatus.PENDING => next is BookingStatus.ACCEPTED or BookingStatus.CANCELLED,
            BookingStatus.ACCEPTED => next == BookingStatus.IN_PROGRESS,
            BookingStatus.IN_PROGRESS => next == BookingStatus.COMPLETED,
            _ => false
        };
        if (!allowed)
            TempData["Error"] = "That booking status change is not allowed.";
        else
        {
            booking.Status = next;
            await db.SaveChangesAsync();
            TempData["Success"] = next == BookingStatus.CANCELLED ? "Booking rejected." : "Booking status updated.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var provider = await GetProviderAsync();
        if (provider is null) return NotFound();
        return View(new ProviderProfileVm
        {
            FullName = provider.User.FullName,
            Phone = provider.User.PhoneNumber ?? "",
            Experience = provider.Experience,
            Availability = provider.Availability
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProviderProfileVm form)
    {
        var provider = await GetProviderAsync();
        if (provider is null) return NotFound();
        if (!ModelState.IsValid) return View(form);
        provider.User.FullName = form.FullName.Trim();
        provider.User.PhoneNumber = form.Phone.Trim();
        provider.Experience = form.Experience;
        provider.Availability = form.Availability.Trim();
        await db.SaveChangesAsync();
        TempData["Success"] = "Provider profile updated successfully.";
        return RedirectToAction(nameof(Profile));
    }

    private Task<ProviderProfile?> GetProviderAsync() =>
        db.ProviderProfiles.Include(x => x.User)
            .FirstOrDefaultAsync(x => x.UserId == User.FindFirstValue(ClaimTypes.NameIdentifier));
}
