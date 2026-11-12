using BackTester.Core.Engine;
using Xunit;

namespace BackTester.Tests;

public class BasicStrategiesRunnerTests
{
    // Same 20 test prices as the engine demo; expected values were computed by hand
    private static readonly decimal[] Prices =
    {
        100, 102, 104, 103, 105, 107, 106, 108, 110, 109,
        111, 113, 112, 114, 106, 98, 92, 86, 80, 75
    };

    [Fact]
    public void BuyAndHold_buys_on_day_1_and_keeps_until_the_end()
    {
        var report = new BasicStrategiesRunner().RunBuyAndHold(Prices, 10000m, 0.001m, 0m);

        Assert.Single(report.Trades);
        Assert.Equal(1, report.Trades[0].Day);
        Assert.Equal(7345.59m, report.EquityCurve[^1], 2);
        Assert.Equal(-0.2654m, report.CumulativeReturn, 4);
    }

    [Fact]
    public void Breakout_buys_on_day_6_and_sells_on_day_15()
    {
        var report = new BasicStrategiesRunner().RunBreakout(Prices, 5, 0m, 10000m, 0.001m, 0m);

        Assert.Equal(2, report.Trades.Count);
        Assert.Equal(6, report.Trades[0].Day);
        Assert.Equal(15, report.Trades[1].Day);
        Assert.Equal(9226.80m, report.EquityCurve[^1], 2);
        Assert.Equal(-0.0773m, report.CumulativeReturn, 4);
    }

    [Fact]
    public void Dca_makes_four_purchases_every_five_days()
    {
        var report = new BasicStrategiesRunner().RunDca(Prices, 5, 4, 10000m, 0.001m);

        Assert.Equal(4, report.Trades.Count);
        Assert.Equal(1, report.Trades[0].Day);
        Assert.Equal(16, report.Trades[3].Day);
        Assert.Equal(7297.13m, report.EquityCurve[^1], 2);
        Assert.Equal(-0.2703m, report.CumulativeReturn, 4);
    }

    [Fact]
    public void Invalid_parameters_raise_an_ArgumentException()
    {
        var runner = new BasicStrategiesRunner();

        Assert.Throws<ArgumentException>(() => runner.RunBreakout(Prices, 0, 0m, 10000m, 0.001m, 0m));
        Assert.Throws<ArgumentException>(() => runner.RunDca(Prices, 5, 0, 10000m, 0.001m));
        Assert.Throws<ArgumentException>(() => runner.RunBuyAndHold(Prices, -1m, 0.001m, 0m));
    }
}