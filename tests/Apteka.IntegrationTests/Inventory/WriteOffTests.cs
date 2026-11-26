using Apteka.Audit.Infrastructure;
using Apteka.Catalog.Domain.Products;
using Apteka.Inventory.Application.WriteOffs;
using Apteka.Inventory.Contracts;
using Apteka.Inventory.Domain.WriteOffs;
using Apteka.Inventory.Infrastructure;
using Apteka.IntegrationTests.Infrastructure;
using Apteka.IntegrationTests.Purchasing;
using Apteka.Sales.Application.Checkout;
using Apteka.Sales.Domain.Sales;
using Apteka.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using static Apteka.IntegrationTests.Purchasing.DeliveryFixtures;

namespace Apteka.IntegrationTests.Inventory;

[Collection(PostgresCollection.Name)]
public class WriteOffTests(PostgresFixture postgres)
{
    private static async Task<Result<Guid>> WriteOffAsync(IServiceProvider provider, params WriteOffLineInput[] lines)
    {
        await using var scope = provider.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<CreateWriteOffHandler>().HandleAsync(new WriteOffInput("изтекъл срок", lines), Ct);
    }

    private static async Task<IReadOnlyList<BatchStock>> BatchesAsync(IServiceProvider provider, Guid product)
    {
        await using var scope = provider.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IStockQueries>().GetBatchesAsync(product, null, Ct);
    }

    [Fact]
    public async Task A_write_off_takes_from_the_chosen_batch_and_is_listed_with_its_cost()
    {
        await using var provider = await ModuleServices.CreateAsync(postgres.ConnectionString);
        var (product, box, tablet) = await StockFixtures.StockedProductAsync(provider, DispensingMode.OverTheCounter, (2, 100), (3, 400));
        var later = (await BatchesAsync(provider, product)).Single(b => b.BatchNo == "B2");

        var created = await WriteOffAsync(provider, new WriteOffLineInput(product, box, 1, later.BatchId), new WriteOffLineInput(product, tablet, 5, later.BatchId));

        Assert.True(created.IsSuccess, created.IsFailure ? created.Error.Message : null);
        var batches = await BatchesAsync(provider, product);
        Assert.Equal(40, batches.Single(b => b.BatchNo == "B1").BaseQuantity); // FEFO would have taken this one
        Assert.Equal(60 - 25, batches.Single(b => b.BatchNo == "B2").BaseQuantity);

        await using var scope = provider.CreateAsyncScope();
        var view = (await scope.ServiceProvider.GetRequiredService<GetWriteOffHandler>().HandleAsync(created.Value, Ct))!;
        Assert.True(view.Number > 0);
        Assert.Equal("изтекъл срок", view.Reason);
        Assert.Equal(["кутия", "таблетка"], view.Lines.Select(l => l.UnitName));
        Assert.Equal(5.00m, view.PurchaseTotal); // 25 tablets × 0,20

        var today = BulgarianTime.Today(DateTimeOffset.UtcNow);
        var listed = await scope.ServiceProvider.GetRequiredService<ListWriteOffsHandler>().HandleAsync(today.AddDays(-1), today.AddDays(1), Ct);
        Assert.Contains(listed, w => w.Id == created.Value && w.LineCount == 2 && w.PurchaseTotal == 5.00m);

        var audit = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
        Assert.True(await audit.AuditLog.AnyAsync(r => r.Entity == nameof(WriteOff) && r.EntityId == created.Value.ToString(), Ct));
    }

    [Fact]
    public async Task More_than_the_batch_has_is_refused_and_nothing_moves()
    {
        await using var provider = await ModuleServices.CreateAsync(postgres.ConnectionString);
        var (product, box, tablet) = await StockFixtures.StockedProductAsync(provider, DispensingMode.OverTheCounter, (1, 400));
        var batch = Assert.Single(await BatchesAsync(provider, product));

        var refused = await WriteOffAsync(provider, new WriteOffLineInput(product, tablet, 15, batch.BatchId), new WriteOffLineInput(product, tablet, 6, batch.BatchId));

        Assert.Equal("inventory.write_off_insufficient", refused.Error.Code);
        Assert.Contains("Ред 2", refused.Error.Message);
        Assert.Equal(20, await StockFixtures.StockAsync(provider, product));

        var (other, _, _) = await StockFixtures.StockedProductAsync(provider, DispensingMode.OverTheCounter, (1, 400));
        var foreign = await WriteOffAsync(provider, new WriteOffLineInput(other, box, 1, batch.BatchId));
        Assert.Equal("inventory.write_off_product", foreign.Error.Code);
    }

