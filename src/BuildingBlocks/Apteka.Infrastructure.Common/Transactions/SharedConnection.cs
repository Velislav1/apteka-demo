using Npgsql;

namespace Apteka.Infrastructure.Common.Transactions;

/// <summary>
/// One PostgreSQL connection per scope, shared by every module DbContext,
/// so that a <see cref="UnitOfWork"/> can put all of them in a single transaction.
/// </summary>
public sealed class SharedConnection(NpgsqlDataSource dataSource) : IAsyncDisposable, IDisposable
{
    private NpgsqlConnection? _connection;

    public NpgsqlConnection Connection => _connection ??= dataSource.CreateConnection();

    public void Dispose() => _connection?.Dispose();

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }
    }
}
