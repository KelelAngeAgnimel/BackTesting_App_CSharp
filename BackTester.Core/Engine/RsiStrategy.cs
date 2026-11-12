using System;
  namespace BackTester.Core.Engine;
public class RsiStrategy
{
    private readonly int period;
    private readonly decimal buyThreshold;
    private readonly decimal sellThreshold;

    public RsiStrategy(int period = 14, decimal buyThreshold = 30m, decimal sellThreshold = 70m)
    {
        if (period < 2)
            throw new ArgumentException("La période du RSI doit être au moins 2.");
        if (buyThreshold < 0 || sellThreshold > 100 || buyThreshold >= sellThreshold)
            throw new ArgumentException("Il faut 0 <= seuil d'achat < seuil de vente <= 100.");

        this.period = period;
        this.buyThreshold = buyThreshold;
        this.sellThreshold = sellThreshold;
    }

    public Signal[] GenerateSignals(decimal[] prices)
    {
        Signal[] signals = new Signal[prices.Length];
        decimal?[] rsi = MomentumIndicators.RSI(prices, period);

        for (int i = 1; i < prices.Length; i++)
        {
            if (rsi[i] is not decimal current || rsi[i - 1] is not decimal previous)
            {
                continue;
            }
            else if (previous >= buyThreshold && current < buyThreshold)
            {
                signals[i] = Signal.Buy;
            }
            else if (previous <= sellThreshold && current > sellThreshold)
            {
                signals[i] = Signal.Sell;
            }
        }

        return signals;
    }
}