using Apteka.SharedKernel;

namespace Apteka.Purchasing.Domain.Payments;

public enum PaymentChannel
{
    Bank = 1,
    Cash = 2,
}

/// <summary>
/// „Плащане към доставчик“ as in Apteka Expert 2: money paid to a supplier, spread over his unpaid deliveries,
/// the chosen one first, then the oldest due date first. What is still owed is never stored: it is always
/// computed from the deliveries, their corrections and these allocations. Payments are never changed or deleted.
/// </summary>
public sealed class SupplierPayment : Entity<Guid>
{
    public const int MaxNoteLength = 200;

    private readonly List<PaymentAllocation> _allocations = [];

    private SupplierPayment()
    {
    }

    private SupplierPayment(Guid id)
        : base(id)
    {
    }

    public Guid SupplierId { get; private set; }

    public DateOnly PaidOn { get; private set; }

    public decimal Amount { get; private set; }

    public PaymentChannel Channel { get; private set; }

    public string? Note { get; private set; }

    public Guid? UserId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyList<PaymentAllocation> Allocations => _allocations;

    /// <param name="owed">What each unpaid delivery of the supplier still owes, oldest due date first.</param>
    /// <param name="first">A delivery to pay first (the one chosen in the list), if any.</param>
    public static Result<SupplierPayment> Create(
        Guid supplierId,
        DateOnly paidOn,
        decimal amount,
        PaymentChannel channel,
        string? note,
        IReadOnlyList<(Guid DeliveryId, decimal Owed)> owed,
        Guid? first,
        Guid? userId,
        DateTimeOffset at)
    {
        amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        if (amount <= 0)
        {
            return Result.Failure<SupplierPayment>(PaymentErrors.AmountMustBePositive);
        }

        var trimmed = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (trimmed is { Length: > MaxNoteLength })
        {
            return Result.Failure<SupplierPayment>(PaymentErrors.NoteTooLong);
        }

        var total = owed.Where(o => o.Owed > 0).Sum(o => o.Owed);
        if (amount > total)
        {
            return Result.Failure<SupplierPayment>(PaymentErrors.MoreThanOwed(total));
        }

        var payment = new SupplierPayment(Guid.CreateVersion7())
        {
            SupplierId = supplierId,
            PaidOn = paidOn,
            Amount = amount,
            Channel = channel,
            Note = trimmed,
            UserId = userId,
            CreatedAt = at,
        };

        var left = amount;
        foreach (var (deliveryId, due) in owed.Where(o => o.Owed > 0).OrderBy(o => o.DeliveryId == first ? 0 : 1))
        {
            var part = Math.Min(left, due);
            payment._allocations.Add(new PaymentAllocation(payment.Id, deliveryId, part));
            left -= part;
            if (left == 0)
            {
                break;
            }
        }

        return Result.Success(payment);
    }
}

public sealed class PaymentAllocation : Entity<Guid>
{
    private PaymentAllocation()
    {
    }

    internal PaymentAllocation(Guid paymentId, Guid deliveryId, decimal amount)
        : base(Guid.CreateVersion7())
    {
        PaymentId = paymentId;
        DeliveryId = deliveryId;
        Amount = amount;
    }

    public Guid PaymentId { get; private set; }

    public Guid DeliveryId { get; private set; }

    public decimal Amount { get; private set; }
}
