using Apteka.Infrastructure.Common;
using Microsoft.EntityFrameworkCore;

namespace Apteka.Audit.Infrastructure;

public static class AuditSchema
{
    public const string Name = "audit";
}

public sealed class AuditDbContext(DbContextOptions<AuditDbContext> options)
    : ModuleDbContext(options, AuditSchema.Name);