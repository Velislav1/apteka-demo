using Apteka.Sales.Application.Customers;
using Apteka.Sales.Domain.Customers;
using Microsoft.EntityFrameworkCore;

namespace Apteka.Sales.Infrastructure.Customers;

internal sealed class CustomerRepository(SalesDbContext context) : ICustomerRepository
{
    public void Add(Customer customer) => context.Customers.Add(customer);

    public Task<Customer?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        context.Customers.SingleOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<bool> EgnExistsAsync(string egn, Guid? exceptId, CancellationToken cancellationToken) =>
        context.Customers.AnyAsync(c => c.Egn == egn && c.Id != exceptId, cancellationToken);

    public async Task<IReadOnlyList<CustomerListItem>> SearchAsync(
        CustomerSearchField field,
        string? text,
        bool includeInactive,
        int limit,
        CancellationToken cancellationToken)
    {
        var query = context.Customers.AsNoTracking();
        if (!includeInactive)
        {
            query = query.Where(c => c.IsActive);
        }

        if (text is not null)
        {
            var digits = new string(text.Where(char.IsAsciiDigit).ToArray());
            query = field switch
            {
                CustomerSearchField.Egn => query.Where(c => c.Egn != null && c.Egn.StartsWith(digits)),
                CustomerSearchField.Code => long.TryParse(digits, out var code) ? query.Where(c => c.Code == code) : query.Where(_ => false),
                CustomerSearchField.Phone => query.Where(c => c.Phone != null && c.Phone.Replace(" ", "").Contains(digits)),
                _ => query.Where(c => EF.Functions.ILike(c.Name, "%" + Escape(text) + "%", "\\")),
            };
        }

        return await query
            .OrderBy(c => c.Name)
            .Take(limit)
            .Select(c => new CustomerListItem(c.Id, c.Code, c.Name, c.Egn, c.Phone, c.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, string>> GetNamesAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        var list = ids.ToArray();
        return await context.Customers.AsNoTracking().Where(c => list.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);

    private static string Escape(string text) =>
        text.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("%", "\\%", StringComparison.Ordinal).Replace("_", "\\_", StringComparison.Ordinal);
}
