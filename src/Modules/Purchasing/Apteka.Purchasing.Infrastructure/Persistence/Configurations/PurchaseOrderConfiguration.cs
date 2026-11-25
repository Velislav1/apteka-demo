using Apteka.Purchasing.Domain.Orders;
using Apteka.Purchasing.Domain.Suppliers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Apteka.Purchasing.Infrastructure.Persistence.Configurations;

internal sealed class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        builder.ToTable("PurchaseOrders");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedNever();
        builder.Property(o => o.Number)
            .HasDefaultValueSql($"nextval('{PurchasingSchema.Name}.{PurchasingSchema.OrderNumberSequence}')")
            .ValueGeneratedOnAdd();
        builder.HasIndex(o => o.Number).IsUnique();
        builder.Property(o => o.Note).HasMaxLength(PurchaseOrder.MaxNoteLength);
        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(10);
        builder.Ignore(o => o.DomainEvents);

        builder.HasOne<Supplier>().WithMany().HasForeignKey(o => o.SupplierId).OnDelete(DeleteBehavior.Restrict);
        builder.Ignore(o => o.Lines);
        builder.HasMany(o => o.AllLines).WithOne().HasForeignKey(l => l.OrderId).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(o => o.AllLines).UsePropertyAccessMode(PropertyAccessMode.Field).HasField("_lines");
    }
}

internal sealed class PurchaseOrderLineConfiguration : IEntityTypeConfiguration<PurchaseOrderLine>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderLine> builder)
    {
        builder.ToTable("PurchaseOrderLines", t => t.HasCheckConstraint("ck_purchase_order_lines_quantity", "quantity > 0"));
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedNever();
        builder.Ignore(l => l.DomainEvents);

        // ProductId / ProductUnitId point to the catalog: no foreign keys across module schemas.
        builder.HasIndex(l => l.ProductId);
    }
}
