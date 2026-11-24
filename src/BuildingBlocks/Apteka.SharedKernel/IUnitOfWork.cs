namespace Apteka.SharedKernel;

/// <summary>
/// Runs work of one or more modules in one database transaction: everything is saved, or nothing.
/// The work returns a <see cref="Result"/>; a failure (or an exception) rolls the transaction back.
/// Nested calls join the outer transaction.
/// </summary>
public interface IUnitOfWork
{
    Task<Result> ExecuteAsync(Func<CancellationToken, Task<Result>> work, CancellationToken cancellationToken = default);

    Task<Result<T>> ExecuteAsync<T>(Func<CancellationToken, Task<Result<T>>> work, CancellationToken cancellationToken = default);
}
