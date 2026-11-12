using System;

namespace BackTester.Core.Engine;

// Runs the strategies written by A: Buy & Hold, Breakout and DCA.
// Same style as BacktestRunner: validate, run, then compute the five metrics.
public class BasicStrategiesRunner
{
    public PerformanceReport RunBuyAndHold(decimal[] closes, decimal initialCapital, decimal feeRate, decimal stopLossRate)
    {
        ValidateCommon(closes, initialCapital, feeRate, stopLossRate);

        var strategy = new BuyAndHoldStrategy();
        Signal[] signals = strategy.GenerateSignals(closes);
        BacktestResult result = new Backtester().Run(closes, signals, initialCapital, feeRate, stopLossRate);

        return BuildReport(result);
    }

    public PerformanceReport RunBreakout(decimal[] closes, int lookback, decimal threshold, decimal initialCapital, decimal feeRate, decimal stopLossRate)
    {
        ValidateCommon(closes, initialCapital, feeRate, stopLossRate);

        if (lookback < 1)
            throw new ArgumentException("lookback must be greater than or equal to 1.");
        if (threshold < 0)
            throw new ArgumentException("threshold must be positive or zero.");
        if (closes.Length <= lookback)
            throw new ArgumentException("Not enough price data for this lookback.");

        var strategy = new BreakoutStrategy(lookback, threshold);
        Signal[] signals = strategy.GenerateSignals(closes);
        BacktestResult result = new Backtester().Run(closes, signals, initialCapital, feeRate, stopLossRate);

        return BuildReport(result);
    }

    // No stop-loss for DCA: it buys little by little, so there is no single entry price
    public PerformanceReport RunDca(decimal[] closes, int intervalDays, int installments, decimal initialCapital, decimal feeRate)
    {
        ValidateCommon(closes, initialCapital, feeRate, 0m);

        if (intervalDays < 1)
            throw new ArgumentException("intervalDays must be greater than or equal to 1.");
        if (installments < 1)
            throw new ArgumentException("installments must be greater than or equal to 1.");

        var strategy = new DcaStrategy(intervalDays, installments);
        BacktestResult result = strategy.Run(closes, initialCapital, feeRate);

        return BuildReport(result);
    }

    private static void ValidateCommon(decimal[] closes, decimal initialCapital, decimal feeRate, decimal stopLossRate)
    {
        if (closes == null || closes.Length < 2)
            throw new ArgumentException("Not enough price data.");
        if (initialCapital <= 0)
            throw new ArgumentException("initialCapital must be a positive value.");
        if (feeRate < 0 || stopLossRate < 0 || stopLossRate >= 1)
            throw new ArgumentException("feeRate must be positive and stopLossRate must be between 0 and 1.");
    }

    private static PerformanceReport BuildReport(BacktestResult result)
    {
        return new PerformanceReport
        {
            EquityCurve = result.EquityCurve,
            Trades = result.Trades,
            CumulativeReturn = PerformanceMetrics.CumulativeReturn(result.EquityCurve),
            AverageDailyReturn = PerformanceMetrics.AverageDailyReturn(result.EquityCurve),
            Volatility = PerformanceMetrics.Volatility(result.EquityCurve),
            MaxDrawdown = PerformanceMetrics.MaxDrawdown(result.EquityCurve),
            SharpeRatio = PerformanceMetrics.SharpeRatio(result.EquityCurve),
        };
    }
}