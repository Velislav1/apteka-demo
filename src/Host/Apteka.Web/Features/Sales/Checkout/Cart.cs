using Apteka.Sales.Application.Checkout;
using Apteka.Sales.Domain.Pricing;

namespace Apteka.Web.Features.Sales.Checkout;

/// <summary>
/// The till's receipt before payment. One line is „current“ (↑/↓ choose it; F4, F8, F12 act on it).
/// Final prices come from the batches at payment.
/// </summary>
public sealed class Cart
{
    private readonly List<CartLine> _lines = [];

    public IReadOnlyList<CartLine> Lines => _lines;

    public CartLine? Current => _lines.Count == 0 ? null : _lines[CurrentIndex];

    public int CurrentIndex { get; private set; }

    public bool IsEmpty => _lines.Count == 0;

    /// <summary>„Ст-ст без ТО“: before discounts.</summary>
    public decimal Gross => _lines.Sum(l => l.Amounts.Gross);

    /// <summary>„ТО“: all discounts.</summary>
    public decimal Discount => _lines.Sum(l => l.Amounts.Discount);

    /// <summary>„СУМА“ to pay.</summary>
    public decimal Total => _lines.Sum(l => l.Amounts.Total);

    /// <summary>Scanning the same product and unit again adds to that line instead of a new one.</summary>
    public CartLine Add(TillProduct product, int quantity, bool prescriptionConfirmed)
    {
        var existing = _lines.FindIndex(l => l.Product.ProductId == product.ProductId && l.Product.UnitId == product.UnitId);
        if (existing >= 0)
        {
            _lines[existing].Quantity += quantity;
            CurrentIndex = existing;
            return _lines[existing];
        }

        var line = new CartLine(product) { Quantity = quantity, PrescriptionConfirmed = prescriptionConfirmed };
        _lines.Add(line);
        CurrentIndex = _lines.Count - 1;
        return line;
    }

    public void Move(int step)
    {
        if (_lines.Count > 0)
        {
            CurrentIndex = Math.Clamp(CurrentIndex + step, 0, _lines.Count - 1);
        }
    }

    public void Select(CartLine line) => CurrentIndex = Math.Max(0, _lines.IndexOf(line));

    public void RemoveCurrent()
    {
        if (_lines.Count == 0)
        {
            return;
        }

        _lines.RemoveAt(CurrentIndex);
        CurrentIndex = Math.Clamp(CurrentIndex, 0, Math.Max(0, _lines.Count - 1));
    }

    /// <summary>F12: the current line in another unit of the same product (quantity, discount and prescription stay).</summary>
    public void ReplaceCurrent(TillProduct inOtherUnit)
    {
        if (Current is not { } old)
        {
            return;
        }

        _lines[CurrentIndex] = new CartLine(inOtherUnit)
        {
            Quantity = old.Quantity,
            DiscountPercent = old.DiscountPercent,
            PrescriptionConfirmed = old.PrescriptionConfirmed,
        };
    }

    public void Clear()
    {
        _lines.Clear();
        CurrentIndex = 0;
    }

    public IReadOnlyList<CheckoutLineInput> ToInput() =>
        _lines.Select(l => new CheckoutLineInput(l.Product.ProductId, l.Product.UnitId, l.Quantity, l.DiscountPercent, l.PrescriptionConfirmed)).ToList();
}

public sealed class CartLine(TillProduct product)
{
    public TillProduct Product { get; } = product;

    public int Quantity { get; set; } = 1;

    public decimal DiscountPercent { get; set; }

    public bool PrescriptionConfirmed { get; set; }

    public LineAmounts Amounts => SalePricing.Line(Product.UnitPrice, Quantity, DiscountPercent, Product.VatRate);

    public decimal Total => Amounts.Total;

    public bool ExceedsStock => Quantity * Product.UnitFactor > Product.AvailableBaseQuantity;
}
