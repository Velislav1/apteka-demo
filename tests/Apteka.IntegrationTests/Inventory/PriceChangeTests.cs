using Apteka.Catalog.Domain.Products;
using Apteka.Inventory.Application.PriceChanges;
using Apteka.Inventory.Contracts;
using Apteka.Inventory.Domain.PriceChanges;
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

[Collection(PostgresCollection.Name)]
public class PriceChangeTests(PostgresFixture postgres)
{
    [Fact]
    public async Task New_prices_by_percent_or_markup_are_rounded_applied_to_the_batch_and_sold_at_the_till()
    {
        await using var provider = await ModuleServices.CreateAsync(postgres.ConnectionString);
        var (product, box, tablet) = await StockFixtures.StockedProductAsync(provider, DispensingMode.OverTheCounter, (2, 400)); // 4,00 a box net, 6,00 sale, VAT 20 %

        await using var scope = provider.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var prices = services.GetRequiredService<PriceChangesHandler>();

        var byPercent = Assert.Single(await prices.ProposeAsync(new PriceProposal(null, [product], PriceMethod.Percent, 7m, 0.05m), Ct));
        Assert.Equal((6.00m, 6.40m), (byPercent.OldPrice, byPercent.NewPrice)); // 6,42 → 6,40 by 0,05
        var byMarkup = Assert.Single(await prices.ProposeAsync(new PriceProposal(null, [product], PriceMethod.Markup, 30m), Ct));
        Assert.Equal(6.24m, byMarkup.NewPrice); // 4,00 × 1,30 × 1,20
        Assert.Equal(25.0m, byMarkup.MarkupOf(byMarkup.OldPrice));

        var applied = await prices.ApplyAsync("нова цена", new Dictionary<Guid, decimal> { [byMarkup.BatchId] = 6.24m }, Ct);
        Assert.True(applied.IsSuccess, applied.IsFailure ? applied.Error.Message : null);
        Assert.Equal(PriceChangeErrors.NothingChanged, (await prices.ApplyAsync(null, new Dictionary<Guid, decimal> { [byMarkup.BatchId] = 6.24m }, Ct)).Error);

        var batch = (await services.GetRequiredService<IStockQueries>().GetBatchAsync(byMarkup.BatchId, Ct))!;
        Assert.Equal((6.24m, 0.3120m), (batch.PricedUnitSalePrice, batch.SalePricePerBaseUnit));
        var sold = await services.GetRequiredService<CompleteSaleHandler>()
            .HandleAsync(new CheckoutInput([new CheckoutLineInput(product, box, 1), new CheckoutLineInput(product, tablet, 2)], PaymentMethod.Card, null), Ct);
        Assert.Equal(6.24m + 0.62m, sold.Value.Total);

        var history = await prices.ListAsync(Ct);
        Assert.Contains(history, h => h.Number == applied.Value && h.Reason == "нова цена" && h.LineCount == 1);
        var context = services.GetRequiredService<InventoryDbContext>();
        var update = await Assert.ThrowsAsync<PostgresException>(() =>
            context.Database.ExecuteSqlAsync($"UPDATE inventory.price_change_lines SET new_price = 1 WHERE batch_id = {byMarkup.BatchId}", Ct));
        Assert.Contains("append-only", update.MessageText, StringComparison.Ordinal);
    }
}
