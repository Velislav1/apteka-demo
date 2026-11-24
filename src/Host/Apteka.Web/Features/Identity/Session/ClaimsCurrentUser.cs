using System.Security.Claims;
using Apteka.Infrastructure.Common.Users;
using Apteka.SharedKernel;

namespace Apteka.Web.Features.Identity.Session;

/// <summary>The logged-in user, read from the login cookie's claims.</summary>
public sealed class ClaimsCurrentUser(ClaimsPrincipal principal) : ICurrentUser
{
    public Guid? UserId => Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public string UserName => principal.Identity?.Name ?? "anonymous";

    public bool IsInRole(string role) => principal.IsInRole(role);

    public static ICurrentUser From(ClaimsPrincipal? principal) =>
        principal?.Identity?.IsAuthenticated == true ? new ClaimsCurrentUser(principal) : AnonymousUser.Instance;
}
