using Apteka.Sales.Application.Customers;
using Apteka.Sales.Domain.Customers;

namespace Apteka.Web.Tests.Features.Sales;

internal sealed class FakeCustomers : ICustomerRepository
{
    public List<Customer> Items { get; } = [];

    public void Add(Customer customer) => Items.Add(customer);

    public Task<Customer?> GetAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(Items.SingleOrDefault(c => c.Id == id));

    public Task<bool> EgnExistsAsync(string egn, Guid? exceptId, CancellationToken cancellationToken) =>
        Task.FromResult(Items.Any(c => c.Egn == egn && c.Id != exceptId));

    public Task<IReadOnlyList<CustomerListItem>> SearchAsync(CustomerSearchField field, string? text, bool includeInactive, int limit, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<CustomerListItem>>(Items
            .Where(c => includeInactive || c.IsActive)
            .Where(c => text is null || field switch
            {
                CustomerSearchField.Egn => c.Egn?.StartsWith(text, StringComparison.Ordinal) == true,
                CustomerSearchField.Code => c.Code.ToString(System.Globalization.CultureInfo.InvariantCulture) == text,
                CustomerSearchField.Phone => c.Phone?.Replace(" ", "", StringComparison.Ordinal).Contains(text, StringComparison.Ordinal) == true,
                _ => c.Name.Contains(text, StringComparison.CurrentCultureIgnoreCase),
            })
            .OrderBy(c => c.Name)
            .Take(limit)
            .Select(c => new CustomerListItem(c.Id, c.Code, c.Name, c.Egn, c.Phone, c.IsActive))
            .ToList());

    public Task<IReadOnlyDictionary<Guid, string>> GetNamesAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyDictionary<Guid, string>>(Items.Where(c => ids.Contains(c.Id)).ToDictionary(c => c.Id, c => c.Name));

    public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
