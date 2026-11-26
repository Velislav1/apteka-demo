using Apteka.Sales.Application.Shifts;
using Apteka.Sales.Domain.Shifts;
using Microsoft.EntityFrameworkCore;

namespace Apteka.Sales.Infrastructure.Shifts;

internal sealed class ShiftRepository(SalesDbContext context) : IShiftRepository
{
    internal const string OneOpenIndex = "ux_cash_shifts_one_open_per_user";

    public void Add(CashShift shift) => context.CashShifts.Add(shift);

    public Task<CashShift?> GetOpenAsync(Guid userId, CancellationToken cancellationToken) =>
        context.CashShifts.Include(s => s.Movements).SingleOrDefaultAsync(s => s.UserId == userId && s.ClosedAt == null, cancellationToken);

    public Task<CashShift?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        context.CashShifts.AsNoTracking().Include(s => s.Movements).SingleOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<IReadOnlyList<CashShift>> ListAsync(int limit, CancellationToken cancellationToken) =>
        await context.CashShifts.AsNoTracking().Include(s => s.Movements).OrderByDescending(s => s.Number).Take(limit).ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);
}
