using System;

namespace BackTester.Core.Engine;

// Breakout: buy when the price goes above the highest close of the last N days,
// sell when it goes below the lowest close of the last N days.
public class BreakoutStrategy
{
    private readonly int lookback;
    private readonly decimal threshold;

    // lookback: number of previous days used (for example 20)
    // threshold: extra margin needed to confirm a breakout (0.01 = 1%)
    public BreakoutStrategy(int lookback, decimal threshold = 0m)
    {
        if (lookback < 1)
            throw new ArgumentException("Lookback must be at least 1.");
        if (threshold < 0m)
            throw new ArgumentException("Threshold cannot be negative.");

        this.lookback = lookback;
        this.threshold = threshold;
    }

    public Signal[] GenerateSignals(decimal[] prices)
    {
        // Every day starts as Hold (the default value of the enum)
        Signal[] signals = new Signal[prices.Length];

        for (int i = lookback; i < prices.Length; i++)
        {
            // Highest and lowest close of the N days BEFORE day i
            decimal highest = prices[i - lookback];
            decimal lowest = prices[i - lookback];
            for (int j = i - lookback + 1; j < i; j++)
            {
                if (prices[j] > highest) highest = prices[j];
                if (prices[j] < lowest) lowest = prices[j];
            }

            if (prices[i] > highest * (1 + threshold))
                signals[i] = Signal.Buy;
            else if (prices[i] < lowest * (1 - threshold))
                signals[i] = Signal.Sell;
        }

        return signals;
    }
}