namespace Apteka.Inventory.Domain.Batches;

/// <summary>Which stock a batch belongs to. NHIF stock is kept separately (in Apteka Expert 2: DEL_TYPE_ID = 2).</summary>
public enum StockType
{
    Retail = 1,
    Nhif = 2,
}
