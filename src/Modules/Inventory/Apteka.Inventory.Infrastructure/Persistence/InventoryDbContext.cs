using Apteka.Infrastructure.Common;
using Apteka.Inventory.Domain.Batches;
using Apteka.Inventory.Domain.StockMovements;
using Apteka.Inventory.Domain.WriteOffs;
using Microsoft.EntityFrameworkCore;

namespace Apteka.Inventory.Infrastructure;

public static class InventorySchema
{
    public const string Name = "inventory";
    public const string WriteOffNumberSequence = "write_off_numbers";
}

public sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options)
    : ModuleDbContext(options, InventorySchema.Name)
{
    public DbSet<Batch> Batches => Set<Batch>();

    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    public DbSet<WriteOff> WriteOffs => Set<WriteOff>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasSequence<long>(InventorySchema.WriteOffNumberSequence, InventorySchema.Name).StartsAt(1);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InventoryDbContext).Assembly);
        UseSnakeCaseNames(modelBuilder);
    }
}
