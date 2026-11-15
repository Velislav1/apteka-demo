using Microsoft.Extensions.DependencyInjection;

namespace Apteka.Infrastructure.Common;

public static class InfrastructureCommonExtensions
{
    public static IServiceCollection AddAptekaCommon(this IServiceCollection services, string connectionString)
    {
        services.AddSingleton(new DatabaseStatus(connectionString));
        return services;
    }
}