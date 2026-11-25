using Apteka.Inventory.Domain.BatchBlocks;
using Apteka.Inventory.Domain.Batches;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Apteka.Inventory.Infrastructure.Persistence.Configurations;

internal sealed class BatchBlockConfiguration : IEntityTypeConfiguration<BatchBlock>
{
    public void Configure(EntityTypeBuilder<BatchBlock> builder)
    {
        builder.ToTable("BatchBlocks");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedNever();
        builder.Property(b => b.Reason).HasMaxLength(BatchBlock.MaxReasonLength);
        builder.Ignore(b => b.DomainEvents);
        builder.HasOne<Batch>().WithMany().HasForeignKey(b => b.BatchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(b => new { b.BatchId, b.At });
    }
}
