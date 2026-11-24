using Apteka.Sales.Domain.Reversals;
using Apteka.Sales.Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Apteka.Sales.Infrastructure.Persistence.Configurations;

internal sealed class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.ToTable("Sales");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        // Gap-free enough for a demo; the fiscal receipt number (later) comes from the fiscal printer.
        builder.Property(s => s.Number)
            .HasDefaultValueSql($"nextval('{SalesSchema.Name}.{SalesSchema.SaleNumberSequence}')")
            .ValueGeneratedOnAdd();
        builder.HasIndex(s => s.Number).IsUnique();

        builder.Property(s => s.SaleType).HasConversion<string>().HasMaxLength(10);
        builder.Property(s => s.PaymentMethod).HasConversion<string>().HasMaxLength(10);
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(10);
        builder.Property(s => s.Total).HasPrecision(12, 2);
        builder.Property(s => s.VatTotal).HasPrecision(12, 2);
        builder.Property(s => s.CashReceived).HasPrecision(12, 2);
        builder.Ignore(s => s.Change);
        builder.Ignore(s => s.DomainEvents);

        builder.HasMany(s => s.Lines).WithOne().HasForeignKey(l => l.SaleId).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(s => s.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(s => s.CreatedAt);
        builder.HasOne<Apteka.Sales.Domain.Customers.Customer>().WithMany().HasForeignKey(s => s.CustomerId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class SaleLineConfiguration : IEntityTypeConfiguration<SaleLine>
{
    public void Configure(EntityTypeBuilder<SaleLine> builder)
    {
        builder.ToTable("SaleLines");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedNever();
        builder.Property(l => l.ProductName).HasMaxLength(200).IsRequired();
        builder.Property(l => l.UnitName).HasMaxLength(50).IsRequired();
        builder.Property(l => l.UnitPrice).HasPrecision(12, 4);
        builder.Property(l => l.DiscountPercent).HasPrecision(5, 2);
        builder.Property(l => l.DiscountAmount).HasPrecision(12, 2);
        builder.Property(l => l.VatRate).HasPrecision(5, 2);
        builder.Property(l => l.LineTotal).HasPrecision(12, 2);
        builder.Property(l => l.VatAmount).HasPrecision(12, 2);
        builder.Ignore(l => l.DomainEvents);
        builder.ToTable(t => t.HasCheckConstraint("ck_sale_lines_quantity", "quantity > 0"));

        // ProductId (catalog) has no foreign key across module schemas.
        builder.HasMany(l => l.Batches).WithOne().HasForeignKey(b => b.SaleLineId).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(l => l.Batches).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(l => l.ProductId);
    }
}

internal sealed class SaleLineBatchConfiguration : IEntityTypeConfiguration<SaleLineBatch>
{
    public void Configure(EntityTypeBuilder<SaleLineBatch> builder)
    {
        builder.ToTable("SaleLineBatches");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedNever();
        builder.Ignore(b => b.DomainEvents);

        // BatchId points to inventory.batches: no foreign key across module schemas.
        builder.HasIndex(b => b.BatchId);
        builder.ToTable(t => t.HasCheckConstraint("ck_sale_line_batches_quantity", "base_quantity > 0"));
    }
}

internal sealed class ReversalConfiguration : IEntityTypeConfiguration<Reversal>
{
    public void Configure(EntityTypeBuilder<Reversal> builder)
    {
        builder.ToTable("Reversals");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.Reason).HasMaxLength(Reversal.MaxReasonLength);
        builder.Ignore(r => r.DomainEvents);

        // A sale can be reversed only once, even by two users at the same time.
        builder.HasOne<Sale>().WithMany().HasForeignKey(r => r.SaleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(r => r.SaleId).IsUnique();
    }
}
