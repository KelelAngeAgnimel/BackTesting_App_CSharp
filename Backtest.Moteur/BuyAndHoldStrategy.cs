using System;

// Buy & Hold: buy on the first day and keep the position until the end.
// It is also used as a benchmark to compare the other strategies.
public class BuyAndHoldStrategy
{
    public Signal[] GenerateSignals(decimal[] prices)
    {
        // Every day starts as Hold (the default value of the enum)
        Signal[] signals = new Signal[prices.Length];

        if (prices.Length > 0)
        {
            // The backtester executes this signal on the next day
            signals[0] = Signal.Buy;
        }

        return signals;
    }
}