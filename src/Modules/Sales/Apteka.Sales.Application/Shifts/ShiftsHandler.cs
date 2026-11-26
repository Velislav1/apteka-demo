using Apteka.Sales.Application.Common;
using Apteka.Sales.Domain.Sales;
using Apteka.Sales.Domain.Shifts;
using Apteka.SharedKernel;

namespace Apteka.Sales.Application.Shifts;

public interface IShiftRepository
{
    void Add(CashShift shift);

    /// <summary>The user's open shift (tracked), or null.</summary>
    Task<CashShift?> GetOpenAsync(Guid userId, CancellationToken cancellationToken);

    Task<CashShift?> GetAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Newest first.</summary>
    Task<IReadOnlyList<CashShift>> ListAsync(int limit, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

/// <summary>
/// „Смяна на касата“: open with the cash in the drawer, put cash in or take it out with a reason, close with the
/// counted cash. Expected cash = opening + the user's cash sales in the shift − cash refunds (corrections) + in − out.
/// </summary>
public sealed class ShiftsHandler(IShiftRepository shifts, ISaleRepository sales, IUnitOfWork unitOfWork, ICurrentUser currentUser, TimeProvider clock)
{
    public const int ListLimit = 60;

    public async Task<ShiftView?> GetCurrentAsync(CancellationToken cancellationToken = default) =>
        currentUser.UserId is { } user && await shifts.GetOpenAsync(user, cancellationToken) is { } shift
            ? await ToViewAsync(shift, cancellationToken)
            : null;

    public async Task<ShiftView?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        await shifts.GetAsync(id, cancellationToken) is { } shift ? await ToViewAsync(shift, cancellationToken) : null;

    public async Task<IReadOnlyList<ShiftView>> ListAsync(CancellationToken cancellationToken = default)
    {
        var list = new List<ShiftView>();
        foreach (var shift in await shifts.ListAsync(ListLimit, cancellationToken))
        {
            list.Add(await ToViewAsync(shift, cancellationToken));
        }

        return list;
    }

    public Task<Result<Guid>> OpenAsync(decimal openingCash, CancellationToken cancellationToken = default) =>
        unitOfWork.ExecuteAsync(async ct =>
        {
            if (currentUser.UserId is not { } user)
            {
                return Result.Failure<Guid>(ShiftErrors.NoUser);
            }

            if (await shifts.GetOpenAsync(user, ct) is not null)
            {
                return Result.Failure<Guid>(ShiftErrors.AlreadyOpen);
            }

            var opened = CashShift.Open(user, openingCash, clock.GetUtcNow());
            if (opened.IsFailure)
            {
                return Result.Failure<Guid>(opened.Error);
            }

            shifts.Add(opened.Value);
            await shifts.SaveChangesAsync(ct);
            return Result.Success(opened.Value.Id);
        }, cancellationToken);

    /// <param name="amount">+ put in, − taken out.</param>
    public Task<Result> MoveCashAsync(decimal amount, string? reason, CancellationToken cancellationToken = default) =>
        unitOfWork.ExecuteAsync(async ct =>
        {
            if (currentUser.UserId is not { } user || await shifts.GetOpenAsync(user, ct) is not { } shift)
            {
                return Result.Failure(ShiftErrors.NoOpenShift);
            }

            var moved = shift.AddMovement(amount, reason, clock.GetUtcNow());
            if (moved.IsSuccess)
            {
                await shifts.SaveChangesAsync(ct);
            }

            return moved;
        }, cancellationToken);

    public Task<Result<Guid>> CloseAsync(decimal countedCash, string? note, CancellationToken cancellationToken = default) =>
        unitOfWork.ExecuteAsync(async ct =>
        {
            if (currentUser.UserId is not { } user || await shifts.GetOpenAsync(user, ct) is not { } shift)
            {
                return Result.Failure<Guid>(ShiftErrors.NoOpenShift);
            }

            var now = clock.GetUtcNow();
            var totals = await TotalsAsync(shift, now, ct);
            var closed = shift.Close(countedCash, totals.Expected(shift), note, now);
            if (closed.IsFailure)
            {
                return Result.Failure<Guid>(closed.Error);
            }

            await shifts.SaveChangesAsync(ct);
            return Result.Success(shift.Id);
        }, cancellationToken);

    private async Task<ShiftView> ToViewAsync(CashShift shift, CancellationToken cancellationToken)
    {
        var totals = await TotalsAsync(shift, shift.ClosedAt ?? clock.GetUtcNow(), cancellationToken);
        return new ShiftView(
            shift.Id,
            shift.Number,
            shift.UserId,
            shift.OpenedAt,
            shift.ClosedAt,
            shift.OpeningCash,
            totals.CashSales,
            totals.CardSales,
            totals.CashRefunds,
            shift.CashIn,
            shift.CashOut,
            shift.ExpectedCash ?? totals.Expected(shift),
            shift.CountedCash,
            shift.Note,
            totals.SaleCount,
            shift.Movements.OrderBy(m => m.At).Select(m => new CashMovementView(m.Amount, m.Reason, m.At)).ToList());
    }

    private async Task<Totals> TotalsAsync(CashShift shift, DateTimeOffset until, CancellationToken cancellationToken)
    {
        var mine = (await sales.ListAsync(shift.OpenedAt, until, cancellationToken))
            .Where(s => s.UserId == shift.UserId && s.Status == SaleStatus.Completed)
            .ToList();
        var refunds = (await sales.ListReturnsAsync(shift.OpenedAt, until, cancellationToken))
            .Where(r => r.Return.UserId == shift.UserId && r.PaymentMethod == PaymentMethod.Cash)
            .Sum(r => r.Return.Total);
        return new Totals(
            mine.Where(s => s.PaymentMethod == PaymentMethod.Cash).Sum(s => s.Total),
            mine.Where(s => s.PaymentMethod == PaymentMethod.Card).Sum(s => s.Total),
            refunds,
            mine.Count);
    }

    private sealed record Totals(decimal CashSales, decimal CardSales, decimal CashRefunds, int SaleCount)
    {
        public decimal Expected(CashShift shift) => shift.OpeningCash + CashSales - CashRefunds + shift.CashIn - shift.CashOut;
    }
}

public sealed record ShiftView(
    Guid Id,
    long Number,
    Guid UserId,
    DateTimeOffset OpenedAt,
    DateTimeOffset? ClosedAt,
    decimal OpeningCash,
    decimal CashSales,
    decimal CardSales,
    decimal CashRefunds,
    decimal CashIn,
    decimal CashOut,
    decimal ExpectedCash,
    decimal? CountedCash,
    string? Note,
    int SaleCount,
    IReadOnlyList<CashMovementView> Movements)
{
    public bool IsOpen => ClosedAt is null;

    /// <summary>Counted − expected: + surplus, − shortage.</summary>
    public decimal? Difference => CountedCash - ExpectedCash;
}

public sealed record CashMovementView(decimal Amount, string Reason, DateTimeOffset At);
