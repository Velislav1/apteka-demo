using Apteka.Infrastructure.Common;
using Apteka.Sales.Domain.Customers;
using Apteka.Sales.Domain.Returns;
using Apteka.Sales.Domain.Reversals;
using Apteka.Sales.Domain.Sales;
using Microsoft.EntityFrameworkCore;

namespace Apteka.Sales.Infrastructure;

public static class SalesSchema
{
    public const string Name = "sales";
    public const string SaleNumberSequence = "sale_numbers";
    public const string CustomerCodeSequence = "customer_codes";
    public const string ReturnNumberSequence = "sale_return_numbers";
}

public sealed class SalesDbContext(DbContextOptions<SalesDbContext> options)
    : ModuleDbContext(options, SalesSchema.Name)
{
    public DbSet<Sale> Sales => Set<Sale>();

    public DbSet<SaleLine> SaleLines => Set<SaleLine>();

    public DbSet<SaleLineBatch> SaleLineBatches => Set<SaleLineBatch>();

    public DbSet<Reversal> Reversals => Set<Reversal>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<SaleReturn> SaleReturns => Set<SaleReturn>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasSequence<long>(SalesSchema.SaleNumberSequence, SalesSchema.Name).StartsAt(1);
        modelBuilder.HasSequence<long>(SalesSchema.CustomerCodeSequence, SalesSchema.Name).StartsAt(1);
        modelBuilder.HasSequence<long>(SalesSchema.ReturnNumberSequence, SalesSchema.Name).StartsAt(1);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SalesDbContext).Assembly);
        UseSnakeCaseNames(modelBuilder);
    }
}
