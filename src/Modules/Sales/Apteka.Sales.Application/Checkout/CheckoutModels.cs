using Apteka.Catalog.Contracts;
using Apteka.Sales.Domain.Sales;

namespace Apteka.Sales.Application.Checkout;

/// <summary>A product found at the till, with the price of the unit it will be sold in.</summary>
/// <param name="UnitPrice">Price (VAT included) from the batch that would be sold first (FEFO).</param>
/// <param name="AvailableBaseQuantity">Sellable stock (not expired), in base units.</param>
public sealed record TillProduct(
    Guid ProductId,
    string Name,
    CatalogDispensingMode DispensingMode,
    decimal VatRate,
    Guid UnitId,
    string UnitName,
    int UnitFactor,
    decimal UnitPrice,
    int AvailableBaseQuantity,
    string? ShelfName = null,
    IReadOnlyList<CatalogUnit>? Units = null)
{
    /// <summary>All units of the product (for F12, change of unit).</summary>
    public IReadOnlyList<CatalogUnit> AllUnits => Units ?? [];

    public bool NeedsPrescription => DispensingMode == CatalogDispensingMode.Prescription;
}

/// <param name="PrescriptionConfirmed">The pharmacist confirmed a prescription for a prescription-only product.</param>
public sealed record CheckoutLineInput(Guid ProductId, Guid UnitId, int Quantity, decimal DiscountPercent = 0m, bool PrescriptionConfirmed = false);

public sealed record CheckoutInput(IReadOnlyList<CheckoutLineInput> Lines, PaymentMethod PaymentMethod, decimal? CashReceived);

public sealed record CompletedSale(Guid SaleId, long Number, decimal Total, decimal? Change);
