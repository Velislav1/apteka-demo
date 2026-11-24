using Apteka.SharedKernel;

namespace Apteka.Catalog.Domain.Products;

/// <summary>Validation errors of the catalog. Messages are shown to the user, so they are in Bulgarian.</summary>
public static class CatalogErrors
{
    public const int MaxNameLength = 200;
    public const int MaxUnitNameLength = 50;
    public const decimal MaxMarkupPercent = 1000m;

    /// <summary>Used when neither the product nor its group has a markup.</summary>
    public const decimal DefaultMarkupPercent = 25m;

    public static readonly Error NameRequired = new("catalog.name_required", "Въведете име на стоката.");
    public static readonly Error NameTooLong = new("catalog.name_too_long", $"Името е по-дълго от {MaxNameLength} знака.");
    public static readonly Error InvalidMarkup = new("catalog.invalid_markup", "Надценката трябва да е от 0 до 1000 %.");
    public static readonly Error InvalidStockLimits = new("catalog.invalid_stock_limits", "Минималният запас не може да е отрицателен или по-голям от максималния.");
    public static readonly Error InvalidDispensingMode = new("catalog.invalid_dispensing_mode", "Изберете режим на отпускане.");
    public static readonly Error InvalidNhifCode = new("catalog.invalid_nhif_code", "Кодът по НЗОК може да съдържа само латински букви и цифри (до 20 знака).");
    public static readonly Error DefaultSaleUnitMissing = new("catalog.default_sale_unit_missing", "Мярката за продажба трябва да е една от мерките на стоката.");
    public static readonly Error ManufacturerNotAvailable = new("catalog.manufacturer_not_available", "Производителят не съществува или е неактивен.");
    public static readonly Error ProductGroupNotAvailable = new("catalog.group_not_available", "Групата не съществува или е неактивна.");
    public static readonly Error ShelfNotAvailable = new("catalog.shelf_not_available", "Рафтът не съществува или е неактивен.");
    public static readonly Error InvalidAtcCode = new("catalog.invalid_atc", "Невалиден ATC код (пример: N02BE01).");
    public static readonly Error InvalidVatRate = new("catalog.invalid_vat", "ДДС може да бъде 0, 9 или 20 %.");

    public static readonly Error UnitsRequired = new("catalog.units_required", "Добавете поне една мярка.");
    public static readonly Error UnitNameRequired = new("catalog.unit_name_required", "Всяка мярка трябва да има име.");
    public static readonly Error UnitNameTooLong = new("catalog.unit_name_too_long", $"Името на мярка е по-дълго от {MaxUnitNameLength} знака.");
    public static readonly Error FactorTooSmall = new("catalog.factor_too_small", "Коефициентът на мярка трябва да е цяло число 1 или повече.");
    public static readonly Error ExactlyOneBaseUnit = new("catalog.one_base_unit", "Трябва да има точно една мярка с коефициент 1 (най-малката мярка).");
    public static readonly Error UnitFactorChanged = new("catalog.factor_changed", "Коефициентът на съществуваща мярка не може да се променя.");
    public static readonly Error UnitRemoved = new("catalog.unit_removed", "Съществуваща мярка не може да се премахва.");
    public static readonly Error UnknownUnit = new("catalog.unknown_unit", "Мярката не принадлежи на тази стока.");

    public static readonly Error InvalidBarcode = new("catalog.invalid_barcode", "Невалиден баркод: очакват се 8, 12, 13 или 14 цифри с вярна контролна цифра.");
    public static readonly Error BarcodeRemoved = new("catalog.barcode_removed", "Съществуващ баркод не може да се премахва; поправете го.");
    public static readonly Error BarcodeUnitMissing = new("catalog.barcode_unit_missing", "Баркодът трябва да е към една от мерките на стоката.");
    public static readonly Error UnknownBarcode = new("catalog.unknown_barcode", "Баркодът не принадлежи на тази стока.");

    public static Error DuplicateUnitName(string name) =>
        new("catalog.duplicate_unit", $"Мярката „{name}“ е въведена повече от веднъж.");

    public static Error DuplicateBarcode(string code) =>
        new("catalog.duplicate_barcode", $"Баркод {code} е въведен повече от веднъж.");

    public static Error BarcodeTaken(string code) =>
        new("catalog.barcode_taken", $"Баркод {code} вече е към друга стока.");

    public static readonly Error ProductNotFound = new("catalog.product_not_found", "Стоката не е намерена.");
}
