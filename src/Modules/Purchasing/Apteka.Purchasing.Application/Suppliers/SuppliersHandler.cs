using Apteka.Purchasing.Domain.Suppliers;
using Apteka.SharedKernel;

namespace Apteka.Purchasing.Application.Suppliers;

/// <summary>Use cases of the „Доставчици“ screen.</summary>
public sealed class SuppliersHandler(ISupplierRepository suppliers)
{
    public Task<IReadOnlyList<SupplierView>> ListAsync(bool includeInactive = true, CancellationToken cancellationToken = default) =>
        suppliers.ListAsync(includeInactive, cancellationToken);

    public async Task<Result<Guid>> CreateAsync(SupplierInput input, CancellationToken cancellationToken = default)
    {
        var created = Supplier.Create(input.Name, input.Eik, input.PaymentTermDays);
        if (created.IsFailure)
        {
            return Result.Failure<Guid>(created.Error);
        }

        var unique = await CheckUniqueAsync(created.Value, cancellationToken);
        if (unique.IsFailure)
        {
            return Result.Failure<Guid>(unique.Error);
        }

        suppliers.Add(created.Value);
        var saved = await suppliers.SaveChangesAsync(cancellationToken);
        return saved.IsSuccess ? Result.Success(created.Value.Id) : Result.Failure<Guid>(saved.Error);
    }

    public async Task<Result> UpdateAsync(Guid id, SupplierInput input, CancellationToken cancellationToken = default)
    {
        var supplier = await suppliers.GetAsync(id, cancellationToken);
        if (supplier is null)
        {
            return Result.Failure(SupplierErrors.NotFound);
        }

        var updated = supplier.Update(input.Name, input.Eik, input.PaymentTermDays, input.IsActive);
        if (updated.IsFailure)
        {
            return updated;
        }

        var unique = await CheckUniqueAsync(supplier, cancellationToken);
        return unique.IsFailure ? unique : await suppliers.SaveChangesAsync(cancellationToken);
    }

    private async Task<Result> CheckUniqueAsync(Supplier supplier, CancellationToken cancellationToken)
    {
        if (await suppliers.NameExistsAsync(supplier.Name, supplier.Id, cancellationToken))
        {
            return Result.Failure(SupplierErrors.NameTaken(supplier.Name));
        }

        return await suppliers.EikExistsAsync(supplier.Eik, supplier.Id, cancellationToken)
            ? Result.Failure(SupplierErrors.EikTaken(supplier.Eik))
            : Result.Success();
    }
}
