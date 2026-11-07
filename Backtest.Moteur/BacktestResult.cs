using System;

public class BacktestResult
{
    public decimal[] EquityCurve;
    public List<Trade> Trades;

    public BacktestResult(decimal[] EquityCurve, List<Trade> trades)
    {
        this.EquityCurve = EquityCurve;
        this.Trades = trades;
    }
}
