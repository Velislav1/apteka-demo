using Apteka.Purchasing.Domain.Expenses;
using Apteka.Purchasing.Domain.Refusals;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Apteka.Purchasing.Infrastructure.Persistence.Configurations;

internal sealed class RefusalConfiguration : IEntityTypeConfiguration<Refusal>
{
    public void Configure(EntityTypeBuilder<Refusal> builder)
    {
        builder.ToTable("Refusals", t => t.HasCheckConstraint("ck_refusals_quantity", "quantity > 0"));
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.Text).HasMaxLength(Refusal.MaxTextLength);
        builder.Property(r => r.Note).HasMaxLength(Refusal.MaxTextLength);
        builder.Ignore(r => r.DomainEvents);
        builder.HasIndex(r => r.CreatedAt);

        // ProductId points to the catalog: no foreign key across module schemas.
        builder.HasIndex(r => r.ProductId);
    }
}

internal sealed class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.ToTable("Expenses", t => t.HasCheckConstraint("ck_expenses_amount", "amount <> 0"));
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Category).HasMaxLength(Expense.MaxTextLength).IsRequired();
        builder.Property(e => e.Amount).HasPrecision(12, 2);
        builder.Property(e => e.DocumentNo).HasMaxLength(30);
        builder.Property(e => e.Note).HasMaxLength(Expense.MaxTextLength);
        builder.Ignore(e => e.DomainEvents);
        builder.HasIndex(e => e.Date);
    }
}
