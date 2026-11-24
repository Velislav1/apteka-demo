using Apteka.Sales.Application.Checkout;
using Apteka.Sales.Domain.Pricing;

namespace Apteka.Web.Features.Sales.Checkout;

/// <summary>The till's cart: what is shown before payment. Final prices come from the batches at payment (F9).</summary>
public sealed class Cart
{
    private readonly List<CartLine> _lines = [];

    public IReadOnlyList<CartLine> Lines => _lines;

    public CartLine? Last => _lines.Count == 0 ? null : _lines[^1];

    public bool IsEmpty => _lines.Count == 0;

    public decimal Total => _lines.Sum(l => l.Total);

    /// <summary>Scanning the same product and unit again adds one more instead of a new line.</summary>
    public CartLine Add(TillProduct product, bool prescriptionConfirmed)
    {
        var existing = _lines.Find(l => l.Product.ProductId == product.ProductId && l.Product.UnitId == product.UnitId);
        if (existing is not null)
        {
            existing.Quantity++;
            _lines.Remove(existing);
            _lines.Add(existing);
            return existing;
        }

        var line = new CartLine(product) { PrescriptionConfirmed = prescriptionConfirmed };
        _lines.Add(line);
        return line;
    }

    public void Remove(CartLine line) => _lines.Remove(line);

    public void Clear() => _lines.Clear();

    public IReadOnlyList<CheckoutLineInput> ToInput() =>
        _lines.Select(l => new CheckoutLineInput(l.Product.ProductId, l.Product.UnitId, l.Quantity, l.DiscountPercent, l.PrescriptionConfirmed)).ToList();
}

public sealed class CartLine(TillProduct product)
{
    public TillProduct Product { get; } = product;

    public int Quantity { get; set; } = 1;

    public decimal DiscountPercent { get; set; }

    public bool PrescriptionConfirmed { get; set; }

    public decimal Total => SalePricing.Line(Product.UnitPrice, Quantity, DiscountPercent, Product.VatRate).Total;

    public bool ExceedsStock => Quantity * Product.UnitFactor > Product.AvailableBaseQuantity;
}
