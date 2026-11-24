using System.Text.Encodings.Web;
using System.Text.Unicode;
using Apteka.Infrastructure.Common;
using Apteka.Catalog.Infrastructure;
using Apteka.Purchasing.Infrastructure;
using Apteka.Inventory.Infrastructure;
using Apteka.Sales.Infrastructure;
using Apteka.Identity.Infrastructure;
using Apteka.Audit.Infrastructure;
using Apteka.Identity.Infrastructure.Seed;
using Apteka.Web.Components;
using Apteka.Web.Features.Identity.Session;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Server.Circuits;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Apteka")
    ?? throw new InvalidOperationException(
        "Connection string 'Apteka' is missing. Copy appsettings.Development.example.json to appsettings.Development.json.");

builder.Services.AddRazorComponents().AddInteractiveServerComponents();

// Keep Cyrillic readable in the generated HTML instead of numeric entities.
builder.Services.AddWebEncoders(options =>
    options.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic));

// The host is the only place where modules are registered.
builder.Services.AddAptekaCommon(connectionString);
builder.Services.AddCatalogModule(connectionString);
builder.Services.AddPurchasingModule(connectionString);
builder.Services.AddInventoryModule(connectionString);
builder.Services.AddSalesModule(connectionString);
builder.Services.AddIdentityModule(connectionString, new IdentitySeedPasswords(
    builder.Configuration["Identity:Seed:ManagerPassword"],
    builder.Configuration["Identity:Seed:PharmacistPassword"]));

// Login with a cookie; every screen needs it (see Features/_Imports.razor).
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = AccountEndpoints.LoginPath;
        options.AccessDeniedPath = "/access-denied";
        options.ExpireTimeSpan = TimeSpan.FromHours(12);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<CircuitHandler, UserCircuitHandler>();
builder.Services.AddAuditModule(connectionString);

var app = builder.Build();

// Development only: bring every module's schema up to date and add the demo data.
// Production databases are migrated explicitly (dotnet ef / migration bundle), never on startup.
if (app.Environment.IsDevelopment())
{
    await app.Services.InitializeModuleDatabasesAsync(seed: true);
}

app.UseRequestLocalization(options => options
    .SetDefaultCulture("bg-BG")
    .AddSupportedCultures("bg-BG")
    .AddSupportedUICultures("bg-BG"));

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseCurrentUser();
app.UseAntiforgery();

app.MapAccountEndpoints();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

await app.RunAsync();

public partial class Program;