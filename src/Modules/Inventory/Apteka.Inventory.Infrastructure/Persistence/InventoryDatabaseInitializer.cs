using Apteka.Infrastructure.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Apteka.Inventory.Infrastructure.Persistence;

/// <summary>Stock is seeded through Purchasing (posted demo deliveries), never directly.</summary>
internal sealed class InventoryDatabaseInitializer(InventoryDbContext context) : IModuleDatabaseInitializer
{
    public int Order => 20;

    public Task MigrateAsync(CancellationToken cancellationToken) => context.Database.MigrateAsync(cancellationToken);

    public Task SeedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
