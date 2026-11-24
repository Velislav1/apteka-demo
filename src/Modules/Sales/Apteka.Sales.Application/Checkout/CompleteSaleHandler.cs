using Apteka.Catalog.Contracts;
using Apteka.Inventory.Contracts;
using Apteka.Sales.Application.Common;
using Apteka.Sales.Domain.Pricing;
using Apteka.Sales.Domain.Sales;
using Apteka.SharedKernel;

namespace Apteka.Sales.Application.Checkout;

/// <summary>
/// Use case „Плащане“ (F9): checks the products, issues the stock (FEFO, batches locked) and saves the sale,
/// all in one transaction. If anything fails, nothing is sold and no stock moves.
/// </summary>
public sealed class CompleteSaleHandler(
    ICatalogLookup catalog,
    IStockIssuing stock,
    ISaleRepository sales,
    IUnitOfWork unitOfWork,
    TimeProvider clock)
{
    public const string DocumentType = "Sale";

    public Task<Result<CompletedSale>> HandleAsync(CheckoutInput input, CancellationToken cancellationToken = default) =>
        unitOfWork.ExecuteAsync(ct => CompleteAsync(input, ct), cancellationToken);

    private async Task<Result<CompletedSale>> CompleteAsync(CheckoutInput input, CancellationToken cancellationToken)
    {
        if (input.Lines.Count == 0)
        {
            return Result.Failure<CompletedSale>(SalesErrors.NoLines);
        }

        var products = (await catalog.GetProductsAsync([.. input.Lines.Select(l => l.ProductId).Distinct()], cancellationToken))
            .ToDictionary(p => p.ProductId);

        var lines = new List<(CheckoutLineInput Input, CatalogProductInfo Product, CatalogUnit Unit, Guid LineId)>();
        foreach (var line in input.Lines)
        {
            if (!products.TryGetValue(line.ProductId, out var product) || !product.IsActive)
            {
                return Result.Failure<CompletedSale>(SalesErrors.ProductNotAvailable(product?.Name ?? "?"));
            }

            if (product.DispensingMode == CatalogDispensingMode.Narcotic)
            {
                return Result.Failure<CompletedSale>(SalesErrors.NarcoticNotAllowed(product.Name));
            }

            if (product.DispensingMode == CatalogDispensingMode.Prescription && !line.PrescriptionConfirmed)
            {
                return Result.Failure<CompletedSale>(SalesErrors.PrescriptionNotConfirmed(product.Name));
            }

            var unit = product.Units.FirstOrDefault(u => u.UnitId == line.UnitId);
            if (unit is null || line.Quantity <= 0)
            {
                return Result.Failure<CompletedSale>(SalesErrors.QuantityMustBePositive(product.Name));
            }

            lines.Add((line, product, unit, Guid.CreateVersion7()));
        }

        var saleId = Guid.CreateVersion7();
        var now = clock.GetUtcNow();
        var today = BulgarianTime.Today(now);
        var specs = new Dictionary<Guid, SaleLineSpec>();

        // One product at a time, always in the same order (by id): tills lock batches in the same order, so no deadlock.
        foreach (var line in lines.OrderBy(l => l.Input.ProductId).ThenBy(l => l.LineId))
        {
            var issued = await stock.IssueAsync(
                new StockIssue(DocumentType, saleId, UserId: null, today, [new StockIssueLine(line.LineId, line.Product.ProductId, line.Input.Quantity * line.Unit.Factor)]),
                cancellationToken);
            if (issued.IsFailure)
            {
                return Result.Failure<CompletedSale>(SalesErrors.NotEnoughStock(line.Product.Name, issued.Error.Message));
            }

            var batches = issued.Value.Single().Batches;
            var first = batches[0];
            specs[line.LineId] = new SaleLineSpec(
                line.LineId,
                line.Product.ProductId,
                line.Product.Name,
                line.Unit.UnitId,
                line.Unit.Name,
                line.Unit.Factor,
                line.Input.Quantity,
                SalePricing.UnitPrice(line.Unit.Factor, first.PricedUnitFactor, first.PricedUnitSalePrice, first.SalePricePerBaseUnit),
                line.Input.DiscountPercent,
                line.Product.VatRate,
                batches.Select(b => new SaleLineBatchSpec(b.BatchId, b.BaseQuantity)).ToList());
        }

        // Keep the order in which the lines were scanned.
        var sale = Sale.Create(saleId, [.. lines.Select(l => specs[l.LineId])], input.PaymentMethod, input.CashReceived, userId: null, now);
        if (sale.IsFailure)
        {
            return Result.Failure<CompletedSale>(sale.Error);
        }

        sales.Add(sale.Value);
        await sales.SaveChangesAsync(cancellationToken);
        return Result.Success(new CompletedSale(sale.Value.Id, sale.Value.Number, sale.Value.Total, sale.Value.Change));
    }
}
