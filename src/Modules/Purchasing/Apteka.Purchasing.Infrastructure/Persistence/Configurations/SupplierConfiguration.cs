using Apteka.Purchasing.Domain.Suppliers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Apteka.Purchasing.Infrastructure.Persistence.Configurations;

internal sealed class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public const string UniqueNameIndex = "ux_suppliers_name";
    public const string UniqueEikIndex = "ux_suppliers_eik";

    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("Suppliers");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.Name).HasMaxLength(Supplier.MaxNameLength).IsRequired();
        builder.Property(s => s.Eik).HasMaxLength(13).IsRequired();
        builder.Ignore(s => s.DomainEvents);

        builder.HasIndex(s => s.Name).IsUnique().HasDatabaseName(UniqueNameIndex);
        builder.HasIndex(s => s.Eik).IsUnique().HasDatabaseName(UniqueEikIndex);
    }
}
