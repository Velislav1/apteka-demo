namespace Apteka.Inventory.Domain.StockMovements;

public enum MovementKind
{
    Delivery = 1,
    Sale = 2,
    Reversal = 3,
    WriteOff = 4,
    Correction = 5,
}
