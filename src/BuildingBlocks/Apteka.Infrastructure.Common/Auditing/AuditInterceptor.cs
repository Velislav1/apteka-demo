using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Reflection;
using Apteka.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using NpgsqlTypes;

namespace Apteka.Infrastructure.Common.Auditing;

/// <summary>
/// Writes one <c>audit.audit_log</c> row per changed entity, in the same transaction as the change (rule 7).
/// If the caller has no transaction, the interceptor opens one before saving and commits it after.
/// </summary>
public sealed class AuditInterceptor(ICurrentUser currentUser, TimeProvider clock) : SaveChangesInterceptor
{
    private const int RowsPerInsert = 500;

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    private readonly Dictionary<DbContext, IDbContextTransaction> _ownedTransactions = [];

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result) =>
        SavingChangesAsync(eventData, result).AsTask().GetAwaiter().GetResult();

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not { } context)
        {
            return result;
        }

        context.ChangeTracker.DetectChanges();
        var rows = CollectRows(context);
        if (rows.Count == 0)
        {
            return result;
        }

        if (context.Database.CurrentTransaction is null)
        {
            _ownedTransactions[context] = await context.Database.BeginTransactionAsync(cancellationToken);
        }

        foreach (var chunk in rows.Chunk(RowsPerInsert))
        {
            await InsertAsync(context, chunk, cancellationToken);
        }

        return result;
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result) =>
        SavedChangesAsync(eventData, result).AsTask().GetAwaiter().GetResult();

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is { } context && _ownedTransactions.Remove(context, out var transaction))
        {
            await using (transaction)
            {
                await transaction.CommitAsync(cancellationToken);
            }
        }

        return result;
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData) =>
        SaveChangesFailedAsync(eventData).GetAwaiter().GetResult();

    public override async Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is { } context && _ownedTransactions.Remove(context, out var transaction))
        {
            await using (transaction)
            {
                await transaction.RollbackAsync(cancellationToken);
            }
        }
    }

    private List<AuditRow> CollectRows(DbContext context)
    {
        var at = clock.GetUtcNow();
        var rows = new List<AuditRow>();

        foreach (var entry in context.ChangeTracker.Entries())
        {
            var action = entry.State switch
            {
                EntityState.Added => AuditActions.Insert,
                EntityState.Modified => AuditActions.Update,
                EntityState.Deleted => AuditActions.Delete,
                _ => null,
            };

            if (action is null || entry.Metadata.IsOwned())
            {
                continue;
            }

            var properties = entry.Properties.Where(p => !p.Metadata.IsShadowProperty() || p.Metadata.IsForeignKey()).ToList();

            string? oldValues = null;
            string? newValues = null;

            switch (entry.State)
            {
                case EntityState.Added:
                    newValues = Serialize(properties, p => p.CurrentValue);
                    break;
                case EntityState.Deleted:
                    oldValues = Serialize(properties, p => p.OriginalValue);
                    break;
                default:
                    var changed = properties.Where(p => p.IsModified && !Equals(p.OriginalValue, p.CurrentValue)).ToList();
                    if (changed.Count == 0)
                    {
                        continue;
                    }

                    oldValues = Serialize(changed, p => p.OriginalValue);
                    newValues = Serialize(changed, p => p.CurrentValue);
                    break;
            }

            rows.Add(new AuditRow(
                Guid.CreateVersion7(),
                currentUser.UserId,
                currentUser.UserName,
                at,
                action,
                entry.Metadata.ClrType.Name,
                KeyOf(entry),
                oldValues,
                newValues));
        }

        return rows;
    }

    private static string KeyOf(EntityEntry entry)
    {
        var key = entry.Metadata.FindPrimaryKey();
        if (key is null)
        {
            return string.Empty;
        }

        return string.Join(
            ",",
            key.Properties.Select(p => Convert.ToString(entry.Property(p.Name).CurrentValue, CultureInfo.InvariantCulture)));
    }

    private const string Hidden = "***";

    private static string Serialize(IEnumerable<PropertyEntry> properties, Func<PropertyEntry, object?> value)
    {
        var values = properties.ToDictionary(p => p.Metadata.Name, p => IsExcluded(p) ? Hidden : value(p));
        return JsonSerializer.Serialize(values, JsonOptions);
    }

    /// <summary>Values of properties marked [AuditExcluded] (password hashes) are never written; only „***“.</summary>
    private static bool IsExcluded(PropertyEntry property) =>
        property.Metadata.PropertyInfo?.GetCustomAttribute<AuditExcludedAttribute>() is not null;

    private static async Task InsertAsync(DbContext context, AuditRow[] rows, CancellationToken cancellationToken)
    {
        var sql = new StringBuilder(
            $"INSERT INTO {AuditLogTable.QualifiedName} " +
            "(id, user_id, user_name, at, action, entity, entity_id, old_values, new_values) VALUES ");
        var parameters = new List<NpgsqlParameter>(rows.Length * 9);

        for (var i = 0; i < rows.Length; i++)
        {
            var row = rows[i];
            var p = i * 9;
            if (i > 0)
            {
                sql.Append(", ");
            }

            sql.Append(CultureInfo.InvariantCulture, $"(@p{p}, @p{p + 1}, @p{p + 2}, @p{p + 3}, @p{p + 4}, @p{p + 5}, @p{p + 6}, @p{p + 7}, @p{p + 8})");

            parameters.Add(new NpgsqlParameter($"p{p}", NpgsqlDbType.Uuid) { Value = row.Id });
            parameters.Add(new NpgsqlParameter($"p{p + 1}", NpgsqlDbType.Uuid) { Value = (object?)row.UserId ?? DBNull.Value });
            parameters.Add(new NpgsqlParameter($"p{p + 2}", NpgsqlDbType.Text) { Value = row.UserName });
            parameters.Add(new NpgsqlParameter($"p{p + 3}", NpgsqlDbType.TimestampTz) { Value = row.At });
            parameters.Add(new NpgsqlParameter($"p{p + 4}", NpgsqlDbType.Text) { Value = row.Action });
            parameters.Add(new NpgsqlParameter($"p{p + 5}", NpgsqlDbType.Text) { Value = row.Entity });
            parameters.Add(new NpgsqlParameter($"p{p + 6}", NpgsqlDbType.Text) { Value = row.EntityId });
            parameters.Add(new NpgsqlParameter($"p{p + 7}", NpgsqlDbType.Jsonb) { Value = (object?)row.OldValues ?? DBNull.Value });
            parameters.Add(new NpgsqlParameter($"p{p + 8}", NpgsqlDbType.Jsonb) { Value = (object?)row.NewValues ?? DBNull.Value });
        }

        await context.Database.ExecuteSqlRawAsync(sql.ToString(), parameters, cancellationToken);
    }

    private sealed record AuditRow(
        Guid Id,
        Guid? UserId,
        string UserName,
        DateTimeOffset At,
        string Action,
        string Entity,
        string EntityId,
        string? OldValues,
        string? NewValues);
}
