using System.Text.Json;
using Apteka.Audit.Infrastructure;
using Apteka.Catalog.Application.Products;
using Apteka.IntegrationTests.Catalog;
using Apteka.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Apteka.IntegrationTests.Audit;

[Collection(PostgresCollection.Name)]
public class AuditLogTests(PostgresFixture postgres)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Creating_a_product_writes_audit_rows_for_the_product_its_units_and_barcodes()
    {
        var user = new TestUser(Guid.CreateVersion7(), "pharmacist");
        await using var provider = await ModuleServices.CreateAsync(postgres.ConnectionString, user);
        var name = CatalogInputs.UniqueName("Одит");

        var id = await ProductPersistenceTests.CreateAsync(provider, name, TestBarcodes.Next());

        await using var scope = provider.CreateAsyncScope();
        var audit = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
        var rows = await audit.AuditLog.Where(r => r.UserId == user.UserId).ToListAsync(Ct);

        var productRow = Assert.Single(rows, r => r.Entity == "Product");
        Assert.Equal("Insert", productRow.Action);
        Assert.Equal(id.ToString(), productRow.EntityId);
        Assert.Equal("pharmacist", productRow.UserName);
        Assert.Null(productRow.OldValues);
        Assert.Equal(name, JsonDocument.Parse(productRow.NewValues!).RootElement.GetProperty("Name").GetString());
        Assert.True(DateTimeOffset.UtcNow - productRow.At < TimeSpan.FromMinutes(1));

        Assert.Equal(3, rows.Count(r => r.Entity == "ProductUnit"));
        Assert.Single(rows, r => r.Entity == "Barcode");
    }

    [Fact]
    public async Task Update_records_old_and_new_values_of_changed_fields_only()
    {
        var user = new TestUser(Guid.CreateVersion7(), "manager");
        await using var provider = await ModuleServices.CreateAsync(postgres.ConnectionString, user);
        var id = await ProductPersistenceTests.CreateAsync(provider, CatalogInputs.UniqueName("Цена"), TestBarcodes.Next());

        await using (var scope = provider.CreateAsyncScope())
        {
            var view = (await scope.ServiceProvider.GetRequiredService<GetProductHandler>().HandleAsync(id, Ct))!;
            var input = CatalogInputs.FromView(view) with { VatRate = 9m };
            Assert.True((await scope.ServiceProvider.GetRequiredService<UpdateProductHandler>().HandleAsync(id, input, Ct)).IsSuccess);
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var audit = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
            var update = await audit.AuditLog.SingleAsync(r => r.UserId == user.UserId && r.Action == "Update", Ct);

            Assert.Equal("Product", update.Entity);
            Assert.Equal("""{"VatRate":20.00}""", update.OldValues!.Replace(" ", string.Empty, StringComparison.Ordinal));
            Assert.Equal("""{"VatRate":9}""", update.NewValues!.Replace(" ", string.Empty, StringComparison.Ordinal));
        }
    }

    [Fact]
    public async Task Failed_save_leaves_no_audit_rows()
    {
        var user = new TestUser(Guid.CreateVersion7(), "rollback");
        await using var provider = await ModuleServices.CreateAsync(postgres.ConnectionString, user);
        var barcode = TestBarcodes.Next();
        await ProductPersistenceTests.CreateAsync(provider, CatalogInputs.UniqueName("Заета"), barcode);

        // Bypass the early check so the database rejects the save inside the audit transaction.
        await using (var scope = provider.CreateAsyncScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IProductRepository>();
            var input = CatalogInputs.Tablets(CatalogInputs.UniqueName("Отказана"), barcode);
            var product = CatalogInputs.ToProduct(input);
            repository.Add(product);

            Assert.True((await repository.SaveChangesAsync(Ct)).IsFailure);

            var audit = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
            Assert.False(await audit.AuditLog.AnyAsync(r => r.EntityId == product.Id.ToString(), Ct));
        }
    }

    [Fact]
    public async Task Audit_rows_cannot_be_changed_or_deleted()
    {
        await using var provider = await ModuleServices.CreateAsync(postgres.ConnectionString);
        await ProductPersistenceTests.CreateAsync(provider, CatalogInputs.UniqueName("Защита"), TestBarcodes.Next());

        await using var scope = provider.CreateAsyncScope();
        var audit = scope.ServiceProvider.GetRequiredService<AuditDbContext>();

        var update = await Assert.ThrowsAsync<PostgresException>(() =>
            audit.Database.ExecuteSqlRawAsync("UPDATE audit.audit_log SET user_name = 'x'", Ct));
        var delete = await Assert.ThrowsAsync<PostgresException>(() =>
            audit.Database.ExecuteSqlRawAsync("DELETE FROM audit.audit_log", Ct));

        Assert.Contains("append-only", update.MessageText, StringComparison.Ordinal);
        Assert.Contains("append-only", delete.MessageText, StringComparison.Ordinal);
    }
}
