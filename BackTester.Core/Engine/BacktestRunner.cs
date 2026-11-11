using System;
using System.Collections.Generic;
using System.Text;

namespace BackTester.Core.Engine;

public class BacktestRunner
{
    public PerformanceReport RunMovingAverage(decimal[] closes, int shortPeriod, int longPeriod, decimal initialCapital, decimal feeRate, decimal stopLossRate)
    {
        // 1. Validation

        if (shortPeriod < 1)
        {
            throw new ArgumentException("shortPeriod must be greater than or equal to 1.");
        }

        if (longPeriod <= shortPeriod)
        {
            throw new ArgumentException("longPeriod must be greater than shortPeriod.");
        }

        if (closes.Length <= longPeriod)
        {
            throw new ArgumentException("Not enough price data for these periods.");
        }

        if (initialCapital <= 0)
        {
            throw new ArgumentException("initialCapital must be a positive value.");
        }

        if (feeRate < 0 || stopLossRate < 0 || stopLossRate >= 1)
        {
            throw new ArgumentException("feeRate must be positive and stopLossRate must be between 0 and 1.");
        }

        var strategy = new MovingAverageCrossoverStrategy(shortPeriod, longPeriod);
        Signal[] signals = strategy.GenerateSignals(closes);
        var backtester = new Backtester();
        BacktestResult result = backtester.Run(closes, signals, initialCapital, feeRate, stopLossRate);



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

    public PairPerformanceReport RunPairTrading(decimal[] closesA, decimal[] closesB, int window, decimal entryZ, decimal exitZ, decimal initialCapital, decimal feeRate)
    {
        // 1. Validation

        if (closesA.Length != closesB.Length)
        {
            throw new ArgumentException("The two price series must have the same length.");
        }

        if (window < 2)
        {
            throw new ArgumentException("Window must be at least 2.");
        }

        if (closesA.Length <= window)
        {
            throw new ArgumentException("Not enough price data for this window.");
        }

        if (entryZ <= 0)
        {
            throw new ArgumentException("entryZ must be a positive value.");
        }

        if (exitZ < 0 || exitZ >= entryZ)
        {
            throw new ArgumentException("exitZ must be smaller than entryZ.");
        }

        if (initialCapital <= 0)
        {
            throw new ArgumentException("initialCapital must be a positive value.");
        }

        if (feeRate < 0)
        {
            throw new ArgumentException("feeRate must be a positive value.");
        }

        var strategy = new PairTradingStrategy(window, entryZ, exitZ);
        PairSignal[] signals = strategy.GenerateSignals(closesA, closesB);
        var backtester = new PairBacktester();
        PairBacktestResult result = backtester.Run(closesA, closesB, signals, initialCapital, feeRate);

        return new PairPerformanceReport
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
