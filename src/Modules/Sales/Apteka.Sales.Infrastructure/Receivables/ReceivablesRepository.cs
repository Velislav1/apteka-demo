using Apteka.Sales.Application.Receivables;
using Apteka.Sales.Domain.CustomerPayments;
using Apteka.Sales.Domain.Sales;
using Microsoft.EntityFrameworkCore;

namespace Apteka.Sales.Infrastructure.Receivables;

internal sealed class ReceivablesRepository(SalesDbContext context) : IReceivablesRepository
{
    public void Add(CustomerPayment payment) => context.CustomerPayments.Add(payment);

    public async Task<IReadOnlyList<OnAccountSale>> ListOnAccountSalesAsync(Guid? customerId, CancellationToken cancellationToken)
    {
        var sales = context.Sales.AsNoTracking().Where(s => s.PaymentMethod == PaymentMethod.OnAccount && s.Status == SaleStatus.Completed && s.CustomerId != null);
        if (customerId is { } id)
        {
            sales = sales.Where(s => s.CustomerId == id);
        }

        return await sales
            .Select(s => new OnAccountSale(
                s.Id,
                s.Number,
                s.CustomerId!.Value,
                s.CreatedAt,
                s.Total,
                context.SaleReturns.Where(r => r.SaleId == s.Id).Sum(r => (decimal?)r.Total) ?? 0m,
                s.PaymentTermDays))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, decimal>> GetPaidAsync(IReadOnlyCollection<Guid> saleIds, CancellationToken cancellationToken)
    {
        var ids = saleIds.ToArray();
        return await context.CustomerPaymentAllocations.AsNoTracking()
            .Where(a => ids.Contains(a.SaleId))
            .GroupBy(a => a.SaleId)
            .Select(g => new { g.Key, Paid = g.Sum(a => a.Amount) })
            .ToDictionaryAsync(g => g.Key, g => g.Paid, cancellationToken);
    }

    public async Task<IReadOnlyList<CustomerPaymentItem>> ListPaymentsAsync(Guid? customerId, DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        var payments = context.CustomerPayments.AsNoTracking().Where(p => p.PaidOn >= from && p.PaidOn <= to);
        if (customerId is { } id)
        {
            payments = payments.Where(p => p.CustomerId == id);
        }

        return await payments
            .OrderByDescending(p => p.PaidOn).ThenByDescending(p => p.CreatedAt)
            .Select(p => new CustomerPaymentItem(p.Id, p.PaidOn, p.CustomerId, p.Amount, p.Channel, p.Note, p.Allocations.Count))
            .ToListAsync(cancellationToken);
    }

    public async Task<decimal> CashTakenAsync(Guid userId, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken) =>
        await context.CustomerPayments.AsNoTracking()
            .Where(p => p.UserId == userId && p.Channel == CustomerPaymentChannel.Cash && p.CreatedAt >= fromUtc && p.CreatedAt < toUtc)
            .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;

    public Task SaveChangesAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);
}
