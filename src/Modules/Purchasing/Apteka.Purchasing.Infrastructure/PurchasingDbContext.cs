using Apteka.Infrastructure.Common;
using Microsoft.EntityFrameworkCore;

namespace Apteka.Purchasing.Infrastructure;

public static class PurchasingSchema
{
    public const string Name = "purchasing";
}

public sealed class PurchasingDbContext(DbContextOptions<PurchasingDbContext> options)
    : ModuleDbContext(options, PurchasingSchema.Name);