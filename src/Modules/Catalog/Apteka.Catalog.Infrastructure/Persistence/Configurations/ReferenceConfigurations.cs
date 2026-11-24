using Apteka.Catalog.Domain.Common;
using Apteka.Catalog.Domain.Manufacturers;
using Apteka.Catalog.Domain.ProductGroups;
using Apteka.Catalog.Domain.Shelves;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Apteka.Catalog.Infrastructure.Persistence.Configurations;

/// <summary>Shared mapping of the simple catalog lists: id, unique name, active flag.</summary>
internal abstract class ReferenceConfiguration<T>(string table) : IEntityTypeConfiguration<T>
    where T : CatalogReference
{
    public virtual void Configure(EntityTypeBuilder<T> builder)
    {
        builder.ToTable(table);
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.Name).HasMaxLength(CatalogReference.MaxNameLength).IsRequired();
        builder.Ignore(r => r.DomainEvents);

        // The database guarantees unique names; the use cases also reject names that differ only by case.
        builder.HasIndex(r => r.Name).IsUnique().HasDatabaseName(UniqueNameIndex(table));
    }

    public static string UniqueNameIndex(string table) => $"ux_{table}_name";
}

internal sealed class ManufacturerConfiguration() : ReferenceConfiguration<Manufacturer>(Tables.Manufacturers);

internal sealed class ProductGroupConfiguration() : ReferenceConfiguration<ProductGroup>(Tables.ProductGroups)
{
    public override void Configure(EntityTypeBuilder<ProductGroup> builder)
    {
        base.Configure(builder);
        builder.Property(g => g.MarkupPercent).HasPrecision(7, 2);
    }
}

internal sealed class ShelfConfiguration() : ReferenceConfiguration<Shelf>(Tables.Shelves);

internal static class Tables
{
    public const string Manufacturers = "manufacturers";
    public const string ProductGroups = "product_groups";
    public const string Shelves = "shelves";
}
