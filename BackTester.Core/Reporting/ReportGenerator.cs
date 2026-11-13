using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BackTester.Core.Engine;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BackTester.Core.Reporting;

// Information about the run that is not stored in PerformanceReport
public class ReportInfo
{
    public string StrategyName { get; set; } = "";
    public string Symbol { get; set; } = "";
    public decimal InitialCapital { get; set; }
    public decimal FeeRate { get; set; }
    public decimal StopLossRate { get; set; }

    // Weekly timeframe: one point per week, figures are per week and not annualized
    public bool IsWeekly { get; set; }

    // Strategy parameters as name/value pairs, for example ("lookback", "20")
    public List<KeyValuePair<string, string>> Parameters { get; set; } = new();
}

// Builds a PDF report from the result of a backtest (single-stock strategies)
public class ReportGenerator
{
    private const int MaxTradeRows = 500;

    // dates: one date per price, aligned with report.EquityCurve
    // chartImage: optional PNG of the equity curve (can be added later)
    public byte[] Generate(ReportInfo info, PerformanceReport report, DateTime[] dates, byte[]? chartImage = null)
    {
        if (report == null || report.EquityCurve == null || report.EquityCurve.Length == 0)
            throw new ArgumentException("The report has no equity curve.");
        if (dates == null || dates.Length != report.EquityCurve.Length)
            throw new ArgumentException("dates must have the same length as the equity curve.");

        // QuestPDF requires the license type to be chosen (Community is free for students)
        QuestPDF.Settings.License = LicenseType.Community;

        decimal finalValue = report.EquityCurve[^1];

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text("Backtest report").FontSize(22).Bold();
                    col.Item().Text($"{info.StrategyName} on {info.Symbol}").FontSize(13);
                    col.Item().Text($"Generated on {DateTime.Now:yyyy-MM-dd HH:mm}").FontSize(8);
                });

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Spacing(12);

                    // 1. Parameters
                    col.Item().Text("Parameters").FontSize(14).Bold();
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c => { c.RelativeColumn(2); c.RelativeColumn(1); });

                        AddRow(table, "Symbol", info.Symbol);
                        AddRow(table, "Period", $"{dates[0]:yyyy-MM-dd} to {dates[^1]:yyyy-MM-dd} ({dates.Length} {(info.IsWeekly ? "weeks" : "days")})");
                        AddRow(table, "Initial capital", Money(info.InitialCapital));
                        AddRow(table, "Fee rate", Percent(info.FeeRate));
                        AddRow(table, "Stop-loss", info.StopLossRate > 0 ? Percent(info.StopLossRate) : "disabled");
                        foreach (var p in info.Parameters)
                            AddRow(table, p.Key, p.Value);
                    });

                    // 2. Performance metrics
                    col.Item().Text("Performance").FontSize(14).Bold();
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c => { c.RelativeColumn(2); c.RelativeColumn(1); });

                        AddRow(table, "Final portfolio value", Money(finalValue));
                        AddRow(table, "Cumulative return", Percent(Convert.ToDecimal(report.CumulativeReturn)));
                        AddRow(table, info.IsWeekly ? "Average weekly return" : "Average daily return", Percent(Convert.ToDecimal(report.AverageDailyReturn)));
                        AddRow(table, info.IsWeekly ? "Volatility (per week)" : "Volatility", Percent(Convert.ToDecimal(report.Volatility)));
                        AddRow(table, "Maximum drawdown", Percent(Convert.ToDecimal(report.MaxDrawdown)));
                        AddRow(table, info.IsWeekly ? "Sharpe ratio (not annualized)" : "Sharpe ratio", Convert.ToDecimal(report.SharpeRatio).ToString("F2", CultureInfo.InvariantCulture));
                        AddRow(table, "Number of trades", report.Trades.Count.ToString(CultureInfo.InvariantCulture));
                    });

                    // 3. Equity curve (only if an image was given)
                    if (chartImage != null)
                    {
                        col.Item().Text("Portfolio value over time").FontSize(14).Bold();
                        col.Item().Image(chartImage);
                    }

                    // 4. Trades
                    col.Item().Text("Trades").FontSize(14).Bold();
                    if (report.Trades.Count == 0)
                    {
                        col.Item().Text("No trade was executed.");
                    }
                    else
                    {
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(40);
                                c.RelativeColumn(2);
                                c.RelativeColumn(1);
                                c.RelativeColumn(1);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(HeaderCell).Text("#");
                                header.Cell().Element(HeaderCell).Text("Date");
                                header.Cell().Element(HeaderCell).Text("Type");
                                header.Cell().Element(HeaderCell).Text("Price");
                            });

                            int number = 1;
                            foreach (var trade in report.Trades.Take(MaxTradeRows))
                            {
                                string date = trade.Day >= 0 && trade.Day < dates.Length
                                    ? dates[trade.Day].ToString("yyyy-MM-dd")
                                    : "?";

                                table.Cell().Element(BodyCell).Text(number.ToString(CultureInfo.InvariantCulture));
                                table.Cell().Element(BodyCell).Text(date);
                                table.Cell().Element(BodyCell).Text(trade.Signal.ToString());
                                table.Cell().Element(BodyCell).Text(Money(trade.Price));
                                number++;
                            }
                        });

                        if (report.Trades.Count > MaxTradeRows)
                            col.Item().Text($"Only the first {MaxTradeRows} trades are shown.").FontSize(8);
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Page ");
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }

    private static void AddRow(TableDescriptor table, string label, string value)
    {
        table.Cell().Element(BodyCell).Text(label);
        table.Cell().Element(BodyCell).AlignRight().Text(value);
    }

    private static IContainer HeaderCell(IContainer c) =>
        c.Background(Colors.Grey.Lighten3).BorderBottom(1).PaddingVertical(4).PaddingHorizontal(4).DefaultTextStyle(x => x.Bold());

    private static IContainer BodyCell(IContainer c) =>
        c.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(4);

    // Metrics are fractions (0.05 = 5%), so we multiply by 100 for the display
    private static string Percent(decimal fraction) =>
        (fraction * 100m).ToString("F2", CultureInfo.InvariantCulture) + " %";

    private static string Money(decimal value) =>
        value.ToString("N2", CultureInfo.InvariantCulture);
}