using Apteka.Catalog.Domain.Products;
using Apteka.Inventory.Contracts;
using Apteka.IntegrationTests.Infrastructure;
using Apteka.IntegrationTests.Purchasing;
using Apteka.Sales.Application.Checkout;
using Apteka.Sales.Application.DailySales;
using Apteka.Sales.Application.Reversals;
using Apteka.Sales.Domain.Sales;
using Apteka.SharedKernel;
using Microsoft.Extensions.DependencyInjection;
using static Apteka.IntegrationTests.Purchasing.DeliveryFixtures;

namespace Apteka.IntegrationTests.Sales;

[Collection(PostgresCollection.Name)]
public class ReportTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Product_card_lists_receipt_sale_and_reversal_with_the_balance_after_each()
    {
        await using var provider = await ModuleServices.CreateAsync(postgres.ConnectionString);
        var (product, box, tablet) = await StockFixtures.StockedProductAsync(provider, DispensingMode.OverTheCounter, (2, 400));
        Guid saleId;

        await using (var scope = provider.CreateAsyncScope())
        {
            saleId = (await scope.ServiceProvider.GetRequiredService<CompleteSaleHandler>()
                .HandleAsync(new CheckoutInput([new CheckoutLineInput(product, tablet, 5)], PaymentMethod.Card, null), Ct)).Value.SaleId;
            Assert.True((await scope.ServiceProvider.GetRequiredService<ReverseSaleHandler>().HandleAsync(saleId, null, Ct)).IsSuccess);
        }

        await using var check = provider.CreateAsyncScope();
        var card = await check.ServiceProvider.GetRequiredService<IStockQueries>()
            .GetProductCardAsync(product, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1), Ct);

        Assert.Equal(0, card.OpeningBaseQuantity);
        Assert.Equal(["Delivery", "Sale", "Reversal"], card.Movements.Select(m => m.Kind));
        Assert.Equal([40, 35, 40], card.Movements.Select(m => m.BalanceAfter));
        Assert.Equal(40, card.ClosingBaseQuantity);

        var later = await check.ServiceProvider.GetRequiredService<IStockQueries>()
            .GetProductCardAsync(product, DateTimeOffset.UtcNow.AddDays(1), DateTimeOffset.UtcNow.AddDays(2), Ct);
        Assert.Equal(40, later.OpeningBaseQuantity);
        Assert.Empty(later.Movements);
    }

    [Fact]
    public async Task Daily_report_counts_profit_from_the_batch_purchase_prices_and_skips_reversed_sales()
    {
        await using var provider = await ModuleServices.CreateAsync(postgres.ConnectionString);

        // Purchase 4,00 € a box of 20 (0,20 € a tablet) without VAT; sale 6,00 € a box with 20 % VAT.
        var (product, box, _) = await StockFixtures.StockedProductAsync(provider, DispensingMode.OverTheCounter, (5, 400));
        Guid kept, reversed;
        await using (var scope = provider.CreateAsyncScope())
        {
            var sell = scope.ServiceProvider.GetRequiredService<CompleteSaleHandler>();
            kept = (await sell.HandleAsync(new CheckoutInput([new CheckoutLineInput(product, box, 2)], PaymentMethod.Card, null), Ct)).Value.SaleId;
            reversed = (await sell.HandleAsync(new CheckoutInput([new CheckoutLineInput(product, box, 1)], PaymentMethod.Card, null), Ct)).Value.SaleId;
            Assert.True((await scope.ServiceProvider.GetRequiredService<ReverseSaleHandler>().HandleAsync(reversed, null, Ct)).IsSuccess);
        }

        await using var check = provider.CreateAsyncScope();
        var report = await check.ServiceProvider.GetRequiredService<DailyReportHandler>()
            .HandleAsync(BulgarianTime.Today(DateTimeOffset.UtcNow), Ct);

        var sale = report.Sales.Single(s => s.Id == kept);
        Assert.Equal(12.00m, sale.Total);
        Assert.Equal(10.00m, sale.NetRevenue); // 12,00 − 2,00 VAT
        Assert.Equal(8.00m, sale.Cost);        // 40 tablets × 0,20
        Assert.Equal(2.00m, sale.Profit);
        Assert.Equal(25.0m, sale.ProfitPercent);

        Assert.Equal(SaleStatus.Reversed, report.Sales.Single(s => s.Id == reversed).Status);
        Assert.True(report.Profit >= 2.00m);
    }
}
