using Apteka.IntegrationTests.Infrastructure;
using Apteka.Purchasing.Application.Consignments;
using Apteka.Purchasing.Application.Corrections;
using Apteka.Purchasing.Application.Deliveries;
using Apteka.Purchasing.Application.Payments;
using Apteka.Purchasing.Domain.Payments;
using Apteka.Sales.Application.Checkout;
using Apteka.Sales.Domain.Sales;
using Microsoft.Extensions.DependencyInjection;
using static Apteka.IntegrationTests.Purchasing.DeliveryFixtures;

namespace Apteka.IntegrationTests.Purchasing;

[Collection(PostgresCollection.Name)]
public class ConsignmentTests(PostgresFixture postgres)
{
    [Fact]
    public async Task A_consignment_is_owed_only_for_what_left_the_shelf_and_the_rest_goes_back()
    {
        await using var provider = await ModuleServices.CreateAsync(postgres.ConnectionString);
        var supplier = await SupplierAsync(provider);
        var (product, box, tablet) = await ProductAsync(provider); // VAT 20 %, 20 tablets a box

        Guid delivery;
        await using (var scope = provider.CreateAsyncScope())
        {
            var saved = await scope.ServiceProvider.GetRequiredService<SaveDeliveryDraftHandler>().HandleAsync(
                null, new DeliveryInput(supplier, "К-" + Guid.NewGuid().ToString("N")[..8], Today, [Line(product, box, 3, "K1")], IsConsignment: true), Ct);
            Assert.True(saved.IsSuccess, saved.IsFailure ? saved.Error.Message : null);
            delivery = saved.Value;
            Assert.True((await scope.ServiceProvider.GetRequiredService<PostDeliveryHandler>().HandleAsync(delivery, Ct)).IsSuccess);
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            Assert.True((await scope.ServiceProvider.GetRequiredService<GetDeliveryHandler>().HandleAsync(delivery, Ct))!.IsConsignment);
            Assert.Empty(await scope.ServiceProvider.GetRequiredService<PayablesHandler>().ListAsync(supplier, Today, cancellationToken: Ct)); // nothing sold yet
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var sold = await scope.ServiceProvider.GetRequiredService<CompleteSaleHandler>()
                .HandleAsync(new CheckoutInput([new CheckoutLineInput(product, box, 1), new CheckoutLineInput(product, tablet, 5)], PaymentMethod.Card, null), Ct);
            Assert.True(sold.IsSuccess, sold.IsFailure ? sold.Error.Message : null);
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var line = (await scope.ServiceProvider.GetRequiredService<GetDeliveryHandler>().HandleAsync(delivery, Ct))!.Lines.Single().Id;
            Assert.True((await scope.ServiceProvider.GetRequiredService<CreateCorrectionHandler>()
                .HandleAsync(new CorrectionInput(delivery, null, Today, "връщане на консигнация", [new CorrectionLineInput(line, -1)]), Ct)).IsSuccess);
        }

        await using var check = provider.CreateAsyncScope();
        var report = await check.ServiceProvider.GetRequiredService<ConsignmentReportHandler>().HandleAsync(supplier, onlyOpen: true, Ct);
        var row = Assert.Single(report.Rows);
        Assert.Equal((60, 20, 25, 0, 15, 25), (row.Received, row.Returned, row.Sold, row.WrittenOff, row.OnHand, row.Chargeable));
        Assert.Equal(6.00m, row.Owed); // 1,25 boxes × 4,00 net × 1,20

        var payables = check.ServiceProvider.GetRequiredService<PayablesHandler>();
        Assert.Equal(6.00m, Assert.Single(await payables.ListAsync(supplier, Today, cancellationToken: Ct)).Owed);
        Assert.True((await payables.PayAsync(supplier, Today, 6.00m, PaymentChannel.Cash, null, delivery, Ct)).IsSuccess);

        var after = await check.ServiceProvider.GetRequiredService<ConsignmentReportHandler>().HandleAsync(supplier, onlyOpen: true, Ct);
        Assert.Equal((6.00m, 6.00m, 0m), (after.Owed, after.Paid, after.Remaining));
        Assert.Single(after.Rows); // 15 tablets still on the shelf: open until returned or sold
    }
}
