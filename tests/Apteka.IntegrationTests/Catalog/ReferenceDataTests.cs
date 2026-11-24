using Apteka.Catalog.Application.Manufacturers;
using Apteka.Catalog.Application.Products;
using Apteka.Catalog.Application.Shelves;
using Apteka.Catalog.Domain.Common;
using Apteka.Catalog.Domain.Products;
using Apteka.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Apteka.IntegrationTests.Catalog;

[Collection(PostgresCollection.Name)]
public class ReferenceDataTests(PostgresFixture postgres)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Names_are_unique_ignoring_case_and_items_can_be_deactivated()
    {
        await using var provider = await ModuleServices.CreateAsync(postgres.ConnectionString);
        await using var scope = provider.CreateAsyncScope();
        var shelves = scope.ServiceProvider.GetRequiredService<ShelvesHandler>();
        var name = CatalogInputs.UniqueName("Витрина");

        var id = (await shelves.CreateAsync(name, Ct)).Value;
        var duplicate = await shelves.CreateAsync(name.ToUpperInvariant(), Ct);
        Assert.Equal(ReferenceErrors.NameTaken(name.ToUpperInvariant()), duplicate.Error);

        Assert.True((await shelves.UpdateAsync(id, name, isActive: false, Ct)).IsSuccess);
        Assert.DoesNotContain(await shelves.ListAsync(includeInactive: false, Ct), s => s.Id == id);
        Assert.Contains(await shelves.ListAsync(includeInactive: true, Ct), s => s.Id == id && !s.IsActive);
    }

    [Fact]
    public async Task Product_cannot_get_an_inactive_manufacturer_but_keeps_one_deactivated_later()
    {
        await using var provider = await ModuleServices.CreateAsync(postgres.ConnectionString);
        Guid active, inactive, productId;

        await using (var scope = provider.CreateAsyncScope())
        {
            var manufacturers = scope.ServiceProvider.GetRequiredService<ManufacturersHandler>();
            active = (await manufacturers.CreateAsync(CatalogInputs.UniqueName("Активен"), Ct)).Value;
            inactive = (await manufacturers.CreateAsync(CatalogInputs.UniqueName("Неактивен"), Ct)).Value;
            Assert.True((await manufacturers.UpdateAsync(inactive, CatalogInputs.UniqueName("Неактивен"), false, Ct)).IsSuccess);
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var create = scope.ServiceProvider.GetRequiredService<CreateProductHandler>();

            var rejected = await create.HandleAsync(
                CatalogInputs.Tablets(CatalogInputs.UniqueName("Отказана"), TestBarcodes.Next()) with { ManufacturerId = inactive }, Ct);
            Assert.Equal(CatalogErrors.ManufacturerNotAvailable, rejected.Error);

            var unknown = await create.HandleAsync(
                CatalogInputs.Tablets(CatalogInputs.UniqueName("Непозната"), TestBarcodes.Next()) with { ShelfId = Guid.CreateVersion7() }, Ct);
            Assert.Equal(CatalogErrors.ShelfNotAvailable, unknown.Error);

            productId = (await create.HandleAsync(
                CatalogInputs.Tablets(CatalogInputs.UniqueName("Приета"), TestBarcodes.Next()) with { ManufacturerId = active }, Ct)).Value;
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var manufacturers = scope.ServiceProvider.GetRequiredService<ManufacturersHandler>();
            var current = (await manufacturers.ListAsync(true, Ct)).Single(m => m.Id == active);
            Assert.True((await manufacturers.UpdateAsync(active, current.Name, isActive: false, Ct)).IsSuccess);
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var view = (await scope.ServiceProvider.GetRequiredService<GetProductHandler>().HandleAsync(productId, Ct))!;
            var saved = await scope.ServiceProvider.GetRequiredService<UpdateProductHandler>()
                .HandleAsync(productId, CatalogInputs.FromView(view) with { VatRate = 9m }, Ct);

            Assert.True(saved.IsSuccess, saved.IsFailure ? saved.Error.Message : null);
        }
    }
}
