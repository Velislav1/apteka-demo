using Apteka.Inventory.Domain.Batches;
using Apteka.Inventory.Domain.WriteOffs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Apteka.Inventory.Infrastructure.Persistence.Configurations;

internal sealed class WriteOffConfiguration : IEntityTypeConfiguration<WriteOff>
{
    public void Configure(EntityTypeBuilder<WriteOff> builder)
    {
        builder.ToTable("WriteOffs");
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id).ValueGeneratedNever();
        builder.Property(w => w.Number)
            .HasDefaultValueSql($"nextval('{InventorySchema.Name}.{InventorySchema.WriteOffNumberSequence}')")
            .ValueGeneratedOnAdd();
        builder.HasIndex(w => w.Number).IsUnique();
        builder.Property(w => w.Reason).HasMaxLength(WriteOff.MaxReasonLength);
        builder.Ignore(w => w.PurchaseTotal);
        builder.Ignore(w => w.DomainEvents);

        builder.HasMany(w => w.Lines).WithOne().HasForeignKey(l => l.WriteOffId).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(w => w.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(w => w.CreatedAt);
    }
}

internal sealed class WriteOffLineConfiguration : IEntityTypeConfiguration<WriteOffLine>
{
    public void Configure(EntityTypeBuilder<WriteOffLine> builder)
    {
        builder.ToTable("WriteOffLines", t => t.HasCheckConstraint("ck_write_off_lines_quantity", "quantity > 0"));
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedNever();
        builder.Property(l => l.ProductName).HasMaxLength(200).IsRequired();
        builder.Property(l => l.UnitName).HasMaxLength(50).IsRequired();
        builder.Property(l => l.BatchNo).HasMaxLength(Batch.MaxBatchNoLength).IsRequired();
        builder.Property(l => l.PurchasePrice).HasPrecision(12, 4);
        builder.Ignore(l => l.BaseQuantity);
        builder.Ignore(l => l.PurchaseTotal);
        builder.Ignore(l => l.DomainEvents);

        builder.HasOne<Batch>().WithMany().HasForeignKey(l => l.BatchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(l => l.ProductId);
    }
}
