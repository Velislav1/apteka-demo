using Apteka.Infrastructure.Common.Auditing;
using Apteka.Infrastructure.Common.Persistence;
using Apteka.Infrastructure.Common.Transactions;
using Apteka.SharedKernel;
using Apteka.Infrastructure.Common.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;

namespace Apteka.Infrastructure.Common;

public static class InfrastructureCommonExtensions
{
    public static IServiceCollection AddAptekaCommon(this IServiceCollection services, string connectionString)
    {
        services.AddSingleton(new DatabaseStatus(connectionString));
        AddShared(services, connectionString);
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
        AddShared(services, connectionString);

        // Every module context of a scope uses the same connection, so IUnitOfWork can span modules.
        services.AddDbContext<TContext>((provider, options) => options
            .UseNpgsql(
                provider.GetRequiredService<SharedConnection>().Connection,
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", schema))
            .AddInterceptors(provider.GetRequiredService<AuditInterceptor>()));
        services.AddScoped<ModuleDbContext>(provider => provider.GetRequiredService<TContext>());

        return services;
    }

    private static void AddShared(IServiceCollection services, string connectionString)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton(_ => NpgsqlDataSource.Create(connectionString));
        services.TryAddScoped<SharedConnection>();
        services.TryAddScoped<IUnitOfWork, UnitOfWork>();

        // Replaced by the Identity module once login exists.
        services.TryAddScoped<ICurrentUser, SystemUser>();
        services.TryAddScoped<AuditInterceptor>();
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
