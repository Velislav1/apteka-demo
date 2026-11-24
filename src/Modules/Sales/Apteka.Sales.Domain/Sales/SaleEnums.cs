namespace Apteka.Sales.Domain.Sales;

public enum SaleType
{
    /// <summary>Свободна продажба.</summary>
    Retail = 1,

    /// <summary>Продажба по НЗОК (not in the demo).</summary>
    Nhif = 2,
}

public enum PaymentMethod
{
    Cash = 1,
    Card = 2,
}

public enum SaleStatus
{
    Completed = 1,

    /// <summary>Сторнирана: the stock went back; the sale itself is kept (rule 6).</summary>
    Reversed = 2,
}
