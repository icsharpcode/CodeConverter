using System;
using System.Collections;

internal partial class Program
{
    public static void Main(string[] args)
    {
        object zs = new[] { 1, 2, 3 };
        foreach (var z in (IEnumerable)zs)
            Console.WriteLine(z);
    }
}