using Apteka.Sales.Domain.CustomerPayments;
using Apteka.Sales.Domain.Customers;
using Apteka.Sales.Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Apteka.Sales.Infrastructure.Persistence.Configurations;

internal sealed class CustomerPaymentConfiguration : IEntityTypeConfiguration<CustomerPayment>
{
    public void Configure(EntityTypeBuilder<CustomerPayment> builder)
    {
        builder.ToTable("CustomerPayments", t => t.HasCheckConstraint("ck_customer_payments_amount", "amount > 0"));
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.Amount).HasPrecision(12, 2);
        builder.Property(p => p.Channel).HasConversion<string>().HasMaxLength(10);
        builder.Property(p => p.Note).HasMaxLength(CustomerPayment.MaxNoteLength);
        builder.Ignore(p => p.DomainEvents);
        builder.HasOne<Customer>().WithMany().HasForeignKey(p => p.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(p => p.Allocations).WithOne().HasForeignKey(a => a.PaymentId).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(p => p.Allocations).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(p => p.PaidOn);
    }
}

internal sealed class CustomerPaymentAllocationConfiguration : IEntityTypeConfiguration<CustomerPaymentAllocation>
{
    public void Configure(EntityTypeBuilder<CustomerPaymentAllocation> builder)
    {
        builder.ToTable("CustomerPaymentAllocations", t => t.HasCheckConstraint("ck_customer_payment_allocations_amount", "amount > 0"));
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.Property(a => a.Amount).HasPrecision(12, 2);
        builder.Ignore(a => a.DomainEvents);
        builder.HasOne<Sale>().WithMany().HasForeignKey(a => a.SaleId).OnDelete(DeleteBehavior.Restrict);
    }
}
