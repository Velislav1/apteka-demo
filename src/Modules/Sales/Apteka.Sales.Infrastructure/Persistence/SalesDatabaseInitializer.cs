using Apteka.Infrastructure.Common.Persistence;
using Apteka.Sales.Domain.Customers;
using Microsoft.EntityFrameworkCore;

namespace Apteka.Sales.Infrastructure.Persistence;

/// <summary>
/// No demo sales: the till starts empty every day. A few invented customers (rule 8: no real names or ЕГН;
/// the numbers below are made up and only have a valid check digit).
/// </summary>
internal sealed class SalesDatabaseInitializer(SalesDbContext context) : IModuleDatabaseInitializer
{
    public int Order => 40;

    public Task MigrateAsync(CancellationToken cancellationToken) => context.Database.MigrateAsync(cancellationToken);

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (await context.Customers.AnyAsync(cancellationToken))
        {
            return;
        }

        CustomerDetails[] demo =
        [
            new("Демо Клиент Първи", Egn.Complete("750101123"), "0888 000 001", "Измислен клиент за демото"),
            new("Демо Клиент Втори", Egn.Complete("820512456"), "0888 000 002", null),
            new("Демо Пациент Трети", Egn.Complete("900520789"), null, "Роден 1990"),
            new("Демо Пациент Четвърти", Egn.Complete("024307321"), "0888 000 004", "Роден 2002"),
            new("Демо Фирма ООД (без ЕГН)", null, "032 000 005", "Клиент без ЕГН"),
        ];

        foreach (var details in demo)
        {
            context.Customers.Add(Customer.Create(details).Value);
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
