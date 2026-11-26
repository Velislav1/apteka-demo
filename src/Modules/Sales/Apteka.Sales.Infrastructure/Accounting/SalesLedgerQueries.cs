using Apteka.Sales.Application.Accounting;
using Microsoft.EntityFrameworkCore;

namespace Apteka.Sales.Infrastructure.Accounting;

internal sealed class SalesLedgerQueries(SalesDbContext context) : ISalesLedgerQueries
{
    public async Task<IReadOnlyList<SalesLedgerEntry>> ListAsync(DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken)
    {
        var sales = await (
                from s in context.Sales.AsNoTracking()
                where s.CreatedAt >= fromUtc && s.CreatedAt < toUtc
                from l in s.Lines
                group l by new { s.Id, s.CreatedAt, l.VatRate } into g
                select new SalesLedgerEntry(g.Key.Id, g.Key.CreatedAt, SalesLedgerKind.Sale, g.Key.VatRate, g.Sum(l => l.LineTotal), g.Sum(l => l.VatAmount)))
            .ToListAsync(cancellationToken);

        var reversals = await (
                from r in context.Reversals.AsNoTracking()
                where r.CreatedAt >= fromUtc && r.CreatedAt < toUtc
                join l in context.SaleLines.AsNoTracking() on r.SaleId equals l.SaleId
                group l by new { r.Id, r.CreatedAt, l.VatRate } into g
                select new SalesLedgerEntry(g.Key.Id, g.Key.CreatedAt, SalesLedgerKind.Reversal, g.Key.VatRate, -g.Sum(l => l.LineTotal), -g.Sum(l => l.VatAmount)))
            .ToListAsync(cancellationToken);

        var returns = await (
                from r in context.SaleReturns.AsNoTracking()
                where r.CreatedAt >= fromUtc && r.CreatedAt < toUtc
                from rl in r.Lines
                join l in context.SaleLines.AsNoTracking() on rl.SaleLineId equals l.Id
                group rl by new { r.Id, r.CreatedAt, l.VatRate } into g
                select new SalesLedgerEntry(g.Key.Id, g.Key.CreatedAt, SalesLedgerKind.Return, g.Key.VatRate, -g.Sum(l => l.Amount), -g.Sum(l => l.VatAmount)))
            .ToListAsync(cancellationToken);

        return [.. sales, .. reversals, .. returns];
    }
}
