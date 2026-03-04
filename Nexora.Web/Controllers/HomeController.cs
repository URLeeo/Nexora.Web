using Microsoft.AspNetCore.Mvc;
using Nexora.Web.Models.Marketing;
using Nexora.Web.Services.Email;
using System.Net;

namespace Nexora.Web.Controllers;

public class HomeController : Controller
{
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _configuration;

    public HomeController(IEmailSender emailSender, IConfiguration configuration)
    {
        _emailSender = emailSender;
        _configuration = configuration;
    }

    public IActionResult Index() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Contact(ContactRequestVm vm, CancellationToken cancellationToken)
    {
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

        var subject = $"Nexora Contact — {vm.FullName}";

        var name = WebUtility.HtmlEncode(vm.FullName);
        var email = WebUtility.HtmlEncode(vm.Email);
        var message = WebUtility.HtmlEncode(vm.Message);

        var body = $@"
<h2>New message from Nexora landing page</h2>
<p><b>Name:</b> {name}</p>
<p><b>Email:</b> {email}</p>
<p><b>Message:</b></p>
<pre style='white-space:pre-wrap;font-family:ui-monospace,SFMono-Regular,Menlo,Monaco,Consolas,Liberation Mono,Courier New,monospace;'>
{message}
</pre>";

        try
        {
            await _emailSender.SendAsync(recipient, subject, body, cancellationToken);
            TempData["ContactSuccess"] = "Thanks! Your message has been sent.";
        }
        catch (Exception)
        {
            TempData["ContactError"] = "Sorry, we couldn't send your message right now.";
        }

        return Redirect("/#contact");
    }
}