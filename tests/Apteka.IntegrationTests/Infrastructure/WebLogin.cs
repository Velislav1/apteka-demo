using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Apteka.IntegrationTests.Infrastructure;

/// <summary>Starts the real web app against the test database and logs in through the login form, like a browser.</summary>
internal static partial class WebLogin
{
    public const string ManagerPassword = "Manager2026test";
    public const string PharmacistPassword = "Pharma2026test";

    public static WebApplicationFactory<Program> Factory(string connectionString)
    {
        // WebApplication.CreateBuilder reads environment variables, which is the reliable way to feed Program.cs.
        Environment.SetEnvironmentVariable("ConnectionStrings__Apteka", connectionString);
        Environment.SetEnvironmentVariable("Identity__Seed__ManagerPassword", ManagerPassword);
        Environment.SetEnvironmentVariable("Identity__Seed__PharmacistPassword", PharmacistPassword);
        return new WebApplicationFactory<Program>();
    }

    /// <returns>The page the login redirected to.</returns>
    public static async Task<HttpResponseMessage> LoginAsync(HttpClient client, string userName, string password, string returnUrl = "/")
    {
        var ct = TestContext.Current.CancellationToken;
        var form = await client.GetStringAsync("/login", ct);
        var token = TokenPattern().Match(form).Groups[1].Value;

        return await client.PostAsync(
            "/account/login",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["userName"] = userName,
                ["password"] = password,
                ["returnUrl"] = returnUrl,
            }),
            ct);
    }

    [GeneratedRegex("name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"")]
    private static partial Regex TokenPattern();
}
