using System.Globalization;
using System.Security.Cryptography;
using Apteka.Catalog.Domain.Products;

namespace Apteka.IntegrationTests.Infrastructure;

/// <summary>Random valid EAN-13 codes in the 29x range (the seed uses 200…), so tests never collide.</summary>
internal static class TestBarcodes
{
    public static string Next()
    {
        var digits = "29" + RandomNumberGenerator.GetInt32(0, 1_000_000_000).ToString("D9", CultureInfo.InvariantCulture) + "0";
        return digits + Gtin.CheckDigit(digits).ToString(CultureInfo.InvariantCulture);
    }
}
