using System;
namespace BackTester.Core.Engine;

public enum PairSignal { Hold, OpenLong, OpenShort, Close }

public class PairTradingStrategy
{
    private int window;
    private decimal entryZ;
    private decimal exitZ;

    public PairTradingStrategy(int window, decimal entryZ, decimal exitZ)
    {
        this.window = window;
        this.entryZ = entryZ;
        this.exitZ = exitZ;
    }

    public PairSignal[] GenerateSignals(decimal[] pricesA, decimal[] pricesB)
    {
        PairSignal[] signals = new PairSignal[pricesA.Length];
        decimal?[] z = Indicator.ZScore(pricesA, pricesB, window);
        int position = 0;

        for (int i = 0; i < pricesA.Length; i++)
        {
            if (z[i] == null)
            {
                continue;
            }

            if (position == 0)
            {
                if (z[i] > entryZ)
                {
                    signals[i] = PairSignal.OpenShort;
                    position = -1;
                }

                else if (z[i] < -entryZ)
                {
                    signals[i] = PairSignal.OpenLong;
                    position = 1;
                }
            }

            else
            {
                if (Math.Abs(z[i].Value) < exitZ)
                {
                    signals[i] = PairSignal.Close;
                    position = 0;
                }
            }

        }

        return signals;
    }
}
