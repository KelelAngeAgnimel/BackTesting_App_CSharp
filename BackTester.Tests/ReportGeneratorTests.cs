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