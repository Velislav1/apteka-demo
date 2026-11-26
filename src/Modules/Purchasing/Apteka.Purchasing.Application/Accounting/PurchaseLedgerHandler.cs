using Apteka.Catalog.Contracts;

namespace Apteka.Purchasing.Application.Accounting;

/// <summary>
/// „Дневник на покупките“ for the accountant: every posted supplier invoice (by its date) and every correction
/// (credit or debit note, by the note's date) in the period, split by VAT rate. Amounts at the net purchase prices.
/// </summary>
public sealed class PurchaseLedgerHandler(IPurchaseLedgerQueries queries, ICatalogLookup catalog)
{
    public async Task<IReadOnlyList<PurchaseLedgerRow>> HandleAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        var documents = await queries.ListAsync(from, to, cancellationToken);
        var vat = (await catalog.GetProductsAsync([.. documents.SelectMany(d => d.Lines).Select(l => l.ProductId).Distinct()], cancellationToken))
            .ToDictionary(p => p.ProductId, p => p.VatRate);

        return documents
            .SelectMany(d => d.Lines
                .GroupBy(l => vat.GetValueOrDefault(l.ProductId))
                .Select(g =>
                {
                    var taxBase = Round(g.Sum(l => l.NetAmount));
                    var tax = Round(taxBase * g.Key / 100m);
                    var kind = d.IsCorrection ? (d.Lines.Sum(l => l.NetAmount) < 0 ? "Кредитно известие" : "Дебитно известие") : "Фактура";
                    return new PurchaseLedgerRow(d.Date, kind, d.DocumentNo, d.SupplierName, d.SupplierEik, d.DeliveryNumber, d.IsConsignment, g.Key, taxBase, tax, taxBase + tax);
                }))
            .OrderBy(r => r.Date).ThenBy(r => r.SupplierName).ThenBy(r => r.DocumentNo).ThenByDescending(r => r.VatRate)
            .ToList();
    }

    private static decimal Round(decimal amount) => decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
}

public interface IPurchaseLedgerQueries
{
    /// <summary>Posted deliveries with the invoice date in [firstDay, lastDay] and corrections with the note date in it.</summary>
    Task<IReadOnlyList<PurchaseDocument>> ListAsync(DateOnly firstDay, DateOnly lastDay, CancellationToken cancellationToken);
}

/// <param name="Lines">Net amounts without VAT; a correction's are its deltas (− for goods given back).</param>
public sealed record PurchaseDocument(
    DateOnly Date,
    bool IsCorrection,
    string DocumentNo,
    string SupplierName,
    string SupplierEik,
    long DeliveryNumber,
    bool IsConsignment,
    IReadOnlyList<PurchaseDocumentLine> Lines);

public sealed record PurchaseDocumentLine(Guid ProductId, decimal NetAmount);

/// <param name="DeliveryNumber">The складова разписка the document belongs to.</param>
public sealed record PurchaseLedgerRow(
    DateOnly Date,
    string Kind,
    string DocumentNo,
    string SupplierName,
    string SupplierEik,
    long DeliveryNumber,
    bool IsConsignment,
    decimal VatRate,
    decimal TaxBase,
    decimal Vat,
    decimal Total);
