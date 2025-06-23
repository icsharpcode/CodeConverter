using System;

public partial class TestClass506
{
    public void Deposit(int Item, bool ColaOnly, bool MonteCarloLogActive, Func<bool> InDevEnv)
    {

        if (ColaOnly) // just log the Cola value
        {
            Console.WriteLine(1);
        }
        else if (Item == 8 | Item == 9) // this is an indexing rate for inflation adjustment
        {
            Console.WriteLine(2);
        }
        else // this for a Roi rate from an assets parameters
        {
            Console.WriteLine(3);
        }
        if (MonteCarloLogActive && InDevEnv()) // Special logging for dev debugging
        {
            Console.WriteLine(4);
            // WriteErrorLog() 'write a blank line
        }
    }
}