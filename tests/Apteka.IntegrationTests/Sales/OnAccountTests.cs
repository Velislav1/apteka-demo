using Apteka.Catalog.Domain.Products;
using Apteka.IntegrationTests.Infrastructure;
using Apteka.IntegrationTests.Purchasing;
using Apteka.Sales.Application.Checkout;
using Apteka.Sales.Application.Customers;
using Apteka.Sales.Application.DailySales;
using Apteka.Sales.Application.Receivables;
using Apteka.Sales.Application.Returns;
using Apteka.Sales.Application.Shifts;
using Apteka.Sales.Domain.CustomerPayments;
using Apteka.Sales.Domain.Customers;
using Apteka.Sales.Domain.Sales;
using Apteka.SharedKernel;
using Microsoft.Extensions.DependencyInjection;
using static Apteka.IntegrationTests.Purchasing.DeliveryFixtures;

namespace Apteka.IntegrationTests.Sales;

[Collection(PostgresCollection.Name)]
public class OnAccountTests(PostgresFixture postgres)
{
    [Fact]
    public async Task A_sale_on_account_is_owed_until_paid_corrections_lower_it_and_cash_payments_go_into_the_shift()
    {
        await using var provider = await ModuleServices.CreateAsync(postgres.ConnectionString, TestUser.Manager());
        var (product, box, _) = await StockFixtures.StockedProductAsync(provider, DispensingMode.OverTheCounter, (5, 400)); // 6,00 a box
        var today = BulgarianTime.Today(DateTimeOffset.UtcNow);

        await using var scope = provider.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var customer = (await services.GetRequiredService<CustomersHandler>()
            .CreateAsync(new CustomerDetails(Apteka.IntegrationTests.Catalog.CatalogInputs.UniqueName("Отложено"), null, null, null), Ct)).Value;
        var checkout = services.GetRequiredService<CompleteSaleHandler>();

        Assert.Equal(SalesErrors.OnAccountNeedsCustomer, (await checkout.HandleAsync(
            new CheckoutInput([new CheckoutLineInput(product, box, 1)], PaymentMethod.OnAccount, null, null, 30), Ct)).Error);

        Assert.True((await services.GetRequiredService<ShiftsHandler>().OpenAsync(0m, Ct)).IsSuccess);
        var sale = (await checkout.HandleAsync(new CheckoutInput([new CheckoutLineInput(product, box, 3)], PaymentMethod.OnAccount, null, customer, 10), Ct)).Value; // 18,00

        var receivables = services.GetRequiredService<ReceivablesHandler>();
        var row = Assert.Single(await receivables.ListAsync(customer, today.AddDays(15), cancellationToken: Ct));
        Assert.Equal((18.00m, 18.00m, today.AddDays(10), 5), (row.Total, row.Owed, row.DueDate, row.DaysOverdue));

        var line = (await services.GetRequiredService<GetSaleHandler>().HandleAsync(sale.SaleId, Ct))!.Lines.Single().Id;
        var back = await services.GetRequiredService<CreateReturnHandler>().HandleAsync(sale.SaleId, new Dictionary<Guid, int> { [line] = 1 }, null, Ct);
        Assert.Equal(PaymentMethod.OnAccount, back.Value.PaymentMethod);
        Assert.Equal(12.00m, Assert.Single(await receivables.ListAsync(customer, today, cancellationToken: Ct)).Owed);

        Assert.True((await receivables.PayAsync(customer, today, 5m, CustomerPaymentChannel.Cash, null, null, Ct)).IsSuccess);
        Assert.Contains("7.00", (await receivables.PayAsync(customer, today, 8m, CustomerPaymentChannel.Card, null, null, Ct)).Error.Message);
        Assert.True((await receivables.PayAsync(customer, today, 7m, CustomerPaymentChannel.Card, null, sale.SaleId, Ct)).IsSuccess);
        Assert.Empty(await receivables.ListAsync(customer, today, cancellationToken: Ct));

        var day = await services.GetRequiredService<DailySalesHandler>().HandleAsync(today, Ct);
        Assert.True(day.OnAccountTotal >= 12.00m);
        Assert.Contains(day.Sales, s => s.Id == sale.SaleId && s.PaymentMethod == PaymentMethod.OnAccount);

        var shift = (await services.GetRequiredService<ShiftsHandler>().GetCurrentAsync(Ct))!;
        Assert.Equal(5.00m, shift.CashSales); // only the cash the customer brought
        Assert.Equal(2, (await receivables.ListPaymentsAsync(customer, today, today, Ct)).Count);
    }
}
