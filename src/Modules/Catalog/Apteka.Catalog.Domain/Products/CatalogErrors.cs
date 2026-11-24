using Apteka.SharedKernel;

namespace Apteka.Catalog.Domain.Products;

/// <summary>Validation errors of the catalog. Messages are shown to the user, so they are in Bulgarian.</summary>
public static class CatalogErrors
{
    public const int MaxNameLength = 200;
    public const int MaxUnitNameLength = 50;

    public static readonly Error NameRequired = new("catalog.name_required", "Въведете име на стоката.");
    public static readonly Error NameTooLong = new("catalog.name_too_long", $"Името е по-дълго от {MaxNameLength} знака.");
    public static readonly Error ManufacturerTooLong = new("catalog.manufacturer_too_long", $"Производителят е по-дълъг от {MaxNameLength} знака.");
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
