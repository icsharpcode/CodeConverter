using System;

public partial class VisualBasicClass
{
    private string SomeDate = "2022-01-01";
    private DateTime? SomeDateDateParsed;

    public VisualBasicClass()
    {
        SomeDateDateParsed = string.IsNullOrEmpty(SomeDate) ? default(DateTime?) : DateTime.Parse(SomeDate);
    }
}