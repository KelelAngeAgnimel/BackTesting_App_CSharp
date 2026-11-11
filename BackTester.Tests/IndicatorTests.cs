using System;
using BackTester.Core.Engine;
using Xunit;

namespace BackTester.Tests;

public class IndicatorTests
{
    [Fact]
    public void SimpleMovingAverage_Period3_IsCorrect()
    {
        decimal[] prices = { 100, 102, 104, 103, 105 };

        decimal?[] result = Indicator.SimpleMovingAverage(prices, 3);

        Assert.Null(result[0]);
        Assert.Null(result[1]);
        Assert.Equal(102m, result[2]);
        Assert.Equal(103m, result[3]);
        Assert.Equal(104m, result[4]);
    }
}
