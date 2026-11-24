namespace Apteka.SharedKernel;

/// <summary>The user on whose behalf the current work runs. Written into documents, stock movements and every audit row.</summary>
public interface ICurrentUser
{
    /// <summary>Null for the system (start-up, seed data) and for nobody logged in.</summary>
    Guid? UserId { get; }

    string UserName { get; }

    bool IsInRole(string role);
}

/// <summary>Marks a property whose value must never be written to the audit log (e.g. a password hash).</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class AuditExcludedAttribute : Attribute;
