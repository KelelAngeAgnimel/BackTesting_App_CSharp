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

    [Fact]
    public void RunPairTrading_OnTwentyDays_OpensAndClosesTwice()
    {
        decimal[] pricesA = { 100, 101.5m, 103, 102, 104.5m, 105, 104, 106.5m, 112, 113,
                          111, 110, 112, 113.5m, 114, 113, 115, 110, 116, 117 };
        decimal[] pricesB = { 50, 50.5m, 51.2m, 51.3m, 51.8m, 52.7m, 52, 52.6m, 53, 53.6m,
                          55.4m, 55.9m, 56, 56.2m, 57.8m, 57, 57.9m, 57.2m, 58.1m, 58.3m };
        var runner = new BacktestRunner();

        PairPerformanceReport report = runner.RunPairTrading(pricesA, pricesB, 5, 1.5m, 1.0m, 10000m, 0.001m);

        Assert.Equal(4, report.Trades.Count);
        Assert.Equal(9, report.Trades[0].Day);
        Assert.Equal(11, report.Trades[1].Day);
        Assert.Equal(18, report.Trades[2].Day);
        Assert.Equal(19, report.Trades[3].Day);
        Assert.Equal(10333.24m, report.EquityCurve[19], 2);
    }
}
