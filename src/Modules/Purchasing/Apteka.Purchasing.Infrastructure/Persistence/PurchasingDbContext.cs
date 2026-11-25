using Apteka.Purchasing.Domain.Corrections;
using Apteka.Infrastructure.Common;
using Apteka.Purchasing.Domain.Deliveries;
using Apteka.Purchasing.Domain.Suppliers;
using Microsoft.EntityFrameworkCore;

namespace Apteka.Purchasing.Infrastructure;

public static class PurchasingSchema
{
    public const string Name = "purchasing";
    public const string DeliveryNumberSequence = "delivery_numbers";
    public const string CorrectionNumberSequence = "delivery_correction_numbers";
    public const string OrderNumberSequence = "purchase_order_numbers";
}

public sealed class PurchasingDbContext(DbContextOptions<PurchasingDbContext> options)
    : ModuleDbContext(options, PurchasingSchema.Name)
{
    public DbSet<Supplier> Suppliers => Set<Supplier>();

    public DbSet<Delivery> Deliveries => Set<Delivery>();

    public DbSet<DeliveryLine> DeliveryLines => Set<DeliveryLine>();

    public DbSet<DeliveryCorrection> DeliveryCorrections => Set<DeliveryCorrection>();

    public DbSet<Apteka.Purchasing.Domain.Payments.SupplierPayment> SupplierPayments => Set<Apteka.Purchasing.Domain.Payments.SupplierPayment>();

    public DbSet<Apteka.Purchasing.Domain.Payments.PaymentAllocation> PaymentAllocations => Set<Apteka.Purchasing.Domain.Payments.PaymentAllocation>();

    public DbSet<Apteka.Purchasing.Domain.Orders.PurchaseOrder> PurchaseOrders => Set<Apteka.Purchasing.Domain.Orders.PurchaseOrder>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasSequence<long>(PurchasingSchema.DeliveryNumberSequence, PurchasingSchema.Name).StartsAt(1);
        modelBuilder.HasSequence<long>(PurchasingSchema.CorrectionNumberSequence, PurchasingSchema.Name).StartsAt(1);
        modelBuilder.HasSequence<long>(PurchasingSchema.OrderNumberSequence, PurchasingSchema.Name).StartsAt(1);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PurchasingDbContext).Assembly);
        UseSnakeCaseNames(modelBuilder);
    }
}
