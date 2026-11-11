using System;
using BackTester.Core.Engine;
using Xunit;

namespace BackTester.Tests;

public class BacktestRunnerTests
{
    private static readonly decimal[] Prices =
    {
        110, 108, 106, 104, 102, 100, 98, 100, 103, 106,
        109, 112, 114, 116, 112, 106, 100, 94, 90, 88
    };

    [Fact]
    public void RunMovingAverage_WithoutStopLoss_SellsOnSignal()
    {
        var runner = new BacktestRunner();

        PerformanceReport report = runner.RunMovingAverage(Prices, 3, 6, 10000m, 0.001m, 0m);

        Assert.Equal(2, report.Trades.Count);
        Assert.Equal(10, report.Trades[0].Day);
        Assert.Equal(16, report.Trades[1].Day);
        Assert.Equal(9155.97m, report.EquityCurve[19], 2);
    }

    [Fact]
    public void RunMovingAverage_WithStopLoss_SellsEarlier()
    {
        var runner = new BacktestRunner();

        PerformanceReport report = runner.RunMovingAverage(Prices, 3, 6, 10000m, 0.001m, 0.02m);

        Assert.Equal(15, report.Trades[1].Day);
        Assert.Equal(9705.33m, report.EquityCurve[19], 2);
    }

    [Fact]
    public void RunMovingAverage_WithInvalidPeriods_Throws()
    {
        var runner = new BacktestRunner();

        Assert.Throws<ArgumentException>(() =>
            runner.RunMovingAverage(Prices, 6, 3, 10000m, 0.001m, 0m));
    }
}
