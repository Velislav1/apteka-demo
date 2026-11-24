namespace Apteka.Infrastructure.Common.Auditing;

/// <summary>
/// The shape of <c>audit.audit_log</c>. The table is created by the Audit module's migration;
/// <see cref="AuditInterceptor"/> writes to it with plain SQL so that BuildingBlocks never reference a module.
/// Keep the two in sync (the Audit integration tests check it).
/// </summary>
public static class AuditLogTable
{
    public const string Schema = "audit";
    public const string Name = "audit_log";
    public const string QualifiedName = Schema + "." + Name;
}

public static class AuditActions
{
    public const string Insert = "Insert";
    public const string Update = "Update";
    public const string Delete = "Delete";
}
