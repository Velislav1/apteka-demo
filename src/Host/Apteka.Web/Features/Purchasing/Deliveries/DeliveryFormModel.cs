using Apteka.Catalog.Contracts;
using Apteka.Purchasing.Application.Deliveries;
using Apteka.Purchasing.Domain.Deliveries;
using Apteka.SharedKernel;

namespace Apteka.Web.Features.Purchasing.Deliveries;

/// <summary>Editable state of the delivery form; converted to <see cref="DeliveryInput"/> on save.</summary>
public sealed class DeliveryFormModel
{
    /// <summary>"" until a supplier is chosen.</summary>
    public string SupplierId { get; set; } = string.Empty;

    public string InvoiceNo { get; set; } = string.Empty;

    public DateOnly InvoiceDate { get; set; }

    public DeliveryStatus Status { get; private set; } = DeliveryStatus.Draft;

    public bool IsReadOnly => Status == DeliveryStatus.Posted;

    public List<LineRow> Lines { get; } = [];

    public decimal PurchaseTotal => Lines.Aggregate(Money.Zero, (sum, l) => sum + l.PurchaseTotal).Amount;

    /// <summary>„Общо по прод. цени“: what the goods are worth at the sale prices (VAT included).</summary>
    public decimal SaleTotal => Lines.Sum(l => decimal.Round(l.SalePrice * l.Quantity, 2, MidpointRounding.AwayFromZero));

    public static DeliveryFormModel New(DateOnly today) => new() { InvoiceDate = today };

    public static DeliveryFormModel From(DeliveryView view)
    {
        var model = new DeliveryFormModel
        {
            SupplierId = view.SupplierId.ToString(),
            InvoiceNo = view.InvoiceNo,
            InvoiceDate = view.InvoiceDate,
            Status = view.Status,
        };

        model.Lines.AddRange(view.Lines.Select(l => new LineRow
        {
            Id = l.Id,
            ProductId = l.ProductId,
            ProductName = l.ProductName,
            VatRate = l.VatRate,
            Units = l.Units,
            UnitId = l.ProductUnitId.ToString(),
            Quantity = l.Quantity,
            UnitPurchasePrice = l.UnitPurchasePrice,
            MarkupPercent = l.MarkupPercent,
            SalePrice = l.SalePrice,
            BatchNo = l.BatchNo,
            ExpiryDate = l.ExpiryDate,
        }));
        return model;
    }

    public DeliveryInput ToInput() => new(
        Guid.TryParse(SupplierId, out var supplier) ? supplier : Guid.Empty,
        InvoiceNo,
        InvoiceDate,
        Lines.Select(l => new DeliveryLineInput(
            l.Id,
            l.ProductId,
            Guid.Parse(l.UnitId),
            l.Quantity,
            l.UnitPurchasePrice,
            l.MarkupPercent,
            l.SalePrice,
            l.BatchNo,
            l.ExpiryDate)).ToList());

    /// <summary>A copy of a line for the entry form (F6 edits a copy; it replaces the line only on Enter).</summary>
    public static LineRow Copy(LineRow line) => new()
    {
        Id = line.Id,
        ProductId = line.ProductId,
        ProductName = line.ProductName,
        VatRate = line.VatRate,
        Units = line.Units,
        UnitId = line.UnitId,
        Quantity = line.Quantity,
        UnitPurchasePrice = line.UnitPurchasePrice,
        MarkupPercent = line.MarkupPercent,
        SalePrice = line.SalePrice,
        BatchNo = line.BatchNo,
        ExpiryDate = line.ExpiryDate,
    };

    /// <summary>A new line for a product, in the scanned unit or else the largest one, not yet in <see cref="Lines"/>.</summary>
    public static LineRow NewLine(CatalogProductInfo product, Guid? unitId, DateOnly today)
    {
        var unit = product.Units.FirstOrDefault(u => u.UnitId == unitId) ?? product.Units.MaxBy(u => u.Factor)!;
        return new LineRow
        {
            ProductId = product.ProductId,
            ProductName = product.Name,
            VatRate = product.VatRate,
            Units = product.Units,
            UnitId = unit.UnitId.ToString(),
            Quantity = 1,
            MarkupPercent = product.MarkupPercent,
            ExpiryDate = today.AddYears(1),
        };
    }

    public sealed class LineRow
    {
        public Guid? Id { get; set; }

        public Guid ProductId { get; init; }

        public string ProductName { get; init; } = string.Empty;

        public decimal VatRate { get; init; }

        public IReadOnlyList<CatalogUnit> Units { get; init; } = [];

        public string UnitId { get; set; } = string.Empty;

        public string UnitName => Units.FirstOrDefault(u => u.UnitId.ToString() == UnitId)?.Name ?? string.Empty;

        public int UnitFactor => Units.FirstOrDefault(u => u.UnitId.ToString() == UnitId)?.Factor ?? 1;

        public int Quantity { get; set; }

        public decimal UnitPurchasePrice { get; set; }

        public decimal? MarkupPercent { get; set; }

        public decimal SalePrice { get; set; }

        public string BatchNo { get; set; } = string.Empty;

        public DateOnly ExpiryDate { get; set; }

        public Money PurchaseTotal => UnitPrice.Of(Math.Max(0, UnitPurchasePrice)).Times(Math.Max(0, Quantity));

        /// <summary>With a markup, the sale price follows the purchase price; typing a sale price clears the markup.</summary>
        public void ApplyMarkup()
        {
            if (MarkupPercent is { } markup && UnitPurchasePrice > 0)
            {
                SalePrice = SalePriceCalculator.FromMarkup(UnitPurchasePrice, markup, VatRate);
            }
        }
    }
}
