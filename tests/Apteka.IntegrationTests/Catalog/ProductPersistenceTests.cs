using Apteka.Catalog.Application.Products;
using Apteka.Catalog.Contracts;
using Apteka.Catalog.Domain.Products;
using Apteka.Catalog.Infrastructure;
using Apteka.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Apteka.IntegrationTests.Catalog;

[Collection(PostgresCollection.Name)]
public class ProductPersistenceTests(PostgresFixture postgres)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Created_product_is_found_by_part_of_the_name_and_by_exact_barcode()
    {
        await using var provider = await ModuleServices.CreateAsync(postgres.ConnectionString);
        var name = CatalogInputs.UniqueName("Тестамол");
        var barcode = TestBarcodes.Next();

        await using (var scope = provider.CreateAsyncScope())
        {
            var created = await scope.ServiceProvider.GetRequiredService<CreateProductHandler>()
                .HandleAsync(CatalogInputs.Tablets(name, barcode), Ct);
            Assert.True(created.IsSuccess, created.IsFailure ? created.Error.Message : null);
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var search = scope.ServiceProvider.GetRequiredService<SearchProductsHandler>();

            var byName = Assert.Single(await search.HandleAsync(name[3..20].ToUpperInvariant(), Ct));
            Assert.Equal(name, byName.Name);
            Assert.Equal("таблетка", byName.BaseUnitName);
            Assert.Equal([barcode], byName.Barcodes);

            var byBarcode = Assert.Single(await search.HandleAsync(barcode, Ct));
            Assert.Equal(byName.Id, byBarcode.Id);
        }
    }

    [Fact]
    public async Task Update_is_saved_and_new_units_and_barcodes_get_ids()
    {
        await using var provider = await ModuleServices.CreateAsync(postgres.ConnectionString);
        var id = await CreateAsync(provider, CatalogInputs.UniqueName("Редакция"), TestBarcodes.Next());
        var extraBarcode = TestBarcodes.Next();

        await using (var scope = provider.CreateAsyncScope())
        {
            var view = (await scope.ServiceProvider.GetRequiredService<GetProductHandler>().HandleAsync(id, Ct))!;
            var input = new ProductInput(
                view.Name,
                view.AtcCode,
                "Друг производител",
                9m,
                view.IsRx,
                IsActive: false,
                [.. view.Units.Select(u => new UnitInput(u.Id, u.Name == "кутия" ? "опаковка" : u.Name, u.Factor)), new UnitInput(null, "стек", 200)],
                [.. view.Barcodes.Select(b => new BarcodeInput(b.Id, b.Code, b.UnitName == "кутия" ? "опаковка" : b.UnitName)), new BarcodeInput(null, extraBarcode, "стек")]);

            var updated = await scope.ServiceProvider.GetRequiredService<UpdateProductHandler>().HandleAsync(id, input, Ct);
            Assert.True(updated.IsSuccess, updated.IsFailure ? updated.Error.Message : null);
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var view = (await scope.ServiceProvider.GetRequiredService<GetProductHandler>().HandleAsync(id, Ct))!;

            Assert.Equal("Друг производител", view.Manufacturer);
            Assert.Equal(9m, view.VatRate);
            Assert.False(view.IsActive);
            Assert.Equal(["стек", "опаковка", "блистер", "таблетка"], view.Units.Select(u => u.Name));
            Assert.Contains(view.Barcodes, b => b.Code == extraBarcode && b.UnitName == "стек");
        }
    }

    [Fact]
    public async Task Barcode_of_another_product_is_rejected()
    {
        await using var provider = await ModuleServices.CreateAsync(postgres.ConnectionString);
        var barcode = TestBarcodes.Next();
        await CreateAsync(provider, CatalogInputs.UniqueName("Първа"), barcode);

        await using var scope = provider.CreateAsyncScope();
        var second = await scope.ServiceProvider.GetRequiredService<CreateProductHandler>()
            .HandleAsync(CatalogInputs.Tablets(CatalogInputs.UniqueName("Втора"), barcode), Ct);

        Assert.True(second.IsFailure);
        Assert.Equal(CatalogErrors.BarcodeTaken(barcode), second.Error);
    }

    [Fact]
    public async Task Unique_index_stops_a_barcode_saved_by_another_user_at_the_same_time()
    {
        await using var provider = await ModuleServices.CreateAsync(postgres.ConnectionString);
        var barcode = TestBarcodes.Next();

        // User A has passed the early check and is about to save ...
        await using var scopeA = provider.CreateAsyncScope();
        var repositoryA = scopeA.ServiceProvider.GetRequiredService<IProductRepository>();
        var input = CatalogInputs.Tablets(CatalogInputs.UniqueName("Състезание A"), barcode);
        var productA = Product.Create(
            new ProductDetails(input.Name, input.AtcCode, input.Manufacturer, input.VatRate, input.IsRx, input.IsActive),
            [.. input.Units.Select(u => new UnitSpec(u.Id, u.Name, u.Factor))],
            [.. input.Barcodes.Select(b => new BarcodeSpec(b.Id, b.Code, b.UnitName))]).Value;
        Assert.Empty(await repositoryA.FindTakenBarcodesAsync([barcode], productA.Id, Ct));
        repositoryA.Add(productA);

        // ... when user B saves a product with the same barcode.
        await CreateAsync(provider, CatalogInputs.UniqueName("Състезание B"), barcode);

        var saved = await repositoryA.SaveChangesAsync(Ct);

        Assert.True(saved.IsFailure);
        Assert.Equal(CatalogErrors.BarcodeTaken(barcode), saved.Error);
    }

    [Fact]
    public async Task Database_allows_only_one_base_unit_per_product()
    {
        await using var provider = await ModuleServices.CreateAsync(postgres.ConnectionString);
        var id = await CreateAsync(provider, CatalogInputs.UniqueName("Индекс"), TestBarcodes.Next());

        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        var ex = await Assert.ThrowsAsync<PostgresException>(() => context.Database.ExecuteSqlAsync(
            $"INSERT INTO catalog.product_units (id, product_id, name, factor) VALUES ({Guid.CreateVersion7()}, {id}, 'брой', 1)",
            Ct));

        Assert.Equal(PostgresErrorCodes.UniqueViolation, ex.SqlState);
    }

    [Fact]
    public async Task Other_modules_find_the_unit_and_factor_by_barcode()
    {
        await using var provider = await ModuleServices.CreateAsync(postgres.ConnectionString);
        var name = CatalogInputs.UniqueName("Справка");
        var barcode = TestBarcodes.Next();
        var id = await CreateAsync(provider, name, barcode);

        await using var scope = provider.CreateAsyncScope();
        var lookup = scope.ServiceProvider.GetRequiredService<ICatalogLookup>();

        var unit = await lookup.FindByBarcodeAsync(barcode, Ct);
        Assert.NotNull(unit);
        Assert.Equal(id, unit.ProductId);
        Assert.Equal("кутия", unit.UnitName);
        Assert.Equal(20, unit.Factor);

        var product = await lookup.GetProductAsync(id, Ct);
        Assert.Equal([1, 10, 20], product!.Units.Select(u => u.Factor));
        Assert.Null(await lookup.FindByBarcodeAsync(TestBarcodes.Next(), Ct));
    }

    internal static async Task<Guid> CreateAsync(IServiceProvider provider, string name, string barcode)
    {
        await using var scope = provider.CreateAsyncScope();
        var created = await scope.ServiceProvider.GetRequiredService<CreateProductHandler>()
            .HandleAsync(CatalogInputs.Tablets(name, barcode), Ct);
        Assert.True(created.IsSuccess, created.IsFailure ? created.Error.Message : null);
        return created.Value;
    }
}
