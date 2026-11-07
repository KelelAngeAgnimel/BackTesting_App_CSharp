using System;
public enum Signal { Hold, Buy, Sell }

public class MovingAverageCrossoverStrategy
{
    private int shortPeriod;
    private int longPeriod;

    public MovingAverageCrossoverStrategy(int shortPeriod, int longPeriod)
    {
        this.shortPeriod = shortPeriod;
        this.longPeriod = longPeriod;
    }

    public Signal[] GenerateSignals(decimal[] prices)
    {
        Signal[] signals = new Signal[prices.Length];

        decimal?[] shortAvg = Indicator.SimpleMovingAverage(prices, shortPeriod);
        decimal?[] longAvg = Indicator.SimpleMovingAverage(prices, longPeriod);
        
        for (int i = 1; i < prices.Length; i++)
        {
            if (shortAvg[i] == null || longAvg[i] == null || shortAvg[i-1] == null || longAvg[i-1] == null)
            {
                continue;
            }
            else if (shortAvg[i-1] <= longAvg[i-1] && shortAvg[i] > longAvg[i])
            {
                signals[i] = Signal.Buy;
            }
            else if (shortAvg[i - 1] >= longAvg[i - 1] && shortAvg[i] < longAvg[i])
            {
                signals [i] = Signal.Sell;
            }
        }

        return signals;
    }
}