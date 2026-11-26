using Apteka.SharedKernel;

namespace Apteka.Purchasing.Domain.Deliveries;

/// <summary>
/// Складова разписка: goods received from a supplier with one invoice.
/// A draft can be edited freely; posting puts the goods in stock and locks the delivery for good.
/// </summary>
public sealed class Delivery : Entity<Guid>
{
    private readonly List<DeliveryLine> _lines = [];

    private Delivery()
    {
        InvoiceNo = string.Empty;
    }

    private Delivery(Guid id)
        : base(id)
    {
        InvoiceNo = string.Empty;
    }

    /// <summary>Складова разписка №: given by the database when the delivery is first saved.</summary>
    public long Number { get; private set; }

    public Guid SupplierId { get; private set; }

    /// <summary>Days after the invoice date by which the supplier must be paid.</summary>
    public int PaymentTermDays { get; private set; }

    /// <summary>Падеж: the date to pay by.</summary>
    public DateOnly DueDate => InvoiceDate.AddDays(PaymentTermDays);

    public string InvoiceNo { get; private set; }

    /// <summary>
    /// Консигнация: the goods stay the supplier's until sold. The pharmacy owes only for what is no longer in stock
    /// (sold, written off, missing); the rest is returned with a correction.
    /// </summary>
    public bool IsConsignment { get; private set; }

    public DateOnly InvoiceDate { get; private set; }

    public DateTimeOffset ReceivedAt { get; private set; }

    public DeliveryStatus Status { get; private set; }

    public DateTimeOffset? PostedAt { get; private set; }

    /// <summary>Who received it; null until the Identity module provides users.</summary>
    public Guid? UserId { get; private set; }

    /// <summary>All lines, including removed ones (kept for the audit trail).</summary>
    public IReadOnlyList<DeliveryLine> AllLines => _lines;

    public IReadOnlyList<DeliveryLine> Lines => [.. _lines.Where(l => !l.IsRemoved).OrderBy(l => l.Position)];

    public Money PurchaseTotal => Lines.Aggregate(Money.Zero, (sum, line) => sum + line.PurchaseTotal);

    public static Result<Delivery> Create(Guid supplierId, string invoiceNo, DateOnly invoiceDate, DateTimeOffset receivedAt, Guid? userId, int paymentTermDays = 0, bool isConsignment = false)
    {
        var delivery = new Delivery(Guid.CreateVersion7())
        {
            Status = DeliveryStatus.Draft,
            ReceivedAt = receivedAt,
            UserId = userId,
        };

        var result = delivery.UpdateHeader(supplierId, invoiceNo, invoiceDate, paymentTermDays, isConsignment);
        return result.IsSuccess ? Result.Success(delivery) : Result.Failure<Delivery>(result.Error);
    }

    public Result UpdateHeader(Guid supplierId, string invoiceNo, DateOnly invoiceDate, int paymentTermDays, bool isConsignment = false)
    {
        if (Status != DeliveryStatus.Draft)
        {
            return Result.Failure(DeliveryErrors.NotDraft);
        }

        var trimmed = invoiceNo.Trim();
        if (trimmed.Length == 0)
        {
            return Result.Failure(DeliveryErrors.InvoiceNoRequired);
        }

        if (trimmed.Length > DeliveryErrors.MaxInvoiceNoLength)
        {
            return Result.Failure(DeliveryErrors.InvoiceNoTooLong);
        }

        if (paymentTermDays is < 0 or > DeliveryErrors.MaxPaymentTermDays)
        {
            return Result.Failure(DeliveryErrors.InvalidPaymentTerm);
        }

        SupplierId = supplierId;
        InvoiceNo = trimmed;
        InvoiceDate = invoiceDate;
        PaymentTermDays = paymentTermDays;
        IsConsignment = isConsignment;
        return Result.Success();
    }

    /// <summary>
    /// Replaces the draft's lines with the requested ones: lines with an id are updated, new ones added,
    /// missing ones marked as removed. Nothing changes if any line is invalid.
    /// </summary>
    public Result SetLines(IReadOnlyList<DeliveryLineSpec> lines)
    {
        if (Status != DeliveryStatus.Draft)
        {
            return Result.Failure(DeliveryErrors.NotDraft);
        }

        for (var i = 0; i < lines.Count; i++)
        {
            var error = Validate(lines[i], i + 1);
            if (error is not null)
            {
                return Result.Failure(error);
            }
        }

        var current = Lines;
        if (lines.Any(l => l.Id is { } id && current.All(c => c.Id != id)))
        {
            return Result.Failure(DeliveryErrors.UnknownLine);
        }

        foreach (var line in current.Where(c => lines.All(l => l.Id != c.Id)))
        {
            line.Remove();
        }

        for (var i = 0; i < lines.Count; i++)
        {
            var spec = lines[i];
            if (spec.Id is { } id)
            {
                current.Single(c => c.Id == id).Apply(spec, i + 1);
            }
            else
            {
                _lines.Add(new DeliveryLine(Id, spec, i + 1));
            }
        }

        return Result.Success();
    }

    /// <summary>Locks the delivery. The caller puts the lines into stock in the same transaction.</summary>
    public Result Post(DateOnly today, DateTimeOffset at)
    {
        if (Status != DeliveryStatus.Draft)
        {
            return Result.Failure(DeliveryErrors.NotDraft);
        }

        var lines = Lines;
        if (lines.Count == 0)
        {
            return Result.Failure(DeliveryErrors.NoLines);
        }

        var expired = lines.FirstOrDefault(l => l.ExpiryDate < today);
        if (expired is not null)
        {
            return Result.Failure(DeliveryErrors.Expired(expired.Position, expired.ExpiryDate));
        }

        Status = DeliveryStatus.Posted;
        PostedAt = at;
        return Result.Success();
    }

    private static Error? Validate(DeliveryLineSpec line, int number)
    {
        if (line.Quantity <= 0 || line.UnitFactor < 1)
        {
            return DeliveryErrors.QuantityMustBePositive(number);
        }

        if (line.UnitPurchasePrice < 0)
        {
            return DeliveryErrors.InvalidPurchasePrice(number);
        }

        if (line.SupplierDiscountPercent is < 0 or > 100)
        {
            return DeliveryErrors.InvalidSupplierDiscount(number);
        }

        if (line.MarkupPercent < 0)
        {
            return DeliveryErrors.InvalidMarkup(number);
        }

        if (line.SalePrice <= 0)
        {
            return DeliveryErrors.InvalidSalePrice(number);
        }

        var batchNo = line.BatchNo.Trim();
        return batchNo.Length is 0 or > DeliveryErrors.MaxBatchNoLength ? DeliveryErrors.BatchNoRequired(number) : null;
    }
}
