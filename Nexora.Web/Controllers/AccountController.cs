using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexora.Web.Data;
using Nexora.Web.Data.Models;
using Nexora.Web.Models.IdentityModels;
using Nexora.Web.Services.Email;
using System.Net;

namespace Nexora.Web.Controllers;

public class AccountController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly IEmailSender _emailSender;

    public AccountController(
        AppDbContext db,
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        IEmailSender emailSender)
    {
        _db = db;
        _userManager = userManager;
        _signInManager = signInManager;
        _emailSender = emailSender;
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

        var slugExists = await _db.Organizations.IgnoreQueryFilters()
            .AnyAsync(x => x.Slug == slug && !x.IsDeleted);

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
            foreach (var err in result.Errors)
                ModelState.AddModelError("", err.Description);

            return View(vm);
        }

        await _userManager.AddToRoleAsync(user, "Owner");

        // EMAIL CONFIRMATION
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

        var confirmUrl = Url.Action(
            "ConfirmEmail",
            "Account",
            new { userId = user.Id, token = token },
            protocol: Request.Scheme,
            host: Request.Host.Value);

        var subject = "Confirm your Nexora account";

        var body = $@"
<div style='font-family:Arial,sans-serif;line-height:1.6'>
<h2>Welcome to Nexora 👋</h2>

<p>Hi <b>{WebUtility.HtmlEncode(user.FullName)}</b>,</p>

<p>Please confirm your email to activate your account:</p>

<p>
<a href='{confirmUrl}'
style='display:inline-block;padding:10px 16px;background:#3056D3;color:#fff;text-decoration:none;border-radius:8px'>
Confirm Email
</a>
</p>

<p style='color:#6b7280;font-size:12px'>
If you didn't create this account you can ignore this email.
</p>
</div>";

        try
        {
            if (!string.IsNullOrWhiteSpace(user.Email))
                await _emailSender.SendAsync(user.Email, subject, body);
        }
        catch
        {
        }

        return View("RegisterSuccess");
    }

    [HttpGet]
    public async Task<IActionResult> ConfirmEmail(Guid userId, string token)
    {
        if (userId == Guid.Empty || string.IsNullOrWhiteSpace(token))
            return View("ConfirmEmailFailed");

        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (user == null)
            return View("ConfirmEmailFailed");

        token = token.Replace(' ', '+');

        var result = await _userManager.ConfirmEmailAsync(user, token);

        return result.Succeeded
            ? View("ConfirmEmailSuccess")
            : View("ConfirmEmailFailed");
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

        var user = await _userManager.FindByEmailAsync(vm.Email);

        if (user == null)
        {
            ModelState.AddModelError("", "Invalid credentials.");
            return View(vm);
        }

        if (!user.EmailConfirmed)
        {
            ModelState.AddModelError("", "Please confirm your email first.");
            return View(vm);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user,
            vm.Password,
            vm.RememberMe,
            false);

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

        return string.IsNullOrWhiteSpace(s)
            ? Guid.NewGuid().ToString("N")[..10]
            : s;
    }
}