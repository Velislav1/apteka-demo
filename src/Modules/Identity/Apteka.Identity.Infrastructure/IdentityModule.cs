using Apteka.Infrastructure.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Apteka.Identity.Infrastructure;

public static class IdentityModule
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services, string connectionString)
    {
        services.AddModuleDbContext<IdentityDbContext>(connectionString, IdentitySchema.Name);

        return services;
    }
}
