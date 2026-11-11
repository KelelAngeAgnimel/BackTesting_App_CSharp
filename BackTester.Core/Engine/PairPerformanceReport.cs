using System;
using System.Collections.Generic;
using System.Text;

namespace BackTester.Core.Engine;

public class PairPerformanceReport
{
    public decimal[] EquityCurve { get; set; } = Array.Empty<decimal>();
    public List<PairTrade> Trades { get; set; } = new List<PairTrade>();
    public decimal CumulativeReturn { get; set; }
    public decimal AverageDailyReturn { get; set; }
    public decimal Volatility { get; set; }
    public decimal MaxDrawdown { get; set; }
    public decimal SharpeRatio { get; set; }
}
