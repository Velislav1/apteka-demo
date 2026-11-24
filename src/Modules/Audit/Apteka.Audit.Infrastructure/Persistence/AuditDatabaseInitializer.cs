using Apteka.Infrastructure.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Apteka.Audit.Infrastructure.Persistence;

internal sealed class AuditDatabaseInitializer(AuditDbContext context) : IModuleDatabaseInitializer
{
    public int Order => 0;

    public Task MigrateAsync(CancellationToken cancellationToken) => context.Database.MigrateAsync(cancellationToken);

    public Task SeedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
