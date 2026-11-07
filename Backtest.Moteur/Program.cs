using System.Diagnostics;

decimal[] prices =
{
    100, 102, 104, 103, 105, 107, 106, 108, 110, 109,
    111, 113, 112, 114, 106, 98, 92, 86, 80, 75
};

var strategy = new MovingAverageCrossoverStrategy(3, 6);
Signal[] signals = strategy.GenerateSignals(prices);

for (int i = 0; i < signals.Length; i++)
    Console.WriteLine($"{i} : {prices[i]} : {signals[i]}");

var backtester = new Backtester();
BacktestResult result = backtester.Run(prices, signals, 10000m, 0.001m, 0m);

for (int i = 0; i < prices.Length; i++)
    Console.WriteLine($"{i} : {prices[i]} : {result.EquityCurve[i]}");

foreach (Trade trade in result.Trades)
    Console.WriteLine($"day {trade.Day} : {trade.Signal} at {trade.Price}");

decimal[] curve = result.EquityCurve;
Console.WriteLine(PerformanceMetrics.CumulativeReturn(curve));
Console.WriteLine(PerformanceMetrics.AverageDailyReturn(curve));
Console.WriteLine(PerformanceMetrics.Volatility(curve));
Console.WriteLine(PerformanceMetrics.MaxDrawdown(curve));
Console.WriteLine(PerformanceMetrics.SharpeRatio(curve));

decimal[] pricesA = { 100, 102, 104, 103, 105, 107, 106, 108, 110, 109, 111, 113 };
decimal[] pricesB = { 50, 51, 52, 51.5m, 52.5m, 53.5m, 53, 54, 50, 49, 55.5m, 56.5m };

decimal?[] z = Indicator.ZScore(pricesA, pricesB, 5);
for (int i = 0; i < z.Length; i++)
    Console.WriteLine($"{i} : {z[i]}");