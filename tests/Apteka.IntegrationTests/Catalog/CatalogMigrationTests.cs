using Apteka.Catalog.Infrastructure;
using Apteka.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace Apteka.IntegrationTests.Catalog;

[Collection(PostgresCollection.Name)]
public class CatalogMigrationTests(PostgresFixture postgres)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Reference_data_migration_keeps_prescription_flag_and_manufacturer_text()
    {
        var connectionString = await CreateEmptyDatabaseAsync();
        await using var context = new CatalogDbContext(new DbContextOptionsBuilder<CatalogDbContext>()
            .UseNpgsql(connectionString, o => o.MigrationsHistoryTable("__EFMigrationsHistory", CatalogSchema.Name))
            .Options);
        var migrator = context.GetService<IMigrator>();

        // A catalog as it was before the reference lists existed.
        await migrator.MigrateAsync("InitialCatalog", Ct);
        var rx = Guid.CreateVersion7();
        var otc = Guid.CreateVersion7();
        var anonymous = Guid.CreateVersion7();
        await context.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO catalog.products (id, name, atc_code, manufacturer, vat_rate, is_rx, is_active) VALUES
              ({rx}, 'Стара Rx', NULL, ' Демо Фарма АД ', 20, TRUE, TRUE),
              ({otc}, 'Стара без рецепта', NULL, 'Демо Фарма АД', 20, FALSE, TRUE),
              ({anonymous}, 'Без производител', NULL, NULL, 20, FALSE, TRUE)
            """,
            Ct);

        await migrator.MigrateAsync(cancellationToken: Ct);

        var products = await context.Products.AsNoTracking().ToDictionaryAsync(p => p.Id, Ct);
        var manufacturer = Assert.Single(await context.Manufacturers.AsNoTracking().ToListAsync(Ct));

        Assert.Equal("Демо Фарма АД", manufacturer.Name);
        Assert.True(manufacturer.IsActive);
        Assert.Equal(Apteka.Catalog.Domain.Products.DispensingMode.Prescription, products[rx].DispensingMode);
        Assert.Equal(Apteka.Catalog.Domain.Products.DispensingMode.OverTheCounter, products[otc].DispensingMode);
        Assert.Equal(manufacturer.Id, products[rx].ManufacturerId);
        Assert.Equal(manufacturer.Id, products[otc].ManufacturerId);
        Assert.Null(products[anonymous].ManufacturerId);
    }

    private async Task<string> CreateEmptyDatabaseAsync()
    {
        var name = "migration_" + Guid.NewGuid().ToString("N");
        await using (var connection = new NpgsqlConnection(postgres.ConnectionString))
        {
            await connection.OpenAsync(Ct);
            await using var command = new NpgsqlCommand($"CREATE DATABASE {name}", connection);
            await command.ExecuteNonQueryAsync(Ct);
        }

        return new NpgsqlConnectionStringBuilder(postgres.ConnectionString) { Database = name }.ConnectionString;
    }
}
