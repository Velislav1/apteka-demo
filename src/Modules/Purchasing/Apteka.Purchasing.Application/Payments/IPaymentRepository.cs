using Apteka.Purchasing.Domain.Payments;

namespace Apteka.Purchasing.Application.Payments;

public interface IPaymentRepository
{
    void Add(SupplierPayment payment);

    /// <summary>Posted deliveries (of one supplier, or all) with their net lines and corrections, for the amounts owed.</summary>
    Task<IReadOnlyList<PayableDelivery>> ListPostedDeliveriesAsync(Guid? supplierId, CancellationToken cancellationToken);

    /// <summary>Paid so far per delivery.</summary>
    Task<IReadOnlyDictionary<Guid, decimal>> GetPaidAsync(IReadOnlyCollection<Guid> deliveryIds, CancellationToken cancellationToken);

    /// <summary>Payments made in [from, to] (Sofia dates), newest first.</summary>
    Task<IReadOnlyList<PaymentListItem>> ListPaymentsAsync(Guid? supplierId, DateOnly from, DateOnly to, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

/// <param name="Lines">Net purchase amounts (without VAT) per product: the delivery's lines and its corrections.</param>
public sealed record PayableDelivery(
    Guid Id,
    long Number,
    Guid SupplierId,
    string SupplierName,
    string InvoiceNo,
    DateOnly InvoiceDate,
    int PaymentTermDays,
    IReadOnlyList<PayableLine> Lines)
{
    public DateOnly DueDate => InvoiceDate.AddDays(PaymentTermDays);
}

public sealed record PayableLine(Guid ProductId, decimal NetAmount);

public sealed record PaymentListItem(Guid Id, DateOnly PaidOn, string SupplierName, decimal Amount, PaymentChannel Channel, string? Note, int DeliveryCount);
