using System;
namespace BackTester.Core.Engine;

public class Trade
{
    public int Day;
    public Signal Signal;
    public decimal Price;

    public Trade(int day, Signal signal, decimal price)
    {
        this.Day = day;
        this.Signal = signal;
        this.Price = price;
    }
}
