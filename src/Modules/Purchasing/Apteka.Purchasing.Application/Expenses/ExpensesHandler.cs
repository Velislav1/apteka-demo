using Apteka.Purchasing.Domain.Expenses;
using Apteka.SharedKernel;

namespace Apteka.Purchasing.Application.Expenses;

public interface IExpenseRepository
{
    void Add(Expense expense);

    /// <summary>Dated in [from, to], newest first.</summary>
    Task<IReadOnlyList<Expense>> ListAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

/// <summary>„Въвеждане на разходи“.</summary>
public sealed class ExpensesHandler(IExpenseRepository expenses, IUnitOfWork unitOfWork, ICurrentUser currentUser, TimeProvider clock)
{
    public Task<Result<Guid>> AddAsync(DateOnly date, string category, decimal amount, string? documentNo, string? note, CancellationToken cancellationToken = default) =>
        unitOfWork.ExecuteAsync(async ct =>
        {
            var created = Expense.Create(date, category, amount, documentNo, note, currentUser.UserId, clock.GetUtcNow());
            if (created.IsFailure)
            {
                return Result.Failure<Guid>(created.Error);
            }

            expenses.Add(created.Value);
            await expenses.SaveChangesAsync(ct);
            return Result.Success(created.Value.Id);
        }, cancellationToken);

    public Task<IReadOnlyList<Expense>> ListAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default) =>
        expenses.ListAsync(from, to, cancellationToken);
}
