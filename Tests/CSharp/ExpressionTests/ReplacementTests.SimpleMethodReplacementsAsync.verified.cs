using System;

public partial class TestSimpleMethodReplacements
{
    public void TestMethod()
    {
        string str1;
        string str2;
        object x;
        var dt = default(DateTime);
        x = DateTime.Now;
        x = DateTime.Today;
        x = System.Threading.Thread.CurrentThread.CurrentCulture.Calendar.GetYear(dt);
        x = System.Threading.Thread.CurrentThread.CurrentCulture.Calendar.GetMonth(dt);
        x = System.Threading.Thread.CurrentThread.CurrentCulture.Calendar.GetDayOfMonth(dt);
        x = System.Threading.Thread.CurrentThread.CurrentCulture.Calendar.GetHour(dt);
        x = System.Threading.Thread.CurrentThread.CurrentCulture.Calendar.GetMinute(dt);
        x = System.Threading.Thread.CurrentThread.CurrentCulture.Calendar.GetSecond(dt);
    }
}