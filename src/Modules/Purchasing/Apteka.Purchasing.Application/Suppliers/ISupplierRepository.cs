using Apteka.Purchasing.Domain.Suppliers;
using Apteka.SharedKernel;

namespace Apteka.Purchasing.Application.Suppliers;

public interface ISupplierRepository
{
    Task<IReadOnlyList<SupplierView>> ListAsync(bool includeInactive, CancellationToken cancellationToken);

    Task<Supplier?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> NameExistsAsync(string name, Guid? exceptId, CancellationToken cancellationToken);

    Task<bool> EikExistsAsync(string eik, Guid? exceptId, CancellationToken cancellationToken);

    void Add(Supplier supplier);

    Task<Result> SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed record SupplierView(Guid Id, string Name, string Eik, int PaymentTermDays, bool IsActive);

public sealed record SupplierInput(string Name, string Eik, int PaymentTermDays, bool IsActive = true);
