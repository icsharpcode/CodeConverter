using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

internal partial class TestClass
{
    private void TestMethod([Optional, DateTimeConstant(599266080000000000L/* #1/1/1900# */)] DateTime pDate)
    {
        var rslt = DateTime.Parse("1900-01-01");
        var rslt2 = DateTime.Parse("2002-08-13 12:14:00");
    }
}