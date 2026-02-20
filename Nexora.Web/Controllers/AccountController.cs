using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexora.Web.Data;
using Nexora.Web.Data.Models;
using Nexora.Web.Models;

namespace Nexora.Web.Controllers;

public class AccountController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;

    public AccountController(AppDbContext db, UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
    {
        _db = db;
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [HttpGet]
    public IActionResult Register() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterVm vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var emailExists = await _userManager.Users.AnyAsync(x => x.Email == vm.Email);
        if (emailExists)
        {
            ModelState.AddModelError("", "Email already exists.");
            return View(vm);
        }

        var slug = Slugify(vm.OrganizationName);

        var slugExists = await _db.Organizations.IgnoreQueryFilters().AnyAsync(x => x.Slug == slug && !x.IsDeleted);
        if (slugExists)
        {
            ModelState.AddModelError("", "Organization name is already taken.");
            return View(vm);
        }

        var org = new Organization
        {
            Name = vm.OrganizationName.Trim(),
            Slug = slug
        };

        _db.Organizations.Add(org);

        var plan = await _db.SubscriptionPlans.FirstAsync(x => x.Code == "starter");
        _db.Subscriptions.Add(new Subscription
        {
            Organization = org,
            PlanId = plan.Id,
            IsActive = true,
            AutoRenew = true,
            StartDateUtc = DateTime.UtcNow
        });

        var user = new AppUser
        {
            FullName = vm.FullName.Trim(),
            Email = vm.Email.Trim(),
            UserName = vm.Email.Trim(),
            Organization = org
        };

        var result = await _userManager.CreateAsync(user, vm.Password);
        if (!result.Succeeded)
        {
            foreach (var err in result.Errors) ModelState.AddModelError("", err.Description);
            return View(vm);
        }

        await _userManager.AddToRoleAsync(user, "Owner");
        await _signInManager.SignInAsync(user, isPersistent: true);

        return RedirectToAction("Index", "Dashboard");
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginVm vm, string? returnUrl = null)
    {
        if (!ModelState.IsValid) return View(vm);

        var result = await _signInManager.PasswordSignInAsync(vm.Email, vm.Password, vm.RememberMe, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            ModelState.AddModelError("", "Invalid credentials.");
            return View(vm);
        }

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Dashboard");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    private static string Slugify(string input)
    {
        var s = (input ?? "").Trim().ToLowerInvariant();
        s = string.Join("-", s.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        s = new string(s.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());
        return string.IsNullOrWhiteSpace(s) ? Guid.NewGuid().ToString("N")[..10] : s;
    }
}