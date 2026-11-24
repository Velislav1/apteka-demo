using Apteka.Audit.Infrastructure;
using Apteka.Inventory.Contracts;
using Apteka.Inventory.Domain.Batches;
using Apteka.Inventory.Infrastructure;
using Apteka.IntegrationTests.Infrastructure;
using Apteka.Purchasing.Application.Deliveries;
using Apteka.Purchasing.Domain.Deliveries;
using Apteka.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using static Apteka.IntegrationTests.Purchasing.DeliveryFixtures;

namespace Apteka.IntegrationTests.Purchasing;

[Collection(PostgresCollection.Name)]
public class DeliveryPostingTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Posting_a_delivery_creates_batches_and_stock_in_base_units()
    {
        await using var provider = await ModuleServices.CreateAsync(postgres.ConnectionString);
        var supplier = await SupplierAsync(provider);
        var (product, box, tablet) = await ProductAsync(provider);
        var id = await DraftAsync(provider, supplier, Line(product, box, 3, "A1", 200), Line(product, tablet, 7, "B2", 100));

        await using (var scope = provider.CreateAsyncScope())
        {
            Assert.Empty(await scope.ServiceProvider.GetRequiredService<IStockQueries>().GetProductStockAsync([product], null, Ct));

            var posted = await scope.ServiceProvider.GetRequiredService<PostDeliveryHandler>().HandleAsync(id, Ct);
            Assert.True(posted.IsSuccess, posted.IsFailure ? posted.Error.Message : null);
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var stock = scope.ServiceProvider.GetRequiredService<IStockQueries>();

            var total = Assert.Single(await stock.GetProductStockAsync([product], null, Ct));
            Assert.Equal((3 * 20) + 7, total.BaseQuantity);
            Assert.Equal(2, total.BatchCount);
            Assert.Equal(Today.AddDays(100), total.NearestExpiry);

            var batches = await stock.GetBatchesAsync(product, null, Ct);
            Assert.Equal(["B2", "A1"], batches.Select(b => b.BatchNo));
            var boxBatch = batches.Single(b => b.BatchNo == "A1");
            Assert.Equal(60, boxBatch.BaseQuantity);
            Assert.Equal(0.2000m, boxBatch.PurchasePricePerBaseUnit);
            Assert.Equal(0.3000m, boxBatch.SalePricePerBaseUnit);
            Assert.Equal((20, 6.00m), (boxBatch.PricedUnitFactor, boxBatch.PricedUnitSalePrice));

            var view = (await scope.ServiceProvider.GetRequiredService<GetDeliveryHandler>().HandleAsync(id, Ct))!;
            Assert.Equal(DeliveryStatus.Posted, view.Status);
            Assert.NotNull(view.PostedAt);
            Assert.Equal(40.00m, view.PurchaseTotal);
        }
    }

    [Fact]
    public async Task Delivery_gets_a_number_and_the_supplier_term_and_the_discount_goes_into_the_batch_price()
    {
        await using var provider = await ModuleServices.CreateAsync(postgres.ConnectionString);
        var supplier = await SupplierAsync(provider);
        var (product, box, _) = await ProductAsync(provider);
        var first = await DraftAsync(provider, supplier, Line(product, box, 2, "D1") with { SupplierDiscountPercent = 10m });
        var second = await DraftAsync(provider, supplier, Line(product, box, 1, "D2"));

        await using (var scope = provider.CreateAsyncScope())
        {
            var get = scope.ServiceProvider.GetRequiredService<GetDeliveryHandler>();
            var view = (await get.HandleAsync(first, Ct))!;
            Assert.True(view.Number > 0);
            Assert.True((await get.HandleAsync(second, Ct))!.Number > view.Number);
            Assert.Equal(30, view.PaymentTermDays);
            Assert.Equal(view.InvoiceDate.AddDays(30), view.DueDate);
            Assert.Equal(3.6000m, view.Lines[0].NetUnitPurchasePrice);
            Assert.Equal(7.20m, view.PurchaseTotal);

            var listed = (await scope.ServiceProvider.GetRequiredService<ListDeliveriesHandler>().HandleAsync(supplier, cancellationToken: Ct)).Single(d => d.Id == first);
            Assert.Equal((view.Number, 7.20m), (listed.Number, listed.PurchaseTotal));

            Assert.True((await scope.ServiceProvider.GetRequiredService<PostDeliveryHandler>().HandleAsync(first, Ct)).IsSuccess);
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var batch = Assert.Single(await scope.ServiceProvider.GetRequiredService<IStockQueries>().GetBatchesAsync(product, null, Ct));
            Assert.Equal(0.1800m, batch.PurchasePricePerBaseUnit);
        }
    }

    [Fact]
    public async Task Payment_term_typed_on_the_delivery_wins_over_the_supplier_term()
    {
        await using var provider = await ModuleServices.CreateAsync(postgres.ConnectionString);
        var supplier = await SupplierAsync(provider);
        var (product, box, _) = await ProductAsync(provider);

        await using var scope = provider.CreateAsyncScope();
        var saved = await scope.ServiceProvider.GetRequiredService<SaveDeliveryDraftHandler>().HandleAsync(
            null, new DeliveryInput(supplier, "T-1", Today, [Line(product, box, 1, "T1")], PaymentTermDays: 7), Ct);
        Assert.True(saved.IsSuccess, saved.IsFailure ? saved.Error.Message : null);

        Assert.Equal(7, (await scope.ServiceProvider.GetRequiredService<GetDeliveryHandler>().HandleAsync(saved.Value, Ct))!.PaymentTermDays);
    }

    [Fact]
    public async Task Expiring_filter_counts_only_batches_that_expire_by_the_date()
    {
        await using var provider = await ModuleServices.CreateAsync(postgres.ConnectionString);
        var supplier = await SupplierAsync(provider);
        var (product, box, _) = await ProductAsync(provider);
        var id = await DraftAsync(provider, supplier, Line(product, box, 1, "SOON", 30), Line(product, box, 2, "LATE", 300));

        await using var scope = provider.CreateAsyncScope();
        Assert.True((await scope.ServiceProvider.GetRequiredService<PostDeliveryHandler>().HandleAsync(id, Ct)).IsSuccess);
        var stock = scope.ServiceProvider.GetRequiredService<IStockQueries>();

        var expiring = Assert.Single(await stock.GetProductStockAsync([product], Today.AddDays(90), Ct));
        Assert.Equal(20, expiring.BaseQuantity);
        Assert.Equal(["SOON"], (await stock.GetBatchesAsync(product, Today.AddDays(90), Ct)).Select(b => b.BatchNo));
    }

    [Fact]
    public async Task Posted_delivery_cannot_be_changed_or_posted_again()
    {
        await using var provider = await ModuleServices.CreateAsync(postgres.ConnectionString);
        var supplier = await SupplierAsync(provider);
        var (product, box, _) = await ProductAsync(provider);
        var id = await DraftAsync(provider, supplier, Line(product, box, 1, "X"));

        await using var scope = provider.CreateAsyncScope();
        var post = scope.ServiceProvider.GetRequiredService<PostDeliveryHandler>();
        Assert.True((await post.HandleAsync(id, Ct)).IsSuccess);

        var view = (await scope.ServiceProvider.GetRequiredService<GetDeliveryHandler>().HandleAsync(id, Ct))!;
        var change = await scope.ServiceProvider.GetRequiredService<SaveDeliveryDraftHandler>().HandleAsync(
            id,
            new DeliveryInput(supplier, view.InvoiceNo, view.InvoiceDate, [Line(product, box, 5, "X")]),
            Ct);

        Assert.Equal(DeliveryErrors.NotDraft, change.Error);
        Assert.Equal(DeliveryErrors.NotDraft, (await post.HandleAsync(id, Ct)).Error);
        Assert.Equal(20, (await scope.ServiceProvider.GetRequiredService<IStockQueries>().GetProductStockAsync([product], null, Ct)).Single().BaseQuantity);
    }

    [Fact]
    public async Task Same_invoice_from_the_same_supplier_is_rejected_but_another_supplier_may_use_it()
    {
        await using var provider = await ModuleServices.CreateAsync(postgres.ConnectionString);
        var (supplierA, supplierB) = (await SupplierAsync(provider), await SupplierAsync(provider));
        var (product, box, _) = await ProductAsync(provider);
        var invoice = "INV-" + Guid.NewGuid().ToString("N")[..8];

        await using var scope = provider.CreateAsyncScope();
        var save = scope.ServiceProvider.GetRequiredService<SaveDeliveryDraftHandler>();
        DeliveryInput Input(Guid supplier) => new(supplier, invoice, Today, [Line(product, box, 1, "Z")]);

        Assert.True((await save.HandleAsync(null, Input(supplierA), Ct)).IsSuccess);
        Assert.Equal(DeliveryErrors.InvoiceTaken(invoice), (await save.HandleAsync(null, Input(supplierA), Ct)).Error);
        Assert.True((await save.HandleAsync(null, Input(supplierB), Ct)).IsSuccess);
    }

    [Fact]
    public async Task When_anything_fails_after_stock_was_written_nothing_is_saved_and_the_delivery_stays_a_draft()
    {
        await using var provider = await ModuleServices.CreateAsync(
            postgres.ConnectionString,
            replace: services => services.AddScoped<IStockReceiving>(sp => new FailingAfterReceive(
                ActivatorUtilities.CreateInstance<Apteka.Inventory.Application.Receiving.ReceiveStockHandler>(sp))));
        var supplier = await SupplierAsync(provider);
        var (product, box, _) = await ProductAsync(provider);
        var id = await DraftAsync(provider, supplier, Line(product, box, 2, "ROLLBACK"));

        await using (var scope = provider.CreateAsyncScope())
        {
            var posted = await scope.ServiceProvider.GetRequiredService<PostDeliveryHandler>().HandleAsync(id, Ct);
            Assert.Equal(FailingAfterReceive.Error, posted.Error);
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var view = (await scope.ServiceProvider.GetRequiredService<GetDeliveryHandler>().HandleAsync(id, Ct))!;
            Assert.Equal(DeliveryStatus.Draft, view.Status);

            var inventory = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
            Assert.False(await inventory.Batches.AnyAsync(b => b.ProductId == product, Ct));

            // No audit rows survived either: nothing about this delivery's stock was ever written for good.
            var audit = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
            var deliveryRows = await audit.AuditLog.Where(r => r.EntityId == id.ToString()).Select(r => r.Action).ToListAsync(Ct);
            Assert.Equal(["Insert"], deliveryRows);
        }
    }

    [Fact]
    public async Task Two_users_posting_the_same_delivery_at_once_receive_the_stock_only_once()
    {
        await using var provider = await ModuleServices.CreateAsync(postgres.ConnectionString);
        var supplier = await SupplierAsync(provider);
        var (product, box, _) = await ProductAsync(provider);
        var id = await DraftAsync(provider, supplier, Line(product, box, 1, "RACE"));

        async Task<Result> PostAsync()
        {
            await using var scope = provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<PostDeliveryHandler>().HandleAsync(id, Ct);
        }

        var results = await Task.WhenAll(PostAsync(), PostAsync());

        Assert.Single(results, r => r.IsSuccess);
        await using var check = provider.CreateAsyncScope();
        Assert.Equal(20, (await check.ServiceProvider.GetRequiredService<IStockQueries>().GetProductStockAsync([product], null, Ct)).Single().BaseQuantity);
    }

    [Fact]
    public async Task Stock_movements_cannot_be_changed_or_deleted()
    {
        await using var provider = await ModuleServices.CreateAsync(postgres.ConnectionString);
        var supplier = await SupplierAsync(provider);
        var (product, box, _) = await ProductAsync(provider);
        var id = await DraftAsync(provider, supplier, Line(product, box, 1, "LOCK"));

        await using var scope = provider.CreateAsyncScope();
        Assert.True((await scope.ServiceProvider.GetRequiredService<PostDeliveryHandler>().HandleAsync(id, Ct)).IsSuccess);
        var inventory = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

        var update = await Assert.ThrowsAsync<PostgresException>(() =>
            inventory.Database.ExecuteSqlAsync($"UPDATE inventory.stock_movements SET quantity_delta = 1000 WHERE document_id = {id}", Ct));
        var delete = await Assert.ThrowsAsync<PostgresException>(() =>
            inventory.Database.ExecuteSqlAsync($"DELETE FROM inventory.stock_movements WHERE document_id = {id}", Ct));

        Assert.Contains("append-only", update.MessageText, StringComparison.Ordinal);
        Assert.Contains("append-only", delete.MessageText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Posting_writes_audit_rows_for_the_delivery_the_batch_and_the_movement_in_the_same_transaction()
    {
        await using var provider = await ModuleServices.CreateAsync(postgres.ConnectionString);
        var supplier = await SupplierAsync(provider);
        var (product, box, _) = await ProductAsync(provider);
        var id = await DraftAsync(provider, supplier, Line(product, box, 1, "AUDIT"));

        await using var scope = provider.CreateAsyncScope();
        Assert.True((await scope.ServiceProvider.GetRequiredService<PostDeliveryHandler>().HandleAsync(id, Ct)).IsSuccess);
        var audit = scope.ServiceProvider.GetRequiredService<AuditDbContext>();

        var statusChange = await audit.AuditLog.SingleAsync(r => r.Entity == nameof(Delivery) && r.EntityId == id.ToString() && r.Action == "Update", Ct);
        Assert.Contains("Posted", statusChange.NewValues!, StringComparison.Ordinal);
        var inventory = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        var batchId = (await inventory.Batches.SingleAsync(b => b.SourceDocumentId == id, Ct)).Id.ToString();
        var movementId = (await inventory.StockMovements.SingleAsync(m => m.DocumentId == id, Ct)).Id.ToString();

        Assert.True(await audit.AuditLog.AnyAsync(r => r.Entity == nameof(Batch) && r.Action == "Insert" && r.EntityId == batchId, Ct));
        Assert.True(await audit.AuditLog.AnyAsync(r => r.Entity == "StockMovement" && r.Action == "Insert" && r.EntityId == movementId, Ct));
    }

    /// <summary>Receives the stock for real, then reports a failure, as if a later step had failed.</summary>
    private sealed class FailingAfterReceive(IStockReceiving inner) : IStockReceiving
    {
        public static readonly Error Error = new("test.failure", "Simulated failure after the stock was written.");

        public async Task<Result> ReceiveAsync(StockReceipt receipt, CancellationToken cancellationToken = default)
        {
            var received = await inner.ReceiveAsync(receipt, cancellationToken);
            Assert.True(received.IsSuccess);
            return Result.Failure(Error);
        }
    }
}
