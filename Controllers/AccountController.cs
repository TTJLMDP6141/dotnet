using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using UrbanServicesMvc.Data;
using UrbanServicesMvc.Models;

namespace UrbanServicesMvc.Controllers;

public sealed class AccountController(
    UserManager<AppUser> users,
    SignInManager<AppUser> signIn,
    AppDbContext db,
    IConfiguration configuration) : Controller
{
    [AllowAnonymous, HttpGet]
    public IActionResult Login(string? accountType)
    {
        var type = NormalizeRole(accountType) ?? "CUSTOMER";
        return View(new LoginVm { AccountType = type });
    }

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginVm form)
    {
        if (!ModelState.IsValid) return View(form);
        var result = await signIn.PasswordSignInAsync(form.Email.Trim(), form.Password, form.RememberMe, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            ModelState.AddModelError("", result.IsLockedOut
                ? "This account is temporarily locked. Try again later."
                : "Email or password is incorrect.");
            return View(form);
        }

        var user = await users.FindByEmailAsync(form.Email.Trim());
        var role = user is null ? null : NormalizeRole(form.AccountType);
        if (user is null || role is null || !await users.IsInRoleAsync(user, role))
        {
            await signIn.SignOutAsync();
            ModelState.AddModelError("", "Choose the account type assigned to this account.");
            return View(form);
        }

        return role switch
        {
            "ADMIN" => RedirectToAction("Index", "Admin"),
            "PROVIDER" => RedirectToAction("Index", "Provider"),
            _ => RedirectToAction("Index", "Customer")
        };
    }

    [AllowAnonymous, HttpGet]
    public IActionResult Register(string? accountType)
    {
        ViewBag.AdminRegistrationEnabled = !string.IsNullOrWhiteSpace(configuration["ADMIN_REGISTRATION_CODE"]);
        return View(new RegisterVm { Role = NormalizeRole(accountType) ?? "CUSTOMER" });
    }

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterVm form)
    {
        ViewBag.AdminRegistrationEnabled = !string.IsNullOrWhiteSpace(configuration["ADMIN_REGISTRATION_CODE"]);
        var role = NormalizeRole(form.Role);
        if (role is null) ModelState.AddModelError(nameof(form.Role), "Choose Customer, Service Provider, or Administrator.");

        if (role == "ADMIN" && !IsValidAdminInvite(form.AdminInviteCode))
            ModelState.AddModelError(nameof(form.AdminInviteCode), "A valid administrator invite code is required.");
        if (!ModelState.IsValid) return View(form);

        var email = form.Email.Trim();
        var user = new AppUser
        {
            UserName = email,
            Email = email,
            FullName = form.FullName.Trim(),
            PhoneNumber = form.Phone.Trim()
        };
        var created = await users.CreateAsync(user, form.Password);
        if (!created.Succeeded)
        {
            AddIdentityErrors(created);
            return View(form);
        }

        var assigned = await users.AddToRoleAsync(user, role!);
        if (!assigned.Succeeded)
        {
            await users.DeleteAsync(user);
            AddIdentityErrors(assigned);
            return View(form);
        }

        if (role == "PROVIDER")
        {
            db.ProviderProfiles.Add(new ProviderProfile
            {
                UserId = user.Id,
                Experience = form.Experience,
                Availability = string.IsNullOrWhiteSpace(form.Availability) ? "Monday to Saturday" : form.Availability.Trim()
            });
            await db.SaveChangesAsync();
        }
        TempData["Success"] = "Your account is ready. Sign in to continue.";
        return RedirectToAction(nameof(Login), new { accountType = role });
    }

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signIn.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    private bool IsValidAdminInvite(string? candidate)
    {
        var expected = configuration["ADMIN_REGISTRATION_CODE"];
        if (string.IsNullOrWhiteSpace(expected) || string.IsNullOrEmpty(candidate)) return false;
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(candidate));
    }

    private void AddIdentityErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
            ModelState.AddModelError("", error.Description);
    }

    private static string? NormalizeRole(string? role) =>
        role?.Trim().ToUpperInvariant() switch
        {
            "CUSTOMER" => "CUSTOMER",
            "PROVIDER" => "PROVIDER",
            "ADMIN" => "ADMIN",
            _ => null
        };
}
