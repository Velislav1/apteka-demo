using Apteka.Sales.Domain.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Apteka.Sales.Infrastructure.Persistence.Configurations;

internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.Code)
            .HasDefaultValueSql($"nextval('{SalesSchema.Name}.{SalesSchema.CustomerCodeSequence}')")
            .ValueGeneratedOnAdd();
        builder.HasIndex(c => c.Code).IsUnique();
        builder.Property(c => c.Name).HasMaxLength(Customer.MaxNameLength).IsRequired();
        builder.Property(c => c.Egn).HasMaxLength(10).IsFixedLength();
        builder.HasIndex(c => c.Egn).IsUnique().HasFilter("egn IS NOT NULL");
        builder.Property(c => c.Phone).HasMaxLength(Customer.MaxPhoneLength);
        builder.Property(c => c.Note).HasMaxLength(Customer.MaxNoteLength);
        builder.HasIndex(c => c.Name);
        builder.Ignore(c => c.DomainEvents);
    }
}
