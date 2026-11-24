using Apteka.Identity.Application.Common;
using Apteka.Identity.Application.Login;
using Apteka.Identity.Application.Users;
using Apteka.Identity.Contracts;
using Apteka.Identity.Infrastructure.Seed;
using Apteka.Identity.Infrastructure.Users;
using Apteka.Infrastructure.Common;
using Apteka.Infrastructure.Common.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Apteka.Identity.Infrastructure;

public static class IdentityModule
{
    public static IServiceCollection AddIdentityModule(
        this IServiceCollection services,
        string connectionString,
        IdentitySeedPasswords? seedPasswords = null)
    {
        services.AddModuleDbContext<IdentityDbContext>(connectionString, IdentitySchema.Name);
        services.AddSingleton(seedPasswords ?? new IdentitySeedPasswords(null, null));
        services.AddScoped<IModuleDatabaseInitializer, IdentitySeeder>();

        // Infrastructure
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<UserRepository>();
        services.AddScoped<IUserRepository>(sp => sp.GetRequiredService<UserRepository>());

        // Use cases
        services.AddScoped<LoginHandler>();
        services.AddScoped<UsersHandler>();

        // Public API for other modules
        services.AddScoped<IUserDirectory>(sp => sp.GetRequiredService<UserRepository>());

        return services;
    }
}
