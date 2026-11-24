using Apteka.Catalog.Contracts;
using Apteka.Inventory.Contracts;
using Apteka.Web.Features.Inventory.Stock;
using Apteka.Web.Tests.Features.Purchasing;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace Apteka.Web.Tests.Features.Inventory;

public class StockPageTests : BunitContext
{
    private readonly FakeCatalogLookup _catalog = new();
    private readonly FakeStockQueries _stock = new();

    public StockPageTests()
    {
        var product = new CatalogProductInfo(
            Guid.CreateVersion7(), "Ибупрофен 400 мг", 20m, CatalogDispensingMode.OverTheCounter, true, null,
            [new CatalogUnit(Guid.CreateVersion7(), "таблетка", 1), new CatalogUnit(Guid.CreateVersion7(), "кутия", 30)]);
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

    private sealed class FakeStockQueries : IStockQueries
    {
        public List<ProductStock> Stock { get; } = [];

        public DateOnly? LastExpiringFilter { get; private set; }

        public Task<IReadOnlyList<ProductStock>> GetProductStockAsync(IReadOnlyCollection<Guid>? productIds, DateOnly? expiringOnOrBefore, CancellationToken cancellationToken = default)
        {
            LastExpiringFilter = expiringOnOrBefore;
            return Task.FromResult<IReadOnlyList<ProductStock>>(Stock);
        }

        public Task<LastReceipt?> GetLastReceiptAsync(Guid productId, CancellationToken cancellationToken = default) =>
            Task.FromResult<LastReceipt?>(null);

        public Task<IReadOnlyList<BatchStock>> GetBatchesAsync(Guid productId, DateOnly? expiringOnOrBefore, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<BatchStock>>([]);
    }
}
