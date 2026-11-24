namespace Apteka.Purchasing.Domain.Deliveries;

public enum DeliveryStatus
{
    /// <summary>Чернова: can still be edited; no stock yet.</summary>
    Draft = 1,

    /// <summary>Публикувана: batches and stock movements exist; the delivery is locked.</summary>
    Posted = 2,
}
