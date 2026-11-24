using Apteka.Catalog.Domain.Products;
using Apteka.Inventory.Application.Stocktakings;
using Apteka.Inventory.Application.WriteOffs;
using Apteka.Inventory.Contracts;
using Apteka.Inventory.Domain.Stocktakings;
using Apteka.Inventory.Infrastructure;
using Apteka.IntegrationTests.Infrastructure;
using Apteka.IntegrationTests.Purchasing;
using Apteka.Sales.Application.Checkout;
using Apteka.Sales.Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using static Apteka.IntegrationTests.Purchasing.DeliveryFixtures;

namespace Apteka.IntegrationTests.Inventory;

/// <summary>Only one stocktaking may be open, so these tests run one after the other and close what they open.</summary>
[Collection(PostgresCollection.Name)]
public class StocktakingTests(PostgresFixture postgres)
{
    private static async Task<Guid> StartForShelfAsync(IServiceProvider provider, Guid productId)
    {
        await using var scope = provider.CreateAsyncScope();
        var services = scope.ServiceProvider;

        // A shelf of its own, so the protocol holds only this test's product.
        var shelves = services.GetRequiredService<Apteka.Catalog.Application.Shelves.ShelvesHandler>();
        var shelf = (await shelves.CreateAsync(Apteka.IntegrationTests.Catalog.CatalogInputs.UniqueName("Рафт"), Ct)).Value;
        var product = (await services.GetRequiredService<Apteka.Catalog.Application.Products.GetProductHandler>().HandleAsync(productId, Ct))!;
        var input = new Apteka.Catalog.Application.Products.ProductInput(
            product.Name, product.AtcCode, product.DispensingMode, product.VatRate, product.IsActive,
            product.Units.Select(u => new Apteka.Catalog.Application.Products.UnitInput(u.Id, u.Name, u.Factor)).ToList(),
            product.Barcodes.Select(b => new Apteka.Catalog.Application.Products.BarcodeInput(b.Id, b.Code, b.UnitName)).ToList(),
            ShelfId: shelf);
        Assert.True((await services.GetRequiredService<Apteka.Catalog.Application.Products.UpdateProductHandler>().HandleAsync(productId, input, Ct)).IsSuccess);

        var started = await services.GetRequiredService<StartStocktakingHandler>().HandleAsync(new StartStocktakingInput(null, shelf), Ct);
        Assert.True(started.IsSuccess, started.IsFailure ? started.Error.Message : null);
        return started.Value;
    }

    private static async Task CloseAsync(IServiceProvider provider, Guid id)
    {
        await using var scope = provider.CreateAsyncScope();
        var closed = await scope.ServiceProvider.GetRequiredService<CloseStocktakingHandler>().HandleAsync(id, Ct);
        Assert.True(closed.IsSuccess, closed.IsFailure ? closed.Error.Message : null);
    }

    [Fact]
    public async Task Counting_and_closing_corrects_the_batches_and_the_products_are_blocked_while_open()
    {
        await using var provider = await ModuleServices.CreateAsync(postgres.ConnectionString);
        var (product, box, tablet) = await StockFixtures.StockedProductAsync(provider, DispensingMode.OverTheCounter, (2, 100), (1, 400));
        var id = await StartForShelfAsync(provider, product);

        await using (var scope = provider.CreateAsyncScope())
        {
            var view = (await scope.ServiceProvider.GetRequiredService<GetStocktakingHandler>().HandleAsync(id, Ct))!;
            Assert.StartsWith("Рафт:", view.Scope, StringComparison.Ordinal);
            Assert.Equal([("B1", 40), ("B2", 20)], view.Lines.Select(l => (l.BatchNo, l.ExpectedQuantity)));

            var count = scope.ServiceProvider.GetRequiredService<CountStocktakingHandler>();
            Assert.True((await count.HandleAsync(id, view.Lines[0].Id, 37, Ct)).IsSuccess);
            Assert.True((await count.HandleAsync(id, view.Lines[1].Id, 21, Ct)).IsSuccess);

            Assert.Equal(StocktakingErrors.AlreadyOpen, (await scope.ServiceProvider.GetRequiredService<StartStocktakingHandler>().HandleAsync(new StartStocktakingInput(null, null), Ct)).Error);
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var sold = await scope.ServiceProvider.GetRequiredService<CompleteSaleHandler>()
                .HandleAsync(new CheckoutInput([new CheckoutLineInput(product, tablet, 1)], PaymentMethod.Card, null), Ct);
            Assert.Contains("инвентаризация", sold.Error.Message, StringComparison.Ordinal);

            var batch = (await scope.ServiceProvider.GetRequiredService<IStockQueries>().GetBatchesAsync(product, null, Ct))[0];
            var writeOff = await scope.ServiceProvider.GetRequiredService<CreateWriteOffHandler>()
                .HandleAsync(new WriteOffInput(null, [new WriteOffLineInput(product, box, 1, batch.BatchId)]), Ct);
            Assert.Equal("inventory.in_stocktaking", writeOff.Error.Code);
        }

        await CloseAsync(provider, id);

        Assert.Equal(37 + 21, await StockFixtures.StockAsync(provider, product));
        await using (var scope = provider.CreateAsyncScope())
        {
            var view = (await scope.ServiceProvider.GetRequiredService<GetStocktakingHandler>().HandleAsync(id, Ct))!;
            Assert.Equal(StocktakingStatus.Closed, view.Status);
            Assert.Equal([-3, 1], view.Lines.Select(l => l.Difference!.Value));
            Assert.Equal(-0.40m, view.DifferenceValue); // −3 + 1 tablets at 0,20

            var sold = await scope.ServiceProvider.GetRequiredService<CompleteSaleHandler>()
                .HandleAsync(new CheckoutInput([new CheckoutLineInput(product, tablet, 1)], PaymentMethod.Card, null), Ct);
            Assert.True(sold.IsSuccess);
        }
    }

    [Fact]
    public async Task A_closed_stocktaking_cannot_be_changed_or_deleted()
    {
        await using var provider = await ModuleServices.CreateAsync(postgres.ConnectionString);
        var (product, _, _) = await StockFixtures.StockedProductAsync(provider, DispensingMode.OverTheCounter, (1, 400));
        var id = await StartForShelfAsync(provider, product);
        await CloseAsync(provider, id);

        await using var scope = provider.CreateAsyncScope();
        Assert.Equal(StocktakingErrors.Closed, (await scope.ServiceProvider.GetRequiredService<CloseStocktakingHandler>().HandleAsync(id, Ct)).Error);

        var context = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        var update = await Assert.ThrowsAsync<PostgresException>(() =>
            context.Database.ExecuteSqlAsync($"UPDATE inventory.stocktaking_lines SET counted_quantity = 1 WHERE stocktaking_id = {id}", Ct));
        var delete = await Assert.ThrowsAsync<PostgresException>(() =>
            context.Database.ExecuteSqlAsync($"DELETE FROM inventory.stocktakings WHERE id = {id}", Ct));
        Assert.Contains("closed", update.MessageText, StringComparison.Ordinal);
        Assert.Contains("never deleted", delete.MessageText, StringComparison.Ordinal);
    }
}
