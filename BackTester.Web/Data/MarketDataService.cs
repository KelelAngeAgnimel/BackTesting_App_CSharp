using System.Globalization;
using BackTester.Core.Models;
using CsvHelper;
using CsvHelper.Configuration;

namespace BackTester.Web.Data;

public record PriceSeries(DateTime[] Dates, decimal[] Closes);

public class MarketDataService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;

    public MarketDataService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _apiKey = config["TwelveData:ApiKey"]
                  ?? throw new Exception("Missing API key (check the user secrets).");
    }

    // For the engine: dates and closing prices, oldest to newest
    public async Task<PriceSeries> GetClosesAsync(string symbol)
    {
        var prices = await GetPricesAsync(symbol);
        return new PriceSeries(
            prices.Select(p => p.Date).ToArray(),
            prices.Select(p => p.Close).ToArray());
    }

    // Full price history of a symbol, oldest to newest
    public async Task<List<PriceBar>> GetPricesAsync(string symbol)
    {
        symbol = Uri.EscapeDataString(symbol.Trim().ToUpperInvariant());
        var url = $"https://api.twelvedata.com/time_series?symbol={symbol}&interval=1day&outputsize=5000&format=CSV&apikey={_apiKey}";
        var text = await _http.GetStringAsync(url);

        // On errors, the API returns a JSON message starting with "{" instead of prices
        if (text.TrimStart().StartsWith("{"))
            throw new Exception("API error: " + text);

        var config = new CsvConfiguration(CultureInfo.InvariantCulture) { Delimiter = ";" };
        using var reader = new StringReader(text);
        using var csv = new CsvReader(reader, config);

        var prices = new List<PriceBar>();
        csv.Read();
        csv.ReadHeader();
        while (csv.Read())
        {
            prices.Add(new PriceBar
            {
                Date = csv.GetField<DateTime>("datetime"),
                Open = csv.GetField<decimal>("open"),
                High = csv.GetField<decimal>("high"),
                Low = csv.GetField<decimal>("low"),
                Close = csv.GetField<decimal>("close"),
                Volume = csv.GetField<long>("volume")
            });
        }

        // The API sends the newest price first, so we sort from oldest to newest
        return prices.OrderBy(p => p.Date).ToList();
    }
}