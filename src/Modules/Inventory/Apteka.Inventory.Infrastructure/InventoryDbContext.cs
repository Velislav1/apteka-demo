using Apteka.Infrastructure.Common;
using Microsoft.EntityFrameworkCore;

namespace Apteka.Inventory.Infrastructure;

public static class InventorySchema
{
    public const string Name = "inventory";
}

public sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options)
    : ModuleDbContext(options, InventorySchema.Name);