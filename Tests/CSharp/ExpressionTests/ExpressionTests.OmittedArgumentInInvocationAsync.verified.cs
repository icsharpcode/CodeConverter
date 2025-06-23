using System;

public static partial class MyExtensions
{
    public static void NewColumn(Type @type, string strV1 = null, string code = "code", int argInt = 1)
    {
    }

    public static void CallNewColumn()
    {
        NewColumn(typeof(MyExtensions));
        NewColumn(null, code: "otherCode");
        NewColumn(null, "fred");
        NewColumn(null, argInt: 2);
    }
}