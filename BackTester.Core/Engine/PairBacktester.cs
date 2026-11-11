using System;
using System.Diagnostics;
namespace BackTester.Core.Engine;

public class PairBacktester
{
    private decimal OpenProfit(int direction, decimal quantityA, decimal quantityB, decimal entryA, decimal entryB, decimal priceA, decimal priceB)
    {
        return direction * quantityA * (priceA - entryA) - direction * quantityB * (priceB - entryB);
    }

    public PairBacktestResult Run(decimal[] pricesA, decimal[] pricesB, PairSignal[] signals, decimal initialCapital, decimal feeRate)
    {
        decimal cash = initialCapital;
        decimal[] equityCurve = new decimal[pricesA.Length];
        List<PairTrade> trades = new List<PairTrade>();

        int direction = 0;   // 0 = no position, 1 = long A / short B, -1 = short A / long B
        decimal quantityA = 0;
        decimal quantityB = 0;
        decimal entryA = 0;
        decimal entryB = 0;

        for (int i = 0; i < pricesA.Length; i++)
        {
            
            if (i>0)
            {
                PairSignal signal = signals[i - 1];
                if ((signal == PairSignal.OpenLong || signal == PairSignal.OpenShort) && direction == 0)
                {
                    decimal notional = cash / 2;
                    quantityA = notional / pricesA[i];
                    quantityB = notional / pricesB[i];
                    entryA = pricesA[i];
                    entryB = pricesB[i];

                    if (signal == PairSignal.OpenLong) direction = 1;
                    if (signal == PairSignal.OpenShort) direction = -1;

                    cash -= 2 * notional * feeRate;
                    trades.Add (new PairTrade(i, signal, pricesA[i], pricesB[i]));
                }

                else if (signal == PairSignal.Close && direction != 0)
                {
                    decimal profit = OpenProfit(direction, quantityA, quantityB, entryA, entryB, pricesA[i], pricesB[i]);
                    decimal fees = feeRate * (quantityA * pricesA[i] + quantityB * pricesB[i]);
                    cash = cash + profit - fees;
                    direction = 0; quantityA = 0; quantityB = 0;
                    trades.Add(new PairTrade(i, signal, pricesA[i], pricesB[i]));
                }
            }
            equityCurve[i] = cash + OpenProfit(direction, quantityA, quantityB, entryA, entryB, pricesA[i], pricesB[i]);
        }
        return new PairBacktestResult(equityCurve, trades);
    }       
}
            