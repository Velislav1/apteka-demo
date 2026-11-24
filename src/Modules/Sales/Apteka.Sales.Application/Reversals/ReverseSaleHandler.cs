using Apteka.Identity.Contracts;
using Apteka.Inventory.Contracts;
using Apteka.Sales.Application.Common;
using Apteka.Sales.Domain.Sales;
using Apteka.SharedKernel;

namespace Apteka.Sales.Application.Reversals;

/// <summary>Use case „Сторно“: marks the sale as reversed and puts every unit back into its own batch, in one transaction.</summary>
/// <remarks>Only a manager may reverse a sale.</remarks>
public sealed class ReverseSaleHandler(ISaleRepository sales, IStockIssuing stock, IUnitOfWork unitOfWork, ICurrentUser currentUser, TimeProvider clock)
{
    public const string DocumentType = "SaleReversal";

    public Task<Result> HandleAsync(Guid saleId, string? reason, CancellationToken cancellationToken = default) =>
        unitOfWork.ExecuteAsync(ct => ReverseAsync(saleId, reason, ct), cancellationToken);

    private async Task<Result> ReverseAsync(Guid saleId, string? reason, CancellationToken cancellationToken)
    {
        if (!currentUser.IsInRole(Roles.Manager))
        {
            return Result.Failure(SalesErrors.ManagersOnly);
        }

        var sale = await sales.GetAsync(saleId, cancellationToken);
        if (sale is null)
        {
            return Result.Failure(SalesErrors.NotFound);
        }

        var reversal = sale.Reverse(reason, currentUser.UserId, clock.GetUtcNow());
        if (reversal.IsFailure)
        {
            return reversal;
        }

        var returned = await stock.ReturnAsync(
            new StockReturn(
                DocumentType,
                reversal.Value.Id,
                currentUser.UserId,
                sale.Lines.SelectMany(l => l.Batches).Select(b => new ReturnedBatch(b.BatchId, b.BaseQuantity)).ToList()),
            cancellationToken);
        if (returned.IsFailure)
        {
            return returned;
        }

        sales.Add(reversal.Value);
        await sales.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
