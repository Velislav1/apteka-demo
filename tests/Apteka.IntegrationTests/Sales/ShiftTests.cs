using Apteka.Catalog.Domain.Products;
using Apteka.IntegrationTests.Infrastructure;
using Apteka.IntegrationTests.Purchasing;
using Apteka.Sales.Application.Checkout;
using Apteka.Sales.Application.DailySales;
using Apteka.Sales.Application.Returns;
using Apteka.Sales.Application.Shifts;
using Apteka.Sales.Domain.Sales;
using Apteka.Sales.Domain.Shifts;
using Microsoft.Extensions.DependencyInjection;
using static Apteka.IntegrationTests.Purchasing.DeliveryFixtures;

namespace Apteka.IntegrationTests.Sales;

[Collection(PostgresCollection.Name)]
public class ShiftTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Expected_cash_counts_the_users_cash_sales_refunds_and_movements_and_closing_keeps_the_difference()
    {
        // A user of its own, so other tests' sales do not count in this shift.
        await using var provider = await ModuleServices.CreateAsync(postgres.ConnectionString, TestUser.Manager());
        var (product, box, _) = await StockFixtures.StockedProductAsync(provider, DispensingMode.OverTheCounter, (5, 400)); // 6,00 a box

        await using var scope = provider.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var shifts = services.GetRequiredService<ShiftsHandler>();
        Assert.True((await shifts.OpenAsync(50m, Ct)).IsSuccess);
        Assert.Equal(ShiftErrors.AlreadyOpen, (await shifts.OpenAsync(10m, Ct)).Error);

        var checkout = services.GetRequiredService<CompleteSaleHandler>();
        var cash = (await checkout.HandleAsync(new CheckoutInput([new CheckoutLineInput(product, box, 2)], PaymentMethod.Cash, 20m), Ct)).Value; // 12,00
        Assert.True((await checkout.HandleAsync(new CheckoutInput([new CheckoutLineInput(product, box, 1)], PaymentMethod.Card, null), Ct)).IsSuccess); // 6,00 card
        var line = (await services.GetRequiredService<GetSaleHandler>().HandleAsync(cash.SaleId, Ct))!.Lines.Single().Id;
        Assert.True((await services.GetRequiredService<CreateReturnHandler>().HandleAsync(cash.SaleId, new Dictionary<Guid, int> { [line] = 1 }, null, Ct)).IsSuccess); // −6,00 cash
        Assert.True((await shifts.MoveCashAsync(-10m, "плащане на куриер", Ct)).IsSuccess);

        var open = (await shifts.GetCurrentAsync(Ct))!;
        Assert.Equal((12.00m, 6.00m, 6.00m, 10.00m), (open.CashSales, open.CardSales, open.CashRefunds, open.CashOut));
        Assert.Equal(46.00m, open.ExpectedCash); // 50 + 12 − 6 − 10

        Assert.True((await shifts.CloseAsync(45.50m, "липсват 0,50", Ct)).IsSuccess);
        Assert.Null(await shifts.GetCurrentAsync(Ct));
        var closed = (await shifts.ListAsync(Ct)).First(s => s.Id == open.Id);
        Assert.Equal((46.00m, 45.50m, -0.50m), (closed.ExpectedCash, closed.CountedCash!.Value, closed.Difference!.Value));
        Assert.Equal(ShiftErrors.NoOpenShift, (await shifts.MoveCashAsync(1m, "x", Ct)).Error);
    }
}
