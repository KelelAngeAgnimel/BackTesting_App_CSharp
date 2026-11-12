using System;
  namespace BackTester.Core.Engine;
public class MomentumStrategy
{
    private readonly int period;
    private readonly decimal threshold;

    public MomentumStrategy(int period = 10, decimal threshold = 0m)
    {
        if (period < 1)
            throw new ArgumentException("La période du ROC doit être au moins 1.");

        this.period = period;
        this.threshold = threshold;
    }

    public Signal[] GenerateSignals(decimal[] prices)
    {
        Signal[] signals = new Signal[prices.Length];
        decimal?[] roc = MomentumIndicators.ROC(prices, period);

        for (int i = 1; i < prices.Length; i++)
        {
            if (roc[i] is not decimal current || roc[i - 1] is not decimal previous)
            {
                continue;
            }
            else if (previous <= threshold && current > threshold)
            {
                signals[i] = Signal.Buy;
            }
            else if (previous >= threshold && current < threshold)
            {
                signals[i] = Signal.Sell;
            }
        }

        return signals;
    }
}