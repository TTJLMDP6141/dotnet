using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UrbanServicesMvc.Data;
using UrbanServicesMvc.Models;

namespace UrbanServicesMvc.Controllers;

[Authorize(Roles = "CUSTOMER")]
public sealed class CustomerController(AppDbContext db, UserManager<AppUser> users) : Controller
{
    public static readonly string[] TimeSlots = ["09:00 AM", "11:00 AM", "01:00 PM", "03:00 PM", "05:00 PM"];

    public async Task<IActionResult> Index()
    {
        var id = CurrentUserId();
        var bookings = await db.Bookings.AsNoTracking().Include(x => x.Service)
            .Where(x => x.CustomerId == id).OrderByDescending(x => x.CreatedAt).ToListAsync();
        ViewBag.UpcomingCount = bookings.Count(x => x.Status is BookingStatus.PENDING or BookingStatus.ACCEPTED or BookingStatus.IN_PROGRESS);
        ViewBag.CompletedCount = bookings.Count(x => x.Status == BookingStatus.COMPLETED);
        return View(bookings.Take(5).ToList());
    }

    [HttpGet]
    public async Task<IActionResult> Book(int id)
    {
        var service = await db.Services.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.Active);
        if (service is null) return NotFound();
        ViewBag.Service = service;
        ViewBag.TimeSlots = TimeSlots;
        var user = await users.GetUserAsync(User);
        return View(new BookingVm { ServiceId = id, BookingDate = DateTime.Today.AddDays(1), Phone = user?.PhoneNumber ?? "" });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Book(BookingVm form)
    {
        var service = await db.Services.FirstOrDefaultAsync(x => x.Id == form.ServiceId && x.Active);
        if (service is null) return NotFound();
        if (form.BookingDate.Date < DateTime.Today)
            ModelState.AddModelError(nameof(form.BookingDate), "Choose today or a future date.");
        if (!TimeSlots.Contains(form.BookingTime))
            ModelState.AddModelError(nameof(form.BookingTime), "Choose one of the available time slots.");
        if (!ModelState.IsValid)
        {
            ViewBag.Service = service;
            ViewBag.TimeSlots = TimeSlots;
            return View(form);
        }

        var booking = new Booking
        {
            CustomerId = CurrentUserId(),
            ServiceId = service.Id,
            BookingDate = form.BookingDate.Date,
            BookingTime = form.BookingTime,
            Address = form.Address.Trim(),
            Phone = form.Phone.Trim(),
            TotalPrice = service.Price,
            Status = BookingStatus.PENDING
        };
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();
        TempData["Success"] = "Booking created successfully.";
        return RedirectToAction(nameof(Confirmation), new { id = booking.Id });
    }

    public async Task<IActionResult> Confirmation(int id)
    {
        var booking = await db.Bookings.AsNoTracking().Include(x => x.Service)
            .FirstOrDefaultAsync(x => x.Id == id && x.CustomerId == CurrentUserId());
        return booking is null ? RedirectToAction(nameof(Bookings)) : View(booking);
    }

    public async Task<IActionResult> Bookings()
    {
        var bookings = await db.Bookings.AsNoTracking().Include(x => x.Service).Include(x => x.Review)
            .Include(x => x.ProviderProfile).ThenInclude(x => x!.User)
            .Where(x => x.CustomerId == CurrentUserId()).OrderByDescending(x => x.CreatedAt).ToListAsync();
        return View(bookings);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var booking = await db.Bookings.FirstOrDefaultAsync(x => x.Id == id && x.CustomerId == CurrentUserId());
        if (booking is null) return RedirectToAction(nameof(Bookings));
        if (booking.Status is not (BookingStatus.PENDING or BookingStatus.ACCEPTED))
            TempData["Error"] = "This booking can no longer be cancelled.";
        else
        {
            booking.Status = BookingStatus.CANCELLED;
            await db.SaveChangesAsync();
            TempData["Success"] = "Booking cancelled successfully.";
        }
        return RedirectToAction(nameof(Bookings));
    }

    [HttpGet]
    public async Task<IActionResult> Review(int id)
    {
        var booking = await db.Bookings.AsNoTracking().Include(x => x.Service)
            .FirstOrDefaultAsync(x => x.Id == id && x.CustomerId == CurrentUserId());
        if (booking is null || booking.Status != BookingStatus.COMPLETED || await db.Reviews.AnyAsync(x => x.BookingId == id))
        {
            TempData["Error"] = "Only completed bookings without a review can be reviewed.";
            return RedirectToAction(nameof(Bookings));
        }
        ViewBag.Booking = booking;
        return View(new ReviewVm { BookingId = id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(ReviewVm form)
    {
        var booking = await db.Bookings.Include(x => x.Service)
            .FirstOrDefaultAsync(x => x.Id == form.BookingId && x.CustomerId == CurrentUserId());
        if (booking is null || booking.Status != BookingStatus.COMPLETED || await db.Reviews.AnyAsync(x => x.BookingId == form.BookingId))
        {
            TempData["Error"] = "Only completed bookings without a review can be reviewed.";
            return RedirectToAction(nameof(Bookings));
        }
        if (!ModelState.IsValid)
        {
            ViewBag.Booking = booking;
            return View(form);
        }
        db.Reviews.Add(new Review
        {
            BookingId = booking.Id,
            UserId = CurrentUserId(),
            ServiceId = booking.ServiceId,
            Rating = form.Rating,
            Comment = form.Comment.Trim()
        });
        await db.SaveChangesAsync();
        TempData["Success"] = "Thank you for sharing your review.";
        return RedirectToAction(nameof(Bookings));
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var user = await users.GetUserAsync(User);
        return user is null ? Challenge() : View(new ProfileVm
        {
            FullName = user.FullName,
            Email = user.Email ?? "",
            Phone = user.PhoneNumber ?? ""
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileVm form)
    {
        var user = await users.GetUserAsync(User);
        if (user is null) return Challenge();
        var email = form.Email.Trim();
        var duplicate = await users.FindByEmailAsync(email);
        if (duplicate is not null && duplicate.Id != user.Id)
            ModelState.AddModelError(nameof(form.Email), "This email is already in use.");
        if (!ModelState.IsValid) return View(form);

        user.FullName = form.FullName.Trim();
        user.PhoneNumber = form.Phone.Trim();
        var emailResult = await users.SetEmailAsync(user, email);
        var nameResult = await users.SetUserNameAsync(user, email);
        var update = await users.UpdateAsync(user);
        foreach (var error in emailResult.Errors.Concat(nameResult.Errors).Concat(update.Errors))
            ModelState.AddModelError("", error.Description);
        if (!emailResult.Succeeded || !nameResult.Succeeded || !update.Succeeded) return View(form);

        TempData["Success"] = "Profile updated successfully.";
        return RedirectToAction(nameof(Profile));
    }

    private string CurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
}
