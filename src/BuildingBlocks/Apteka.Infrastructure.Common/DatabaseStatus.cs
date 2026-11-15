using Npgsql;

namespace Apteka.Infrastructure.Common;

/// <summary>Reports whether PostgreSQL accepts connections (used by the home page).</summary>
public sealed class DatabaseStatus(string connectionString)
{
    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            return true;
        }
        catch (Exception ex) when (ex is NpgsqlException or TimeoutException or InvalidOperationException)
        {
            return false;
        }
    }
}