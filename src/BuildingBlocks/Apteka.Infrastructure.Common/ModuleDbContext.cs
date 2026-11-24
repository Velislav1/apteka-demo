using Apteka.Infrastructure.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Apteka.Infrastructure.Common;

/// <summary>Base for every module DbContext: each module owns exactly one PostgreSQL schema.</summary>
public abstract class ModuleDbContext(DbContextOptions options, string schema) : DbContext(options)
{
    public string Schema { get; } = schema;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        base.OnModelCreating(modelBuilder);
    }

    /// <summary>Call at the end of <see cref="OnModelCreating"/> in a module, after its configurations are applied.</summary>
    protected static void UseSnakeCaseNames(ModelBuilder modelBuilder) => SnakeCaseNaming.Apply(modelBuilder);
}
