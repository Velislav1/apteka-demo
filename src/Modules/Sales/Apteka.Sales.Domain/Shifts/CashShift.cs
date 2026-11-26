using Apteka.SharedKernel;

namespace Apteka.Sales.Domain.Shifts;

/// <summary>
/// „Смяна“ at the till: the cash a user starts with, cash put in or taken out with a reason (служебно въведени /
/// изведени суми), and at the end the counted cash against the expected one. What is expected is computed from the
/// sales; it is stored only when the shift is closed, together with the count. A closed shift never changes.
/// </summary>
public sealed class CashShift : Entity<Guid>
{
    public const int MaxTextLength = 200;

    private readonly List<CashMovement> _movements = [];

    private CashShift()
    {
    }

    private CashShift(Guid id)
        : base(id)
    {
    }

    public long Number { get; private set; }

    public Guid UserId { get; private set; }

    public DateTimeOffset OpenedAt { get; private set; }

    /// <summary>Начална сума in the drawer.</summary>
    public decimal OpeningCash { get; private set; }

    public DateTimeOffset? ClosedAt { get; private set; }

    /// <summary>Expected cash when closed: opening + cash sales − cash refunds + put in − taken out.</summary>
    public decimal? ExpectedCash { get; private set; }

    public decimal? CountedCash { get; private set; }

    public string? Note { get; private set; }

    public bool IsOpen => ClosedAt is null;

    public decimal? Difference => CountedCash - ExpectedCash;

    public IReadOnlyList<CashMovement> Movements => _movements;

    public decimal CashIn => _movements.Where(m => m.Amount > 0).Sum(m => m.Amount);

    public decimal CashOut => -_movements.Where(m => m.Amount < 0).Sum(m => m.Amount);

    public static Result<CashShift> Open(Guid userId, decimal openingCash, DateTimeOffset at)
    {
        if (openingCash < 0)
        {
            return Result.Failure<CashShift>(ShiftErrors.NegativeAmount);
        }

        return Result.Success(new CashShift(Guid.CreateVersion7()) { UserId = userId, OpeningCash = Round(openingCash), OpenedAt = at });
    }

    /// <param name="amount">+ put into the drawer, − taken out.</param>
    public Result AddMovement(decimal amount, string? reason, DateTimeOffset at)
    {
        if (!IsOpen)
        {
            return Result.Failure(ShiftErrors.Closed);
        }

        amount = Round(amount);
        var text = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        if (amount == 0)
        {
            return Result.Failure(ShiftErrors.ZeroAmount);
        }

        if (text is null)
        {
            return Result.Failure(ShiftErrors.ReasonRequired);
        }

        if (text.Length > MaxTextLength)
        {
            return Result.Failure(ShiftErrors.TooLong);
        }

        _movements.Add(new CashMovement(Id, amount, text, at));
        return Result.Success();
    }

    /// <param name="expectedCash">Computed by the caller from the sales of the shift.</param>
    public Result Close(decimal countedCash, decimal expectedCash, string? note, DateTimeOffset at)
    {
        if (!IsOpen)
        {
            return Result.Failure(ShiftErrors.Closed);
        }

        if (countedCash < 0)
        {
            return Result.Failure(ShiftErrors.NegativeAmount);
        }

        var text = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (text is { Length: > MaxTextLength })
        {
            return Result.Failure(ShiftErrors.TooLong);
        }

        CountedCash = Round(countedCash);
        ExpectedCash = Round(expectedCash);
        Note = text;
        ClosedAt = at;
        return Result.Success();
    }

    private static decimal Round(decimal amount) => decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
}

/// <summary>Cash put into (+) or taken out of (−) the drawer outside sales, with the reason.</summary>
public sealed class CashMovement : Entity<Guid>
{
    private CashMovement()
    {
        Reason = string.Empty;
    }

    internal CashMovement(Guid shiftId, decimal amount, string reason, DateTimeOffset at)
        : base(Guid.CreateVersion7())
    {
        ShiftId = shiftId;
        Amount = amount;
        Reason = reason;
        At = at;
    }

    public Guid ShiftId { get; private set; }

    public decimal Amount { get; private set; }

    public string Reason { get; private set; }

    public DateTimeOffset At { get; private set; }
}

/// <summary>Errors of „Смяна“, shown to the user in Bulgarian.</summary>
public static class ShiftErrors
{
    public static readonly Error AlreadyOpen = new("sales.shift_open", "Вече имате отворена смяна.");
    public static readonly Error NoOpenShift = new("sales.shift_none", "Нямате отворена смяна.");
    public static readonly Error Closed = new("sales.shift_closed", "Смяната е приключена и не може да се променя.");
    public static readonly Error NegativeAmount = new("sales.shift_negative", "Сумата не може да е отрицателна.");
    public static readonly Error ZeroAmount = new("sales.shift_zero", "Въведете сума, различна от нула.");
    public static readonly Error ReasonRequired = new("sales.shift_reason", "Въведете основание за внасянето или тегленето.");
    public static readonly Error TooLong = new("sales.shift_too_long", $"Текстът е по-дълъг от {CashShift.MaxTextLength} знака.");
    public static readonly Error NoUser = new("sales.shift_no_user", "Смяна се отваря само от влязъл потребител.");
}
