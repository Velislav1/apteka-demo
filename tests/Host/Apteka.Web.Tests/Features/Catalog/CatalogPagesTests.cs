using Apteka.Catalog.Application.Common;
using Apteka.Catalog.Application.Manufacturers;
using Apteka.Catalog.Application.ProductGroups;
using Apteka.Catalog.Application.Products;
using Apteka.Catalog.Application.Shelves;
using Apteka.Catalog.Domain.Manufacturers;
using Apteka.Catalog.Domain.ProductGroups;
using Apteka.Catalog.Domain.Products;
using Apteka.Catalog.Domain.Shelves;
using Apteka.Web.Features.Catalog.Manufacturers;
using Apteka.Web.Features.Catalog.Products;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Apteka.Web.Tests.Features.Catalog;

public class CatalogPagesTests : BunitContext
{
    private readonly FakeProductQueries _queries = new();
    private readonly FakeProductRepository _repository = new();
    private readonly FakeReferenceRepository<Manufacturer> _manufacturers = new();

    public CatalogPagesTests()
    {
        // The lists load a small script for the F5 key; the tests do not need it.
        JSInterop.Mode = JSRuntimeMode.Loose;

        Services.AddSingleton<IProductQueries>(_queries);
        Services.AddSingleton<IProductRepository>(_repository);
        Services.AddScoped<SearchProductsHandler>();
        Services.AddScoped<GetProductHandler>();
        Services.AddScoped<CreateProductHandler>();
        Services.AddScoped<UpdateProductHandler>();
        Services.AddScoped<ProductReferenceRules>();
        Services.AddSingleton<Apteka.SharedKernel.IUnitOfWork>(new Apteka.Web.Tests.Features.Purchasing.PassThroughUnitOfWork());
        Services.AddSingleton<IReferenceRepository<Manufacturer>>(_manufacturers);
        Services.AddSingleton<IReferenceRepository<ProductGroup>>(new FakeReferenceRepository<ProductGroup>());
        Services.AddSingleton<IReferenceRepository<Shelf>>(new FakeReferenceRepository<Shelf>());
        Services.AddScoped<ManufacturersHandler>();
        Services.AddScoped<ProductGroupsHandler>();
        Services.AddScoped<ShelvesHandler>();

        _queries.Items.Add(new ProductListItem(Guid.CreateVersion7(), "Парацетамол 500 мг", "Демо Фарма АД", "Нервна система", 20m, DispensingMode.OverTheCounter, true, "таблетка", ["2000000000015"]));
        _queries.Items.Add(new ProductListItem(Guid.CreateVersion7(), "Амоксицилин 500 мг", "Примерфарм АД", null, 20m, DispensingMode.Prescription, false, "капсула", ["2000000000022"]));
    }

    [Fact]
    public void Product_list_shows_all_products_with_Rx_and_inactive_marks()
    {
        var page = Render<ProductList>();

        var rows = page.FindAll("tbody tr");
        Assert.Equal(2, rows.Count);
        Assert.Contains("Парацетамол 500 мг", rows[0].TextContent);
        Assert.Contains("20 %", rows[0].TextContent);
        Assert.Contains("Rx", rows[1].TextContent);
        Assert.Contains("inactive", rows[1].ClassName);
    }

    [Fact]
    public void Enter_in_the_search_box_searches_by_barcode()
    {
        var page = Render<ProductList>();

        var search = page.Find("input[type=search]");
        search.Input("2000000000022");
        search.KeyDown("Enter");

        Assert.Equal("2000000000022", _queries.Searches[^1]);
        var row = Assert.Single(page.FindAll("tbody tr"));
        Assert.Contains("Амоксицилин", row.TextContent);
    }

    [Fact]
    public void Saving_a_product_without_a_base_unit_shows_the_error_in_Bulgarian()
    {
        var page = Render<ProductEdit>();

        page.Find("#name").Change("Нова стока");
        page.Find("tbody input[type=number]").Change("10");
        page.FindAll("button").Single(b => b.TextContent == "Запиши").Click();

        Assert.Contains("точно една мярка с коефициент 1", page.Find(".alert.error").TextContent);
        Assert.Empty(_repository.Added);
    }

    [Fact]
    public void Saving_a_valid_new_product_opens_its_edit_page()
    {
        var page = Render<ProductEdit>();

        page.Find("#name").Change("Нова стока");
        page.FindAll("button").Single(b => b.TextContent == "Запиши").Click();

        var product = Assert.Single(_repository.Added);
        Assert.EndsWith($"catalog/{product.Id}", Services.GetRequiredService<NavigationManager>().Uri, StringComparison.Ordinal);
    }

    [Fact]
    public void Edit_page_shows_the_stored_product_with_VAT_selected_and_fixed_factors()
    {
        var id = Guid.CreateVersion7();
        var box = Guid.CreateVersion7();
        var manufacturer = Manufacturer.Create("Демо Фарма АД").Value;
        _manufacturers.Items.Add(manufacturer);
        _queries.Views[id] = new ProductView(
            id, "Ибупрофен 400 мг", "M01AE01", DispensingMode.Narcotic, 20.00m, true, "PG123", manufacturer.Id, null, null, "кутия",
            [new ProductUnitView(box, "кутия", 30), new ProductUnitView(Guid.CreateVersion7(), "таблетка", 1)],
            [new BarcodeView(Guid.CreateVersion7(), "2000000000046", box, "кутия")]);

        var page = Render<ProductEdit>(p => p.Add(x => x.Id, id));

        Assert.Equal("Ибупрофен 400 мг", page.Find("#name").GetAttribute("value"));
        Assert.Equal("20", page.Find("#vat").GetAttribute("value"));
        Assert.Equal(manufacturer.Id.ToString(), page.Find("#manufacturer").GetAttribute("value"));
        Assert.Equal(nameof(DispensingMode.Narcotic), page.Find("#mode").GetAttribute("value"));
        Assert.Equal("кутия", page.Find("#saleunit").GetAttribute("value"));
        Assert.All(page.FindAll("tbody input[type=number]"), i => Assert.True(i.HasAttribute("disabled")));
        Assert.Empty(page.FindAll("button.link"));
    }

    [Fact]
    public void Manufacturers_page_adds_an_item_and_shows_the_Bulgarian_error_for_an_empty_name()
    {
        var page = Render<ManufacturersPage>();

        page.FindAll("button").Single(b => b.TextContent == "Добави").Click();
        Assert.Equal("Въведете име.", page.Find(".alert.error").TextContent);

        page.Find("input[aria-label='Ново име']").Input("Демо Фарма АД");
        page.FindAll("button").Single(b => b.TextContent == "Добави").Click();

        Assert.Equal("Демо Фарма АД", Assert.Single(_manufacturers.Items).Name);
        Assert.Equal("Демо Фарма АД", page.Find("tbody input[aria-label='Име']").GetAttribute("value"));
    }
}
