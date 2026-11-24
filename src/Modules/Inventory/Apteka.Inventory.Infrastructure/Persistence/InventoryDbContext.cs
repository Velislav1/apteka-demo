using Apteka.Infrastructure.Common;
using Apteka.Inventory.Domain.Batches;
using Apteka.Inventory.Domain.StockMovements;
using Microsoft.EntityFrameworkCore;

namespace Apteka.Inventory.Infrastructure;

public static class InventorySchema
{
    public const string Name = "inventory";
}

public sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options)
    : ModuleDbContext(options, InventorySchema.Name)
{
    public DbSet<Batch> Batches => Set<Batch>();

    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InventoryDbContext).Assembly);
        UseSnakeCaseNames(modelBuilder);
    }
}
