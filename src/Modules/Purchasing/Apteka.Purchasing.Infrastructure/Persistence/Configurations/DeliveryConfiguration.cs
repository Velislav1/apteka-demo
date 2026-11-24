using Apteka.Purchasing.Domain.Deliveries;
using Apteka.Purchasing.Domain.Suppliers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Apteka.Purchasing.Infrastructure.Persistence.Configurations;

internal sealed class DeliveryConfiguration : IEntityTypeConfiguration<Delivery>
{
    public const string UniqueInvoiceIndex = "ux_deliveries_supplier_id_invoice_no";

    public void Configure(EntityTypeBuilder<Delivery> builder)
    {
        builder.ToTable("Deliveries");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever();
        builder.Property(d => d.InvoiceNo).HasMaxLength(DeliveryErrors.MaxInvoiceNoLength).IsRequired();
        builder.Property(d => d.Status).HasConversion<string>().HasMaxLength(10);
        builder.Ignore(d => d.Lines);
        builder.Ignore(d => d.PurchaseTotal);
        builder.Ignore(d => d.DomainEvents);

        builder.HasMany(d => d.AllLines).WithOne().HasForeignKey(l => l.DeliveryId).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(d => d.AllLines).UsePropertyAccessMode(PropertyAccessMode.Field).HasField("_lines");
        builder.HasOne<Supplier>().WithMany().HasForeignKey(d => d.SupplierId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(d => new { d.SupplierId, d.InvoiceNo }).IsUnique().HasDatabaseName(UniqueInvoiceIndex);
        builder.HasIndex(d => d.ReceivedAt);
    }
}

internal sealed class DeliveryLineConfiguration : IEntityTypeConfiguration<DeliveryLine>
{
    public void Configure(EntityTypeBuilder<DeliveryLine> builder)
    {
        builder.ToTable("DeliveryLines");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedNever();
        builder.Property(l => l.UnitPurchasePrice).HasPrecision(12, 4);
        builder.Property(l => l.SalePrice).HasPrecision(12, 4);
        builder.Property(l => l.MarkupPercent).HasPrecision(7, 2);
        builder.Property(l => l.BatchNo).HasMaxLength(DeliveryErrors.MaxBatchNoLength).IsRequired();
        builder.Ignore(l => l.BaseQuantity);
        builder.Ignore(l => l.PurchaseTotal);
        builder.Ignore(l => l.DomainEvents);
        builder.ToTable(t => t.HasCheckConstraint("ck_delivery_lines_quantity", "quantity > 0 AND unit_factor >= 1"));

        // ProductId / ProductUnitId point to the catalog schema: no foreign keys across modules.
        builder.HasIndex(l => l.ProductId);
    }
}
