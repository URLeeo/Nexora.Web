using System.Security.Claims;

namespace Nexora.Web.Extensions;

public static class ClaimsExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
        => Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public static Guid GetOrganizationId(this ClaimsPrincipal user)
        => Guid.Parse(user.FindFirstValue("orgId")!);
}