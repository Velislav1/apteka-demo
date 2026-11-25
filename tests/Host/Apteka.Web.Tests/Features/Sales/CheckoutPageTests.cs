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
    private readonly FakeCustomers _customers = new();

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
        Services.AddSingleton<Apteka.Sales.Application.Customers.ICustomerRepository>(_customers);
        Services.AddScoped<Apteka.Sales.Application.Customers.CustomersHandler>();
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

    private static string Quantity(IRenderedComponent<CheckoutPage> page, int row = 0) =>
        page.FindAll(".till-lines tbody tr")[row].QuerySelectorAll("td")[2].TextContent;

    [Fact]
    public void Enter_adds_a_line_the_same_barcode_adds_one_more_and_a_star_sets_the_quantity()
    {
        var page = Render<CheckoutPage>();

        Type(page, OtcBarcode);
        Type(page, OtcBarcode);
        Assert.Equal("2", Quantity(page));

        Type(page, "5*");
        Assert.Equal("5", Quantity(page));

        Type(page, "3*" + OtcBarcode);
        Assert.Single(page.FindAll(".till-lines tbody tr"));
        Assert.Equal("8", Quantity(page));
        Assert.Contains("48,00", page.Find(".till-total").TextContent);
    }

    [Fact]
    public void F4_gives_a_discount_F12_changes_the_unit_and_F8_removes_the_current_line()
    {
        var page = Render<CheckoutPage>();
        Type(page, OtcBarcode);
        Type(page, RxBarcode);
        Type(page, string.Empty); // confirm the prescription

        Press(page, "ArrowUp");
        Press(page, "F4");
        Type(page, "10");
        Assert.Contains("Ст-ст без ТО", page.Find(".till-sum").TextContent);
        Assert.Contains("0,60", page.Find(".till-sum").TextContent); // 10 % of 6,00

        Press(page, "F12");
        Assert.Equal("таблетка", page.FindAll(".till-lines tbody tr")[0].QuerySelectorAll("td")[3].TextContent);

        Press(page, "F8");
        var rows = page.FindAll(".till-lines tbody tr");
        Assert.Single(rows);
        Assert.Contains("Амоксицилин", rows[0].TextContent);
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
    public void F10_then_Enter_with_cash_completes_the_sale_and_shows_the_change()
    {
        var page = Render<CheckoutPage>();
        Type(page, OtcBarcode);

        Press(page, "F10");
        Assert.Contains("Плащане", page.Find(".till-prompt").TextContent);
        Type(page, "10");

        var sale = Assert.Single(_sales.Saved);
        Assert.Equal(PaymentMethod.Cash, sale.PaymentMethod);
        Assert.Contains("ресто 4,00", page.Find(".alert.success").TextContent);
        Assert.Empty(page.FindAll(".till-lines tbody tr"));
    }

    [Fact]
    public void F10_twice_pays_by_card()
    {
        var page = Render<CheckoutPage>();
        Type(page, OtcBarcode);

        Press(page, "F10");
        Press(page, "F10");

        Assert.Equal(PaymentMethod.Card, Assert.Single(_sales.Saved).PaymentMethod);
    }

    [Fact]
    public void Percent_command_gives_an_overall_discount_to_all_lines_and_to_lines_added_later()
    {
        var page = Render<CheckoutPage>();
        Type(page, OtcBarcode);
        Type(page, "%10");
        Type(page, RxBarcode);
        Type(page, string.Empty); // confirm the prescription
        Assert.Contains("обща 10 %", page.Find(".till-sum").TextContent);

        Type(page, "%x");
        Assert.Contains("Обща отстъпка", page.Markup);

        page.Find("#till-input").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Enter", CtrlKey = true });

        var sale = Assert.Single(_sales.Saved);
        Assert.Equal([10m, 10m], sale.Lines.Select(l => l.DiscountPercent));
        Assert.DoesNotContain("обща", page.Find(".till-sum").TextContent);
    }

    [Fact]
    public void F2_with_an_egn_attaches_the_customer_and_the_sale_keeps_it()
    {
        var customer = Apteka.Sales.Domain.Customers.Customer.Create(new(
            "Демо Клиент", Apteka.Sales.Domain.Customers.Egn.Complete("750101123"), null, null)).Value;
        _customers.Items.Add(customer);
        _customers.Items.Add(Apteka.Sales.Domain.Customers.Customer.Create(new("Демо Клиентка", null, null, null)).Value);
        var page = Render<CheckoutPage>();
        Type(page, OtcBarcode);

        Press(page, "F2");
        Type(page, "Нямаго");
        Assert.Contains("Няма клиент", page.Markup);
        Type(page, "Демо");
        Assert.Equal(2, page.FindAll(".till-matches li").Count);
        Press(page, "Escape");
        Press(page, "F2");
        Type(page, customer.Egn!);
        Assert.Contains("Демо Клиент", page.Find(".till-customer").TextContent);

        page.Find("#till-input").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Enter", CtrlKey = true });

        Assert.Equal(customer.Id, Assert.Single(_sales.Saved).CustomerId);
        Assert.Empty(page.FindAll(".till-customer"));
    }

    [Fact]
    public void Ctrl_Enter_completes_the_sale_in_cash_with_the_exact_amount()
    {
        var page = Render<CheckoutPage>();
        Type(page, OtcBarcode);

        page.Find("#till-input").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Enter", CtrlKey = true });

        var sale = Assert.Single(_sales.Saved);
        Assert.Equal(PaymentMethod.Cash, sale.PaymentMethod);
        Assert.Equal(0m, sale.Change);
    }

    /// <summary>One batch of every product: 100 tablets at 6,00 € a box of 20.</summary>
    private sealed class OneBatchStock : IStockQueries, IStockIssuing
    {
        private static BatchStock Batch() => new(Guid.CreateVersion7(), "L1", DateOnly.FromDateTime(DateTime.Today).AddYears(1), 100, 0.2m, 0.3m, 20, 6m, DateTimeOffset.UtcNow);

        public Task<IReadOnlyList<ProductStock>> GetProductStockAsync(IReadOnlyCollection<Guid>? productIds, DateOnly? expiringOnOrBefore, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ProductStock>>([]);

        public Task<IReadOnlyList<ProductBatchStock>> GetAllBatchesAsync(IReadOnlyCollection<Guid>? productIds, DateOnly? expiringOnOrBefore, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ProductBatchStock>>([]);

        public Task<ProductCard> GetProductCardAsync(Guid productId, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ProductCard(0, []));

        public Task<IReadOnlyDictionary<Guid, int>> GetSoldQuantitiesAsync(IReadOnlyCollection<Guid>? productIds, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, int>>(new Dictionary<Guid, int>());

        public Task<IReadOnlyDictionary<Guid, decimal>> GetPurchasePricesAsync(IReadOnlyCollection<Guid> batchIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, decimal>>(batchIds.ToDictionary(id => id, _ => 0.2m));

        public Task<LastReceipt?> GetLastReceiptAsync(Guid productId, CancellationToken cancellationToken = default) =>
            Task.FromResult<LastReceipt?>(null);

        public Task<IReadOnlyList<BatchStock>> GetBatchesAsync(Guid productId, DateOnly? expiringOnOrBefore, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<BatchStock>>([Batch()]);

        public Task<Result<IReadOnlyList<IssuedLine>>> IssueAsync(StockIssue issue, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Success<IReadOnlyList<IssuedLine>>(issue.Lines
                .Select(l => new IssuedLine(l.LineId, l.ProductId, [new IssuedBatch(Guid.CreateVersion7(), "L1", DateOnly.MaxValue, l.BaseQuantity, 0.3m, 20, 6m)]))
                .ToList()));

        public Task<Result> ReturnAsync(StockReturn stockReturn, CancellationToken cancellationToken = default) => Task.FromResult(Result.Success());
    }
}
