public class PairBacktestResult
{
    public decimal[] EquityCurve;
    public List<PairTrade> Trades;

    public PairBacktestResult(decimal[] equityCurve, List<PairTrade> trades)
    {
        this.EquityCurve = equityCurve;
        this.Trades = trades;
    }
}