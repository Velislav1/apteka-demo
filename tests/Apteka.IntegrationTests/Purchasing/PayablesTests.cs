using Apteka.IntegrationTests.Infrastructure;
using Apteka.Purchasing.Application.Corrections;
using Apteka.Purchasing.Application.Deliveries;
using Apteka.Purchasing.Application.Payments;
using Apteka.Purchasing.Domain.Payments;
using Microsoft.Extensions.DependencyInjection;
using static Apteka.IntegrationTests.Purchasing.DeliveryFixtures;

namespace Apteka.IntegrationTests.Purchasing;

[Collection(PostgresCollection.Name)]
public class PayablesTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Owed_amount_has_vat_and_corrections_and_payments_lower_it_until_the_invoice_is_paid()
    {
        await using var provider = await ModuleServices.CreateAsync(postgres.ConnectionString);
        var supplier = await SupplierAsync(provider); // 30 days to pay
        var (product, box, _) = await ProductAsync(provider); // VAT 20 %
        var delivery = await DraftAsync(provider, supplier, Line(product, box, 3, "P1")); // 3 × 4,00 = 12,00 net

        Guid line;
        await using (var scope = provider.CreateAsyncScope())
        {
            Assert.True((await scope.ServiceProvider.GetRequiredService<PostDeliveryHandler>().HandleAsync(delivery, Ct)).IsSuccess);
            line = (await scope.ServiceProvider.GetRequiredService<GetDeliveryHandler>().HandleAsync(delivery, Ct))!.Lines.Single().Id;
            Assert.True((await scope.ServiceProvider.GetRequiredService<CreateCorrectionHandler>()
                .HandleAsync(new CorrectionInput(delivery, "КИ", Today, null, [new CorrectionLineInput(line, -1)]), Ct)).IsSuccess); // −4,00 net
        }

        await using var check = provider.CreateAsyncScope();
        var payables = check.ServiceProvider.GetRequiredService<PayablesHandler>();
        var row = Assert.Single(await payables.ListAsync(supplier, Today.AddDays(45), cancellationToken: Ct));
        Assert.Equal((9.60m, 9.60m, 15, "1–30 дни"), (row.Total, row.Owed, row.DaysOverdue, row.Bucket)); // (12 − 4) × 1,20; due at +30

        Assert.Contains("9.60", (await payables.PayAsync(supplier, Today, 10m, PaymentChannel.Bank, null, null, Ct)).Error.Message);
        Assert.True((await payables.PayAsync(supplier, Today, 5m, PaymentChannel.Bank, "аванс", null, Ct)).IsSuccess);
        Assert.Equal(4.60m, Assert.Single(await payables.ListAsync(supplier, Today, cancellationToken: Ct)).Owed);
        Assert.True((await payables.PayAsync(supplier, Today, 4.60m, PaymentChannel.Cash, null, delivery, Ct)).IsSuccess);

        Assert.Empty(await payables.ListAsync(supplier, Today, cancellationToken: Ct));
        Assert.Equal(0m, Assert.Single(await payables.ListAsync(supplier, Today, onlyUnpaid: false, Ct)).Owed);
        var payments = await payables.ListPaymentsAsync(supplier, Today, Today, Ct);
        Assert.Equal([4.60m, 5.00m], payments.Select(p => p.Amount).Order());
    }
}
