using Apteka.Purchasing.Application.Expenses;
using Apteka.Purchasing.Domain.Expenses;
using Microsoft.EntityFrameworkCore;

namespace Apteka.Purchasing.Infrastructure.Expenses;

internal sealed class ExpenseRepository(PurchasingDbContext context) : IExpenseRepository
{
    public void Add(Expense expense) => context.Expenses.Add(expense);

    public async Task<IReadOnlyList<Expense>> ListAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken) =>
        await context.Expenses.AsNoTracking().Where(e => e.Date >= from && e.Date <= to).OrderByDescending(e => e.Date).ThenByDescending(e => e.CreatedAt).ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);
}
