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

/// <param name="Lines">Net purchase amounts (without VAT) per product: the delivery's lines and its corrections. Empty for a consignment.</param>
/// <param name="ConsignmentLines">A consignment's lines: what is owed depends on what is still in stock.</param>
public sealed record PayableDelivery(
    Guid Id,
    long Number,
    Guid SupplierId,
    string SupplierName,
    string InvoiceNo,
    DateOnly InvoiceDate,
    int PaymentTermDays,
    IReadOnlyList<PayableLine> Lines,
    bool IsConsignment = false,
    IReadOnlyList<ConsignmentLine>? ConsignmentLines = null)
{
    public DateOnly DueDate => InvoiceDate.AddDays(PaymentTermDays);
}

public sealed record PayableLine(Guid ProductId, decimal NetAmount);

/// <summary>A line of a consignment, in the line's unit (<paramref name="UnitFactor"/> base units each).</summary>
/// <param name="CorrectedQuantity">The corrections of the line (− returned to the supplier).</param>
/// <param name="NetUnitPrice">Purchase price per unit of the line after the supplier's discount, without VAT.</param>
public sealed record ConsignmentLine(
    Guid DeliveryLineId,
    Guid ProductId,
    string BatchNo,
    DateOnly ExpiryDate,
    int UnitFactor,
    int Quantity,
    int CorrectedQuantity,
    decimal NetUnitPrice)
{
    /// <summary>Received less returned, in base units.</summary>
    public int ReceivedBaseQuantity => (Quantity + CorrectedQuantity) * UnitFactor;
}

public sealed record PaymentListItem(Guid Id, DateOnly PaidOn, string SupplierName, decimal Amount, PaymentChannel Channel, string? Note, int DeliveryCount);
