using Microsoft.EntityFrameworkCore;

namespace Apteka.Infrastructure.Common;

/// <summary>Base for every module DbContext: each module owns exactly one PostgreSQL schema.</summary>
public abstract class ModuleDbContext(DbContextOptions options, string schema) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(schema);
        base.OnModelCreating(modelBuilder);
    }
}