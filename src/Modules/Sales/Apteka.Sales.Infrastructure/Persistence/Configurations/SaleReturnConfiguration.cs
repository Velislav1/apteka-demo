using Apteka.Sales.Domain.Returns;
using Apteka.Sales.Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Apteka.Sales.Infrastructure.Persistence.Configurations;

internal sealed class SaleReturnConfiguration : IEntityTypeConfiguration<SaleReturn>
{
    public void Configure(EntityTypeBuilder<SaleReturn> builder)
    {
        builder.ToTable("SaleReturns");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.Number)
            .HasDefaultValueSql($"nextval('{SalesSchema.Name}.{SalesSchema.ReturnNumberSequence}')")
            .ValueGeneratedOnAdd();
        builder.HasIndex(r => r.Number).IsUnique();
        builder.Property(r => r.Reason).HasMaxLength(SaleReturn.MaxReasonLength);
        builder.Property(r => r.Total).HasPrecision(12, 2);
        builder.Property(r => r.VatTotal).HasPrecision(12, 2);
        builder.Ignore(r => r.DomainEvents);

        builder.HasOne<Sale>().WithMany().HasForeignKey(r => r.SaleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(r => r.Lines).WithOne().HasForeignKey(l => l.ReturnId).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(r => r.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(r => r.CreatedAt);
    }
}

internal sealed class SaleReturnLineConfiguration : IEntityTypeConfiguration<SaleReturnLine>
{
    public void Configure(EntityTypeBuilder<SaleReturnLine> builder)
    {
        builder.ToTable("SaleReturnLines", t => t.HasCheckConstraint("ck_sale_return_lines_quantity", "quantity > 0"));
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedNever();
        builder.Property(l => l.Amount).HasPrecision(12, 2);
        builder.Property(l => l.VatAmount).HasPrecision(12, 2);
        builder.Ignore(l => l.DomainEvents);

        builder.HasOne<SaleLine>().WithMany().HasForeignKey(l => l.SaleLineId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(l => l.Batches).WithOne().HasForeignKey(b => b.ReturnLineId).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(l => l.Batches).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class SaleReturnLineBatchConfiguration : IEntityTypeConfiguration<SaleReturnLineBatch>
{
    public void Configure(EntityTypeBuilder<SaleReturnLineBatch> builder)
    {
        builder.ToTable("SaleReturnLineBatches", t => t.HasCheckConstraint("ck_sale_return_line_batches_quantity", "base_quantity > 0"));
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedNever();
        builder.Ignore(b => b.DomainEvents);

        // BatchId points to inventory.batches: no foreign key across module schemas.
        builder.HasIndex(b => b.BatchId);
    }
}
