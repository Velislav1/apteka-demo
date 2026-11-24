using Apteka.Inventory.Domain.Batches;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Apteka.Inventory.Infrastructure.Persistence.Configurations;

internal sealed class BatchConfiguration : IEntityTypeConfiguration<Batch>
{
    public void Configure(EntityTypeBuilder<Batch> builder)
    {
        builder.ToTable("Batches");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedNever();
        builder.Property(b => b.BatchNo).HasMaxLength(Batch.MaxBatchNoLength).IsRequired();
        builder.Property(b => b.StockType).HasConversion<string>().HasMaxLength(10);
        builder.Property(b => b.PurchasePrice).HasPrecision(12, 4);
        builder.Property(b => b.SalePrice).HasPrecision(12, 4);
        builder.Property(b => b.PricedUnitSalePrice).HasPrecision(12, 4);
        builder.Property(b => b.SourceDocumentType).HasMaxLength(30).IsRequired();
        builder.Ignore(b => b.DomainEvents);

        // ProductId points to catalog.products: no foreign key across module schemas.
        builder.HasIndex(b => new { b.ProductId, b.ExpiryDate });
        builder.HasIndex(b => b.SourceLineId).IsUnique().HasDatabaseName("ux_batches_source_line_id");
    }
}

internal sealed class StockMovementConfiguration : IEntityTypeConfiguration<Domain.StockMovements.StockMovement>
{
    public void Configure(EntityTypeBuilder<Domain.StockMovements.StockMovement> builder)
    {
        builder.ToTable("StockMovements");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();
        builder.Property(m => m.Kind).HasConversion<string>().HasMaxLength(20);
        builder.Property(m => m.DocumentType).HasMaxLength(30).IsRequired();
        builder.Ignore(m => m.DomainEvents);

        builder.HasOne<Batch>().WithMany().HasForeignKey(m => m.BatchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(m => new { m.DocumentType, m.DocumentId });
    }
}
