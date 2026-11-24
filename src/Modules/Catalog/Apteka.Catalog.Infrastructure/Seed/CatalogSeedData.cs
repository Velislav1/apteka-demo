using Apteka.Catalog.Domain.Products;

namespace Apteka.Catalog.Infrastructure.Seed;

/// <summary>
/// 50 made-up demo products (rule 8: no real pharmacy data). Names are generic substances with
/// invented manufacturers; barcodes are EAN-13 in the in-store range 200–299, so they never clash with real goods.
/// </summary>
internal static class CatalogSeedData
{
    private static readonly string[] Manufacturers = ["Демо Фарма АД", "Балкан Лек ЕООД", "Примерфарм АД", "Тестова Лаборатория ООД"];

    // (name, ATC, Rx, pack form)
    private static readonly (string Name, string? Atc, bool IsRx, Pack Pack)[] Items =
    [
        ("Парацетамол 500 мг таблетки", "N02BE01", false, Pack.Tablets(20, 10)),
        ("Парацетамол 120 мг/5 мл сироп", "N02BE01", false, Pack.Single("флакон")),
        ("Ибупрофен 200 мг таблетки", "M01AE01", false, Pack.Tablets(20, 10)),
        ("Ибупрофен 400 мг таблетки", "M01AE01", false, Pack.Tablets(30, 10)),
        ("Ибупрофен 100 мг/5 мл суспензия", "M01AE01", false, Pack.Single("флакон")),
        ("Ацетилсалицилова киселина 100 мг таблетки", "B01AC06", false, Pack.Tablets(30, 10)),
        ("Метамизол 500 мг таблетки", "N02BB02", false, Pack.Tablets(20, 10)),
        ("Диклофенак 50 мг таблетки", "M01AB05", true, Pack.Tablets(30, 10)),
        ("Диклофенак 1% гел", "M02AA15", false, Pack.Single("туба")),
        ("Амоксицилин 500 мг капсули", "J01CA04", true, Pack.Capsules(16, 8)),
        ("Амоксицилин/клавуланова киселина 875/125 мг таблетки", "J01CR02", true, Pack.Tablets(14, 7)),
        ("Азитромицин 500 мг таблетки", "J01FA10", true, Pack.Tablets(3, 3)),
        ("Цефуроксим 500 мг таблетки", "J01DC02", true, Pack.Tablets(10, 10)),
        ("Ципрофлоксацин 500 мг таблетки", "J01MA02", true, Pack.Tablets(10, 10)),
        ("Метформин 850 мг таблетки", "A10BA02", true, Pack.Tablets(60, 15)),
        ("Метформин 1000 мг таблетки", "A10BA02", true, Pack.Tablets(30, 15)),
        ("Гликлазид 60 мг таблетки с удължено освобождаване", "A10BB09", true, Pack.Tablets(30, 15)),
        ("Аторвастатин 20 мг таблетки", "C10AA05", true, Pack.Tablets(30, 10)),
        ("Розувастатин 10 мг таблетки", "C10AA07", true, Pack.Tablets(28, 14)),
        ("Бисопролол 5 мг таблетки", "C07AB07", true, Pack.Tablets(30, 10)),
        ("Метопролол 50 мг таблетки", "C07AB02", true, Pack.Tablets(30, 10)),
        ("Амлодипин 5 мг таблетки", "C08CA01", true, Pack.Tablets(30, 10)),
        ("Амлодипин 10 мг таблетки", "C08CA01", true, Pack.Tablets(30, 10)),
        ("Лизиноприл 10 мг таблетки", "C09AA03", true, Pack.Tablets(28, 14)),
        ("Периндоприл 5 мг таблетки", "C09AA04", true, Pack.Tablets(30, 10)),
        ("Лозартан 50 мг таблетки", "C09CA01", true, Pack.Tablets(28, 14)),
        ("Валсартан 80 мг таблетки", "C09CA03", true, Pack.Tablets(28, 14)),
        ("Хидрохлоротиазид 25 мг таблетки", "C03AA03", true, Pack.Tablets(20, 10)),
        ("Фуроземид 40 мг таблетки", "C03CA01", true, Pack.Tablets(20, 10)),
        ("Клопидогрел 75 мг таблетки", "B01AC04", true, Pack.Tablets(28, 14)),
        ("Омепразол 20 мг капсули", "A02BC01", false, Pack.Capsules(14, 7)),
        ("Пантопразол 40 мг таблетки", "A02BC02", true, Pack.Tablets(28, 14)),
        ("Лоперамид 2 мг капсули", "A07DA03", false, Pack.Capsules(10, 10)),
        ("Метоклопрамид 10 мг таблетки", "A03FA01", true, Pack.Tablets(50, 10)),
        ("Цетиризин 10 мг таблетки", "R06AE07", false, Pack.Tablets(10, 10)),
        ("Лоратадин 10 мг таблетки", "R06AX13", false, Pack.Tablets(10, 10)),
        ("Дезлоратадин 5 мг таблетки", "R06AX27", false, Pack.Tablets(30, 10)),
        ("Амброксол 30 мг таблетки", "R05CB06", false, Pack.Tablets(20, 10)),
        ("Амброксол 15 мг/5 мл сироп", "R05CB06", false, Pack.Single("флакон")),
        ("Ксилометазолин 0,1% спрей за нос", "R01AA07", false, Pack.Single("флакон")),
        ("Левотироксин 50 мкг таблетки", "H03AA01", true, Pack.Tablets(50, 25)),
        ("Левотироксин 100 мкг таблетки", "H03AA01", true, Pack.Tablets(50, 25)),
        ("Алопуринол 100 мг таблетки", "M04AA01", true, Pack.Tablets(50, 10)),
        ("Сертралин 50 мг таблетки", "N06AB06", true, Pack.Tablets(28, 14)),
        ("Есциталопрам 10 мг таблетки", "N06AB10", true, Pack.Tablets(28, 14)),
        ("Витамин C 500 мг таблетки", "A11GA01", false, Pack.Tablets(30, 10)),
        ("Витамин D3 1000 IU капки", "A11CC05", false, Pack.Single("флакон")),
        ("Магнезий 375 мг таблетки", null, false, Pack.Tablets(30, 10)),
        ("Физиологичен разтвор 0,9% ампули", null, false, Pack.Ampoules(20)),
        ("Слънцезащитен крем SPF 50", null, false, Pack.Single("брой")),
    ];

