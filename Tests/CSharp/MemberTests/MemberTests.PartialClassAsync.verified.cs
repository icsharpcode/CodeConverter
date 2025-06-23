using System;

public partial class TestClass
{
    partial void DoNothing()
    {
        Console.WriteLine("Hello");
    }
}

public partial class TestClass // VB doesn't require partial here (when just a single class omits it)
{
    partial void DoNothing();
}