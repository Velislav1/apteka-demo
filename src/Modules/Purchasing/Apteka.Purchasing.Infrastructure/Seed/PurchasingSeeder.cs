using Apteka.Catalog.Contracts;
using Apteka.Infrastructure.Common.Persistence;
using Apteka.Purchasing.Application.Deliveries;
using Apteka.Purchasing.Application.Suppliers;
using Apteka.Purchasing.Domain.Deliveries;
using Apteka.Purchasing.Domain.Suppliers;
using Apteka.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Apteka.Purchasing.Infrastructure.Seed;

/// <summary>
/// Made-up suppliers (rule 8: no real company data) and demo deliveries. The deliveries go through the real
/// use cases (save draft → post), so the demo stock in Inventory is created exactly as in daily work.
/// </summary>
internal sealed class PurchasingSeeder(
    PurchasingDbContext context,
    SuppliersHandler suppliers,
    SaveDeliveryDraftHandler saveDraft,
    PostDeliveryHandler post,
    ICatalogLookup catalog,
    TimeProvider clock) : IModuleDatabaseInitializer
{
    // Inventory (20) must exist before deliveries are posted.
    public int Order => 30;

    internal static readonly (string Name, string EikPrefix, int PaymentTermDays)[] Suppliers =
    [
        ("Фарма Дистрибуция Демо ООД", "20000001", 30),
        ("Медика Склад Демо ЕАД", "20000002", 45),
        ("Здраве Търговия Демо ООД", "20000003", 60),
    ];

    public Task MigrateAsync(CancellationToken cancellationToken) => context.Database.MigrateAsync(cancellationToken);

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (!await context.Suppliers.AnyAsync(cancellationToken))
        {
            foreach (var (name, prefix, days) in Suppliers)
            {
                Require(await suppliers.CreateAsync(new SupplierInput(name, Eik.Complete9(prefix), days), cancellationToken));
            }
        }

        if (await context.Deliveries.AnyAsync(cancellationToken))
        {
            return;
        }

        var supplierIds = await context.Suppliers.OrderBy(s => s.Name).Select(s => s.Id).ToListAsync(cancellationToken);
        var products = await catalog.SearchProductsAsync(string.Empty, 60, cancellationToken);
        if (products.Count == 0 || supplierIds.Count == 0)
        {
            return;
        }

        var today = BulgarianTime.Today(clock.GetUtcNow());

        // Two posted deliveries (the demo stock) and one draft.
        var first = Lines(products.Take(20), today, seed: 1);
        var second = Lines(products.Skip(20).Take(20), today, seed: 2);
        var draft = Lines(products.Skip(40).Take(3), today, seed: 3);

        var firstId = Require(await saveDraft.HandleAsync(null, new DeliveryInput(supplierIds[0], "1000000101", today.AddDays(-7), first), cancellationToken));
        Require(await post.HandleAsync(firstId, cancellationToken));

        var secondId = Require(await saveDraft.HandleAsync(null, new DeliveryInput(supplierIds[1 % supplierIds.Count], "2000000202", today.AddDays(-2), second), cancellationToken));
        Require(await post.HandleAsync(secondId, cancellationToken));

        Require(await saveDraft.HandleAsync(null, new DeliveryInput(supplierIds[2 % supplierIds.Count], "3000000303", today, draft), cancellationToken));
    }

    /// <summary>One line per product, in its largest unit, with deterministic made-up prices, batches and expiry dates.</summary>
    private static List<DeliveryLineInput> Lines(IEnumerable<CatalogProductInfo> products, DateOnly today, int seed) =>
        products.Select((product, i) =>
        {
            var unit = product.Units.MaxBy(u => u.Factor)!;
            var purchase = decimal.Round(1.80m + (((i * 37) + (seed * 11)) % 90 / 10m), 2);
            const decimal markup = 25m;

            // A few batches expire within 90 days, so the „изтича до 90 дни“ filter has something to show.
            var expiry = i < 3 ? today.AddDays(30 + (i * 25)) : today.AddMonths(6 + ((i + seed) % 24));

            return new DeliveryLineInput(
                null,
                product.ProductId,
                unit.UnitId,
                Quantity: 2 + ((i + seed) % 9),
                purchase,
                markup,
                SalePriceCalculator.FromMarkup(purchase, markup, product.VatRate),
                $"L{seed}{i + 1:D3}{today:yy}",
                expiry);
        }).ToList();

    private static T Require<T>(Result<T> result) =>
        result.IsSuccess ? result.Value : throw new InvalidOperationException($"Demo data could not be created: {result.Error.Message}");

    private static void Require(Result result)
    {
        if (result.IsFailure)
        {
            throw new InvalidOperationException($"Demo data could not be created: {result.Error.Message}");
        }
    }
}
