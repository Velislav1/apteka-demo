using Apteka.Catalog.Domain.Manufacturers;
using Apteka.Catalog.Domain.ProductGroups;
using Apteka.Catalog.Domain.Products;
using Apteka.Catalog.Domain.Shelves;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Apteka.Catalog.Infrastructure.Persistence.Configurations;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.Name).HasMaxLength(CatalogErrors.MaxNameLength).IsRequired();

        // Номенклатурен номер from a sequence; existing products get one when the column is added.
        builder.Property(p => p.Code)
            .HasDefaultValueSql($"nextval('{CatalogSchema.Name}.{CatalogSchema.ProductCodeSequence}')")
            .ValueGeneratedOnAdd();
        builder.HasIndex(p => p.Code).IsUnique();
        builder.Property(p => p.LatinName).HasMaxLength(CatalogErrors.MaxNameLength);
        builder.Property(p => p.GenericName).HasMaxLength(CatalogErrors.MaxNameLength);
        builder.Property(p => p.MarkupPercent).HasPrecision(7, 2);
        builder.Property(p => p.AtcCode).HasMaxLength(7);
        builder.Property(p => p.DispensingMode).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.NhifCode).HasMaxLength(20);
        builder.Property(p => p.VatRate).HasPrecision(5, 2);
        builder.Ignore(p => p.IsRx);
        builder.Ignore(p => p.BaseUnit);
        builder.Ignore(p => p.DomainEvents);

        builder.HasMany(p => p.Units).WithOne().HasForeignKey(u => u.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(p => p.Barcodes).WithOne().HasForeignKey(b => b.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(p => p.Units).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(p => p.Barcodes).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasOne<Manufacturer>().WithMany().HasForeignKey(p => p.ManufacturerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ProductGroup>().WithMany().HasForeignKey(p => p.ProductGroupId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Shelf>().WithMany().HasForeignKey(p => p.ShelfId).OnDelete(DeleteBehavior.Restrict);

        // No foreign key for DefaultSaleUnitId: product → unit → product would be a cycle on insert.
        // The aggregate guarantees the unit is one of the product's own units.

        builder.HasIndex(p => p.Name);
        builder.HasIndex(p => p.NhifCode);
    }
}

internal sealed class ProductUnitConfiguration : IEntityTypeConfiguration<ProductUnit>
{
    public void Configure(EntityTypeBuilder<ProductUnit> builder)
    {
        builder.ToTable("ProductUnits");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedNever();
        builder.Property(u => u.Name).HasMaxLength(CatalogErrors.MaxUnitNameLength).IsRequired();
        builder.Ignore(u => u.IsBaseUnit);
        builder.Ignore(u => u.DomainEvents);
        builder.ToTable(t => t.HasCheckConstraint("ck_product_units_factor", "factor >= 1"));

        // Exactly one base unit per product, enforced by the database as well as the domain.
        builder.HasIndex(u => u.ProductId).IsUnique().HasFilter("factor = 1").HasDatabaseName("ux_product_units_one_base_unit");
        builder.HasIndex(u => new { u.ProductId, u.Name }).IsUnique();
    }
}

internal sealed class BarcodeConfiguration : IEntityTypeConfiguration<Barcode>
{
    public void Configure(EntityTypeBuilder<Barcode> builder)
    {
        builder.ToTable("Barcodes");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedNever();
        builder.Property(b => b.Code).HasMaxLength(14).IsRequired();
        builder.Ignore(b => b.DomainEvents);

        builder.HasIndex(b => b.Code).IsUnique().HasDatabaseName(BarcodeConfiguration.UniqueCodeIndex);
        builder.HasOne<ProductUnit>().WithMany().HasForeignKey(b => b.ProductUnitId).OnDelete(DeleteBehavior.Restrict);
    }

    public const string UniqueCodeIndex = "ux_barcodes_code";
}
