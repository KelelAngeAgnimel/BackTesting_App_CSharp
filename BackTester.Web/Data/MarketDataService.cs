using System.Globalization;
using BackTester.Core.Models;
using CsvHelper;
using CsvHelper.Configuration;

namespace BackTester.Web.Data;

public record PriceSeries(DateTime[] Dates, decimal[] Closes);

public class MarketDataService
{
    // A cached file younger than this is used without calling the API
    private static readonly TimeSpan CacheLifetime = TimeSpan.FromHours(24);

    private readonly HttpClient _http;
    private readonly string? _apiKey;
    private readonly string _cacheFolder;

    public MarketDataService(HttpClient http, IConfiguration config, IWebHostEnvironment env)
    {
        _http = http;
        _apiKey = config["TwelveData:ApiKey"]; // can be missing on some machines
        _cacheFolder = Path.Combine(env.ContentRootPath, "DataCache");
        Directory.CreateDirectory(_cacheFolder);
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
        symbol = symbol.Trim().ToUpperInvariant();
        if (symbol.Length == 0 || symbol.Any(c => !char.IsLetterOrDigit(c) && c != '.' && c != '-'))
            throw new ArgumentException("Invalid symbol: " + symbol);

        var text = await GetCsvTextAsync(symbol);
        return ParseCsv(text);
    }

    private async Task<string> GetCsvTextAsync(string symbol)
    {
        var path = Path.Combine(_cacheFolder, symbol + ".csv");
        var cacheExists = File.Exists(path);

        // 1. Fresh file in the cache: no API call
        if (cacheExists && DateTime.UtcNow - File.GetLastWriteTimeUtc(path) < CacheLifetime)
            return await File.ReadAllTextAsync(path);

        // 2. Otherwise download from the API and save it
        try
        {
            var text = await DownloadCsvAsync(symbol);
            await File.WriteAllTextAsync(path, text);
            return text;
        }
        catch when (cacheExists)
        {
            // 3. The download failed: fall back to the old cached file
            return await File.ReadAllTextAsync(path);
        }
    }

    private async Task<string> DownloadCsvAsync(string symbol)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
            throw new Exception("Missing API key (check the user secrets).");

        var url = $"https://api.twelvedata.com/time_series?symbol={Uri.EscapeDataString(symbol)}&interval=1day&outputsize=5000&format=CSV&apikey={_apiKey}";
        var text = await _http.GetStringAsync(url);

        // On errors, the API returns a JSON message starting with "{" instead of prices
        if (text.TrimStart().StartsWith("{"))
            throw new Exception("API error: " + text);

        return text;
    }

    private static List<PriceBar> ParseCsv(string text)
    {
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