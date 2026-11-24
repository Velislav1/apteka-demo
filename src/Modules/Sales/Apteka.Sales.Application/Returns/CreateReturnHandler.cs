using Apteka.Identity.Contracts;
using Apteka.Inventory.Contracts;
using Apteka.Sales.Application.Common;
using Apteka.Sales.Domain.Returns;
using Apteka.Sales.Domain.Sales;
using Apteka.SharedKernel;

namespace Apteka.Sales.Application.Returns;

/// <summary>
/// Use case „Корекция на продажба“ (кредитно известие): gives back part of a sale — the goods into their batches,
/// the money to the customer — in one transaction. Only a manager may do it, as with сторно.
/// </summary>
public sealed class CreateReturnHandler(ISaleRepository sales, IStockIssuing stock, IUnitOfWork unitOfWork, ICurrentUser currentUser, TimeProvider clock)
{
    public const string DocumentType = "SaleReturn";

    /// <param name="quantities">Sale line id → quantity given back, in the line's unit.</param>
    public Task<Result<ReturnDone>> HandleAsync(Guid saleId, IReadOnlyDictionary<Guid, int> quantities, string? reason, CancellationToken cancellationToken = default) =>
        unitOfWork.ExecuteAsync(ct => CreateAsync(saleId, quantities, reason, ct), cancellationToken);

    private async Task<Result<ReturnDone>> CreateAsync(Guid saleId, IReadOnlyDictionary<Guid, int> quantities, string? reason, CancellationToken cancellationToken)
    {
        if (!currentUser.IsInRole(Roles.Manager))
        {
            return Result.Failure<ReturnDone>(ReturnErrors.ManagersOnly);
        }

        var sale = await sales.GetAsync(saleId, cancellationToken);
        if (sale is null)
        {
            return Result.Failure<ReturnDone>(SalesErrors.NotFound);
        }

        var earlier = await sales.ListReturnsAsync(saleId, cancellationToken);
        var created = SaleReturn.Create(Guid.CreateVersion7(), sale, earlier, quantities, reason, currentUser.UserId, clock.GetUtcNow());
        if (created.IsFailure)
        {
            return Result.Failure<ReturnDone>(created.Error);
        }

        var saleReturn = created.Value;
        var returned = await stock.ReturnAsync(
            new StockReturn(
                DocumentType,
                saleReturn.Id,
                currentUser.UserId,
                saleReturn.Lines.SelectMany(l => l.Batches).Select(b => new ReturnedBatch(b.BatchId, b.BaseQuantity)).ToList()),
            cancellationToken);
        if (returned.IsFailure)
        {
            return Result.Failure<ReturnDone>(returned.Error);
        }

        sales.Add(saleReturn);
        await sales.SaveChangesAsync(cancellationToken);
        return Result.Success(new ReturnDone(saleReturn.Id, saleReturn.Number, saleReturn.Total, sale.PaymentMethod));
    }
}

/// <param name="PaymentMethod">How the sale was paid; the refund goes the same way.</param>
public sealed record ReturnDone(Guid ReturnId, long Number, decimal Total, PaymentMethod PaymentMethod);
