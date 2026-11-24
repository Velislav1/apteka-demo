namespace Apteka.Infrastructure.Common.Persistence;

/// <summary>
/// Applies a module's migrations (and, in Development, its seed data).
/// Each module with tables registers one in its Add&lt;Module&gt;Module(); the Host runs them in <see cref="Order"/>.
/// </summary>
public interface IModuleDatabaseInitializer
{
    /// <summary>Lower runs first. Audit is 0 because every other module writes audit rows while seeding.</summary>
    int Order { get; }

    Task MigrateAsync(CancellationToken cancellationToken);

    Task SeedAsync(CancellationToken cancellationToken);
}
