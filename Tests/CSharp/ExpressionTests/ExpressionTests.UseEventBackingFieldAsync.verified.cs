using System;
using System.Diagnostics;

public partial class Foo
{
    public event EventHandler<EventArgs> Bar;

    protected void OnBar(EventArgs e)
    {
        if (Bar is null)
        {
            Debug.WriteLine("No subscriber");
        }
        else
        {
            Bar?.Invoke(this, e);
        }
    }
}