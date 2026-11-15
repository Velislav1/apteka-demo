using Apteka.Infrastructure.Common;
using Microsoft.EntityFrameworkCore;

namespace Apteka.Identity.Infrastructure;

public static class IdentitySchema
{
    public const string Name = "identity";
}

public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options)
    : ModuleDbContext(options, IdentitySchema.Name);