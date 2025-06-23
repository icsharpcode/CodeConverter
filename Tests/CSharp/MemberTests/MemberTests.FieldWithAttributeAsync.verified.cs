using System;

internal partial class TestClass
{
    [ThreadStatic]
    private static int First;
}