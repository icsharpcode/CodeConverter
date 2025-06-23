using System;

internal static partial class Module1
{

    public partial class TestClass
    {
        public string Foo { get; private set; }

        public TestClass()
        {
            Foo = "abc";
        }
    }

    public static void Main()
    {
        Test02();
    }

    private static void Test02()
    {
        var t = new TestClass();
        string argvalue = t.Foo;
        Test02Sub(ref argvalue);
    }

    private static void Test02Sub(ref string value)
    {
        Console.WriteLine(value);
    }

}