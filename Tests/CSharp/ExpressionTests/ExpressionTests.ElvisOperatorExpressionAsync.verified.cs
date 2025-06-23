using System;

internal partial class TestClass3
{
    private partial class Rec
    {
        public Rec Prop { get; private set; } = new Rec();
    }
    private Rec TestMethod(string str)
    {
        int length = (str?.Length) ?? -1;
        Console.WriteLine(length);
        Console.ReadKey();
        return new Rec()?.Prop?.Prop?.Prop;
    }
}