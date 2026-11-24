using Apteka.Purchasing.Domain.Corrections;
using Apteka.Purchasing.Domain.Deliveries;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Apteka.Purchasing.Infrastructure.Persistence.Configurations;

internal sealed class DeliveryCorrectionConfiguration : IEntityTypeConfiguration<DeliveryCorrection>
{
    public void Configure(EntityTypeBuilder<DeliveryCorrection> builder)
    {
        builder.ToTable("DeliveryCorrections");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.Number)
            .HasDefaultValueSql($"nextval('{PurchasingSchema.Name}.{PurchasingSchema.CorrectionNumberSequence}')")
            .ValueGeneratedOnAdd();
        builder.HasIndex(c => c.Number).IsUnique();
        builder.Property(c => c.NoteNo).HasMaxLength(DeliveryCorrection.MaxNoteNoLength);
        builder.Property(c => c.Reason).HasMaxLength(DeliveryCorrection.MaxReasonLength);
        builder.Ignore(c => c.PurchaseTotal);
        builder.Ignore(c => c.DomainEvents);

        builder.HasOne<Delivery>().WithMany().HasForeignKey(c => c.DeliveryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(c => c.Lines).WithOne().HasForeignKey(l => l.CorrectionId).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(c => c.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(c => c.CreatedAt);
    }
}

internal sealed class DeliveryCorrectionLineConfiguration : IEntityTypeConfiguration<DeliveryCorrectionLine>
{
    public void Configure(EntityTypeBuilder<DeliveryCorrectionLine> builder)
    {
        builder.ToTable("DeliveryCorrectionLines", t => t.HasCheckConstraint("ck_delivery_correction_lines_delta", "quantity_delta <> 0"));
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedNever();
        builder.Property(l => l.UnitPurchasePrice).HasPrecision(12, 4);
        builder.Ignore(l => l.BaseQuantityDelta);
        builder.Ignore(l => l.PurchaseTotal);
        builder.Ignore(l => l.DomainEvents);

        builder.HasOne<DeliveryLine>().WithMany().HasForeignKey(l => l.DeliveryLineId).OnDelete(DeleteBehavior.Restrict);
    }
}
