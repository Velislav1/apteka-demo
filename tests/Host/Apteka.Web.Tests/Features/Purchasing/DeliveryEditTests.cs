using Apteka.Catalog.Contracts;
using Apteka.Inventory.Contracts;
using Apteka.Purchasing.Application.Deliveries;
using Apteka.Purchasing.Application.Suppliers;
using Apteka.Purchasing.Domain.Deliveries;
using Apteka.Purchasing.Domain.Suppliers;
using Apteka.SharedKernel;
using Apteka.Web.Features.Purchasing.Deliveries;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace Apteka.Web.Tests.Features.Purchasing;

public class DeliveryEditTests : BunitContext
{
    private readonly FakeCatalogLookup _catalog = new();
    private readonly FakeDeliveryRepository _deliveries = new();
    private readonly FakeSupplierRepository _suppliers = new();
    private readonly FakeStockReceiving _stock = new();
    private readonly Guid _box = Guid.CreateVersion7();
    private readonly Supplier _supplier = Supplier.Create("Фарма Демо ООД", Eik.Complete9("20000001"), 30).Value;

    public DeliveryEditTests()
    {
        var product = new CatalogProductInfo(
            Guid.CreateVersion7(), "Парацетамол 500 мг", 20m, CatalogDispensingMode.OverTheCounter, true, null,
            [new CatalogUnit(Guid.CreateVersion7(), "таблетка", 1), new CatalogUnit(_box, "кутия", 20)]);
        _catalog.Products.Add(product);
        _catalog.Barcodes["2000000000015"] = (product.ProductId, _box);
        _suppliers.Items.Add(_supplier);

        Services.AddSingleton<ICatalogLookup>(_catalog);
        Services.AddSingleton<IDeliveryRepository>(_deliveries);
        Services.AddSingleton<ISupplierRepository>(_suppliers);
        Services.AddSingleton<IStockReceiving>(_stock);
        Services.AddSingleton<IUnitOfWork>(new PassThroughUnitOfWork());
        Services.AddSingleton<ICurrentUser>(new FakeUser());
        Services.AddSingleton(TimeProvider.System);
        Services.AddScoped<SuppliersHandler>();
        Services.AddScoped<SaveDeliveryDraftHandler>();
        Services.AddScoped<PostDeliveryHandler>();
        Services.AddScoped<GetDeliveryHandler>();
    }

    [Fact]
    public void Scanned_barcode_adds_a_line_in_the_scanned_unit_and_markup_suggests_the_sale_price()
    {
        var page = Render<DeliveryEdit>();

        page.Find("#productsearch").Input("2000000000015");
        page.Find("#productsearch").KeyDown("Enter");

        var row = page.Find("table.lines tbody tr");
        Assert.Contains("Парацетамол 500 мг", row.TextContent);
        Assert.Equal(_box.ToString(), row.QuerySelector("select")!.GetAttribute("value"));

        page.Find("input[aria-label='Покупна цена']").Change("2.40");

        // 2,40 + 25 % markup + 20 % VAT = 3,60
        Assert.Equal("3.60", page.Find("input[aria-label='Продажна цена']").GetAttribute("value"));
    }

    [Fact]
    public void Saving_a_draft_stores_it_and_posting_asks_for_confirmation_then_receives_the_stock()
    {
        var page = Render<DeliveryEdit>();
        page.Find("#supplier").Change(_supplier.Id.ToString());
        page.Find("#invoice").Change("0000001");
        page.Find("#productsearch").Input("парацетамол");
        page.Find("#productsearch").KeyDown("Enter");
        page.Find("input[aria-label='Количество']").Change("3");
        page.Find("input[aria-label='Партида']").Change("L1");
        page.Find("input[aria-label='Покупна цена']").Change("2.40");

        page.FindAll("button").Single(b => b.TextContent == "Публикувай").Click();
        Assert.Empty(_stock.Receipts);

        page.FindAll("button").Single(b => b.TextContent == "Потвърди публикуването").Click();

        var delivery = Assert.Single(_deliveries.Items);
        Assert.Equal(DeliveryStatus.Posted, delivery.Status);
        var line = Assert.Single(Assert.Single(_stock.Receipts).Lines);
        Assert.Equal(60, line.BaseQuantity);
        Assert.Equal(0.12m, line.PurchasePricePerBaseUnit);
    }

    [Fact]
    public void Validation_error_is_shown_in_Bulgarian_and_nothing_is_saved()
    {
        var page = Render<DeliveryEdit>();
        page.Find("#supplier").Change(_supplier.Id.ToString());

        page.FindAll("button").Single(b => b.TextContent == "Запиши").Click();

        Assert.Equal("Въведете номер на фактурата.", page.Find(".alert.error").TextContent);
        Assert.Empty(_deliveries.Items);
    }
}
