using System.Globalization;
using System.Text;

namespace Apteka.Web.Features.Shared;

/// <summary>Draws an EAN-13 (or EAN-8 padded is not supported) barcode as inline SVG, for printed price labels.</summary>
public static class Ean13Svg
{
    private static readonly string[] L = ["0001101", "0011001", "0010011", "0111101", "0100011", "0110001", "0101111", "0111011", "0110111", "0001011"];
    private static readonly string[] G = ["0100111", "0110011", "0011011", "0100001", "0011101", "0111001", "0000101", "0010001", "0001001", "0010111"];
    private static readonly string[] R = ["1110010", "1100110", "1101100", "1000010", "1011100", "1001110", "1010000", "1000100", "1001000", "1110100"];

    /// <summary>Which of the six left digits use the G set, by the first digit.</summary>
    private static readonly string[] Parity = ["LLLLLL", "LLGLGG", "LLGGLG", "LLGGGL", "LGLLGG", "LGGLLG", "LGGGLL", "LGLGLG", "LGLGGL", "LGGLGL"];

    /// <summary>The 95 bars and spaces of an EAN-13 (1 = bar), or null when the code is not 13 digits.</summary>
    public static string? Modules(string code)
    {
        if (code is not { Length: 13 } || !code.All(char.IsAsciiDigit))
        {
            return null;
        }

        var parity = Parity[code[0] - '0'];
        var bits = new StringBuilder("101");
        for (var i = 1; i <= 6; i++)
        {
            var digit = code[i] - '0';
            bits.Append(parity[i - 1] == 'L' ? L[digit] : G[digit]);
        }

        bits.Append("01010");
        for (var i = 7; i <= 12; i++)
        {
            bits.Append(R[code[i] - '0']);
        }

        return bits.Append("101").ToString();
    }

    /// <summary>An SVG of the barcode with the digits under it, or empty markup when it cannot be drawn.</summary>
    public static string Render(string code, int height = 40)
    {
        if (Modules(code) is not { } bits)
        {
            return string.Empty;
        }

        const int quiet = 9;
        var width = bits.Length + (2 * quiet);
        var svg = new StringBuilder();
        svg.Append(CultureInfo.InvariantCulture, $"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {width} {height + 10}\" class=\"ean13\" role=\"img\" aria-label=\"{code}\">");
        svg.Append(CultureInfo.InvariantCulture, $"<rect width=\"{width}\" height=\"{height + 10}\" fill=\"#fff\"/>");
        for (var i = 0; i < bits.Length;)
        {
            if (bits[i] == '0')
            {
                i++;
                continue;
            }

            var start = i;
            while (i < bits.Length && bits[i] == '1')
            {
                i++;
            }

            svg.Append(CultureInfo.InvariantCulture, $"<rect x=\"{quiet + start}\" y=\"0\" width=\"{i - start}\" height=\"{height}\" fill=\"#000\"/>");
        }

        svg.Append(CultureInfo.InvariantCulture, $"<text x=\"{width / 2}\" y=\"{height + 9}\" font-size=\"9\" text-anchor=\"middle\" font-family=\"monospace\">{code}</text></svg>");
        return svg.ToString();
    }
}
