using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Nexora.Web.Data;
using Nexora.Web.Data.Entities;
using Nexora.Web.Models.Marketing;
using Nexora.Web.Services.Email;
using System.Net;

namespace Nexora.Web.Controllers;

public class HomeController : Controller
{
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _configuration;
    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly ILogger<HomeController> _logger;

    // Rate limit: max 3 messages per 5 minutes per IP
    private const int RateLimitMax = 3;
    private static readonly TimeSpan RateLimitWindow = TimeSpan.FromMinutes(5);

    public HomeController(
        IEmailSender emailSender,
        IConfiguration configuration,
        AppDbContext db,
        IMemoryCache cache,
        ILogger<HomeController> logger)
    {
        _emailSender = emailSender;
        _configuration = configuration;
        _db = db;
        _cache = cache;
        _logger = logger;
    }

    public IActionResult Index() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Contact(ContactRequestVm vm, CancellationToken cancellationToken)
    {
        // Honeypot: bots will fill hidden fields
        if (!string.IsNullOrWhiteSpace(vm.Website))
        {
            TempData["ContactSuccess"] = "Thanks! Your message has been sent.";
            return Redirect("/#contact");
        }

        if (!ModelState.IsValid)
        {
            TempData["ContactError"] = "Please fill in all fields correctly.";
            return Redirect("/#contact");
        }

        var recipient = _configuration["Contact:RecipientEmail"];
        if (string.IsNullOrWhiteSpace(recipient))
        {
            TempData["ContactError"] = "Contact is not configured yet.";
            return Redirect("/#contact");
        }

        // Rate limit
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var cacheKey = $"contact:rl:{ip}";
        var count = _cache.Get<int?>(cacheKey) ?? 0;
        if (count >= RateLimitMax)
        {
            TempData["ContactError"] = "Too many requests. Please try again in a few minutes.";
            return Redirect("/#contact");
        }
        _cache.Set(cacheKey, count + 1, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = RateLimitWindow
        });

        // Save to DB (admin panel can read)
        var msg = new ContactMessage
        {
            FullName = vm.FullName.Trim(),
            Email = vm.Email.Trim(),
            Message = vm.Message.Trim(),
            IpAddress = ip,
            UserAgent = Request.Headers.UserAgent.ToString(),
            IsRead = false
        };

        _db.ContactMessages.Add(msg);
        await _db.SaveChangesAsync(cancellationToken);

        // Email template
        var subject = $"Nexora Contact — {vm.FullName}";
        var safeName = WebUtility.HtmlEncode(vm.FullName);
        var safeEmail = WebUtility.HtmlEncode(vm.Email);
        var safeMessage = WebUtility.HtmlEncode(vm.Message);

        var body = $@"
<div style='font-family: Inter, -apple-system, BlinkMacSystemFont, Segoe UI, Roboto, Arial, sans-serif; line-height:1.5; color:#0b1220;'>
  <div style='max-width:640px;margin:0 auto;border:1px solid #e5e7eb;border-radius:14px;overflow:hidden;'>
    <div style='background:#3056D3;padding:18px 22px;color:white;'>
      <div style='font-size:18px;font-weight:800;'>Nexora</div>
      <div style='opacity:.9;'>New contact message</div>
    </div>
    <div style='padding:22px;'>
      <p style='margin:0 0 10px;'><b>Name:</b> {safeName}</p>
      <p style='margin:0 0 10px;'><b>Email:</b> {safeEmail}</p>
      <p style='margin:14px 0 8px;'><b>Message:</b></p>
      <div style='white-space:pre-wrap;background:#f9fafb;border:1px solid #e5e7eb;border-radius:10px;padding:12px 14px;font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, Liberation Mono, Courier New, monospace;'>
{safeMessage}
      </div>
      <p style='margin:14px 0 0;color:#6b7280;font-size:12px;'>IP: {WebUtility.HtmlEncode(ip)} | UA: {WebUtility.HtmlEncode(Request.Headers.UserAgent.ToString())}</p>
    </div>
  </div>
</div>";

        try
        {
            await _emailSender.SendAsync(recipient, subject, body, cancellationToken);
            TempData["ContactSuccess"] = "Thanks! Your message has been sent.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send contact email. MessageId={MessageId}", msg.Id);
            TempData["ContactError"] = "Saved your message, but couldn't send email right now. Please try later.";
        }

        return Redirect("/#contact");
    }
}
