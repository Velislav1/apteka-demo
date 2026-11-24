using Apteka.Audit.Domain.AuditLog;
using Apteka.Infrastructure.Common;
using Apteka.Infrastructure.Common.Auditing;
using Microsoft.EntityFrameworkCore;

namespace Apteka.Audit.Infrastructure;

public static class AuditSchema
{
    public const string Name = AuditLogTable.Schema;
}

public sealed class AuditDbContext(DbContextOptions<AuditDbContext> options)
    : ModuleDbContext(options, AuditSchema.Name)
{
    public DbSet<AuditLogEntry> AuditLog => Set<AuditLogEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AuditLogEntry>(b =>
        {
            b.ToTable(AuditLogTable.Name);
            b.HasKey(e => e.Id);
            b.Property(e => e.UserName).HasMaxLength(100).IsRequired();
            b.Property(e => e.At).IsRequired();
            b.Property(e => e.Action).HasMaxLength(20).IsRequired();
            b.Property(e => e.Entity).HasMaxLength(100).IsRequired();
            b.Property(e => e.EntityId).HasMaxLength(100).IsRequired();
            b.Property(e => e.OldValues).HasColumnType("jsonb");
            b.Property(e => e.NewValues).HasColumnType("jsonb");
            b.HasIndex(e => new { e.Entity, e.EntityId });
            b.HasIndex(e => e.At);
        });

        UseSnakeCaseNames(modelBuilder);
    }
}
