using BackTester.Web.Data;

namespace BackTester.Web.Services;

public record BacktestRequest(
    string Strategy, string Symbol, decimal InitialCapital,
    decimal FeesPercent, decimal StopLossPercent,
    DateTime Start, DateTime End, int ShortMa, int LongMa);

public record TradeRow(DateTime Date, string Side, decimal Price);

public record BacktestOutput(
    BacktestRequest Request,
    DateTime[] Dates,
    decimal[] Equity,
    List<TradeRow> Trades,
    decimal CumulativeReturn,
    decimal AverageDailyReturn,
    decimal Volatility,
    decimal MaxDrawdown,
    decimal Sharpe,
    decimal BuyAndHoldReturn);

public class BacktestException(string message) : Exception(message);

// Garde le dernier résultat pour passer de la page Config à la page Results
public class BacktestState
{
    public BacktestOutput? Last { get; set; }
}

public class BacktestService(MarketDataService market)
{
    public async Task<BacktestOutput> RunAsync(BacktestRequest req)
    {
        PriceSeries series;
        try
        {
            series = await market.GetClosesAsync(req.Symbol);
        }
        catch (HttpRequestException)
        {
            throw new BacktestException("Impossible de joindre le service de données. Vérifie ta connexion.");
        }
        catch (Exception ex) when (ex.Message.StartsWith("API error"))
        {
            throw new BacktestException(Explain(ex.Message, req.Symbol));
        }

        // On garde seulement les jours de la période choisie
        var idx = Enumerable.Range(0, series.Dates.Length)
            .Where(i => series.Dates[i] >= req.Start.Date && series.Dates[i] <= req.End.Date)
            .ToArray();

        if (idx.Length < req.LongMa + 5)
            throw new BacktestException(
                $"Pas assez de données sur cette période ({idx.Length} jours). Élargis les dates ou réduis les moyennes mobiles.");

        var dates = idx.Select(i => series.Dates[i]).ToArray();
        var prices = idx.Select(i => series.Closes[i]).ToArray();

        // Moteur de B : les frais et le stop-loss sont des fractions (0,001 = 0,1 %)
        var strategy = new MovingAverageCrossoverStrategy(req.ShortMa, req.LongMa);
        var signals = strategy.GenerateSignals(prices);
        var result = new Backtester().Run(
            prices, signals, req.InitialCapital,
            req.FeesPercent / 100m, req.StopLossPercent / 100m);

        // Un Trade du moteur contient un numéro de jour : on le convertit en date
        var trades = result.Trades
            .Select(t => new TradeRow(dates[t.Day], t.Signal.ToString().ToUpperInvariant(), t.Price))
            .ToList();

        var curve = result.EquityCurve;
        return new BacktestOutput(
            req, dates, curve, trades,
            PerformanceMetrics.CumulativeReturn(curve),
            PerformanceMetrics.AverageDailyReturn(curve),
            PerformanceMetrics.Volatility(curve),
            PerformanceMetrics.MaxDrawdown(curve),
            PerformanceMetrics.SharpeRatio(curve),
            prices[^1] / prices[0] - 1);
    }

    // Transforme le message brut de l'API en message clair
    private static string Explain(string apiMessage, string symbol)
    {
        var m = apiMessage.ToLowerInvariant();
        if (m.Contains("429") || m.Contains("credits") || m.Contains("limit"))
            return "Quota de l'API atteint. Attends une minute (ou demain) avant de relancer.";
        if (m.Contains("symbol") || m.Contains("400") || m.Contains("404"))
            return $"Symbole inconnu : {symbol.ToUpperInvariant()}.";
        return "Erreur de l'API : " + apiMessage;
    }
}