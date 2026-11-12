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
    decimal Sharpe,
    PerformanceReport? Report = null); // engine report of single-symbol strategies, used for the PDF

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
                new StrategyParam("ShortPeriod", "Short period", 20, 1, true),
                new StrategyParam("LongPeriod", "Long period", 50, 2, true),
            },
            p => p["ShortPeriod"] >= p["LongPeriod"]
                ? "The short period must be smaller than the long period." : null,
            p => (int)p["LongPeriod"] + 1,
            i =>
            {
                var r = new BacktestRunner().RunMovingAverage(
                    i.ClosesA, (int)i.Params["ShortPeriod"], (int)i.Params["LongPeriod"],
                    i.InitialCapital, i.FeeRate, i.StopLossRate);
                return ToResult(r);
            }),

        new StrategyDefinition(
            "pair", "Pair Trading", IsPair: true, SupportsStopLoss: false,
            new[]
            {
                new StrategyParam("Window", "Window", 20, 2, true),
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

        new StrategyDefinition(
            "buyhold", "Buy & Hold", IsPair: false, SupportsStopLoss: true,
            Array.Empty<StrategyParam>(),
            _ => null,
            _ => 2,
            i => ToResult(new BasicStrategiesRunner().RunBuyAndHold(
                i.ClosesA, i.InitialCapital, i.FeeRate, i.StopLossRate))),

        new StrategyDefinition(
            "breakout", "Breakout", IsPair: false, SupportsStopLoss: true,
            new[]
            {
                new StrategyParam("Lookback", "Lookback", 20, 1, true),
                // Shown in %, divided by 100 before the engine call (it expects a fraction)
                new StrategyParam("Threshold", "Threshold (%, divided by 100 for the engine)", 0, 0, false),
            },
            _ => null,
            p => (int)p["Lookback"] + 1,
            i => ToResult(new BasicStrategiesRunner().RunBreakout(
                i.ClosesA, (int)i.Params["Lookback"], i.Params["Threshold"] / 100m,
                i.InitialCapital, i.FeeRate, i.StopLossRate))),

        new StrategyDefinition(
            "dca", "Dollar-Cost Averaging (DCA)", IsPair: false, SupportsStopLoss: false,
            new[]
            {
                new StrategyParam("IntervalDays", "Interval between purchases", 20, 1, true),
                new StrategyParam("Installments", "Number of purchases", 12, 1, true),
            },
            _ => null,
            _ => 2,
            i => ToResult(new BasicStrategiesRunner().RunDca(
                i.ClosesA, (int)i.Params["IntervalDays"], (int)i.Params["Installments"],
                i.InitialCapital, i.FeeRate))),
    };

    // Common shape for the strategies that run on one symbol
    private static EngineResult ToResult(PerformanceReport r) =>
        new(r.EquityCurve,
            r.Trades.Select(t => new EngineTrade(t.Day, t.Signal.ToString(), t.Price, null)).ToList(),
            r.CumulativeReturn, r.AverageDailyReturn, r.Volatility, r.MaxDrawdown, r.SharpeRatio, r);

    public static StrategyDefinition Get(string id) =>
        All.FirstOrDefault(s => s.Id == id)
        ?? throw new BacktestException("Unknown strategy: " + id);
}
