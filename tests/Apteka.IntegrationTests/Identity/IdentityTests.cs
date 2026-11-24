using Apteka.Audit.Infrastructure;
using Apteka.Catalog.Domain.Products;
using Apteka.Identity.Application.Login;
using Apteka.Identity.Application.Users;
using Apteka.Identity.Domain.Users;
using Apteka.IntegrationTests.Infrastructure;
using Apteka.IntegrationTests.Purchasing;
using Apteka.Sales.Application.Checkout;
using Apteka.Sales.Application.Reversals;
using Apteka.Sales.Domain.Sales;
using Apteka.Sales.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static Apteka.IntegrationTests.Purchasing.DeliveryFixtures;

namespace Apteka.IntegrationTests.Identity;

[Collection(PostgresCollection.Name)]
public class IdentityTests(PostgresFixture postgres)
{
    private static async Task<Guid> CreateUserAsync(IServiceProvider provider, string userName, UserRole role, string password = "parola2026")
    {
        await using var scope = provider.CreateAsyncScope();
        var created = await scope.ServiceProvider.GetRequiredService<UsersHandler>()
            .CreateAsync(new NewUserInput(userName, "Тест " + userName, role, password), Ct);
        Assert.True(created.IsSuccess, created.IsFailure ? created.Error.Message : null);
        return created.Value;
    }

    private static string UniqueUserName() => "u" + Guid.NewGuid().ToString("N")[..12];

    [Fact]
    public async Task Login_succeeds_with_the_right_password_and_locks_after_five_wrong_ones()
    {
        await using var provider = await ModuleServices.CreateAsync(postgres.ConnectionString);
        var userName = UniqueUserName();
        await CreateUserAsync(provider, userName, UserRole.Pharmacist);

        await using var scope = provider.CreateAsyncScope();
        var login = scope.ServiceProvider.GetRequiredService<LoginHandler>();

        var ok = await login.HandleAsync(userName.ToUpperInvariant(), "parola2026", Ct);
        Assert.True(ok.IsSuccess);
        Assert.Equal(UserRole.Pharmacist, ok.Value.Role);

        for (var i = 0; i < 4; i++)
        {
            Assert.Equal(UserErrors.InvalidCredentials, (await login.HandleAsync(userName, "wrong", Ct)).Error);
        }

        Assert.Equal("identity.locked_out", (await login.HandleAsync(userName, "wrong", Ct)).Error.Code);
        Assert.Equal("identity.locked_out", (await login.HandleAsync(userName, "parola2026", Ct)).Error.Code);
        Assert.Equal(UserErrors.InvalidCredentials, (await login.HandleAsync("nobody-" + userName, "parola2026", Ct)).Error);
    }

    [Fact]
    public async Task Inactive_user_cannot_log_in_and_the_last_active_manager_cannot_be_removed()
    {
        await using var provider = await ModuleServices.CreateAsync(postgres.ConnectionString);
        var userName = UniqueUserName();
        var id = await CreateUserAsync(provider, userName, UserRole.Manager);

        await using var scope = provider.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UsersHandler>();
        var managers = (await users.ListAsync(Ct)).Where(u => u.IsActive && u.Role == UserRole.Manager).ToList();

        // Deactivate every other manager; the last one must stay.
        foreach (var other in managers.Where(m => m.Id != id))
        {
            Assert.True((await users.UpdateAsync(other.Id, other.DisplayName, other.Role, false, Ct)).IsSuccess);
        }

        Assert.Equal(UserErrors.LastManager, (await users.UpdateAsync(id, "Тест", UserRole.Pharmacist, true, Ct)).Error);
        Assert.Equal(UserErrors.LastManager, (await users.UpdateAsync(id, "Тест", UserRole.Manager, false, Ct)).Error);

        // Put the other managers back, then deactivating this one is fine and its login stops.
        foreach (var other in managers.Where(m => m.Id != id))
        {
            Assert.True((await users.UpdateAsync(other.Id, other.DisplayName, other.Role, true, Ct)).IsSuccess);
        }

        if (managers.Count > 1)
        {
            Assert.True((await users.UpdateAsync(id, "Тест", UserRole.Manager, false, Ct)).IsSuccess);
            Assert.Equal(UserErrors.InvalidCredentials, (await scope.ServiceProvider.GetRequiredService<LoginHandler>().HandleAsync(userName, "parola2026", Ct)).Error);
        }
    }

