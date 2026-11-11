using BackTester.Web.Data;

namespace BackTester.Web.Services;

// Fees and stop-loss are percentages here (0.1 = 0.1 %); SymbolB is only for pair strategies
public record BacktestRequest(
    string StrategyId, string Symbol, string? SymbolB,
    DateTime Start, DateTime End,
    decimal InitialCapital, decimal FeesPercent, decimal StopLossPercent,
    IReadOnlyDictionary<string, decimal> Params);

// Date is the real date of the trade; PriceB is only filled for pair trading
public record TradeRow(DateTime Date, string Type, decimal Price, decimal? PriceB);

// Ratios are fractions (0.05 = 5 %). BuyAndHoldReturn is null for pair strategies.
public record BacktestOutput(
    BacktestRequest Request,
    string StrategyName,
    DateTime[] Dates,
    decimal[] Equity,
    List<TradeRow> Trades,
    decimal CumulativeReturn,
    decimal AverageDailyReturn,
    decimal Volatility,
    decimal MaxDrawdown,
    decimal Sharpe,
    decimal? BuyAndHoldReturn,
    decimal FinalCapital,
    decimal[] Prices) // closing prices of the (first) symbol on the period
{
    public int TradeCount => Trades.Count;
}

// One strategy chosen on the Comparison page, with its parameters
public record StrategySelection(string StrategyId, IReadOnlyDictionary<string, decimal> Params);

public record ComparisonRequest(
    string Symbol, DateTime Start, DateTime End,
    decimal InitialCapital, decimal FeesPercent, decimal StopLossPercent,
    IReadOnlyList<StrategySelection> Strategies);

public class BacktestException(string message) : Exception(message);

// Keeps the last result to pass from the Config page to the Results page
public class BacktestState
{
    public BacktestOutput? Last { get; set; }
}

public class BacktestService(MarketDataService market)
{
    public async Task<BacktestOutput> RunAsync(BacktestRequest req)
    {
        var strategy = StrategyCatalog.Get(req.StrategyId);
        CheckCommon(req.Start, req.End, req.InitialCapital, req.FeesPercent, req.StopLossPercent);

        if (strategy.IsPair && string.IsNullOrWhiteSpace(req.SymbolB))
            throw new BacktestException("Enter the second symbol for this strategy.");
        if (strategy.IsPair && SameSymbol(req.Symbol, req.SymbolB!))
            throw new BacktestException("The two symbols must be different.");

        var a = await LoadAsync(req.Symbol);
        var b = strategy.IsPair ? await LoadAsync(req.SymbolB!) : null;
        return Execute(strategy, req, a, b);
    }

    // For the Comparison page: prices are downloaded once, then every strategy runs on them
    public async Task<List<BacktestOutput>> CompareAsync(ComparisonRequest req)
    {
        if (req.Strategies.Count == 0)
            throw new BacktestException("Select at least one strategy.");
        CheckCommon(req.Start, req.End, req.InitialCapital, req.FeesPercent, req.StopLossPercent);

        var definitions = req.Strategies.Select(s => StrategyCatalog.Get(s.StrategyId)).ToList();
        if (definitions.Any(d => d.IsPair))
            throw new BacktestException("Pair Trading needs two symbols and cannot be compared here.");

        var series = await LoadAsync(req.Symbol);

        var results = new List<BacktestOutput>();
        for (var i = 0; i < definitions.Count; i++)
        {
            var one = new BacktestRequest(
                definitions[i].Id, req.Symbol, null, req.Start, req.End,
                req.InitialCapital, req.FeesPercent, req.StopLossPercent, req.Strategies[i].Params);
            results.Add(Execute(definitions[i], one, series, null));
        }
        return results;
    }

    private static BacktestOutput Execute(
        StrategyDefinition strategy, BacktestRequest req, PriceSeries a, PriceSeries? b)
    {
        var error = strategy.Validate(req.Params);
        if (error is not null) throw new BacktestException(error);

        // Keep only the chosen period BEFORE calling the engine, so the curve matches the dates
        var (dates, closesA, closesB) = Filter(req, a, b);

        var needed = strategy.MinDays(req.Params);
        if (dates.Length < needed)
            throw new BacktestException(
                $"Not enough data on this period ({dates.Length} days, at least {needed} needed). " +
                "Widen the dates or reduce the periods.");

        var input = new RunInput(
            closesA, closesB, req.InitialCapital,
            req.FeesPercent / 100m,
            strategy.SupportsStopLoss ? req.StopLossPercent / 100m : 0m,
            req.Params);

        EngineResult r;
        try
        {
            r = strategy.Run(input);
        }
        catch (ArgumentException ex)
        {
            // The engine messages are readable English sentences: show them as they are
            throw new BacktestException(ex.Message);
        }

        var trades = r.Trades
            .Select(t => new TradeRow(dates[t.Day], t.Type, t.Price, t.PriceB))
            .ToList();

        decimal? buyAndHold = strategy.IsPair ? null : closesA[^1] / closesA[0] - 1;

        return new BacktestOutput(
            req, strategy.Name, dates, r.Equity, trades,
            r.CumulativeReturn, r.AverageDailyReturn, r.Volatility, r.MaxDrawdown, r.Sharpe,
            buyAndHold, r.Equity[^1], closesA);
    }

