using Apteka.Purchasing.Domain.Deliveries;
using Apteka.Purchasing.Domain.Payments;
using Apteka.Purchasing.Domain.Suppliers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Apteka.Purchasing.Infrastructure.Persistence.Configurations;

internal sealed class SupplierPaymentConfiguration : IEntityTypeConfiguration<SupplierPayment>
{
    public void Configure(EntityTypeBuilder<SupplierPayment> builder)
    {
        builder.ToTable("SupplierPayments", t => t.HasCheckConstraint("ck_supplier_payments_amount", "amount > 0"));
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.Amount).HasPrecision(12, 2);
        builder.Property(p => p.Channel).HasConversion<string>().HasMaxLength(10);
        builder.Property(p => p.Note).HasMaxLength(SupplierPayment.MaxNoteLength);
        builder.Ignore(p => p.DomainEvents);

        builder.HasOne<Supplier>().WithMany().HasForeignKey(p => p.SupplierId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(p => p.Allocations).WithOne().HasForeignKey(a => a.PaymentId).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(p => p.Allocations).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(p => p.PaidOn);
    }
}

internal sealed class PaymentAllocationConfiguration : IEntityTypeConfiguration<PaymentAllocation>
{
    public void Configure(EntityTypeBuilder<PaymentAllocation> builder)
    {
        builder.ToTable("PaymentAllocations", t => t.HasCheckConstraint("ck_payment_allocations_amount", "amount > 0"));
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.Property(a => a.Amount).HasPrecision(12, 2);
        builder.Ignore(a => a.DomainEvents);
        builder.HasOne<Delivery>().WithMany().HasForeignKey(a => a.DeliveryId).OnDelete(DeleteBehavior.Restrict);
    }
}
