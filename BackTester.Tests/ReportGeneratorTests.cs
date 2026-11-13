using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BackTester.Core.Engine;
using BackTester.Core.Reporting;
using Xunit;

namespace BackTester.Tests;

public class ReportGeneratorTests
{
    private static (ReportInfo, PerformanceReport, DateTime[]) Sample(int count = 20)
    {
        decimal[] prices = Enumerable.Range(0, count)
            .Select(i => 100m + (i % 7) * 3m - i * 0.5m)
            .ToArray();
        DateTime[] dates = Enumerable.Range(0, count)
            .Select(i => new DateTime(2026, 1, 5).AddDays(i))
            .ToArray();
        var report = new BasicStrategiesRunner().RunBreakout(prices, 5, 0m, 10000m, 0.001m, 0m);
        var info = new ReportInfo { StrategyName = "Breakout", Symbol = "TEST", InitialCapital = 10000m, FeeRate = 0.001m };
        return (info, report, dates);
    }

    [Fact]
    public void Generate_with_charts_returns_a_valid_pdf_larger_than_without()
    {
        var (info, report, dates) = Sample();
        var generator = new ReportGenerator();

        byte[] without = generator.Generate(info, report, dates);
        byte[] with = generator.Generate(info, report, dates, includeCharts: true);

        Assert.True(with.Length > 1000);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(with, 0, 4));
        Assert.Equal("%PDF", Encoding.ASCII.GetString(without, 0, 4));
        Assert.True(with.Length > without.Length);
        File.WriteAllBytes(Path.Combine(Path.GetTempPath(), "report-test-charts.pdf"), with);
    }

    [Fact]
    public void Generate_with_charts_and_weekly_info_works()
    {
        var (info, report, dates) = Sample(30);
        info.IsWeekly = true;
        byte[] pdf = new ReportGenerator().Generate(info, report, dates, includeCharts: true);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(pdf, 0, 4));
    }

    [Fact]
    public void Chart_svg_handles_flat_and_single_point_series()
    {
        Assert.Contains("<polyline", ChartSvg.Line(new[] { 5m, 5m, 5m }, "#000000", false));
        Assert.Contains("<polyline", ChartSvg.Line(new[] { 5m }, "#000000", true));
        Assert.Equal(new[] { 0m, -0.5m, 0m }, ChartSvg.DrawdownSeries(new[] { 100m, 50m, 100m }));
    }

    [Fact]
    public void Generate_returns_a_valid_pdf()
    {
        decimal[] prices =
        {
            100, 102, 104, 103, 105, 107, 106, 108, 110, 109,
            111, 113, 112, 114, 106, 98, 92, 86, 80, 75
        };
        DateTime[] dates = Enumerable.Range(0, prices.Length)
            .Select(i => new DateTime(2026, 1, 5).AddDays(i))
            .ToArray();

        var report = new BasicStrategiesRunner().RunBreakout(prices, 5, 0m, 10000m, 0.001m, 0m);

        var info = new ReportInfo
        {
            StrategyName = "Breakout",
            Symbol = "TEST",
            InitialCapital = 10000m,
            FeeRate = 0.001m,
            StopLossRate = 0m,
            Parameters =
            {
                new KeyValuePair<string, string>("lookback", "5"),
                new KeyValuePair<string, string>("threshold", "0")
            }
        };

        byte[] pdf = new ReportGenerator().Generate(info, report, dates);

        Assert.True(pdf.Length > 1000);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(pdf, 0, 4));

        // Saved so that we can open it and check the layout by eye
        File.WriteAllBytes(Path.Combine(Path.GetTempPath(), "report-test.pdf"), pdf);
    }
}