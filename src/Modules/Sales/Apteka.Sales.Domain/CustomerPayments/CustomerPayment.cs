using Apteka.SharedKernel;

namespace Apteka.Sales.Domain.CustomerPayments;

public enum CustomerPaymentChannel
{
    Cash = 1,
    Card = 2,
    Bank = 3,
}

/// <summary>
/// „Плащане от клиент“ (Apteka Expert 2) for sales on account: spread over the customer's unpaid sales, the chosen one
/// first, then the oldest due date. What a customer still owes is always computed (sales − corrections − payments),
/// never stored. Payments never change.
/// </summary>
public sealed class CustomerPayment : Entity<Guid>
{
    public const int MaxNoteLength = 200;

    private readonly List<CustomerPaymentAllocation> _allocations = [];

    private CustomerPayment()
    {
    }

    private CustomerPayment(Guid id)
        : base(id)
    {
    }

    public Guid CustomerId { get; private set; }

    public DateOnly PaidOn { get; private set; }

    public decimal Amount { get; private set; }

    public CustomerPaymentChannel Channel { get; private set; }

    public string? Note { get; private set; }

    public Guid? UserId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyList<CustomerPaymentAllocation> Allocations => _allocations;

    /// <param name="owed">What each unpaid sale still owes, oldest due date first.</param>
    public static Result<CustomerPayment> Create(
        Guid customerId,
        DateOnly paidOn,
        decimal amount,
        CustomerPaymentChannel channel,
        string? note,
        IReadOnlyList<(Guid SaleId, decimal Owed)> owed,
        Guid? first,
        Guid? userId,
        DateTimeOffset at)
    {
        amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        if (amount <= 0)
        {
            return Result.Failure<CustomerPayment>(CustomerPaymentErrors.AmountMustBePositive);
        }

        var trimmed = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (trimmed is { Length: > MaxNoteLength })
        {
            return Result.Failure<CustomerPayment>(CustomerPaymentErrors.NoteTooLong);
        }

        var total = owed.Where(o => o.Owed > 0).Sum(o => o.Owed);
        if (amount > total)
        {
            return Result.Failure<CustomerPayment>(CustomerPaymentErrors.MoreThanOwed(total));
        }

        var payment = new CustomerPayment(Guid.CreateVersion7())
        {
            CustomerId = customerId,
            PaidOn = paidOn,
            Amount = amount,
            Channel = channel,
            Note = trimmed,
            UserId = userId,
            CreatedAt = at,
        };

        var left = amount;
        foreach (var (saleId, due) in owed.Where(o => o.Owed > 0).OrderBy(o => o.SaleId == first ? 0 : 1))
        {
            var part = Math.Min(left, due);
            payment._allocations.Add(new CustomerPaymentAllocation(payment.Id, saleId, part));
            left -= part;
            if (left == 0)
            {
                break;
            }
        }

        return Result.Success(payment);
    }
}

public sealed class CustomerPaymentAllocation : Entity<Guid>
{
    private CustomerPaymentAllocation()
    {
    }

    internal CustomerPaymentAllocation(Guid paymentId, Guid saleId, decimal amount)
        : base(Guid.CreateVersion7())
    {
        PaymentId = paymentId;
        SaleId = saleId;
        Amount = amount;
    }

    public Guid PaymentId { get; private set; }

    public Guid SaleId { get; private set; }

    public decimal Amount { get; private set; }
}

/// <summary>Errors of „Плащания от клиенти“, shown to the user in Bulgarian.</summary>
public static class CustomerPaymentErrors
{
    public static readonly Error AmountMustBePositive = new("sales.customer_payment_amount", "Сумата трябва да е по-голяма от нула.");
    public static readonly Error NoteTooLong = new("sales.customer_payment_note", $"Бележката е по-дълга от {CustomerPayment.MaxNoteLength} знака.");

    public static Error MoreThanOwed(decimal owed) =>
        new("sales.customer_payment_too_much", $"Сумата е по-голяма от дълга на клиента ({owed:0.00} €).");
}
