using Apteka.Infrastructure.Common;
using Microsoft.EntityFrameworkCore;

namespace Apteka.Sales.Infrastructure;

public static class SalesSchema
{
    public const string Name = "sales";
}

public sealed class SalesDbContext(DbContextOptions<SalesDbContext> options)
    : ModuleDbContext(options, SalesSchema.Name);