    private static void CheckCommon(
        DateTime start, DateTime end, decimal capital, decimal fees, decimal stopLoss)
    {
        if (start >= end)
            throw new BacktestException("The start date must be before the end date.");
        if (capital <= 0)
            throw new BacktestException("The initial capital must be positive.");
        if (fees < 0 || fees >= 100)
            throw new BacktestException("The fees must be between 0 and 100 %.");
        if (stopLoss < 0 || stopLoss >= 100)
            throw new BacktestException("The stop-loss must be between 0 and 100 %.");
    }

    private static bool SameSymbol(string x, string y) =>
        string.Equals(x.Trim(), y.Trim(), StringComparison.OrdinalIgnoreCase);

    // Dates of the period (for a pair: only the days present in both series)
    private static (DateTime[] Dates, decimal[] A, decimal[]? B) Filter(
        BacktestRequest req, PriceSeries a, PriceSeries? b)
    {
        var from = req.Start.Date;
        var to = req.End.Date;

        if (b is null)
        {
            var idx = Enumerable.Range(0, a.Dates.Length)
                .Where(i => a.Dates[i] >= from && a.Dates[i] <= to)
                .ToArray();
            return (idx.Select(i => a.Dates[i]).ToArray(), idx.Select(i => a.Closes[i]).ToArray(), null);
        }

        var priceB = new Dictionary<DateTime, decimal>();
        for (var i = 0; i < b.Dates.Length; i++) priceB[b.Dates[i]] = b.Closes[i];

        var dates = new List<DateTime>();
        var closesA = new List<decimal>();
        var closesB = new List<decimal>();
        for (var i = 0; i < a.Dates.Length; i++)
        {
            if (a.Dates[i] < from || a.Dates[i] > to) continue;
            if (!priceB.TryGetValue(a.Dates[i], out var pb)) continue;
            dates.Add(a.Dates[i]);
            closesA.Add(a.Closes[i]);
            closesB.Add(pb);
        }
        return (dates.ToArray(), closesA.ToArray(), closesB.ToArray());
    }

    // Downloads one symbol and turns every technical error into a clear message
    private async Task<PriceSeries> LoadAsync(string symbol)
    {
        symbol = symbol.Trim().ToUpperInvariant();
        if (symbol.Length == 0)
            throw new BacktestException("Enter a symbol (e.g. AAPL).");

        try
        {
            return await market.GetClosesAsync(symbol);
        }
        catch (ArgumentException ex) when (ex.Message.StartsWith("Invalid symbol"))
        {
            throw new BacktestException($"Unknown symbol: {symbol}.");
        }
        catch (HttpRequestException ex)
        {
            throw new BacktestException(ex.StatusCode switch
            {
                System.Net.HttpStatusCode.TooManyRequests => QuotaMessage,
                System.Net.HttpStatusCode.BadRequest or System.Net.HttpStatusCode.NotFound
                    => $"Unknown symbol: {symbol}.",
                _ => "Cannot reach the data service. Check your internet connection."
            });
        }
        catch (TaskCanceledException)
        {
            throw new BacktestException("The data service took too long to answer. Try again.");
        }
        catch (Exception ex) when (ex.Message.StartsWith("API error"))
        {
            throw new BacktestException(Explain(ex.Message, symbol));
        }
        catch (Exception ex) when (ex.Message.StartsWith("Missing API key"))
        {
            throw new BacktestException("The API key is missing. Check the user secrets.");
        }
        catch (Exception ex) when (ex is FormatException or CsvHelper.CsvHelperException)
        {
            throw new BacktestException($"The data received for {symbol} could not be read.");
        }
    }

    private const string QuotaMessage =
        "API quota reached. Wait a minute (or until tomorrow) before running again.";

    // Turns the raw API message into a clear one
    private static string Explain(string apiMessage, string symbol)
    {
        var m = apiMessage.ToLowerInvariant();
        if (m.Contains("429") || m.Contains("credits") || m.Contains("limit"))
            return QuotaMessage;
        if (m.Contains("symbol") || m.Contains("400") || m.Contains("404"))
            return $"Unknown symbol: {symbol}.";
        return "The data service returned an error. Try again later.";
    }
}
