using Apteka.Catalog.Application.ProductGroups;
using Apteka.Catalog.Application.Products;
using Apteka.Catalog.Contracts;
using Apteka.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Apteka.IntegrationTests.Catalog;

[Collection(PostgresCollection.Name)]
public class CodesAndMarkupsTests(PostgresFixture postgres)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Every_product_gets_its_own_code_and_can_be_found_by_it()
    {
        await using var provider = await ModuleServices.CreateAsync(postgres.ConnectionString);
        var first = await ProductPersistenceTests.CreateAsync(provider, CatalogInputs.UniqueName("Код А"), TestBarcodes.Next());
        var second = await ProductPersistenceTests.CreateAsync(provider, CatalogInputs.UniqueName("Код Б"), TestBarcodes.Next());

        await using var scope = provider.CreateAsyncScope();
        var lookup = scope.ServiceProvider.GetRequiredService<ICatalogLookup>();
        var a = (await lookup.GetProductAsync(first, Ct))!;
        var b = (await lookup.GetProductAsync(second, Ct))!;

        Assert.True(a.Code > 0);
        Assert.True(b.Code > a.Code);
        Assert.Equal(first, (await lookup.FindByCodeAsync(a.Code, Ct))!.ProductId);

        var found = await scope.ServiceProvider.GetRequiredService<SearchProductsHandler>().HandleAsync(b.Code.ToString(System.Globalization.CultureInfo.InvariantCulture), Ct);
        Assert.Contains(found, p => p.Id == second);
    }

    [Fact]
    public async Task Markup_is_the_products_else_the_groups_else_25_percent()
    {
        await using var provider = await ModuleServices.CreateAsync(postgres.ConnectionString);
        Guid group, inGroup, own, none;

        await using (var scope = provider.CreateAsyncScope())
        {
            var groups = scope.ServiceProvider.GetRequiredService<ProductGroupsHandler>();
            group = (await groups.CreateAsync(CatalogInputs.UniqueName("Група"), Ct)).Value;
            Assert.True((await groups.SetMarkupAsync(group, 18m, Ct)).IsSuccess);

            var create = scope.ServiceProvider.GetRequiredService<CreateProductHandler>();
            inGroup = (await create.HandleAsync(CatalogInputs.Tablets(CatalogInputs.UniqueName("В група"), TestBarcodes.Next()) with { ProductGroupId = group }, Ct)).Value;
            own = (await create.HandleAsync(CatalogInputs.Tablets(CatalogInputs.UniqueName("Собствена"), TestBarcodes.Next()) with { ProductGroupId = group, MarkupPercent = 40m }, Ct)).Value;
            none = (await create.HandleAsync(CatalogInputs.Tablets(CatalogInputs.UniqueName("Без"), TestBarcodes.Next()), Ct)).Value;
        }

        await using var check = provider.CreateAsyncScope();
        var lookup = check.ServiceProvider.GetRequiredService<ICatalogLookup>();
        Assert.Equal(18m, (await lookup.GetProductAsync(inGroup, Ct))!.MarkupPercent);
        Assert.Equal(40m, (await lookup.GetProductAsync(own, Ct))!.MarkupPercent);
        Assert.Equal(25m, (await lookup.GetProductAsync(none, Ct))!.MarkupPercent);
    }
}
