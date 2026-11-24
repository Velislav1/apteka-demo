using Apteka.Infrastructure.Common;
using Apteka.Purchasing.Domain.Deliveries;
using Apteka.Purchasing.Domain.Suppliers;
using Microsoft.EntityFrameworkCore;

namespace Apteka.Purchasing.Infrastructure;

public static class PurchasingSchema
{
    public const string Name = "purchasing";
    public const string DeliveryNumberSequence = "delivery_numbers";
}

public sealed class PurchasingDbContext(DbContextOptions<PurchasingDbContext> options)
    : ModuleDbContext(options, PurchasingSchema.Name)
{
    public DbSet<Supplier> Suppliers => Set<Supplier>();

    public DbSet<Delivery> Deliveries => Set<Delivery>();

    public DbSet<DeliveryLine> DeliveryLines => Set<DeliveryLine>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasSequence<long>(PurchasingSchema.DeliveryNumberSequence, PurchasingSchema.Name).StartsAt(1);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PurchasingDbContext).Assembly);
        UseSnakeCaseNames(modelBuilder);
    }
}
