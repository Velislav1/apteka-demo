using Apteka.Sales.Domain.Sales;
using Apteka.SharedKernel;

namespace Apteka.Sales.Domain.Reversals;

/// <summary>Сторно: the record that a whole sale was undone, by whom and why. The sale itself stays.</summary>
public sealed class Reversal : Entity<Guid>
{
    public const int MaxReasonLength = 200;

    private Reversal()
    {
    }

    private Reversal(Guid id)
        : base(id)
    {
    }

    public Guid SaleId { get; private set; }

    public string? Reason { get; private set; }

    public Guid? UserId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    internal static Result<Reversal> Create(Guid saleId, string? reason, Guid? userId, DateTimeOffset at)
    {
        var trimmed = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        if (trimmed is { Length: > MaxReasonLength })
        {
            return Result.Failure<Reversal>(SalesErrors.ReasonTooLong);
        }

        return Result.Success(new Reversal(Guid.CreateVersion7())
        {
            SaleId = saleId,
            Reason = trimmed,
            UserId = userId,
            CreatedAt = at,
        });
    }
}
