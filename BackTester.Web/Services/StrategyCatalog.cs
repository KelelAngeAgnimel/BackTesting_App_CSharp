using BackTester.Core.Engine;

namespace BackTester.Web.Services;

// One numeric parameter of a strategy (used to build the form)
public record StrategyParam(string Key, string Label, decimal Default, decimal Min, bool IsInteger);

// What the engine returns, in a common shape for every strategy
public record EngineResult(
    decimal[] Equity,
    List<EngineTrade> Trades,
    decimal CumulativeReturn,
    decimal AverageDailyReturn,
    decimal Volatility,
    decimal MaxDrawdown,
    decimal Sharpe);

// Day is an index in the price series. PriceB is only used by pair trading.
public record EngineTrade(int Day, string Type, decimal Price, decimal? PriceB);

// Values the screen gives to a strategy (fees and stop-loss already as fractions)
public record RunInput(
    decimal[] ClosesA, decimal[]? ClosesB,
    decimal InitialCapital, decimal FeeRate, decimal StopLossRate,
    IReadOnlyDictionary<string, decimal> Params);

public record StrategyDefinition(
    string Id,
    string Name,
    bool IsPair,
    bool SupportsStopLoss,
    IReadOnlyList<StrategyParam> Params,
    Func<IReadOnlyDictionary<string, decimal>, string?> Validate, // error message or null
    Func<IReadOnlyDictionary<string, decimal>, int> MinDays,
    Func<RunInput, EngineResult> Run);

public static class StrategyCatalog
{
    // To add a strategy once it exists in BacktestRunner: add one entry to this list.
    public static readonly IReadOnlyList<StrategyDefinition> All = new[]
    {
        new StrategyDefinition(
            "ma", "Moving Average Crossover", IsPair: false, SupportsStopLoss: true,
            new[]
            {
                new StrategyParam("ShortPeriod", "Short period (days)", 20, 1, true),
                new StrategyParam("LongPeriod", "Long period (days)", 50, 2, true),
            },
            p => p["ShortPeriod"] >= p["LongPeriod"]
                ? "The short period must be smaller than the long period." : null,
            p => (int)p["LongPeriod"] + 1,
            i =>
            {
                var r = new BacktestRunner().RunMovingAverage(
                    i.ClosesA, (int)i.Params["ShortPeriod"], (int)i.Params["LongPeriod"],
                    i.InitialCapital, i.FeeRate, i.StopLossRate);
                return new EngineResult(
                    r.EquityCurve,
                    r.Trades.Select(t => new EngineTrade(t.Day, t.Signal.ToString(), t.Price, null)).ToList(),
                    r.CumulativeReturn, r.AverageDailyReturn, r.Volatility, r.MaxDrawdown, r.SharpeRatio);
            }),

        new StrategyDefinition(
            "pair", "Pair Trading", IsPair: true, SupportsStopLoss: false,
            new[]
            {
                new StrategyParam("Window", "Window (days)", 20, 2, true),
                new StrategyParam("EntryZ", "Entry threshold (z-score)", 2.0m, 0.1m, false),
                new StrategyParam("ExitZ", "Exit threshold (z-score)", 0.5m, 0, false),
            },
            p => p["ExitZ"] >= p["EntryZ"]
                ? "The exit threshold must be smaller than the entry threshold." : null,
            p => (int)p["Window"] + 1,
            i =>
            {
                var r = new BacktestRunner().RunPairTrading(
                    i.ClosesA, i.ClosesB!, (int)i.Params["Window"],
                    i.Params["EntryZ"], i.Params["ExitZ"], i.InitialCapital, i.FeeRate);
                return new EngineResult(
                    r.EquityCurve,
                    r.Trades.Select(t => new EngineTrade(t.Day, t.Signal.ToString(), t.PriceA, t.PriceB)).ToList(),
                    r.CumulativeReturn, r.AverageDailyReturn, r.Volatility, r.MaxDrawdown, r.SharpeRatio);
            }),
    };

    public static StrategyDefinition Get(string id) =>
        All.FirstOrDefault(s => s.Id == id)
        ?? throw new BacktestException("Unknown strategy: " + id);
}
