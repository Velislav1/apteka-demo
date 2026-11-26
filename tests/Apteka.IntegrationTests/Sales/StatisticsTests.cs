using Apteka.Catalog.Domain.Products;
using Apteka.Inventory.Application.WriteOffs;
using Apteka.Inventory.Contracts;
using Apteka.IntegrationTests.Infrastructure;
using Apteka.IntegrationTests.Purchasing;
using Apteka.Sales.Application.Checkout;
using Apteka.Sales.Application.DailySales;
using Apteka.Sales.Application.Returns;
using Apteka.Sales.Application.Statistics;
using Apteka.Sales.Domain.Sales;
using Apteka.SharedKernel;
using Microsoft.Extensions.DependencyInjection;
using static Apteka.IntegrationTests.Purchasing.DeliveryFixtures;

namespace Apteka.IntegrationTests.Sales;

[Collection(PostgresCollection.Name)]
public class StatisticsTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Turnover_balances_and_statistics_subtract_returns_with_profit_and_abc()
    {
        await using var provider = await ModuleServices.CreateAsync(postgres.ConnectionString);
        var (product, box, tablet) = await StockFixtures.StockedProductAsync(provider, DispensingMode.OverTheCounter, (3, 400)); // 60 at 0,20 cost, 6,00 a box
        var today = BulgarianTime.Today(DateTimeOffset.UtcNow);

        await using var scope = provider.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var sale = (await services.GetRequiredService<CompleteSaleHandler>()
            .HandleAsync(new CheckoutInput([new CheckoutLineInput(product, box, 2)], PaymentMethod.Card, null), Ct)).Value;
        var line = (await services.GetRequiredService<GetSaleHandler>().HandleAsync(sale.SaleId, Ct))!.Lines.Single().Id;
        Assert.True((await services.GetRequiredService<CreateReturnHandler>().HandleAsync(sale.SaleId, new Dictionary<Guid, int> { [line] = 1 }, null, Ct)).IsSuccess);
        var batch = (await services.GetRequiredService<IStockQueries>().GetBatchesAsync(product, null, Ct)).Single();
        Assert.True((await services.GetRequiredService<CreateWriteOffHandler>()
            .HandleAsync(new WriteOffInput(null, [new WriteOffLineInput(product, tablet, 5, batch.BatchId)]), Ct)).IsSuccess);

        var turnover = Assert.Single(await services.GetRequiredService<IStockQueries>()
            .GetTurnoverAsync([product], BulgarianTime.StartOfDayUtc(today), BulgarianTime.StartOfDayUtc(today.AddDays(1)), Ct));
        Assert.Equal((0, 60, 20, 5, 35), (turnover.Opening, turnover.Received, turnover.Sold, turnover.WrittenOff, turnover.Closing));
        Assert.Equal(7.00m, turnover.ClosingValue); // 35 × 0,20

        var row = (await services.GetRequiredService<SalesStatisticsHandler>().HandleAsync(today, today, StatisticsView.Products, Ct))
            .Single(r => r.Key == product.ToString());
        Assert.Equal((20, 6.00m, 5.00m, 4.00m, 1.00m), (row.BaseQuantity!.Value, row.Total, row.Net, row.Cost, row.Profit)); // 1 box kept: 6,00 with 20 % VAT
        Assert.Equal((25.0m, 20.0m), (row.MarkupPercent!.Value, row.MarginPercent!.Value));
        Assert.NotNull(row.AbcClass);
    }
}
