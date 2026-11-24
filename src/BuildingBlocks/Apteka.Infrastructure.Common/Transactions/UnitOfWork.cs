using System.Data;
using Apteka.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Apteka.Infrastructure.Common.Transactions;

/// <summary>
/// One transaction on the <see cref="SharedConnection"/>, joined by every module DbContext of the scope.
/// Audit rows written by the contexts land in the same transaction.
/// </summary>
internal sealed class UnitOfWork(SharedConnection shared, IServiceProvider services) : IUnitOfWork
{
    private bool _active;

    public async Task<Result> ExecuteAsync(Func<CancellationToken, Task<Result>> work, CancellationToken cancellationToken = default) =>
        await RunAsync(work, r => r.IsSuccess, cancellationToken);

    public async Task<Result<T>> ExecuteAsync<T>(Func<CancellationToken, Task<Result<T>>> work, CancellationToken cancellationToken = default) =>
        await RunAsync(work, r => r.IsSuccess, cancellationToken);

    private async Task<TResult> RunAsync<TResult>(
        Func<CancellationToken, Task<TResult>> work,
        Func<TResult, bool> succeeded,
        CancellationToken cancellationToken)
    {
        if (_active)
        {
            return await work(cancellationToken);
        }

        var connection = shared.Connection;
        var openedHere = connection.State != ConnectionState.Open;
        if (openedHere)
        {
            await connection.OpenAsync(cancellationToken);
        }

        var contexts = services.GetServices<ModuleDbContext>().ToList();
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        _active = true;

        try
        {
            foreach (var context in contexts)
            {
                await context.Database.UseTransactionAsync(transaction, cancellationToken);
            }

            var result = await work(cancellationToken);
            if (succeeded(result))
            {
                await transaction.CommitAsync(cancellationToken);
            }
            else
            {
                await RollbackAsync(transaction, contexts);
            }

            return result;
        }
        catch
        {
            await RollbackAsync(transaction, contexts);
            throw;
        }
        finally
        {
            foreach (var context in contexts)
            {
                await context.Database.UseTransactionAsync(null, CancellationToken.None);
            }

            _active = false;
            if (openedHere)
            {
                await connection.CloseAsync();
            }
        }
    }

    /// <summary>Rolls back and forgets unsaved or half-saved entities, so the scope starts clean.</summary>
    private static async Task RollbackAsync(System.Data.Common.DbTransaction transaction, List<ModuleDbContext> contexts)
    {
        await transaction.RollbackAsync(CancellationToken.None);
        foreach (var context in contexts)
        {
            context.ChangeTracker.Clear();
        }
    }
}
