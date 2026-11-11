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

        var strategy = new BuyAndHoldStrategy();
        Signal[] signals = strategy.GenerateSignals(prices);

        var backtester = new Backtester();
        BacktestResult result = backtester.Run(prices, signals, 10000m, 0.001m, 0m);

        Console.WriteLine("--- Buy & Hold ---");
        foreach (Trade trade in result.Trades)
            Console.WriteLine($"day {trade.Day} : {trade.Signal} at {trade.Price}");

        Console.WriteLine($"Final value: {result.EquityCurve[^1]}");
        Console.WriteLine($"Cumulative return: {PerformanceMetrics.CumulativeReturn(result.EquityCurve)}");
    }
}