using Apteka.Identity.Domain.Users;
using Apteka.Infrastructure.Common;
using Microsoft.EntityFrameworkCore;

namespace Apteka.Identity.Infrastructure;

public static class IdentitySchema
{
    public const string Name = "identity";
}

public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options)
    : ModuleDbContext(options, IdentitySchema.Name)
{
    public const string UniqueUserNameIndex = "ux_users_user_name";

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(b =>
        {
            b.ToTable("Users");
            b.HasKey(u => u.Id);
            b.Property(u => u.Id).ValueGeneratedNever();
            b.Property(u => u.UserName).HasMaxLength(50).IsRequired();
            b.Property(u => u.DisplayName).HasMaxLength(User.MaxDisplayNameLength).IsRequired();
            b.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);
            b.Property(u => u.PasswordHash).HasMaxLength(500).IsRequired();
            b.Ignore(u => u.DomainEvents);
            b.HasIndex(u => u.UserName).IsUnique().HasDatabaseName(UniqueUserNameIndex);
        });

        UseSnakeCaseNames(modelBuilder);
    }
}
