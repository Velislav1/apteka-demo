using Apteka.Catalog.Contracts;
using Apteka.Inventory.Contracts;
using Apteka.Purchasing.Application.Deliveries;
using Apteka.Purchasing.Application.Suppliers;
using Apteka.Purchasing.Domain.Deliveries;
using Apteka.Purchasing.Domain.Suppliers;
using Apteka.SharedKernel;
using Apteka.Web.Features.Purchasing.Deliveries;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace Apteka.Web.Tests.Features.Purchasing;

public class DeliveryEditTests : BunitContext
{
    private readonly FakeCatalogLookup _catalog = new();
    private readonly FakeDeliveryRepository _deliveries = new();
    private readonly FakeSupplierRepository _suppliers = new();
    private readonly FakeStockReceiving _stock = new();
    private readonly FakeCorrectionRepository _corrections = new();
    private readonly Guid _box = Guid.CreateVersion7();
    private readonly Supplier _supplier = Supplier.Create("Фарма Демо ООД", Eik.Complete9("20000001"), 30).Value;

    public DeliveryEditTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

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
        Services.AddSingleton<IStockQueries>(new NoStockQueries());
        Services.AddSingleton<IUnitOfWork>(new PassThroughUnitOfWork());
        Services.AddSingleton<ICurrentUser>(new FakeUser());
        Services.AddSingleton(TimeProvider.System);
        Services.AddScoped<SuppliersHandler>();
        Services.AddScoped<SaveDeliveryDraftHandler>();
        Services.AddScoped<PostDeliveryHandler>();
        Services.AddScoped<GetDeliveryHandler>();
        Services.AddSingleton<Apteka.Purchasing.Application.Corrections.IDeliveryCorrectionRepository>(_corrections);
        Services.AddScoped<Apteka.Purchasing.Application.Corrections.DeliveryCorrectionsHandler>();
        Services.AddScoped<Apteka.Purchasing.Application.Corrections.CreateCorrectionHandler>();
        Services.AddScoped<ListDeliveriesHandler>();
    }

    private Guid Draft()
    {
        var delivery = Delivery.Create(_supplier.Id, "0000001", DateOnly.FromDateTime(DateTime.Today), DateTimeOffset.UtcNow, null).Value;
        _deliveries.Items.Add(delivery);
        return delivery.Id;
    }

    private IRenderedComponent<DeliveryEdit> Open(Guid id) => Render<DeliveryEdit>(p => p.Add(x => x.Id, id));

    private static void Scan(IRenderedComponent<DeliveryEdit> page, string text)
    {
        page.Find("#productsearch").Input(text);
        page.Find("#productsearch").KeyDown("Enter");
    }

    [Fact]
    public void Step_one_saves_the_invoice_and_opens_the_draft()
    {
        var page = Render<DeliveryEdit>();

        page.Find("#supplier").Change(_supplier.Id.ToString());
        page.Find("#invoice").Change("0000123");
        page.FindAll("button").Single(b => b.TextContent == "Потвърди").Click();

        var delivery = Assert.Single(_deliveries.Items);
        Assert.Equal("0000123", delivery.InvoiceNo);
        Assert.EndsWith($"purchasing/deliveries/{delivery.Id}", Services.GetRequiredService<NavigationManager>().Uri, StringComparison.Ordinal);
    }

    [Fact]
    public void Step_one_shows_a_Bulgarian_error_and_saves_nothing_without_an_invoice_number()
    {
        var page = Render<DeliveryEdit>();
        page.Find("#supplier").Change(_supplier.Id.ToString());

        page.FindAll("button").Single(b => b.TextContent == "Потвърди").Click();

        Assert.Equal("Въведете номер на фактурата.", page.Find(".alert.error").TextContent);
        Assert.Empty(_deliveries.Items);
    }

    [Fact]
    public void A_scanned_barcode_fills_the_form_in_its_unit_and_markup_suggests_the_sale_price()
    {
        var page = Open(Draft());

        Scan(page, "2000000000015");
        Assert.Equal(_box.ToString(), page.Find(".qty select").GetAttribute("value"));

        page.Find("input[step='0.0001']").Input("2.40");

        // 2,40 + 25 % markup + 20 % VAT = 3,60
        Assert.Equal("3.60", page.FindAll(".line-grid input[type=number]")[4].GetAttribute("value"));
    }

    [Fact]
    public void Supplier_discount_lowers_the_purchase_price_and_the_suggested_sale_price()
    {
        var page = Open(Draft());

        Scan(page, "2000000000015");
        page.Find("input[step='0.0001']").Input("2.40");
        page.Find(".line-grid input[max='100']").Input("10");

        // 2,40 − 10 % = 2,16; + 25 % markup + 20 % VAT = 3,24
        Assert.Equal("2,16 €", page.Find(".line-grid output").TextContent);
        Assert.Equal("3.24", page.FindAll(".line-grid input[type=number]")[4].GetAttribute("value"));
    }

    [Fact]
    public void Enter_adds_the_line_and_saves_the_draft_F8_deletes_it_and_Ctrl_Enter_twice_posts()
    {
        var id = Draft();
        var page = Open(id);

        Scan(page, "2000000000015");
        page.Find("#quantity").Input("3");
        page.Find("input[step='0.0001']").Input("2.40");
        page.Find(".line-grid input[maxlength='30']").Input("L1");
        page.Find(".line-grid").KeyDown("Enter");

        var delivery = _deliveries.Items.Single();
        Assert.Equal(60, Assert.Single(delivery.Lines).BaseQuantity);
        Assert.Single(page.FindAll(".lines-table tbody tr"));

        page.Find(".lines-table tbody tr").Click();
        page.Find("div").KeyDown(new KeyboardEventArgs { Key = "Enter", CtrlKey = true });
        Assert.Contains("Потвърди приключването", page.Markup);
        page.Find("div").KeyDown(new KeyboardEventArgs { Key = "Enter", CtrlKey = true });

        Assert.Equal(DeliveryStatus.Posted, delivery.Status);
        Assert.Equal(60, Assert.Single(Assert.Single(_stock.Receipts).Lines).BaseQuantity);
    }

    [Fact]
    public void F8_deletes_the_selected_line()
    {
        var page = Open(Draft());
        Scan(page, "2000000000015");
        page.Find("input[step='0.0001']").Input("2.40");
        page.Find(".line-grid input[maxlength='30']").Input("L1");
        page.Find(".line-grid").KeyDown("Enter");

        page.Find(".lines-table tbody tr").Click();
        page.Find("div").KeyDown("F8");

        Assert.Empty(_deliveries.Items.Single().Lines);
        Assert.Empty(page.FindAll(".lines-table tbody tr"));
    }

    [Fact]
    public void Correction_form_for_a_posted_delivery_saves_the_minus_and_moves_the_stock_after_Ctrl_Enter_twice()
    {
        var id = Draft();
        var delivery = _deliveries.Items.Single();
        var productId = _catalog.Products[0].ProductId;
        Assert.True(delivery.SetLines([new DeliveryLineSpec(null, productId, _box, 20, 3, 2.40m, 25m, 3.60m, "L1", DateOnly.FromDateTime(DateTime.Today).AddYears(1))]).IsSuccess);
        Assert.True(delivery.Post(DateOnly.FromDateTime(DateTime.Today), DateTimeOffset.UtcNow).IsSuccess);

        Services.GetRequiredService<NavigationManager>().NavigateTo($"purchasing/corrections/new?delivery={id}");
        var page = Render<Apteka.Web.Features.Purchasing.Corrections.CorrectionEdit>();

        page.Find("input.delta").Input("-1");
        Assert.Contains("-2,40", page.Markup);
        page.Find("div").KeyDown(new KeyboardEventArgs { Key = "Enter", CtrlKey = true });
        Assert.Empty(_corrections.Items);
        page.Find("div").KeyDown(new KeyboardEventArgs { Key = "Enter", CtrlKey = true });

        var correction = Assert.Single(_corrections.Items);
        Assert.Equal(-1, Assert.Single(correction.Lines).QuantityDelta);
        Assert.Equal(-20, Assert.Single(Assert.Single(_stock.Corrections).Lines).BaseQuantityDelta);
    }

    [Fact]
    public void A_scanned_2D_code_fills_the_batch_and_the_expiry_date()
    {
        var page = Open(Draft());

        Scan(page, "(01)02000000000015(17)280600(10)DM-77(21)SER1");

        Assert.Equal("DM-77", page.Find(".line-grid input[maxlength='30']").GetAttribute("value"));
        Assert.Equal("2028-06-30", page.Find(".line-grid input[type=date]").GetAttribute("value"));
        Assert.Equal(_box.ToString(), page.Find(".qty select").GetAttribute("value"));
    }
}
