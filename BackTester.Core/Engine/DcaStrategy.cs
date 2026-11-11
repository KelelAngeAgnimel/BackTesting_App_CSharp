using System;
using System.Collections.Generic;

namespace BackTester.Core.Engine;

// DCA (Dollar-Cost Averaging): the capital is split into equal parts
// and invested at regular intervals, whatever the price is.
// The engine's Backtester can only invest all the cash at once,
// so DCA has its own small simulator that returns the same BacktestResult.
public class DcaStrategy
{
    private readonly int intervalDays;
    private readonly int installments;

    // intervalDays: number of days between two purchases (for example 20)
    // installments: number of equal parts the capital is split into (for example 12)
    public DcaStrategy(int intervalDays, int installments)
    {
        if (intervalDays < 1)
            throw new ArgumentException("Interval must be at least 1 day.");
        if (installments < 1)
            throw new ArgumentException("There must be at least 1 installment.");

        this.intervalDays = intervalDays;
        this.installments = installments;
    }

    public BacktestResult Run(decimal[] prices, decimal initialCapital, decimal feeRate)
    {
        decimal cash = initialCapital;
        decimal shares = 0;
        decimal amountPerInstallment = initialCapital / installments;
        int done = 0;

        decimal[] equityCurve = new decimal[prices.Length];
        List<Trade> trades = new List<Trade>();

        for (int i = 0; i < prices.Length; i++)
        {
            // First purchase on day 1 (like the engine), then every intervalDays
            bool isPurchaseDay = i >= 1 && (i - 1) % intervalDays == 0 && done < installments;

            if (isPurchaseDay && cash > 0)
            {
                decimal amount = Math.Min(amountPerInstallment, cash);
                shares += amount * (1 - feeRate) / prices[i];
                cash -= amount;
                done++;
                trades.Add(new Trade(i, Signal.Buy, prices[i]));
            }

            // Value of the portfolio = cash not invested yet + shares at today's price
            equityCurve[i] = cash + shares * prices[i];
        }

        return new BacktestResult(equityCurve, trades);
    }
}