    [Fact]
    public async Task Only_a_manager_manages_users_and_reverses_sales()
    {
        await using var provider = await ModuleServices.CreateAsync(postgres.ConnectionString, TestUser.Pharmacist());
        await using var scope = provider.CreateAsyncScope();

        var created = await scope.ServiceProvider.GetRequiredService<UsersHandler>()
            .CreateAsync(new NewUserInput(UniqueUserName(), "X", UserRole.Manager, "parola2026"), Ct);
        Assert.Equal(UserErrors.ManagersOnly, created.Error);

        var reversed = await scope.ServiceProvider.GetRequiredService<ReverseSaleHandler>().HandleAsync(Guid.CreateVersion7(), null, Ct);
        Assert.Equal(SalesErrors.ManagersOnly, reversed.Error);
    }

    [Fact]
    public async Task The_logged_in_user_is_written_on_the_sale_the_stock_movement_and_the_audit_row_without_the_password_hash()
    {
        var cashier = TestUser.Pharmacist();
        await using var provider = await ModuleServices.CreateAsync(postgres.ConnectionString, cashier);
        var (product, box, _) = await StockFixtures.StockedProductAsync(provider, DispensingMode.OverTheCounter, (2, 400));

        await using (var sell = provider.CreateAsyncScope())
        {
            var sold = await sell.ServiceProvider.GetRequiredService<CompleteSaleHandler>()
                .HandleAsync(new CheckoutInput([new CheckoutLineInput(product, box, 1)], PaymentMethod.Card, null), Ct);
            Assert.True(sold.IsSuccess);

            var sale = await sell.ServiceProvider.GetRequiredService<SalesDbContext>().Sales.SingleAsync(s => s.Id == sold.Value.SaleId, Ct);
            Assert.Equal(cashier.UserId, sale.UserId);
            var movement = await sell.ServiceProvider.GetRequiredService<Apteka.Inventory.Infrastructure.InventoryDbContext>()
                .StockMovements.SingleAsync(m => m.DocumentId == sale.Id, Ct);
            Assert.Equal(cashier.UserId, movement.UserId);
            Assert.True(await sell.ServiceProvider.GetRequiredService<AuditDbContext>().AuditLog
                .AnyAsync(r => r.EntityId == sale.Id.ToString() && r.UserId == cashier.UserId && r.UserName == cashier.UserName, Ct));
        }

        await using var manager = await ModuleServices.CreateAsync(postgres.ConnectionString);
        var userId = await CreateUserAsync(manager, UniqueUserName(), UserRole.Pharmacist, "Secret2026");
        await using var check = manager.CreateAsyncScope();
        var row = await check.ServiceProvider.GetRequiredService<AuditDbContext>().AuditLog
            .SingleAsync(r => r.Entity == nameof(User) && r.EntityId == userId.ToString(), Ct);
        Assert.Contains("\"PasswordHash\":\"***\"", row.NewValues!.Replace(" ", string.Empty, StringComparison.Ordinal), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Web_screens_need_a_login_and_users_are_only_for_managers()
    {
        await using var factory = WebLogin.Factory(postgres.ConnectionString);
        using var anonymous = factory.CreateClient(new() { AllowAutoRedirect = false });

        var redirect = await anonymous.GetAsync("/sales/checkout", Ct);
        Assert.Equal(System.Net.HttpStatusCode.Redirect, redirect.StatusCode);
        Assert.StartsWith("/login", redirect.Headers.Location!.PathAndQuery, StringComparison.Ordinal);

        using var pharmacist = factory.CreateClient();
        await WebLogin.LoginAsync(pharmacist, "pharmacist", WebLogin.PharmacistPassword);
        Assert.Contains("Директна продажба", await pharmacist.GetStringAsync("/sales/checkout", Ct));
        var users = await pharmacist.GetAsync("/identity/users", Ct);
        var usersHtml = await users.Content.ReadAsStringAsync(Ct);
        Assert.DoesNotContain("Нов потребител", usersHtml);
        Assert.Contains("Нямате достъп", usersHtml);

        using var wrong = factory.CreateClient();
        var failed = await WebLogin.LoginAsync(wrong, "manager", "wrong-password1");
        Assert.Contains("Грешно потребителско име или парола", await failed.Content.ReadAsStringAsync(Ct));
    }
}
