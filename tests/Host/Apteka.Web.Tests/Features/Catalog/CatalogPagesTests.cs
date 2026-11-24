using Apteka.Catalog.Application.Products;
using Apteka.Web.Features.Catalog;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Apteka.Web.Tests.Features.Catalog;

public class CatalogPagesTests : BunitContext
{
    private readonly FakeProductQueries _queries = new();
    private readonly FakeProductRepository _repository = new();

    public CatalogPagesTests()
    {
        Services.AddSingleton<IProductQueries>(_queries);
        Services.AddSingleton<IProductRepository>(_repository);
        Services.AddScoped<SearchProductsHandler>();
        Services.AddScoped<GetProductHandler>();
        Services.AddScoped<CreateProductHandler>();
        Services.AddScoped<UpdateProductHandler>();

        _queries.Items.Add(new ProductListItem(Guid.CreateVersion7(), "Парацетамол 500 мг", "Демо Фарма АД", 20m, false, true, "таблетка", ["2000000000015"]));
        _queries.Items.Add(new ProductListItem(Guid.CreateVersion7(), "Амоксицилин 500 мг", "Примерфарм АД", 20m, true, false, "капсула", ["2000000000022"]));
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
        _queries.Views[id] = new ProductView(
            id, "Ибупрофен 400 мг", "M01AE01", "Демо Фарма АД", 20.00m, false, true,
            [new ProductUnitView(box, "кутия", 30), new ProductUnitView(Guid.CreateVersion7(), "таблетка", 1)],
            [new BarcodeView(Guid.CreateVersion7(), "2000000000046", box, "кутия")]);

        var page = Render<ProductEdit>(p => p.Add(x => x.Id, id));

        Assert.Equal("Ибупрофен 400 мг", page.Find("#name").GetAttribute("value"));
        Assert.Equal("20", page.Find("#vat").GetAttribute("value"));
        Assert.All(page.FindAll("tbody input[type=number]"), i => Assert.True(i.HasAttribute("disabled")));
        Assert.Empty(page.FindAll("button.link"));
    }
}
