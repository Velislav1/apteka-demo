using Apteka.SharedKernel;

namespace Apteka.Inventory.Domain.Stocktakings;

/// <summary>Errors of „Инвентаризация“, shown to the user in Bulgarian.</summary>
public static class StocktakingErrors
{
    public static readonly Error NothingToCount = new("inventory.stocktaking_empty", "Няма стоки с наличност за инвентаризация в избрания обхват.");
    public static readonly Error AlreadyOpen = new("inventory.stocktaking_open", "Вече има започната инвентаризация. Приключете я, преди да започнете нова.");
    public static readonly Error Closed = new("inventory.stocktaking_closed", "Инвентаризацията е приключена и не може да се променя.");
    public static readonly Error NotFound = new("inventory.stocktaking_not_found", "Инвентаризацията не е намерена.");
    public static readonly Error UnknownLine = new("inventory.stocktaking_unknown_line", "Редът не е от тази инвентаризация.");
    public static readonly Error NegativeCount = new("inventory.stocktaking_negative", "Намереното количество не може да е отрицателно.");

    public static Error InStocktaking(long number) =>
        new("inventory.in_stocktaking", $"Стоката е в инвентаризация № {number}. Приключете инвентаризацията, за да я продавате, заприхождавате или бракувате.");
}
