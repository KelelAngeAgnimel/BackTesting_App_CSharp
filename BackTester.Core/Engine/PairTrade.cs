using System;
namespace BackTester.Core.Engine;

public class PairTrade
{
    public int Day;
    public PairSignal Signal;
    public decimal PriceA;
    public decimal PriceB;

    public PairTrade(int day, PairSignal signal, decimal priceA, decimal priceB)
    {
        this.Day = day;
        this.Signal = signal;
        this.PriceA = priceA;
        this.PriceB = priceB;
    }
}
