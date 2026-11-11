
public static class MomentumIndicators
{
    // ROC en % : (prix[i] / prix[i-N] - 1) * 100
    public static decimal?[] ROC(decimal[] prices, int period)
    {
        var result = new decimal?[prices.Length];
        for (int i = period; i < prices.Length; i++)
            result[i] = (prices[i] / prices[i - period] - 1m) * 100m;
        return result;
    }

    // RSI de Wilder
    public static decimal?[] RSI(decimal[] prices, int period = 14)
    {
        var result = new decimal?[prices.Length];
        if (prices.Length <= period) return result;

        // 1re valeur : moyenne simple des gains et des pertes sur 'period' jours
        decimal avgGain = 0, avgLoss = 0;
        for (int i = 1; i <= period; i++)
        {
            var change = prices[i] - prices[i - 1];
            if (change > 0) avgGain += change; else avgLoss -= change;
        }
        avgGain /= period;
        avgLoss /= period;
        result[period] = ToRsi(avgGain, avgLoss);

        // Valeurs suivantes : moyenne lissée de Wilder
        for (int i = period + 1; i < prices.Length; i++)
        {
            var change = prices[i] - prices[i - 1];
            var gain = change > 0 ? change : 0m;
            var loss = change < 0 ? -change : 0m;
            avgGain = (avgGain * (period - 1) + gain) / period;
            avgLoss = (avgLoss * (period - 1) + loss) / period;
            result[i] = ToRsi(avgGain, avgLoss);
        }
        return result;
    }

    private static decimal ToRsi(decimal avgGain, decimal avgLoss)
        => avgLoss == 0 ? 100m : 100m - 100m / (1m + avgGain / avgLoss);
}