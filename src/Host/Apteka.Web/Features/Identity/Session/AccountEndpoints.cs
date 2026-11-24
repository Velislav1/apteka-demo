using System.Security.Claims;
using Apteka.Identity.Application.Login;
using Apteka.Infrastructure.Common.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace Apteka.Web.Features.Identity.Session;

/// <summary>Login and logout are plain HTTP form posts: only an HTTP response can set or clear the login cookie.</summary>
public static class AccountEndpoints
{
    public const string LoginPath = "/login";

    public static void UseCurrentUser(this WebApplication app) =>
        app.Use(async (context, next) =>
        {
            context.RequestServices.GetRequiredService<CurrentUserAccessor>().User = ClaimsCurrentUser.From(context.User);
            await next();
        });

    public static void MapAccountEndpoints(this WebApplication app)
    {
        app.MapPost("/account/login", async (
            [FromForm] string userName,
            [FromForm] string password,
            [FromForm] string? returnUrl,
            LoginHandler login,
            HttpContext http) =>
        {
            var result = await login.HandleAsync(userName, password, http.RequestAborted);
            if (result.IsFailure)
            {
                var error = result.Error.Code == "identity.locked_out" ? "locked" : "invalid";
                return Results.LocalRedirect($"{LoginPath}?error={error}&userName={Uri.EscapeDataString(userName)}&returnUrl={Uri.EscapeDataString(SafeReturnUrl(returnUrl))}");
            }

            var user = result.Value;
            var identity = new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.UserName),
                    new Claim("display_name", user.DisplayName),
                    new Claim(ClaimTypes.Role, user.RoleName),
                ],
                CookieAuthenticationDefaults.AuthenticationScheme);

            await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
            return Results.LocalRedirect(SafeReturnUrl(returnUrl));
        });

        app.MapPost("/account/logout", async (HttpContext http) =>
        {
            await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.LocalRedirect(LoginPath);
        });
    }

    /// <summary>Only pages of this application (no open redirect to other sites).</summary>
    private static string SafeReturnUrl(string? returnUrl) =>
        !string.IsNullOrEmpty(returnUrl) && returnUrl.StartsWith('/') && !returnUrl.StartsWith("//", StringComparison.Ordinal) && !returnUrl.StartsWith("/\\", StringComparison.Ordinal)
            ? returnUrl
            : "/";

    public static string DisplayName(this ClaimsPrincipal user) => user.FindFirstValue("display_name") ?? user.Identity?.Name ?? string.Empty;
}
