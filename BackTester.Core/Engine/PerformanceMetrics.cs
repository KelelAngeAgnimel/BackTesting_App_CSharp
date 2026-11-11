using System;
namespace BackTester.Core.Engine;

public static class PerformanceMetrics
{
    public static decimal[] DailyReturns(decimal[] equityCurve)
    {
        decimal[] returns = new decimal[equityCurve.Length - 1];

        // for i from 1 to the end:
        for (int i = 1; i < equityCurve.Length; i++)
        {
            returns[i - 1] = (equityCurve[i] - equityCurve[i - 1]) / equityCurve[i - 1];
        }

        return returns;
    }

    public static decimal CumulativeReturn(decimal[] equityCurve)
    {
       // the last value is equityCurve[equityCurve.Length - 1]
        return (equityCurve[equityCurve.Length - 1] - equityCurve[0]) / equityCurve[0];
    }

    public static decimal AverageDailyReturn(decimal[] equityCurve)
    {
        decimal sum = 0;
        decimal[] dailyReturns = DailyReturns(equityCurve);
        for (int i = 0; i < dailyReturns.Length; i++)
        {
            sum += dailyReturns[i];
        }

        return sum / dailyReturns.Length;
    }

    public static decimal Volatility(decimal[] equityCurve)
    {
        decimal[] dailyReturns = DailyReturns(equityCurve);
        decimal mean = AverageDailyReturn(equityCurve);

        decimal sumOfSquares = 0;
        for (int i = 0; i < dailyReturns.Length; i++)
        {
            decimal diff = dailyReturns[i] - mean;
            sumOfSquares += diff * diff;
        }
        decimal variance = sumOfSquares / (dailyReturns.Length - 1);
        return (decimal)Math.Sqrt((double)variance);
    }

    public static decimal MaxDrawdown(decimal[] equityCurve)
    {
        decimal peak = equityCurve[0];
        decimal maxDrawdown = 0;

        for (int i = 1; i < equityCurve.Length; i++)
        {
            if (equityCurve[i] > peak)
            {
                peak = equityCurve[i];
            }
            decimal drawdown = (peak - equityCurve[i]) / peak;

            if (drawdown > maxDrawdown)
            {
                maxDrawdown = drawdown;
            }
        }

        return maxDrawdown;
    }

    public static decimal SharpeRatio(decimal[] equityCurve)
    {
        decimal averageReturn = AverageDailyReturn(equityCurve);
        decimal volatility = Volatility(equityCurve);
        decimal dailyRiskFreeRate = 0.02m / 252m;

        if (volatility == 0)
        {
            return 0; // Avoid division by zero
        }
        else
        {
            decimal dailySharpe = (averageReturn - dailyRiskFreeRate) / volatility;
            return dailySharpe * (decimal)Math.Sqrt(252);
        }
    }
}



