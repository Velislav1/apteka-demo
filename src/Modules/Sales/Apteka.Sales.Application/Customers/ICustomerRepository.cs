using Apteka.Sales.Domain.Customers;

namespace Apteka.Sales.Application.Customers;

public interface ICustomerRepository
{
    void Add(Customer customer);

    /// <summary>Tracked for changes.</summary>
    Task<Customer?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> EgnExistsAsync(string egn, Guid? exceptId, CancellationToken cancellationToken);

    /// <summary>By name; see <see cref="CustomerSearchField"/> for how each field matches.</summary>
    Task<IReadOnlyList<CustomerListItem>> SearchAsync(CustomerSearchField field, string? text, bool includeInactive, int limit, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, string>> GetNamesAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

/// <summary>The search tabs of „Клиенти“.</summary>
public enum CustomerSearchField
{
    /// <summary>Part of the name, any case.</summary>
    Name = 1,

    /// <summary>ЕГН starting with the typed digits.</summary>
    Egn = 2,

    /// <summary>Exact customer code.</summary>
    Code = 3,

    /// <summary>Phone containing the typed digits (spaces ignored).</summary>
    Phone = 4,
}

public sealed record CustomerListItem(Guid Id, long Code, string Name, string? Egn, string? Phone, bool IsActive);
