using System.Globalization;
using System.Text;

namespace BackTester.Core.Reporting;

// Builds small SVG charts by hand (no extra dependency). The SVG only contains shapes:
// the axis labels are written by the PDF layout, so no font is needed inside the SVG.
public static class ChartSvg
{
    public const int Width = 600;
    public const int Height = 180;

    // Drawdown of each point compared with the highest value reached so far (0 or negative)
    public static decimal[] DrawdownSeries(decimal[] equity)
    {
        var result = new decimal[equity.Length];
        decimal peak = 0m;
        for (int i = 0; i < equity.Length; i++)
        {
            if (equity[i] > peak) peak = equity[i];
            result[i] = peak > 0m ? equity[i] / peak - 1m : 0m;
        }
        return result;
    }

    // Line chart of a series; fill = also colour the area between the line and the bottom
    public static string Line(decimal[] values, string color, bool fill)
    {
        if (values == null || values.Length == 0)
            throw new ArgumentException("The series is empty.");

        double min = (double)values.Min();
        double max = (double)values.Max();
        if (max - min < 1e-12) { min -= 1; max += 1; } // flat line: avoid dividing by zero

        var inv = CultureInfo.InvariantCulture;
        var points = new StringBuilder();
        for (int i = 0; i < values.Length; i++)
        {
            double x = values.Length == 1 ? Width / 2.0 : (double)i / (values.Length - 1) * Width;
            double y = Height - ((double)values[i] - min) / (max - min) * Height;
            points.Append(x.ToString("0.##", inv)).Append(',').Append(y.ToString("0.##", inv)).Append(' ');
        }

        var svg = new StringBuilder();
        svg.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{Width}\" height=\"{Height}\" viewBox=\"0 0 {Width} {Height}\">");
        svg.Append($"<rect x=\"0\" y=\"0\" width=\"{Width}\" height=\"{Height}\" fill=\"white\" stroke=\"#bbbbbb\" stroke-width=\"1\"/>");
        for (int g = 1; g < 4; g++)
        {
            string gy = (Height * g / 4.0).ToString("0.##", inv);
            svg.Append($"<line x1=\"0\" y1=\"{gy}\" x2=\"{Width}\" y2=\"{gy}\" stroke=\"#e0e0e0\" stroke-width=\"1\"/>");
        }
        if (fill && values.Length > 1)
            svg.Append($"<polygon points=\"0,{Height} {points}{Width},{Height}\" fill=\"{color}\" fill-opacity=\"0.15\"/>");
        svg.Append($"<polyline points=\"{points.ToString().TrimEnd()}\" fill=\"none\" stroke=\"{color}\" stroke-width=\"1.5\" stroke-linejoin=\"round\"/>");
        svg.Append("</svg>");
        return svg.ToString();
    }
}
