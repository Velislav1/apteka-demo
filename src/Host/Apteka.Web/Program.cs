using System.Text.Encodings.Web;
using System.Text.Unicode;
using Apteka.Infrastructure.Common;
using Apteka.Catalog.Infrastructure;
using Apteka.Purchasing.Infrastructure;
using Apteka.Inventory.Infrastructure;
using Apteka.Sales.Infrastructure;
using Apteka.Identity.Infrastructure;
using Apteka.Audit.Infrastructure;
using Apteka.Web.Components;

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
builder.Services.AddIdentityModule(connectionString);
builder.Services.AddAuditModule(connectionString);

var app = builder.Build();

app.UseRequestLocalization(options => options
    .SetDefaultCulture("bg-BG")
    .AddSupportedCultures("bg-BG")
    .AddSupportedUICultures("bg-BG"));

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();

public partial class Program;