using Apteka.Catalog.Domain.Products;
using Apteka.IntegrationTests.Infrastructure;
using Apteka.IntegrationTests.Purchasing;
using Apteka.Sales.Application.Checkout;
using Apteka.Sales.Application.DailySales;
using Apteka.Sales.Application.Returns;
using Apteka.Sales.Application.Reversals;
using Apteka.Sales.Domain.Returns;
using Apteka.Sales.Domain.Sales;
using Apteka.Sales.Infrastructure;
using Apteka.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using static Apteka.IntegrationTests.Purchasing.DeliveryFixtures;

namespace Apteka.IntegrationTests.Sales;

[Collection(PostgresCollection.Name)]
public class SaleReturnTests(PostgresFixture postgres)
{
    private static async Task<(CompletedSale Sale, Guid LineId, Guid ProductId)> SoldAsync(IServiceProvider provider, PaymentMethod payment)
    {
        // Two batches of one box each: the sale of 2 boxes takes one from each (FEFO).
        var (product, box, _) = await StockFixtures.StockedProductAsync(provider, DispensingMode.OverTheCounter, (1, 100), (2, 400));
        await using var scope = provider.CreateAsyncScope();
        var sold = await scope.ServiceProvider.GetRequiredService<CompleteSaleHandler>()
            .HandleAsync(new CheckoutInput([new CheckoutLineInput(product, box, 2)], payment, payment == PaymentMethod.Cash ? 20m : null), Ct);
        Assert.True(sold.IsSuccess, sold.IsFailure ? sold.Error.Message : null);
        var view = (await scope.ServiceProvider.GetRequiredService<GetSaleHandler>().HandleAsync(sold.Value.SaleId, Ct))!;
        return (sold.Value, view.Lines.Single().Id, product);
    }

    [Fact]
    public async Task A_correction_gives_back_one_box_into_the_later_batch_and_the_day_counts_the_refund()
    {
        await using var provider = await ModuleServices.CreateAsync(postgres.ConnectionString);
        var (sale, line, product) = await SoldAsync(provider, PaymentMethod.Cash);
        var today = BulgarianTime.Today(DateTimeOffset.UtcNow);
        decimal cashBefore;
        await using (var scope = provider.CreateAsyncScope())
        {
            cashBefore = (await scope.ServiceProvider.GetRequiredService<DailySalesHandler>().HandleAsync(today, Ct)).CashTotal;
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var done = await scope.ServiceProvider.GetRequiredService<CreateReturnHandler>().HandleAsync(sale.SaleId, new Dictionary<Guid, int> { [line] = 1 }, "рекламация", Ct);
            Assert.True(done.IsSuccess, done.IsFailure ? done.Error.Message : null);
            Assert.Equal((6.00m, PaymentMethod.Cash), (done.Value.Total, done.Value.PaymentMethod));
        }

        Assert.Equal(60 - 40 + 20, await StockFixtures.StockAsync(provider, product));
        await using (var scope = provider.CreateAsyncScope())
        {
            var batches = await scope.ServiceProvider.GetRequiredService<Apteka.Inventory.Contracts.IStockQueries>().GetBatchesAsync(product, null, Ct);
            Assert.Equal([("B2", 40)], batches.Select(b => (b.BatchNo, b.BaseQuantity))); // B1 was sold out; the box went back to B2

            var view = (await scope.ServiceProvider.GetRequiredService<GetSaleHandler>().HandleAsync(sale.SaleId, Ct))!;
            Assert.Equal(1, view.Lines.Single().ReturnedQuantity);
            Assert.Equal(6.00m, view.NetTotal);

            var day = await scope.ServiceProvider.GetRequiredService<DailySalesHandler>().HandleAsync(today, Ct);
            Assert.Equal(cashBefore - 6.00m, day.CashTotal);
            Assert.Contains(day.Returns!, r => r.SaleId == sale.SaleId && r.Total == 6.00m);

            var report = await scope.ServiceProvider.GetRequiredService<DailyReportHandler>().HandleAsync(today, Ct);
            var minus = report.Sales.Single(s => s.IsReturn && s.Number == sale.Number);
            Assert.Equal((-6.00m, -4.00m), (minus.Total, minus.Cost));

            Assert.Equal(ReturnErrors.HasReturns, (await scope.ServiceProvider.GetRequiredService<ReverseSaleHandler>().HandleAsync(sale.SaleId, null, Ct)).Error);
            Assert.Contains("най-много 1", (await scope.ServiceProvider.GetRequiredService<CreateReturnHandler>()
                .HandleAsync(sale.SaleId, new Dictionary<Guid, int> { [line] = 2 }, null, Ct)).Error.Message);
        }
    }

    [Fact]
    public async Task Only_a_manager_corrects_a_sale_and_a_saved_correction_never_changes()
    {
        await using var provider = await ModuleServices.CreateAsync(postgres.ConnectionString);
        var (sale, line, _) = await SoldAsync(provider, PaymentMethod.Card);

        await using (var pharmacist = await ModuleServices.CreateAsync(postgres.ConnectionString, TestUser.Pharmacist()))
        await using (var scope = pharmacist.CreateAsyncScope())
        {
            var refused = await scope.ServiceProvider.GetRequiredService<CreateReturnHandler>().HandleAsync(sale.SaleId, new Dictionary<Guid, int> { [line] = 1 }, null, Ct);
            Assert.Equal(ReturnErrors.ManagersOnly, refused.Error);
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var done = await scope.ServiceProvider.GetRequiredService<CreateReturnHandler>().HandleAsync(sale.SaleId, new Dictionary<Guid, int> { [line] = 2 }, null, Ct);
            Assert.True(done.IsSuccess);
            Assert.Equal(12.00m, done.Value.Total);

            var context = scope.ServiceProvider.GetRequiredService<SalesDbContext>();
            var update = await Assert.ThrowsAsync<PostgresException>(() =>
                context.Database.ExecuteSqlAsync($"UPDATE sales.sale_returns SET total = 0 WHERE id = {done.Value.ReturnId}", Ct));
            Assert.Contains("append-only", update.MessageText, StringComparison.Ordinal);

            var number = (await scope.ServiceProvider.GetRequiredService<FindSaleHandler>().HandleAsync(sale.Number, Ct))!.Value;
            Assert.Equal(sale.SaleId, number);
        }
    }
}
