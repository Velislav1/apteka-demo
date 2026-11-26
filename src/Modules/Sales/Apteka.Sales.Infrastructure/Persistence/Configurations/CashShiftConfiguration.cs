using Apteka.Sales.Domain.Shifts;
using Apteka.Sales.Infrastructure.Shifts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Apteka.Sales.Infrastructure.Persistence.Configurations;

internal sealed class CashShiftConfiguration : IEntityTypeConfiguration<CashShift>
{
    public void Configure(EntityTypeBuilder<CashShift> builder)
    {
        builder.ToTable("CashShifts");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.Number)
            .HasDefaultValueSql($"nextval('{SalesSchema.Name}.{SalesSchema.ShiftNumberSequence}')")
            .ValueGeneratedOnAdd();
        builder.HasIndex(s => s.Number).IsUnique();
        builder.Property(s => s.OpeningCash).HasPrecision(12, 2);
        builder.Property(s => s.ExpectedCash).HasPrecision(12, 2);
        builder.Property(s => s.CountedCash).HasPrecision(12, 2);
        builder.Property(s => s.Note).HasMaxLength(CashShift.MaxTextLength);
        builder.Ignore(s => s.IsOpen);
        builder.Ignore(s => s.Difference);
        builder.Ignore(s => s.CashIn);
        builder.Ignore(s => s.CashOut);
        builder.Ignore(s => s.DomainEvents);

        // One open shift per user.
        builder.HasIndex(s => s.UserId).IsUnique().HasFilter("closed_at IS NULL").HasDatabaseName(ShiftRepository.OneOpenIndex);

        builder.HasMany(s => s.Movements).WithOne().HasForeignKey(m => m.ShiftId).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(s => s.Movements).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class CashMovementConfiguration : IEntityTypeConfiguration<CashMovement>
{
    public void Configure(EntityTypeBuilder<CashMovement> builder)
    {
        builder.ToTable("CashMovements", t => t.HasCheckConstraint("ck_cash_movements_amount", "amount <> 0"));
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();
        builder.Property(m => m.Amount).HasPrecision(12, 2);
        builder.Property(m => m.Reason).HasMaxLength(CashShift.MaxTextLength).IsRequired();
        builder.Ignore(m => m.DomainEvents);
    }
}
