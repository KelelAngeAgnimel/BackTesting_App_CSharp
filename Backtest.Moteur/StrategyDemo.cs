using System;

// Quick manual test of my strategies (console output only)
public static class StrategyDemo
{
    public static void Run()
    {
        decimal[] prices =
        {
            100, 102, 104, 103, 105, 107, 106, 108, 110, 109,
            111, 113, 112, 114, 106, 98, 92, 86, 80, 75
        };

        Show("Buy & Hold", new Backtester().Run(prices, new BuyAndHoldStrategy().GenerateSignals(prices), 10000m, 0.001m, 0m));
        Show("Breakout (lookback 5)", new Backtester().Run(prices, new BreakoutStrategy(5).GenerateSignals(prices), 10000m, 0.001m, 0m));
        Show("DCA (4 parts, every 5 days)", new DcaStrategy(5, 4).Run(prices, 10000m, 0.001m));
    }

    private static void Show(string name, BacktestResult result)
    {
        Console.WriteLine($"--- {name} ---");
        foreach (Trade trade in result.Trades)
            Console.WriteLine($"day {trade.Day} : {trade.Signal} at {trade.Price}");

        Console.WriteLine($"Final value: {result.EquityCurve[^1]}");
        Console.WriteLine($"Cumulative return: {PerformanceMetrics.CumulativeReturn(result.EquityCurve)}");
    }
}