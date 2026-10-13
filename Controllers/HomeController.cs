using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UrbanServicesMvc.Data;
using UrbanServicesMvc.Models;

namespace UrbanServicesMvc.Controllers;

public sealed class HomeController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? q)
    {
        var services = db.Services.AsNoTracking().Where(x => x.Active);
        if (!string.IsNullOrWhiteSpace(q))
            services = services.Where(x => x.Name.Contains(q) || x.Category.Contains(q) || x.Description.Contains(q));
        ViewBag.Query = q ?? "";
        return View(await services.OrderBy(x => x.Name).ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var service = await db.Services.AsNoTracking()
            .Include(x => x.Reviews).ThenInclude(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id && x.Active);
        return service is null ? NotFound() : View(new ServiceDetailsVm { Service = service, Reviews = service.Reviews.OrderByDescending(x => x.CreatedAt).ToList() });
    }

    public IActionResult AccessDenied() => View();
}
