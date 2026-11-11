using System;
namespace BackTester.Core.Engine;

public class Backtester
{
    public Backtester() { }
    public BacktestResult Run(decimal[] prices, Signal[] signals, decimal initialCapital, decimal feeRate, decimal StopLossRate)
    {
        decimal cash = initialCapital;
        decimal shares = 0;
        decimal entryPrice = 0;
        decimal[] EquityCurve = new decimal[prices.Length];
        List<Trade> trades = new List<Trade>();

        for (int i = 0; i < prices.Length; i++)
        {
            if (i > 0)
            {
                if (StopLossRate>0 && shares > 0 && (prices[i] < entryPrice * (1 - StopLossRate)))
                {
                    cash = shares * prices[i] * (1 - feeRate);
                    shares = 0;
                    trades.Add(new Trade(i, Signal.Sell, prices[i]));
                }

                else if (signals[i - 1] == Signal.Buy && shares == 0)
                {
                    shares = cash * (1 - feeRate) / prices[i];
                    entryPrice = prices[i];
                    cash = 0;
                    trades.Add(new Trade(i, Signal.Buy, prices[i]));
                }

                else if (signals[i-1] == Signal.Sell && shares > 0)
                {
                    cash = shares * prices[i] * (1 - feeRate);
                    shares = 0;
                    trades.Add(new Trade(i, Signal.Sell, prices[i]));
                }
            }
            EquityCurve[i] = cash + shares * prices[i];
        }
        return new BacktestResult(EquityCurve, trades);
    }
}


