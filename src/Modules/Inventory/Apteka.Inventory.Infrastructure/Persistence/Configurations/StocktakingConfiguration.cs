using Apteka.Inventory.Domain.Batches;
using Apteka.Inventory.Domain.Stocktakings;
using Apteka.Inventory.Infrastructure.Stocktakings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Apteka.Inventory.Infrastructure.Persistence.Configurations;

internal sealed class StocktakingConfiguration : IEntityTypeConfiguration<Stocktaking>
{
    public void Configure(EntityTypeBuilder<Stocktaking> builder)
    {
        builder.ToTable("Stocktakings");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.Number)
            .HasDefaultValueSql($"nextval('{InventorySchema.Name}.{InventorySchema.StocktakingNumberSequence}')")
            .ValueGeneratedOnAdd();
        builder.HasIndex(s => s.Number).IsUnique();
        builder.Property(s => s.Scope).HasMaxLength(Stocktaking.MaxScopeLength).IsRequired();
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(10);
        builder.Ignore(s => s.DomainEvents);

        // At most one open stocktaking at a time.
        builder.HasIndex(s => s.Status).IsUnique().HasFilter("status = 'Open'").HasDatabaseName(StocktakingRepository.OneOpenIndex);

        builder.HasMany(s => s.Lines).WithOne().HasForeignKey(l => l.StocktakingId).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(s => s.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class StocktakingLineConfiguration : IEntityTypeConfiguration<StocktakingLine>
{
    public void Configure(EntityTypeBuilder<StocktakingLine> builder)
    {
        builder.ToTable("StocktakingLines", t => t.HasCheckConstraint("ck_stocktaking_lines_counted", "counted_quantity IS NULL OR counted_quantity >= 0"));
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedNever();
        builder.Property(l => l.ProductName).HasMaxLength(200).IsRequired();
        builder.Property(l => l.BatchNo).HasMaxLength(Batch.MaxBatchNoLength).IsRequired();
        builder.Property(l => l.PurchasePrice).HasPrecision(12, 4);
        builder.Ignore(l => l.DifferenceValue);
        builder.Ignore(l => l.DomainEvents);

        builder.HasOne<Batch>().WithMany().HasForeignKey(l => l.BatchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(l => l.ProductId);
    }
}
