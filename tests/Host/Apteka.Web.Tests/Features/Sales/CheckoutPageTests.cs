using Apteka.Catalog.Contracts;
using Apteka.Inventory.Contracts;
using Apteka.Sales.Application.Checkout;
using Apteka.Sales.Application.Common;
using Apteka.Sales.Domain.Reversals;
using Apteka.Sales.Domain.Sales;
using Apteka.SharedKernel;
using Apteka.Web.Features.Sales.Checkout;
using Apteka.Web.Tests.Features.Purchasing;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace Apteka.Web.Tests.Features.Sales;

public class CheckoutPageTests : BunitContext
{
    private const string OtcBarcode = "2000000000015";
    private const string RxBarcode = "2000000000107";

    private readonly FakeCatalogLookup _catalog = new();
    private readonly FakeSales _sales = new();

    public CheckoutPageTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        AddProduct("Парацетамол 500 мг", CatalogDispensingMode.OverTheCounter, OtcBarcode);
        AddProduct("Амоксицилин 500 мг", CatalogDispensingMode.Prescription, RxBarcode);

        var stock = new OneBatchStock();
        Services.AddSingleton<ICatalogLookup>(_catalog);
        Services.AddSingleton<IStockQueries>(stock);
        Services.AddSingleton<IStockIssuing>(stock);
        Services.AddSingleton<ISaleRepository>(_sales);
        Services.AddSingleton<IUnitOfWork>(new PassThroughUnitOfWork());
        Services.AddSingleton<ICurrentUser>(new FakeUser());
        Services.AddSingleton(TimeProvider.System);
        Services.AddScoped<FindTillProductHandler>();
        Services.AddScoped<CompleteSaleHandler>();
    }

    private void AddProduct(string name, CatalogDispensingMode mode, string barcode)
    {
        var box = Guid.CreateVersion7();
        var product = new CatalogProductInfo(Guid.CreateVersion7(), name, 20m, mode, true, box,
            [new CatalogUnit(Guid.CreateVersion7(), "таблетка", 1), new CatalogUnit(box, "кутия", 20)]);
        _catalog.Products.Add(product);
        _catalog.Barcodes[barcode] = (product.ProductId, box);
    }

    private static void Type(IRenderedComponent<CheckoutPage> page, string text)
    {
        page.Find("#till-input").Input(text);
        page.Find("#till-input").KeyDown("Enter");
    }

    private static void Press(IRenderedComponent<CheckoutPage> page, string key) => page.Find("#till-input").KeyDown(key);

    [Fact]
    public void Enter_adds_a_line_the_same_barcode_adds_one_more_and_F2_sets_the_quantity()
    {
        var page = Render<CheckoutPage>();

        Type(page, OtcBarcode);
        Type(page, OtcBarcode);
        Assert.Equal("2", page.Find(".till-lines tbody tr td:nth-child(4)").TextContent);

        Press(page, "F2");
        Assert.Contains("Количество", page.Find(".till-prompt").TextContent);
        Type(page, "5");

        Assert.Single(page.FindAll(".till-lines tbody tr"));
        Assert.Equal("5", page.Find(".till-lines tbody tr td:nth-child(4)").TextContent);
        Assert.Contains("30,00", page.Find(".till-total").TextContent);
    }

    [Fact]
    public void Esc_twice_cancels_the_whole_sale()
    {
        var page = Render<CheckoutPage>();
        Type(page, OtcBarcode);

        Press(page, "Escape");
        Assert.Contains("Отказ на цялата продажба", page.Find(".till-prompt").TextContent);
        Press(page, "Escape");

        Assert.Empty(page.FindAll(".till-lines tbody tr"));
        Assert.Empty(_sales.Saved);
    }

    [Fact]
    public void Prescription_product_is_added_only_after_Enter_confirms_the_prescription()
    {
        var page = Render<CheckoutPage>();

        Type(page, RxBarcode);
        Assert.Contains("по лекарско предписание", page.Find(".till-prompt").TextContent);
        Assert.Empty(page.FindAll(".till-lines tbody tr"));

        Press(page, "Escape");
        Assert.Empty(page.FindAll(".till-lines tbody tr"));

        Type(page, RxBarcode);
        Type(page, string.Empty);
        Assert.Contains("Rx", page.Find(".till-lines tbody tr").TextContent);
    }

    [Fact]
    public void F9_then_Enter_with_cash_completes_the_sale_and_shows_the_change()
    {
        var page = Render<CheckoutPage>();
        Type(page, OtcBarcode);

        Press(page, "F9");
        Assert.Contains("Плащане", page.Find(".till-prompt").TextContent);
        Type(page, "10");

        var sale = Assert.Single(_sales.Saved);
        Assert.Equal(PaymentMethod.Cash, sale.PaymentMethod);
        Assert.Contains("ресто 4,00", page.Find(".alert.success").TextContent);
        Assert.Empty(page.FindAll(".till-lines tbody tr"));
    }

    [Fact]
    public void F9_twice_pays_by_card()
    {
        var page = Render<CheckoutPage>();
        Type(page, OtcBarcode);

        Press(page, "F9");
        Press(page, "F9");

        Assert.Equal(PaymentMethod.Card, Assert.Single(_sales.Saved).PaymentMethod);
    }

    /// <summary>One batch of every product: 100 tablets at 6,00 € a box of 20.</summary>
    private sealed class OneBatchStock : IStockQueries, IStockIssuing
    {
        private static BatchStock Batch() => new(Guid.CreateVersion7(), "L1", DateOnly.FromDateTime(DateTime.Today).AddYears(1), 100, 0.2m, 0.3m, 20, 6m, DateTimeOffset.UtcNow);

        public Task<IReadOnlyList<ProductStock>> GetProductStockAsync(IReadOnlyCollection<Guid>? productIds, DateOnly? expiringOnOrBefore, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ProductStock>>([]);

        public Task<IReadOnlyList<BatchStock>> GetBatchesAsync(Guid productId, DateOnly? expiringOnOrBefore, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<BatchStock>>([Batch()]);

        public Task<Result<IReadOnlyList<IssuedLine>>> IssueAsync(StockIssue issue, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Success<IReadOnlyList<IssuedLine>>(issue.Lines
                .Select(l => new IssuedLine(l.LineId, l.ProductId, [new IssuedBatch(Guid.CreateVersion7(), "L1", DateOnly.MaxValue, l.BaseQuantity, 0.3m, 20, 6m)]))
                .ToList()));

        public Task<Result> ReturnAsync(StockReturn stockReturn, CancellationToken cancellationToken = default) => Task.FromResult(Result.Success());
    }

    private sealed class FakeSales : ISaleRepository
    {
        public List<Sale> Saved { get; } = [];

        public Task<Sale?> GetAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(Saved.SingleOrDefault(s => s.Id == id));

        public Task<IReadOnlyList<Sale>> ListAsync(DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Sale>>(Saved);

        public void Add(Sale sale) => Saved.Add(sale);

        public void Add(Reversal reversal)
        {
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
