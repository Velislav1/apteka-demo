using Apteka.Infrastructure.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Apteka.Sales.Infrastructure.Persistence;

/// <summary>No demo sales: the till starts empty every day.</summary>
internal sealed class SalesDatabaseInitializer(SalesDbContext context) : IModuleDatabaseInitializer
{
    public int Order => 40;

    public Task MigrateAsync(CancellationToken cancellationToken) => context.Database.MigrateAsync(cancellationToken);

    public Task SeedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
