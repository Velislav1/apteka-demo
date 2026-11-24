namespace Apteka.Audit.Domain.AuditLog;

/// <summary>
/// One recorded change: who, when, what, old and new values as JSON.
/// Rows are written by the shared audit interceptor and are never updated or deleted.
/// </summary>
public sealed class AuditLogEntry
{
    private AuditLogEntry()
    {
    }

    public Guid Id { get; private set; }

    public Guid? UserId { get; private set; }

    public string UserName { get; private set; } = string.Empty;

    public DateTimeOffset At { get; private set; }

    public string Action { get; private set; } = string.Empty;

    public string Entity { get; private set; } = string.Empty;

    public string EntityId { get; private set; } = string.Empty;

    public string? OldValues { get; private set; }

    public string? NewValues { get; private set; }
}
