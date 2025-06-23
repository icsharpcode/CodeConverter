using System;

public partial class TestForDates
{
    public static void WriteStatus(DateTime? adminDate, DateTime chartingTimeAllowanceEnd)
    {
        if (adminDate is null || adminDate > chartingTimeAllowanceEnd)
        {
            adminDate = DateTime.Now;
        }
    }
}