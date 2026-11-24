using Apteka.Catalog.Domain.Products;
using Apteka.IntegrationTests.Infrastructure;
using Apteka.Purchasing.Application.Corrections;
using Apteka.Purchasing.Application.Deliveries;
using Apteka.Purchasing.Infrastructure;
using Apteka.Sales.Application.Checkout;
using Apteka.Sales.Domain.Sales;
using Apteka.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using static Apteka.IntegrationTests.Purchasing.DeliveryFixtures;

namespace Apteka.IntegrationTests.Purchasing;

[Collection(PostgresCollection.Name)]
public class CorrectionTests(PostgresFixture postgres)
{
    private static async Task<(Guid DeliveryId, Guid LineId, Guid ProductId, Guid BoxId)> PostedAsync(IServiceProvider provider, int boxes)
    {
        var supplier = await SupplierAsync(provider);
        var (product, box, _) = await ProductAsync(provider);
        var id = await DraftAsync(provider, supplier, Line(product, box, boxes, "K1"));

        await using var scope = provider.CreateAsyncScope();
        Assert.True((await scope.ServiceProvider.GetRequiredService<PostDeliveryHandler>().HandleAsync(id, Ct)).IsSuccess);
        var view = (await scope.ServiceProvider.GetRequiredService<GetDeliveryHandler>().HandleAsync(id, Ct))!;
        return (id, view.Lines.Single().Id, product, box);
    }

    private static async Task<Result<Guid>> CorrectAsync(IServiceProvider provider, Guid deliveryId, Guid lineId, int delta)
    {
        await using var scope = provider.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<CreateCorrectionHandler>()
            .HandleAsync(new CorrectionInput(deliveryId, "КИ-1", Today, "върната стока", [new CorrectionLineInput(lineId, delta)]), Ct);
    }

    [Fact]
    public async Task A_credit_note_takes_the_boxes_out_of_the_batch_and_is_listed_with_the_delivery()
    {
        await using var provider = await ModuleServices.CreateAsync(postgres.ConnectionString);
        var (delivery, line, product, _) = await PostedAsync(provider, 3);

        var created = await CorrectAsync(provider, delivery, line, -1);

        Assert.True(created.IsSuccess, created.IsFailure ? created.Error.Message : null);
        Assert.Equal(40, await StockFixtures.StockAsync(provider, product));

        await using var scope = provider.CreateAsyncScope();
        var view = (await scope.ServiceProvider.GetRequiredService<GetCorrectionHandler>().HandleAsync(created.Value, Ct))!;
        Assert.True(view.Number > 0);
        Assert.Equal(-4.00m, view.PurchaseTotal);
        Assert.Equal(("K1", -1), (view.Lines.Single().BatchNo, view.Lines.Single().QuantityDelta));

        var ofDelivery = await scope.ServiceProvider.GetRequiredService<DeliveryCorrectionsHandler>().HandleAsync(delivery, Ct);
        Assert.Equal(-1, ofDelivery.QuantityByLine[line]);
        var listed = await scope.ServiceProvider.GetRequiredService<ListCorrectionsHandler>().HandleAsync(Today.AddDays(-2), Today.AddDays(1), Ct);
        Assert.Contains(listed, c => c.Id == created.Value && c.DeliveryId == delivery);
    }

    [Fact]
    public async Task Taking_back_more_than_received_or_what_was_already_sold_is_refused()
    {
        await using var provider = await ModuleServices.CreateAsync(postgres.ConnectionString);
        var (delivery, line, product, box) = await PostedAsync(provider, 2);

        Assert.Equal("purchasing.correction_below_zero", (await CorrectAsync(provider, delivery, line, -3)).Error.Code);

        await using (var scope = provider.CreateAsyncScope())
        {
            var sold = await scope.ServiceProvider.GetRequiredService<CompleteSaleHandler>()
                .HandleAsync(new CheckoutInput([new CheckoutLineInput(product, box, 2)], PaymentMethod.Card, null), Ct);
            Assert.True(sold.IsSuccess);
        }

        var refused = await CorrectAsync(provider, delivery, line, -1);
        Assert.Equal("inventory.correction_below_zero", refused.Error.Code);
        Assert.Contains("K1", refused.Error.Message);
        Assert.Empty((await provider.CreateAsyncScope().ServiceProvider.GetRequiredService<DeliveryCorrectionsHandler>().HandleAsync(delivery, Ct)).Items);

        Assert.True((await CorrectAsync(provider, delivery, line, 1)).IsSuccess); // debit note: one more box arrived
        Assert.Equal(20, await StockFixtures.StockAsync(provider, product));
    }

    [Fact]
    public async Task A_saved_correction_cannot_be_changed_or_deleted()
    {
        await using var provider = await ModuleServices.CreateAsync(postgres.ConnectionString);
        var (delivery, line, _, _) = await PostedAsync(provider, 2);
        var id = (await CorrectAsync(provider, delivery, line, -1)).Value;

        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<PurchasingDbContext>();
        var update = await Assert.ThrowsAsync<PostgresException>(() =>
            context.Database.ExecuteSqlAsync($"UPDATE purchasing.delivery_correction_lines SET quantity_delta = -2 WHERE correction_id = {id}", Ct));
        var delete = await Assert.ThrowsAsync<PostgresException>(() =>
            context.Database.ExecuteSqlAsync($"DELETE FROM purchasing.delivery_corrections WHERE id = {id}", Ct));
        Assert.Contains("append-only", update.MessageText, StringComparison.Ordinal);
        Assert.Contains("append-only", delete.MessageText, StringComparison.Ordinal);
    }
}