    [Fact]
    public async Task Write_off_and_sale_of_the_last_box_at_the_same_time_only_one_passes()
    {
        await using var provider = await ModuleServices.CreateAsync(postgres.ConnectionString);
        var (product, box, _) = await StockFixtures.StockedProductAsync(provider, DispensingMode.OverTheCounter, (1, 400));
        var batch = Assert.Single(await BatchesAsync(provider, product));

        async Task<bool> Sell()
        {
            await using var scope = provider.CreateAsyncScope();
            var sold = await scope.ServiceProvider.GetRequiredService<CompleteSaleHandler>()
                .HandleAsync(new CheckoutInput([new CheckoutLineInput(product, box, 1)], PaymentMethod.Card, null), Ct);
            return sold.IsSuccess;
        }

        async Task<bool> WriteOff() => (await WriteOffAsync(provider, new WriteOffLineInput(product, box, 1, batch.BatchId))).IsSuccess;

        var results = await Task.WhenAll(Task.Run(Sell, Ct), Task.Run(WriteOff, Ct));

        Assert.Single(results, ok => ok);
        Assert.Equal(0, await StockFixtures.StockAsync(provider, product));
    }

    [Fact]
    public async Task A_saved_write_off_cannot_be_changed_or_deleted()
    {
        await using var provider = await ModuleServices.CreateAsync(postgres.ConnectionString);
        var (product, box, _) = await StockFixtures.StockedProductAsync(provider, DispensingMode.OverTheCounter, (2, 400));
        var batch = Assert.Single(await BatchesAsync(provider, product));
        var id = (await WriteOffAsync(provider, new WriteOffLineInput(product, box, 1, batch.BatchId))).Value;

        await using var scope = provider.CreateAsyncScope();
        var inventory = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        var update = await Assert.ThrowsAsync<PostgresException>(() =>
            inventory.Database.ExecuteSqlAsync($"UPDATE inventory.write_off_lines SET quantity = 5 WHERE write_off_id = {id}", Ct));
        var delete = await Assert.ThrowsAsync<PostgresException>(() =>
            inventory.Database.ExecuteSqlAsync($"DELETE FROM inventory.write_offs WHERE id = {id}", Ct));
        Assert.Contains("append-only", update.MessageText, StringComparison.Ordinal);
        Assert.Contains("append-only", delete.MessageText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_protocol_needs_a_recipient_and_the_list_shows_only_the_chosen_kind()
    {
        await using var provider = await ModuleServices.CreateAsync(postgres.ConnectionString);
        var (product, box, _) = await StockFixtures.StockedProductAsync(provider, DispensingMode.OverTheCounter, (3, 400));
        var batch = Assert.Single(await BatchesAsync(provider, product));

        await using var scope = provider.CreateAsyncScope();
        var create = scope.ServiceProvider.GetRequiredService<CreateWriteOffHandler>();
        var line = new WriteOffLineInput(product, box, 1, batch.BatchId);
        Assert.Equal(WriteOffErrors.RecipientRequired, (await create.HandleAsync(new WriteOffInput(null, [line], WriteOffKind.Protocol), Ct)).Error);

        var protocol = (await create.HandleAsync(new WriteOffInput("договор", [line], WriteOffKind.Protocol, "Отделение Демо"), Ct)).Value;
        var sample = (await create.HandleAsync(new WriteOffInput(null, [line], WriteOffKind.Sample), Ct)).Value;
        Assert.Equal(20, await StockFixtures.StockAsync(provider, product));

        var today = BulgarianTime.Today(DateTimeOffset.UtcNow);
        var list = scope.ServiceProvider.GetRequiredService<ListWriteOffsHandler>();
        var protocols = await list.HandleAsync(today, today, Ct, WriteOffKind.Protocol);
        Assert.Contains(protocols, w => w.Id == protocol && w.Recipient == "Отделение Демо");
        Assert.DoesNotContain(protocols, w => w.Id == sample);
        Assert.Equal(WriteOffKind.Sample, (await scope.ServiceProvider.GetRequiredService<GetWriteOffHandler>().HandleAsync(sample, Ct))!.Kind);
    }
}
