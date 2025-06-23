using System;

public partial class TestClass2
{
    public bool CanDoWork(object Something)
    {
        switch (true)
        {
            case object _ when DateTime.Today.DayOfWeek == DayOfWeek.Saturday | DateTime.Today.DayOfWeek == DayOfWeek.Sunday:
                {
                    // we do not work on weekends
                    return false;
                }
            case object _ when !IsSqlAlive():
                {
                    // Database unavailable
                    return false;
                }
            case object _ when Something is int:
                {
                    // Do something with the Integer
                    return true;
                }

            default:
                {
                    // Do something else
                    return false;
                }
        }
    }

    private bool IsSqlAlive()
    {
        // Do something to test SQL Server
        return true;
    }
}