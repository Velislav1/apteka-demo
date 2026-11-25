using Apteka.Catalog.Contracts;
using Apteka.Purchasing.Application.Suppliers;
using Apteka.Purchasing.Domain.Payments;
using Apteka.SharedKernel;

namespace Apteka.Purchasing.Application.Payments;

/// <summary>
/// „Задължения към доставчиците“ and „Плащания към доставчици“. An invoice's amount is its net lines (and
/// corrections) with the products' VAT; what is owed is that amount less the payments allocated to it.
/// </summary>
public sealed class PayablesHandler(
    IPaymentRepository payments,
    ISupplierRepository suppliers,
    ICatalogLookup catalog,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider clock)
{
    /// <param name="onlyUnpaid">Leave out the deliveries paid in full.</param>
    public async Task<IReadOnlyList<PayableRow>> ListAsync(Guid? supplierId, DateOnly asOf, bool onlyUnpaid = true, CancellationToken cancellationToken = default)
    {
        var deliveries = await payments.ListPostedDeliveriesAsync(supplierId, cancellationToken);
        var vat = (await catalog.GetProductsAsync([.. deliveries.SelectMany(d => d.Lines).Select(l => l.ProductId).Distinct()], cancellationToken))
            .ToDictionary(p => p.ProductId, p => p.VatRate);
        var paid = await payments.GetPaidAsync([.. deliveries.Select(d => d.Id)], cancellationToken);

        return deliveries
            .Select(d =>
            {
                var total = d.Lines.Sum(l => Round(l.NetAmount * (1 + (vat.GetValueOrDefault(l.ProductId) / 100m))));
                var paidSoFar = paid.GetValueOrDefault(d.Id);
                return new PayableRow(d.Id, d.Number, d.SupplierId, d.SupplierName, d.InvoiceNo, d.InvoiceDate, d.DueDate, total, paidSoFar, total - paidSoFar,
                    Math.Max(0, asOf.DayNumber - d.DueDate.DayNumber));
            })
            .Where(r => !onlyUnpaid || r.Owed > 0)
            .OrderBy(r => r.DueDate)
            .ThenBy(r => r.Number)
            .ToList();
    }

    public Task<IReadOnlyList<PaymentListItem>> ListPaymentsAsync(Guid? supplierId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default) =>
        payments.ListPaymentsAsync(supplierId, from, to, cancellationToken);

    /// <param name="firstDeliveryId">The delivery chosen in the list, paid first; the rest goes to the oldest due dates.</param>
    public Task<Result<Guid>> PayAsync(Guid supplierId, DateOnly paidOn, decimal amount, PaymentChannel channel, string? note, Guid? firstDeliveryId, CancellationToken cancellationToken = default) =>
        unitOfWork.ExecuteAsync(async ct =>
        {
            if (await suppliers.GetAsync(supplierId, ct) is null)
            {
                return Result.Failure<Guid>(PaymentErrors.SupplierNotFound);
            }

            var owed = await ListAsync(supplierId, paidOn, onlyUnpaid: true, ct);
            var created = SupplierPayment.Create(
                supplierId, paidOn, amount, channel, note, owed.Select(o => (o.DeliveryId, o.Owed)).ToList(), firstDeliveryId, currentUser.UserId, clock.GetUtcNow());
            if (created.IsFailure)
            {
                return Result.Failure<Guid>(created.Error);
            }

            payments.Add(created.Value);
            await payments.SaveChangesAsync(ct);
            return Result.Success(created.Value.Id);
        }, cancellationToken);

    private static decimal Round(decimal amount) => decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
}

/// <param name="DaysOverdue">0 when the due date has not passed.</param>
public sealed record PayableRow(
    Guid DeliveryId,
    long Number,
    Guid SupplierId,
    string SupplierName,
    string InvoiceNo,
    DateOnly InvoiceDate,
    DateOnly DueDate,
    decimal Total,
    decimal Paid,
    decimal Owed,
    int DaysOverdue)
{
    /// <summary>„Ненастъпил“, „1–30“, „31–60“ or „над 60“ days overdue.</summary>
    public string Bucket => DaysOverdue switch
    {
        0 => "ненастъпил",
        <= 30 => "1–30 дни",
        <= 60 => "31–60 дни",
        _ => "над 60 дни",
    };
}
