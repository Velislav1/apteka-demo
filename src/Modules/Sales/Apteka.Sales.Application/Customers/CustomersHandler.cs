using Apteka.Sales.Application.Common;
using Apteka.Sales.Domain.Customers;
using Apteka.SharedKernel;

namespace Apteka.Sales.Application.Customers;

/// <summary>„Клиенти“: search, create, edit (no deletion; a customer is deactivated).</summary>
public sealed class CustomersHandler(ICustomerRepository customers, ISaleRepository sales, IUnitOfWork unitOfWork)
{
    public const int DefaultLimit = 50;
    public const int SalesLimit = 100;

    public Task<IReadOnlyList<CustomerListItem>> SearchAsync(CustomerSearchField field, string? text, bool includeInactive = false, CancellationToken cancellationToken = default) =>
        customers.SearchAsync(field, string.IsNullOrWhiteSpace(text) ? null : text.Trim(), includeInactive, DefaultLimit, cancellationToken);

    public async Task<CustomerView?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var customer = await customers.GetAsync(id, cancellationToken);
        if (customer is null)
        {
            return null;
        }

        var history = await sales.ListForCustomerAsync(id, SalesLimit, cancellationToken);
        return new CustomerView(
            customer.Id,
            customer.Code,
            customer.Name,
            customer.Egn,
            customer.Phone,
            customer.Note,
            customer.IsActive,
            history.Select(s => new CustomerSaleItem(s.Id, s.Number, s.CreatedAt, s.Total, s.Status)).ToList());
    }

    public Task<Result<Guid>> CreateAsync(CustomerDetails details, CancellationToken cancellationToken = default) =>
        unitOfWork.ExecuteAsync(async ct =>
        {
            if (await EgnTakenAsync(details.Egn, null, ct))
            {
                return Result.Failure<Guid>(CustomerErrors.EgnTaken);
            }

            var created = Customer.Create(details);
            if (created.IsFailure)
            {
                return Result.Failure<Guid>(created.Error);
            }

            customers.Add(created.Value);
            await customers.SaveChangesAsync(ct);
            return Result.Success(created.Value.Id);
        }, cancellationToken);

    public Task<Result> UpdateAsync(Guid id, CustomerDetails details, CancellationToken cancellationToken = default) =>
        unitOfWork.ExecuteAsync(async ct =>
        {
            var customer = await customers.GetAsync(id, ct);
            if (customer is null)
            {
                return Result.Failure(CustomerErrors.NotFound);
            }

            if (await EgnTakenAsync(details.Egn, id, ct))
            {
                return Result.Failure(CustomerErrors.EgnTaken);
            }

            var updated = customer.Update(details);
            if (updated.IsFailure)
            {
                return updated;
            }

            await customers.SaveChangesAsync(ct);
            return Result.Success();
        }, cancellationToken);

    private async Task<bool> EgnTakenAsync(string? egn, Guid? exceptId, CancellationToken cancellationToken) =>
        !string.IsNullOrWhiteSpace(egn) && await customers.EgnExistsAsync(egn.Trim(), exceptId, cancellationToken);
}

public sealed record CustomerView(
    Guid Id,
    long Code,
    string Name,
    string? Egn,
    string? Phone,
    string? Note,
    bool IsActive,
    IReadOnlyList<CustomerSaleItem> Sales);

public sealed record CustomerSaleItem(Guid Id, long Number, DateTimeOffset CreatedAt, decimal Total, Domain.Sales.SaleStatus Status);
