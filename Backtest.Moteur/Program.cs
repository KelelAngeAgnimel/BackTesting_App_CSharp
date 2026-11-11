using System.Diagnostics;
StrategyDemo.Run();
return;
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

decimal[] pricesA = { 100, 101.5m, 103, 102, 104.5m, 105, 104, 106.5m, 112, 113, 111, 110, 112, 113.5m, 114, 113, 115, 110, 116, 117 };
decimal[] pricesB = { 50, 50.5m, 51.2m, 51.3m, 51.8m, 52.7m, 52, 52.6m, 53, 53.6m, 55.4m, 55.9m, 56, 56.2m, 57.8m, 57, 57.9m, 57.2m, 58.1m, 58.3m };

decimal?[] z = Indicator.ZScore(pricesA, pricesB, 5);
for (int i = 0; i < z.Length; i++)
    Console.WriteLine($"{i} : {z[i]}");

var pairStrategy = new PairTradingStrategy(5, 1.5m, 1.0m);
PairSignal[] pairSignals = pairStrategy.GenerateSignals(pricesA, pricesB);

for (int i = 0; i < pairSignals.Length; i++)
    Console.WriteLine($"{i} : {pairSignals[i]}");

var pairBacktester = new PairBacktester();
PairBacktestResult pairResult = pairBacktester.Run(pricesA, pricesB, pairSignals, 10000m, 0.001m);

for (int i = 0; i < pricesA.Length; i++)
    Console.WriteLine($"{i} : {pairResult.EquityCurve[i]}");

foreach (PairTrade t in pairResult.Trades)
    Console.WriteLine($"day {t.Day} : {t.Signal} at A={t.PriceA}, B={t.PriceB}");

decimal[] pairCurve = pairResult.EquityCurve;
Console.WriteLine(PerformanceMetrics.CumulativeReturn(pairCurve));
Console.WriteLine(PerformanceMetrics.AverageDailyReturn(pairCurve));
Console.WriteLine(PerformanceMetrics.Volatility(pairCurve));
Console.WriteLine(PerformanceMetrics.MaxDrawdown(pairCurve));
Console.WriteLine(PerformanceMetrics.SharpeRatio(pairCurve));
