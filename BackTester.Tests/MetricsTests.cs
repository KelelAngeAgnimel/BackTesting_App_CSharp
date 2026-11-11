using System;
using BackTester.Core.Engine;
using Xunit;

namespace BackTester.Tests;

public class MetricsTests
{
    [Fact]
    public void CumulativeReturn_ForSimpleCurve_IsCorrect()
    {
        // Arrange
        decimal[] curve = { 100, 110, 99, 120 };

        // Act
        decimal result = PerformanceMetrics.CumulativeReturn(curve);

        // Assert
        Assert.Equal(0.2m, result);
    }

    [Fact]
    public void MaxDrawdown_ForSimpleCurve_IsCorrect()
    {
        decimal[] curve = { 100, 110, 99, 120 };

        decimal result = PerformanceMetrics.MaxDrawdown(curve);

        Assert.Equal(0.1m, result);
    }

    [Fact]
    public void Volatility_ForSimpleCurve_IsCorrect()
    {
        decimal[] curve = { 100, 110, 99, 120 };

        decimal result = PerformanceMetrics.Volatility(curve);

        Assert.Equal(0.1581m, result, 4);
    }
}
