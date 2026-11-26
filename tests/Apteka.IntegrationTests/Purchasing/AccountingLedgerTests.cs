using Apteka.IntegrationTests.Infrastructure;
using Apteka.Purchasing.Application.Accounting;
using Apteka.Purchasing.Application.Corrections;
using Apteka.Purchasing.Application.Deliveries;
using Apteka.Sales.Application.Accounting;
using Apteka.Sales.Application.Checkout;
using Apteka.Sales.Application.Reversals;
using Apteka.Sales.Domain.Sales;
using Apteka.SharedKernel;
using Microsoft.Extensions.DependencyInjection;
using static Apteka.IntegrationTests.Purchasing.DeliveryFixtures;

namespace Apteka.IntegrationTests.Purchasing;

[Collection(PostgresCollection.Name)]
public class AccountingLedgerTests(PostgresFixture postgres)
{
    [Fact]
    public async Task The_purchase_ledger_has_the_invoice_and_its_credit_note_split_by_vat()
    {
        await using var provider = await ModuleServices.CreateAsync(postgres.ConnectionString);
        var supplier = await SupplierAsync(provider);
        var (product, box, _) = await ProductAsync(provider); // VAT 20 %
        var delivery = await DraftAsync(provider, supplier, Line(product, box, 3, "L1")); // 12,00 net

        long number;
        await using (var scope = provider.CreateAsyncScope())
        {
            Assert.True((await scope.ServiceProvider.GetRequiredService<PostDeliveryHandler>().HandleAsync(delivery, Ct)).IsSuccess);
            var view = (await scope.ServiceProvider.GetRequiredService<GetDeliveryHandler>().HandleAsync(delivery, Ct))!;
            number = view.Number;
            Assert.True((await scope.ServiceProvider.GetRequiredService<CreateCorrectionHandler>()
                .HandleAsync(new CorrectionInput(delivery, "КИ-7", Today, null, [new CorrectionLineInput(view.Lines.Single().Id, -1)]), Ct)).IsSuccess);
        }

        await using var check = provider.CreateAsyncScope();
        var rows = (await check.ServiceProvider.GetRequiredService<PurchaseLedgerHandler>().HandleAsync(Today, Today, Ct))
            .Where(r => r.DeliveryNumber == number)
            .ToList();

        Assert.Equal(2, rows.Count);
        var invoice = rows.Single(r => r.Kind == "Фактура");
        Assert.Equal((20m, 12.00m, 2.40m, 14.40m), (invoice.VatRate, invoice.TaxBase, invoice.Vat, invoice.Total));
        var note = rows.Single(r => r.Kind == "Кредитно известие");
        Assert.Equal(("КИ-7", -4.00m, -0.80m), (note.DocumentNo, note.TaxBase, note.Vat));
        Assert.Matches("^[0-9]{9}([0-9]{4})?$", invoice.SupplierEik);
    }

    [Fact]
    public async Task The_sales_ledger_counts_a_reversal_as_a_negative_document_of_its_day()
    {
        await using var provider = await ModuleServices.CreateAsync(postgres.ConnectionString);
        var (product, box, _) = await StockFixtures.StockedProductAsync(provider, Apteka.Catalog.Domain.Products.DispensingMode.OverTheCounter, (3, 400));
        var today = BulgarianTime.Today(DateTimeOffset.UtcNow);

        async Task<IReadOnlyList<SalesLedgerRow>> LedgerAsync()
        {
            await using var scope = provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<SalesLedgerHandler>().HandleAsync(today, today, Ct);
        }

        static (int Documents, decimal Base, decimal Vat) Of(IReadOnlyList<SalesLedgerRow> rows, SalesLedgerKind kind) =>
            rows.Where(r => r.Kind == kind && r.VatRate == 20m).Aggregate((0, 0m, 0m), (sum, r) => (sum.Item1 + r.Documents, sum.Item2 + r.TaxBase, sum.Item3 + r.Vat));

        var before = await LedgerAsync();
        await using (var scope = provider.CreateAsyncScope())
        {
            var sold = await scope.ServiceProvider.GetRequiredService<CompleteSaleHandler>()
                .HandleAsync(new CheckoutInput([new CheckoutLineInput(product, box, 1)], PaymentMethod.Card, null), Ct);
            Assert.True(sold.IsSuccess, sold.IsFailure ? sold.Error.Message : null);
            Assert.True((await scope.ServiceProvider.GetRequiredService<ReverseSaleHandler>().HandleAsync(sold.Value.SaleId, "грешка", Ct)).IsSuccess);
        }

        var after = await LedgerAsync();
        var (saleDocs, saleBase, saleVat) = Of(after, SalesLedgerKind.Sale);
        var (saleDocsBefore, saleBaseBefore, saleVatBefore) = Of(before, SalesLedgerKind.Sale);
        Assert.Equal((1, 5.00m, 1.00m), (saleDocs - saleDocsBefore, saleBase - saleBaseBefore, saleVat - saleVatBefore)); // 6,00 with VAT
        var (revDocs, revBase, revVat) = Of(after, SalesLedgerKind.Reversal);
        var (revDocsBefore, revBaseBefore, revVatBefore) = Of(before, SalesLedgerKind.Reversal);
        Assert.Equal((1, -5.00m, -1.00m), (revDocs - revDocsBefore, revBase - revBaseBefore, revVat - revVatBefore));
    }
}