    public static IEnumerable<(ProductDetails Details, List<UnitSpec> Units, List<BarcodeSpec> Barcodes)> Products()
    {
        for (var i = 0; i < Items.Length; i++)
        {
            var (name, atc, isRx, pack) = Items[i];
            var details = new ProductDetails(
                name,
                atc,
                Manufacturers[i % Manufacturers.Length],
                VatRates.Standard,
                isRx,
                IsActive: true);

            var barcode = new BarcodeSpec(null, Ean13(i + 1), pack.Units[0].Name);

            yield return (details, pack.Units, [barcode]);
        }
    }

    /// <summary>EAN-13 starting with 200 (in-store range) and a valid check digit.</summary>
    internal static string Ean13(int number)
    {
        var digits = "200" + number.ToString("D9", System.Globalization.CultureInfo.InvariantCulture);
        return digits + Gtin.CheckDigit(digits);
    }

    private sealed record Pack(List<UnitSpec> Units)
    {
        public static Pack Tablets(int perBox, int perBlister) => Blistered("таблетка", perBox, perBlister);

        public static Pack Capsules(int perBox, int perBlister) => Blistered("капсула", perBox, perBlister);

        public static Pack Ampoules(int perBox) =>
            new([new UnitSpec(null, "кутия", perBox), new UnitSpec(null, "ампула", 1)]);

        public static Pack Single(string unit) => new([new UnitSpec(null, unit, 1)]);

        private static Pack Blistered(string baseUnit, int perBox, int perBlister) =>
            perBox == perBlister
                ? new([new UnitSpec(null, "кутия", perBox), new UnitSpec(null, baseUnit, 1)])
                : new([new UnitSpec(null, "кутия", perBox), new UnitSpec(null, "блистер", perBlister), new UnitSpec(null, baseUnit, 1)]);
    }
}
