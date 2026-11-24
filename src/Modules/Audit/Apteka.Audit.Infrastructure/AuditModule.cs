using Apteka.Audit.Infrastructure.Persistence;
using Apteka.Infrastructure.Common;
using Apteka.Infrastructure.Common.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Apteka.Audit.Infrastructure;

public static class AuditModule
{
    public static IServiceCollection AddAuditModule(this IServiceCollection services, string connectionString)
    {
        services.AddModuleDbContext<AuditDbContext>(connectionString, AuditSchema.Name);
        services.AddScoped<IModuleDatabaseInitializer, AuditDatabaseInitializer>();

        return services;
    }
}
