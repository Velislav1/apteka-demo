using Apteka.Infrastructure.Common.Auditing;
using Apteka.Infrastructure.Common.Persistence;
using Apteka.Infrastructure.Common.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Apteka.Infrastructure.Common;

public static class InfrastructureCommonExtensions
{
    public static IServiceCollection AddAptekaCommon(this IServiceCollection services, string connectionString)
    {
        services.AddSingleton(new DatabaseStatus(connectionString));
        services.TryAddSingleton(TimeProvider.System);

        // Replaced by the Identity module once login exists.
        services.TryAddScoped<ICurrentUser, SystemUser>();
        services.TryAddScoped<AuditInterceptor>();

        return services;
    }

    /// <summary>
    /// Registers a module DbContext on PostgreSQL: its own schema for tables and migration history,
    /// and the audit interceptor (every change writes an audit row).
    /// </summary>
    public static IServiceCollection AddModuleDbContext<TContext>(
        this IServiceCollection services,
        string connectionString,
        string schema)
        where TContext : ModuleDbContext
    {
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddScoped<ICurrentUser, SystemUser>();
        services.TryAddScoped<AuditInterceptor>();

        services.AddDbContext<TContext>((provider, options) => options
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", schema))
            .AddInterceptors(provider.GetRequiredService<AuditInterceptor>()));

        return services;
    }

    /// <summary>Runs every module's migrations (and seed data when <paramref name="seed"/> is true), Audit first.</summary>
    public static async Task InitializeModuleDatabasesAsync(
        this IServiceProvider services,
        bool seed,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var initializers = scope.ServiceProvider.GetServices<IModuleDatabaseInitializer>().OrderBy(i => i.Order).ToList();

        foreach (var initializer in initializers)
        {
            await initializer.MigrateAsync(cancellationToken);
        }

        if (!seed)
        {
            return;
        }

        foreach (var initializer in initializers)
        {
            await initializer.SeedAsync(cancellationToken);
        }
    }
}
