using Apteka.Catalog.Contracts;
using Apteka.Inventory.Contracts;
using Apteka.Web.Features.Inventory.BelowMinimum;
using Apteka.Web.Features.Inventory.Expired;
using Apteka.Web.Features.Inventory.Stock;
using Apteka.Web.Tests.Features.Purchasing;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace Apteka.Web.Tests.Features.Inventory;

public class StockPageTests : BunitContext
{
    private readonly FakeCatalogLookup _catalog = new();
    private readonly FakeStockQueries _stock = new();
    private readonly CatalogGroup _group = new(Guid.CreateVersion7(), "Обезболяващи");
    private readonly CatalogProductInfo _product;

    public StockPageTests()
    {
        var product = _product = new CatalogProductInfo(
            Guid.CreateVersion7(), "Ибупрофен 400 мг", 20m, CatalogDispensingMode.OverTheCounter, true, null,
            [new CatalogUnit(Guid.CreateVersion7(), "таблетка", 1), new CatalogUnit(Guid.CreateVersion7(), "кутия", 30)],
            Code: 7, ProductGroupId: _group.Id, MinStock: 90, MaxStock: 150);
        _catalog.Groups.Add(_group);
        _catalog.Products.Add(product);
        _stock.Stock.Add(new ProductStock(product.ProductId, 65, DateOnly.FromDateTime(DateTime.Today).AddDays(-1), 2));

        Services.AddSingleton<ICatalogLookup>(_catalog);
        Services.AddSingleton<IStockQueries>(_stock);
        Services.AddSingleton(TimeProvider.System);
    }

    [Fact]
    public void Stock_is_shown_in_boxes_and_base_units_and_an_expired_date_is_marked()
    {
        var page = Render<StockPage>();

        var row = page.Find("tbody tr");
        Assert.Contains("2 × кутия + 5 × таблетка", row.TextContent);
        Assert.Contains("65 таблетка", row.TextContent);
        Assert.Contains("expired", row.QuerySelector("td.expiry")!.ClassName);
    }

    [Fact]
    public void Expiring_filter_asks_for_batches_expiring_within_90_days()
    {
        var page = Render<StockPage>();

        page.Find("#expiring").Change(true);

        // „Today“ is the Sofia date, which may differ by a day from the test machine's date.
        var expected = DateOnly.FromDateTime(DateTime.Today).AddDays(90);
        Assert.InRange(_stock.LastExpiringFilter!.Value.DayNumber, expected.DayNumber - 1, expected.DayNumber + 1);
    }

    [Fact]
    public void Group_filter_asks_only_for_the_groups_products()
    {
        var other = _product with { ProductId = Guid.CreateVersion7(), Name = "Друга", ProductGroupId = null };
        _catalog.Products.Add(other);
        var page = Render<StockPage>();

        page.Find("#group").Change(_group.Id.ToString());

        Assert.Equal([_product.ProductId], _stock.LastProductIds);
    }

    [Fact]
    public void Expired_report_lists_batches_by_the_date_with_their_value()
    {
        var expired = DateOnly.FromDateTime(DateTime.Today).AddDays(-3);
        _stock.Batches.Add(new ProductBatchStock(_product.ProductId, new BatchStock(Guid.CreateVersion7(), "EXP1", expired, 45, 0.2m, 0.3m, 30, 9m, DateTimeOffset.UtcNow)));

        var page = Render<ExpiredPage>();

        var row = page.Find("tbody tr");
        Assert.Contains("EXP1", row.TextContent);
        Assert.Contains("1 × кутия + 15 × таблетка", row.TextContent);
        Assert.Contains("13,50", row.TextContent); // 45 × 0,30
        Assert.Contains("expired", row.QuerySelector("td.expiry")!.ClassName);
        Assert.NotNull(_stock.LastExpiringFilter);
    }

    [Fact]
    public void Below_minimum_report_shows_products_under_their_minimum_and_how_much_to_order()
    {
        var page = Render<BelowMinimumPage>(); // 65 on hand, minimum 90, maximum 150

        var row = page.Find("tbody tr");
        Assert.Contains("Ибупрофен 400 мг", row.TextContent);
        Assert.Contains("2 × кутия + 25 × таблетка", row.TextContent); // 150 − 65 = 85

        _stock.Stock[0] = _stock.Stock[0] with { BaseQuantity = 100 };
        page.Find("#group").Change(string.Empty);
        Assert.Empty(page.FindAll("tbody tr"));
    }

    private sealed class FakeStockQueries : IStockQueries
    {
        public List<ProductStock> Stock { get; } = [];

        public List<ProductBatchStock> Batches { get; } = [];

        public DateOnly? LastExpiringFilter { get; private set; }

        public IReadOnlyCollection<Guid>? LastProductIds { get; private set; }

        public Task<IReadOnlyList<ProductStock>> GetProductStockAsync(IReadOnlyCollection<Guid>? productIds, DateOnly? expiringOnOrBefore, CancellationToken cancellationToken = default)
        {
            LastExpiringFilter = expiringOnOrBefore;
            LastProductIds = productIds;
            return Task.FromResult<IReadOnlyList<ProductStock>>(Stock.Where(s => productIds is null || productIds.Contains(s.ProductId)).ToList());
        }

        public Task<IReadOnlyList<ProductBatchStock>> GetAllBatchesAsync(IReadOnlyCollection<Guid>? productIds, DateOnly? expiringOnOrBefore, CancellationToken cancellationToken = default)
        {
            LastExpiringFilter = expiringOnOrBefore;
            return Task.FromResult<IReadOnlyList<ProductBatchStock>>(Batches.Where(b => expiringOnOrBefore is null || b.Batch.ExpiryDate <= expiringOnOrBefore).ToList());
        }

        public Task<ProductCard> GetProductCardAsync(Guid productId, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ProductCard(0, []));

        public Task<IReadOnlyList<TurnoverRow>> GetTurnoverAsync(IReadOnlyCollection<Guid>? productIds, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TurnoverRow>>([]);

        public Task<BatchInfo?> GetBatchAsync(Guid batchId, CancellationToken cancellationToken = default) => Task.FromResult<BatchInfo?>(null);

        public Task<IReadOnlyDictionary<Guid, int>> GetSoldQuantitiesAsync(IReadOnlyCollection<Guid>? productIds, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, int>>(new Dictionary<Guid, int>());

        public Task<IReadOnlyDictionary<Guid, decimal>> GetPurchasePricesAsync(IReadOnlyCollection<Guid> batchIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, decimal>>(batchIds.ToDictionary(id => id, _ => 0.2m));

        public Task<LastReceipt?> GetLastReceiptAsync(Guid productId, CancellationToken cancellationToken = default) =>
            Task.FromResult<LastReceipt?>(null);

        public Task<IReadOnlyList<BatchStock>> GetBatchesAsync(Guid productId, DateOnly? expiringOnOrBefore, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<BatchStock>>([]);
    }
}
