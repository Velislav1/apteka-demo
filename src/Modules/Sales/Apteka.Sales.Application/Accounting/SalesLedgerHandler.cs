using Apteka.SharedKernel;

namespace Apteka.Sales.Application.Accounting;

/// <summary>
/// „Дневник на продажбите“ for the accountant: per day (Sofia time), kind of document and VAT rate, the number of
/// documents, the tax base and the VAT. A reversal or a return is a document of its own day, with negative amounts.
/// </summary>
public sealed class SalesLedgerHandler(ISalesLedgerQueries queries)
{
    public async Task<IReadOnlyList<SalesLedgerRow>> HandleAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        var entries = await queries.ListAsync(BulgarianTime.StartOfDayUtc(from), BulgarianTime.StartOfDayUtc(to.AddDays(1)), cancellationToken);

        return entries
            .GroupBy(e => (Day: BulgarianTime.Today(e.At), e.Kind, e.VatRate))
            .Select(g =>
            {
                var total = g.Sum(e => e.Total);
                var vat = g.Sum(e => e.Vat);
                return new SalesLedgerRow(g.Key.Day, g.Key.Kind, g.Key.VatRate, g.Select(e => e.DocumentId).Distinct().Count(), total - vat, vat, total);
            })
            .OrderBy(r => r.Day).ThenBy(r => r.Kind).ThenByDescending(r => r.VatRate)
            .ToList();
    }
}

public interface ISalesLedgerQueries
{
    /// <summary>Sales, reversals and returns made in [fromUtc, toUtc), one entry per document and VAT rate; returns and reversals negative.</summary>
    Task<IReadOnlyList<SalesLedgerEntry>> ListAsync(DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken);
}

public enum SalesLedgerKind
{
    Sale = 1,
    Reversal = 2,
    Return = 3,
}

/// <param name="Total">With VAT.</param>
public sealed record SalesLedgerEntry(Guid DocumentId, DateTimeOffset At, SalesLedgerKind Kind, decimal VatRate, decimal Total, decimal Vat);

/// <param name="TaxBase">Данъчна основа: <paramref name="Total"/> less <paramref name="Vat"/>.</param>
public sealed record SalesLedgerRow(DateOnly Day, SalesLedgerKind Kind, decimal VatRate, int Documents, decimal TaxBase, decimal Vat, decimal Total)
{
    public string KindName => Kind switch
    {
        SalesLedgerKind.Sale => "Продажби",
        SalesLedgerKind.Reversal => "Сторно",
        _ => "Корекции",
    };
}
