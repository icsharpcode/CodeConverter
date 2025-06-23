using System;

public partial class Issue869
{
    public void Main()
    {
        int i = new Func<int>(() => 2)();


        Console.WriteLine(i);
    }
}