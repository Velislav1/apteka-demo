using Apteka.Inventory.Domain.Batches;
using Apteka.Inventory.Domain.PriceChanges;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Apteka.Inventory.Infrastructure.Persistence.Configurations;

internal sealed class PriceChangeConfiguration : IEntityTypeConfiguration<PriceChange>
{
    public void Configure(EntityTypeBuilder<PriceChange> builder)
    {
        builder.ToTable("PriceChanges");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.Number)
            .HasDefaultValueSql($"nextval('{InventorySchema.Name}.{InventorySchema.PriceChangeNumberSequence}')")
            .ValueGeneratedOnAdd();
        builder.HasIndex(c => c.Number).IsUnique();
        builder.Property(c => c.Reason).HasMaxLength(PriceChange.MaxReasonLength);
        builder.Ignore(c => c.DomainEvents);
        builder.HasMany(c => c.Lines).WithOne().HasForeignKey(l => l.PriceChangeId).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(c => c.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class PriceChangeLineConfiguration : IEntityTypeConfiguration<PriceChangeLine>
{
    public void Configure(EntityTypeBuilder<PriceChangeLine> builder)
    {
        builder.ToTable("PriceChangeLines", t => t.HasCheckConstraint("ck_price_change_lines_new_price", "new_price > 0"));
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedNever();
        builder.Property(l => l.OldPrice).HasPrecision(12, 4);
        builder.Property(l => l.NewPrice).HasPrecision(12, 4);
        builder.Ignore(l => l.DomainEvents);
        builder.HasOne<Batch>().WithMany().HasForeignKey(l => l.BatchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(l => l.ProductId);
    }
}
