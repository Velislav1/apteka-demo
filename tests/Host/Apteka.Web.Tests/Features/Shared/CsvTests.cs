using System.Text;
using Apteka.Web.Features.Shared;

namespace Apteka.Web.Tests.Features.Shared;

public class CsvTests
{
    [Fact]
    public void Writes_semicolons_decimal_commas_and_quotes_for_excel()
    {
        var bytes = Csv.Build(["Дата", "Име", "Сума"], [[new DateOnly(2026, 11, 3), "Фарма; \"Юг\"", 1234.5m], [null, "да", -0.8m]]);

        Assert.Equal(Encoding.UTF8.GetPreamble(), bytes[..3]);
        Assert.Equal("Дата;Име;Сума\r\n03.11.2026;\"Фарма; \"\"Юг\"\"\";1234,50\r\n;да;-0,80\r\n", Encoding.UTF8.GetString(bytes[3..]));
    }
}
