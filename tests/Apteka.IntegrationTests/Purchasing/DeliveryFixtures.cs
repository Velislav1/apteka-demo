using Apteka.Catalog.Application.Products;
using Apteka.IntegrationTests.Catalog;
using Apteka.IntegrationTests.Infrastructure;
using Apteka.Purchasing.Application.Deliveries;
using Apteka.Purchasing.Application.Suppliers;
using Apteka.Purchasing.Domain.Suppliers;
using Microsoft.Extensions.DependencyInjection;

namespace Apteka.IntegrationTests.Purchasing;

/// <summary>Creates the catalog product, supplier and draft a delivery test needs.</summary>
internal static class DeliveryFixtures
{
    private static int _eikCounter = Random.Shared.Next(10_000_000, 90_000_000);

    public static CancellationToken Ct => TestContext.Current.CancellationToken;

    public static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);

    public static async Task<Guid> SupplierAsync(IServiceProvider provider)
    {
        await using var scope = provider.CreateAsyncScope();
        var eik = Eik.Complete9(Interlocked.Increment(ref _eikCounter).ToString("D8", System.Globalization.CultureInfo.InvariantCulture));
        var created = await scope.ServiceProvider.GetRequiredService<SuppliersHandler>()
            .CreateAsync(new SupplierInput(CatalogInputs.UniqueName("Доставчик"), eik, 30), Ct);
        Assert.True(created.IsSuccess, created.IsFailure ? created.Error.Message : null);
        return created.Value;
    }

    /// <summary>A product with units кутия (20), блистер (10), таблетка (1); returns its id and the box unit id.</summary>
    public static async Task<(Guid ProductId, Guid BoxId, Guid TabletId)> ProductAsync(IServiceProvider provider)
    {
        var productId = await ProductPersistenceTests.CreateAsync(provider, CatalogInputs.UniqueName("Доставена"), TestBarcodes.Next());
        await using var scope = provider.CreateAsyncScope();
        var view = (await scope.ServiceProvider.GetRequiredService<GetProductHandler>().HandleAsync(productId, Ct))!;
        return (productId, view.Units.Single(u => u.Name == "кутия").Id, view.Units.Single(u => u.Name == "таблетка").Id);
    }

    public static DeliveryLineInput Line(Guid productId, Guid unitId, int quantity, string batch, int expiresInDays = 400) =>
        new(null, productId, unitId, quantity, 4.00m, 25m, 6.00m, batch, Today.AddDays(expiresInDays));

    public static async Task<Guid> DraftAsync(IServiceProvider provider, Guid supplierId, params DeliveryLineInput[] lines)
    {
        await using var scope = provider.CreateAsyncScope();
        var saved = await scope.ServiceProvider.GetRequiredService<SaveDeliveryDraftHandler>()
            .HandleAsync(null, new DeliveryInput(supplierId, CatalogInputs.UniqueName("Ф")[..30], Today, lines), Ct);
        Assert.True(saved.IsSuccess, saved.IsFailure ? saved.Error.Message : null);
        return saved.Value;
    }
}
