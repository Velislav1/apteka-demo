using Apteka.Sales.Application.Customers;
using Apteka.Sales.Domain.CustomerPayments;
using Apteka.Sales.Domain.Customers;
using Apteka.SharedKernel;

namespace Apteka.Sales.Application.Receivables;

public interface IReceivablesRepository
{
    void Add(CustomerPayment payment);

    /// <summary>Completed sales on account (of one customer, or all), with what corrections gave back of each.</summary>
    Task<IReadOnlyList<OnAccountSale>> ListOnAccountSalesAsync(Guid? customerId, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, decimal>> GetPaidAsync(IReadOnlyCollection<Guid> saleIds, CancellationToken cancellationToken);

    /// <summary>Payments made in [from, to] (Sofia dates), newest first.</summary>
    Task<IReadOnlyList<CustomerPaymentItem>> ListPaymentsAsync(Guid? customerId, DateOnly from, DateOnly to, CancellationToken cancellationToken);

    /// <summary>Cash taken from customers by a user in [fromUtc, toUtc), for the cash shift.</summary>
    Task<decimal> CashTakenAsync(Guid userId, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

/// <param name="Returned">Given back with corrections of the sale.</param>
public sealed record OnAccountSale(Guid SaleId, long Number, Guid CustomerId, DateTimeOffset CreatedAt, decimal Total, decimal Returned, int PaymentTermDays)
{
    public DateOnly DueDate => BulgarianTime.Today(CreatedAt).AddDays(PaymentTermDays);
}

public sealed record CustomerPaymentItem(Guid Id, DateOnly PaidOn, Guid CustomerId, decimal Amount, CustomerPaymentChannel Channel, string? Note, int SaleCount);

/// <summary>
/// „Вземания от клиенти“ and „Плащания от клиенти“: what customers owe for sales on account (total − corrections −
/// payments, computed), how overdue it is, and taking a payment.
/// </summary>
public sealed class ReceivablesHandler(IReceivablesRepository receivables, ICustomerRepository customers, IUnitOfWork unitOfWork, ICurrentUser currentUser, TimeProvider clock)
{
    public async Task<IReadOnlyList<ReceivableRow>> ListAsync(Guid? customerId, DateOnly asOf, bool onlyUnpaid = true, CancellationToken cancellationToken = default)
    {
        var sales = await receivables.ListOnAccountSalesAsync(customerId, cancellationToken);
        var paid = await receivables.GetPaidAsync([.. sales.Select(s => s.SaleId)], cancellationToken);
        var names = await customers.GetNamesAsync([.. sales.Select(s => s.CustomerId).Distinct()], cancellationToken);

        return sales
            .Select(s =>
            {
                var paidSoFar = paid.GetValueOrDefault(s.SaleId);
                var owed = s.Total - s.Returned - paidSoFar;
                return new ReceivableRow(s.SaleId, s.Number, s.CustomerId, names.GetValueOrDefault(s.CustomerId, "?"), s.CreatedAt, s.DueDate,
                    s.Total - s.Returned, paidSoFar, owed, Math.Max(0, asOf.DayNumber - s.DueDate.DayNumber));
            })
            .Where(r => !onlyUnpaid || r.Owed > 0)
            .OrderBy(r => r.DueDate)
            .ThenBy(r => r.Number)
            .ToList();
    }

    public async Task<IReadOnlyList<(CustomerPaymentItem Payment, string CustomerName)>> ListPaymentsAsync(Guid? customerId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        var payments = await receivables.ListPaymentsAsync(customerId, from, to, cancellationToken);
        var names = await customers.GetNamesAsync([.. payments.Select(p => p.CustomerId).Distinct()], cancellationToken);
        return payments.Select(p => (p, names.GetValueOrDefault(p.CustomerId, "?"))).ToList();
    }

    public Task<Result<Guid>> PayAsync(Guid customerId, DateOnly paidOn, decimal amount, CustomerPaymentChannel channel, string? note, Guid? firstSaleId, CancellationToken cancellationToken = default) =>
        unitOfWork.ExecuteAsync(async ct =>
        {
            if (await customers.GetAsync(customerId, ct) is null)
            {
                return Result.Failure<Guid>(CustomerErrors.NotFound);
            }

            var owed = await ListAsync(customerId, paidOn, onlyUnpaid: true, ct);
            var created = CustomerPayment.Create(customerId, paidOn, amount, channel, note, owed.Select(o => (o.SaleId, o.Owed)).ToList(), firstSaleId, currentUser.UserId, clock.GetUtcNow());
            if (created.IsFailure)
            {
                return Result.Failure<Guid>(created.Error);
            }

            receivables.Add(created.Value);
            await receivables.SaveChangesAsync(ct);
            return Result.Success(created.Value.Id);
        }, cancellationToken);
}

public sealed record ReceivableRow(
    Guid SaleId,
    long Number,
    Guid CustomerId,
    string CustomerName,
    DateTimeOffset CreatedAt,
    DateOnly DueDate,
    decimal Total,
    decimal Paid,
    decimal Owed,
    int DaysOverdue);
