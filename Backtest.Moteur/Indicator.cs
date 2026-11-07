using System;

public static class Indicator
{
    public static decimal?[] SimpleMovingAverage(decimal[] prices, int period)
    {
        decimal?[] results = new decimal?[prices.Length];
        for (int i = 0; i < prices.Length; i++)
        {
            if (i < period - 1)
            {
                results[i] = null;
            }
            else
            {
                decimal sum = 0;
                for (int j = i - period + 1; j <= i; j++)
                {
                    sum += prices[j];
                }
                results[i] = sum / period;
            }
        }
        return results;
    }

    public static decimal?[] ZScore(decimal[] pricesA, decimal[] pricesB, int window)
    {
        decimal?[] zScores = new decimal?[pricesA.Length];

        decimal[] ratio = new decimal[pricesA.Length];
        for (int i = 0; i < pricesA.Length; i++)
        {
            ratio[i] = pricesA[i] / pricesB[i];
        }

        for (int i = window - 1; i < pricesA.Length; i++)
        {
            decimal sum = 0;
            for (int j = i - window + 1; j <= i; j++)
            {
                sum += ratio[j];
            }
            decimal mean = sum / window;

            decimal sumOfSquares = 0;
            for (int j = i - window + 1; j <= i; j++)
            {
                sumOfSquares += (ratio[j] - mean) * (ratio[j] - mean);

            }
            decimal std = (decimal)Math.Sqrt((double)(sumOfSquares / (window - 1)));

            if (std == 0)
            {
                continue;
            }
            zScores[i] = (ratio[i] - mean) / std;
            
        }

        return zScores;
    }
}

