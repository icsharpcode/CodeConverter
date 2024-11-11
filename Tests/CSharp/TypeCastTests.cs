using System;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp;

public class TypeCastTests : ConverterTestBase
{
    [Fact]
    public async Task NumericStringToEnumAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Public Class NumericStringToEnum
    Public Shared Sub Main()
        MsgBox(NameOf(Main), ""1"", True)
    End Sub
End Class" + Environment.NewLine, @"using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

public partial class NumericStringToEnum
{
    public static void Main()
    {
        Interaction.MsgBox(nameof(Main), (MsgBoxStyle)Conversions.ToInteger(""1""), true);
    }
}" + Environment.NewLine);
    }

    [Fact]
    public async Task CIntObjectToIntegerAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Class Class1
    Private Sub Test()
        Dim o As Object = 5
        Dim i As Integer = CInt(o)
    End Sub
End Class" + Environment.NewLine, @"using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal partial class Class1
{
    private void Test()
    {
        object o = 5;
        int i = Conversions.ToInteger(o);
    }
}" + Environment.NewLine);
    }

    [Fact]
    public async Task CDateAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Public Class Class1
    Sub Foo()
        Dim x = CDate(""2019-09-04"")
    End Sub
End Class", @"using System;
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

public partial class Class1
{
    public void Foo()
    {
        DateTime x = Conversions.ToDate(""2019-09-04"");
    }
}");
    }

    [Fact]
    public async Task CastObjectToStringAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Class Class1
    Private Sub Test()
        Dim o As Object = ""Test""
        Dim s As String = CStr(o)
    End Sub
End Class" + Environment.NewLine, @"using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal partial class Class1
{
    private void Test()
    {
        object o = ""Test"";
        string s = Conversions.ToString(o);
    }
}" + Environment.NewLine);
    }

    [Fact]
    public async Task ImplicitCastObjectToStringAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Class Class1
    Private Sub Test()
        Dim o As Object = ""Test""
        Dim s As String = o
    End Sub
End Class" + Environment.NewLine, @"using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal partial class Class1
{
    private void Test()
    {
        object o = ""Test"";
        string s = Conversions.ToString(o);
    }
}" + Environment.NewLine);
    }

    [Fact]
    public async Task CastArrayListAssignmentToStringAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Class Class1
    Private Sub Test()
        Dim x As New ArrayList
        x.Add(""a"")

        Dim xs(1) As String

        xs(0) = x(0)
    End Sub
End Class" + Environment.NewLine, @"using System.Collections;
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal partial class Class1
{
    private void Test()
    {
        var x = new ArrayList();
        x.Add(""a"");

        var xs = new string[2];

        xs[0] = Conversions.ToString(x[0]);
    }
}" + Environment.NewLine);
    }

    [Fact]
    public async Task ImplicitCastObjecStringToStringAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Class Class1
    Private Sub Test()
        Dim o As Object = ""Test""
        Dim s As String = o
    End Sub
End Class" + Environment.NewLine, @"using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal partial class Class1
{
    private void Test()
    {
        object o = ""Test"";
        string s = Conversions.ToString(o);
    }
}" + Environment.NewLine);
    }

    [Fact]
    public async Task ExplicitOperatorInvocation_Issue678Async()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Imports System.Drawing

Public Class AShape
    Private PaneArea As RectangleF
    Private _OuterGap As Integer
    Public Sub SetSize(ByVal clientRectangle As Rectangle)
        Dim area = RectangleF.op_Implicit(clientRectangle)
        area.Inflate(-Me._OuterGap, -Me._OuterGap)
        Me.PaneArea = area
    End Sub
End Class", @"using System.Drawing;

public partial class AShape
{
    private RectangleF PaneArea;
    private int _OuterGap;
    public void SetSize(Rectangle clientRectangle)
    {
        var area = (RectangleF)clientRectangle;
        area.Inflate(-_OuterGap, -_OuterGap);
        PaneArea = area;
    }
}");
    }

    [Fact]
    public async Task CTypeFractionalAndBooleanToIntegralsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"
Enum TestEnum
    None = 1
End Enum

Class Class1
    Private Sub Test(b as Boolean, f as Single, d as Double, m as Decimal)
        Dim i = CType(b, Integer)
        i = CType(f, Integer)
        i = CType(d, Integer)
        i = CType(m, Integer)

        Dim ui = CType(b, UInteger)
        ui = CType(f, UInteger)
        ui = CType(d, UInteger)
        ui = CType(m, UInteger)

        Dim s = CType(b, Short)
        s = CType(f, Short)
        s = CType(d, Short)
        s = CType(m, Short)

        Dim l = CType(b, Long)
        l = CType(f, Long)
        l = CType(d, Long)
        l = CType(m, Long)

        Dim byt = CType(b, Byte)
        byt = CType(f, Byte)
        byt = CType(d, Byte)
        byt = CType(m, Byte)

        Dim e = CType(b, TestEnum)
        e = CType(f, TestEnum)
        e = CType(d, TestEnum)
        e = CType(m, TestEnum)
    End Sub

    Private Sub TestNullable(b as Boolean?, f as Single?, d as Double?, m as Decimal?)
        Dim i = CType(b, Integer)
        i = CType(f, Integer)
        i = CType(d, Integer)
        i = CType(m, Integer)

        Dim ui = CType(b, UInteger)
        ui = CType(f, UInteger)
        ui = CType(d, UInteger)
        ui = CType(m, UInteger)

        Dim s = CType(b, Short)
        s = CType(f, Short)
        s = CType(d, Short)
        s = CType(m, Short)

        Dim l = CType(b, Long)
        l = CType(f, Long)
        l = CType(d, Long)
        l = CType(m, Long)

        Dim byt = CType(b, Byte)
        byt = CType(f, Byte)
        byt = CType(d, Byte)
        byt = CType(m, Byte)

        Dim e = CType(b, TestEnum)
        e = CType(f, TestEnum)
        e = CType(d, TestEnum)
        e = CType(m, TestEnum)
    End Sub
End Class",
            @"using System;
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal enum TestEnum
{
    None = 1
}

internal partial class Class1
{
    private void Test(bool b, float f, double d, decimal m)
    {
        int i = Conversions.ToInteger(b);
        i = (int)Math.Round(f);
        i = (int)Math.Round(d);
        i = (int)Math.Round(m);

        uint ui = Conversions.ToUInteger(b);
        ui = (uint)Math.Round(f);
        ui = (uint)Math.Round(d);
        ui = (uint)Math.Round(m);

        short s = Conversions.ToShort(b);
        s = (short)Math.Round(f);
        s = (short)Math.Round(d);
        s = (short)Math.Round(m);

        long l = Conversions.ToLong(b);
        l = (long)Math.Round(f);
        l = (long)Math.Round(d);
        l = (long)Math.Round(m);

        byte byt = Conversions.ToByte(b);
        byt = (byte)Math.Round(f);
        byt = (byte)Math.Round(d);
        byt = (byte)Math.Round(m);

        TestEnum e = (TestEnum)Conversions.ToInteger(b);
        e = (TestEnum)Math.Round(f);
        e = (TestEnum)Math.Round(d);
        e = (TestEnum)Math.Round(m);
    }

    private void TestNullable(bool? b, float? f, double? d, decimal? m)
    {
        int i = Conversions.ToInteger(b.Value);
        i = (int)Math.Round(f.Value);
        i = (int)Math.Round(d.Value);
        i = (int)Math.Round(m.Value);

        uint ui = Conversions.ToUInteger(b.Value);
        ui = (uint)Math.Round(f.Value);
        ui = (uint)Math.Round(d.Value);
        ui = (uint)Math.Round(m.Value);

        short s = Conversions.ToShort(b.Value);
        s = (short)Math.Round(f.Value);
        s = (short)Math.Round(d.Value);
        s = (short)Math.Round(m.Value);

        long l = Conversions.ToLong(b.Value);
        l = (long)Math.Round(f.Value);
        l = (long)Math.Round(d.Value);
        l = (long)Math.Round(m.Value);

        byte byt = Conversions.ToByte(b.Value);
        byt = (byte)Math.Round(f.Value);
        byt = (byte)Math.Round(d.Value);
        byt = (byte)Math.Round(m.Value);

        TestEnum e = (TestEnum)Conversions.ToInteger(b.Value);
        e = (TestEnum)Math.Round(f.Value);
        e = (TestEnum)Math.Round(d.Value);
        e = (TestEnum)Math.Round(m.Value);
    }
}
");
    }

    [Fact]
    public async Task CTypeFractionalAndBooleanToNullableIntegralsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"
Enum TestEnum
    None = 1
End Enum

Class Class1
    Private Sub Test(b as Boolean, f as Single, d as Double, m as Decimal)
        Dim i = CType(b, Integer?)
        i = CType(f, Integer?)
        i = CType(d, Integer?)
        i = CType(m, Integer?)

        Dim ui = CType(b, UInteger?)
        ui = CType(f, UInteger?)
        ui = CType(d, UInteger?)
        ui = CType(m, UInteger?)

        Dim s = CType(b, Short?)
        s = CType(f, Short?)
        s = CType(d, Short?)
        s = CType(m, Short?)

        Dim l = CType(b, Long?)
        l = CType(f, Long?)
        l = CType(d, Long?)
        l = CType(m, Long?)

        Dim byt = CType(b, Byte?)
        byt = CType(f, Byte?)
        byt = CType(d, Byte?)
        byt = CType(m, Byte?)

        Dim e = CType(b, TestEnum?)
        e = CType(f, TestEnum?)
        e = CType(d, TestEnum?)
        e = CType(m, TestEnum?)
    End Sub

    Private Sub TestNullable(b as Boolean?, f as Single?, d as Double?, m as Decimal?)
        Dim i = CType(b, Integer?)
        i = CType(f, Integer?)
        i = CType(d, Integer?)
        i = CType(m, Integer?)

        Dim ui = CType(b, UInteger?)
        ui = CType(f, UInteger?)
        ui = CType(d, UInteger?)
        ui = CType(m, UInteger?)

        Dim s = CType(b, Short?)
        s = CType(f, Short?)
        s = CType(d, Short?)
        s = CType(m, Short?)

        Dim l = CType(b, Long?)
        l = CType(f, Long?)
        l = CType(d, Long?)
        l = CType(m, Long?)

        Dim byt = CType(b, Byte?)
        byt = CType(f, Byte?)
        byt = CType(d, Byte?)
        byt = CType(m, Byte?)

        Dim e = CType(b, TestEnum?)
        e = CType(f, TestEnum?)
        e = CType(d, TestEnum?)
        e = CType(m, TestEnum?)
    End Sub
End Class",
            @"using System;
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal enum TestEnum
{
    None = 1
}

internal partial class Class1
{
    private void Test(bool b, float f, double d, decimal m)
    {
        int? i = Conversions.ToInteger(b);
        i = (int?)Math.Round(f);
        i = (int?)Math.Round(d);
        i = (int?)Math.Round(m);

        uint? ui = Conversions.ToUInteger(b);
        ui = (uint?)Math.Round(f);
        ui = (uint?)Math.Round(d);
        ui = (uint?)Math.Round(m);

        short? s = Conversions.ToShort(b);
        s = (short?)Math.Round(f);
        s = (short?)Math.Round(d);
        s = (short?)Math.Round(m);

        long? l = Conversions.ToLong(b);
        l = (long?)Math.Round(f);
        l = (long?)Math.Round(d);
        l = (long?)Math.Round(m);

        byte? byt = Conversions.ToByte(b);
        byt = (byte?)Math.Round(f);
        byt = (byte?)Math.Round(d);
        byt = (byte?)Math.Round(m);

        TestEnum? e = (TestEnum?)Conversions.ToInteger(b);
        e = (TestEnum?)Math.Round(f);
        e = (TestEnum?)Math.Round(d);
        e = (TestEnum?)Math.Round(m);
    }

    private void TestNullable(bool? b, float? f, double? d, decimal? m)
    {
        int? i = b.HasValue ? Conversions.ToInteger(b.Value) : null;
        i = f.HasValue ? (int?)Math.Round(f.Value) : null;
        i = d.HasValue ? (int?)Math.Round(d.Value) : null;
        i = m.HasValue ? (int?)Math.Round(m.Value) : null;

        uint? ui = b.HasValue ? Conversions.ToUInteger(b.Value) : null;
        ui = f.HasValue ? (uint?)Math.Round(f.Value) : null;
        ui = d.HasValue ? (uint?)Math.Round(d.Value) : null;
        ui = m.HasValue ? (uint?)Math.Round(m.Value) : null;

        short? s = b.HasValue ? Conversions.ToShort(b.Value) : null;
        s = f.HasValue ? (short?)Math.Round(f.Value) : null;
        s = d.HasValue ? (short?)Math.Round(d.Value) : null;
        s = m.HasValue ? (short?)Math.Round(m.Value) : null;

        long? l = b.HasValue ? Conversions.ToLong(b.Value) : null;
        l = f.HasValue ? (long?)Math.Round(f.Value) : null;
        l = d.HasValue ? (long?)Math.Round(d.Value) : null;
        l = m.HasValue ? (long?)Math.Round(m.Value) : null;

        byte? byt = b.HasValue ? Conversions.ToByte(b.Value) : null;
        byt = f.HasValue ? (byte?)Math.Round(f.Value) : null;
        byt = d.HasValue ? (byte?)Math.Round(d.Value) : null;
        byt = m.HasValue ? (byte?)Math.Round(m.Value) : null;

        TestEnum? e = b.HasValue ? (TestEnum?)Conversions.ToInteger(b.Value) : null;
        e = f.HasValue ? (TestEnum?)Math.Round(f.Value) : null;
        e = d.HasValue ? (TestEnum?)Math.Round(d.Value) : null;
        e = m.HasValue ? (TestEnum?)Math.Round(m.Value) : null;
    }
}
");
    }
    
    [Fact]
    public async Task CastObjectToGenericListAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Class Class1
    Private Sub Test()
        Dim o As Object = New System.Collections.Generic.List(Of Integer)()
        Dim l As System.Collections.Generic.List(Of Integer) = CType(o, System.Collections.Generic.List(Of Integer))
    End Sub
End Class",
            @"using System.Collections.Generic;

internal partial class Class1
{
    private void Test()
    {
        object o = new List<int>();
        List<int> l = (List<int>)o;
    }
}");
    }

    [Fact]
    public async Task CastingToEnumRightSideShouldBeForcedToBeIntegralAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"
Public Class C
    Public Enum OrderStatus
        Pending = 0
        Fullfilled = 1
    End Enum
 
    Sub Test1()
        Dim val As Object = ""1""
        Dim os1 = CType(val, OrderStatus)
        Dim os2 As OrderStatus = val

        Dim null1 = CType(val, OrderStatus?)
        Dim null2 As OrderStatus? = val
    End Sub
    Sub Test2()
        Dim val As String = ""1""
        Dim os1 = CType(val, OrderStatus)
        Dim os2 As OrderStatus = val

        Dim null1 = CType(val, OrderStatus?)
        Dim null2 As OrderStatus? = val
    End Sub
    Sub Test3()
        Dim val As Object = 1
        Dim os1 = CType(val, OrderStatus)
        Dim os2 As OrderStatus = val

        Dim null1 = CType(val, OrderStatus?)
        Dim null2 As OrderStatus? = val
    End Sub
    Sub Test4()
        Dim val = CType(1.5D, Object)
        Dim os1 = CType(val, OrderStatus)
        Dim os2 As OrderStatus = val

        Dim null1 = CType(val, OrderStatus?)
        Dim null2 As OrderStatus? = val
    End Sub
End Class",
            @"using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

public partial class C
{
    public enum OrderStatus
    {
        Pending = 0,
        Fullfilled = 1
    }

    public void Test1()
    {
        object val = ""1"";
        OrderStatus os1 = (OrderStatus)Conversions.ToInteger(val);
        OrderStatus os2 = (OrderStatus)Conversions.ToInteger(val);

        OrderStatus? null1 = (OrderStatus?)val;
        OrderStatus? null2 = (OrderStatus?)val;
    }
    public void Test2()
    {
        string val = ""1"";
        OrderStatus os1 = (OrderStatus)Conversions.ToInteger(val);
        OrderStatus os2 = (OrderStatus)Conversions.ToInteger(val);

        OrderStatus? null1 = (OrderStatus?)Conversions.ToInteger(val);
        OrderStatus? null2 = (OrderStatus?)Conversions.ToInteger(val);
    }
    public void Test3()
    {
        object val = 1;
        OrderStatus os1 = (OrderStatus)Conversions.ToInteger(val);
        OrderStatus os2 = (OrderStatus)Conversions.ToInteger(val);

        OrderStatus? null1 = (OrderStatus?)val;
        OrderStatus? null2 = (OrderStatus?)val;
    }
    public void Test4()
    {
        object val = 1.5m;
        OrderStatus os1 = (OrderStatus)Conversions.ToInteger(val);
        OrderStatus os2 = (OrderStatus)Conversions.ToInteger(val);

        OrderStatus? null1 = (OrderStatus?)val;
        OrderStatus? null2 = (OrderStatus?)val;
    }
}");
    }

    [Fact]
    public async Task CTypeObjectToIntegerAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Class Class1
    Private Sub Test()
        Dim o As Object = 5
        Dim i As System.Nullable(Of Integer) = CInt(o)
        Dim s As String = CType(o, Integer).ToString()
    End Sub
End Class",
            @"using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal partial class Class1
{
    private void Test()
    {
        object o = 5;
        int? i = Conversions.ToInteger(o);
        string s = Conversions.ToInteger(o).ToString();
    }
}");
    }

    [Fact]
    public async Task CastingStringToEnumShouldUseConversionsToIntegerAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"
Enum TestEnum
    None = 0
End Enum

Class Class1
    Sub TestEnumCast(str as String)
        Dim enm  As TestEnum = str
    End Sub
End Class",
            @"using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal enum TestEnum
{
    None = 0
}

internal partial class Class1
{
    public void TestEnumCast(string str)
    {
        TestEnum enm = (TestEnum)Conversions.ToInteger(str);
    }
}
");
    }

    [Fact]
    public async Task CastingIntegralTypeToEnumShouldUseExplicitCastAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"
Enum TestEnum
    None = 0
End Enum
Enum TestEnum2
    None = 1
End Enum
Class Class1
    Private Sub TestIntegrals(b as Byte, s as Short, i as Integer, l as Long, e as TestEnum2)
        Dim res = CType(b, TestEnum)
        res = CType(s, TestEnum)
        res = CType(i, TestEnum)
        res = CType(l, TestEnum)
        res = CType(e, TestEnum)
    End Sub

    Private Sub TestNullableIntegrals(b as Byte?, s as Short?, i as Integer?, l as Long?, e as TestEnum2?)
        Dim res = CType(b, TestEnum)
        res = CType(s, TestEnum)
        res = CType(i, TestEnum)
        res = CType(l, TestEnum)
        res = CType(e, TestEnum)
    End Sub

    Private Sub TestUnsignedIntegrals(b as SByte, s as UShort, i as UInteger, l as ULong)
        Dim res = CType(b, TestEnum)
        res = CType(s, TestEnum)
        res = CType(i, TestEnum)
        res = CType(l, TestEnum)
    End Sub

    Private Sub TestNullableUnsignedIntegrals(b as SByte?, s as UShort?, i as UInteger?, l as ULong?)
        Dim res = CType(b, TestEnum)
        res = CType(s, TestEnum)
        res = CType(i, TestEnum)
        res = CType(l, TestEnum)
    End Sub
End Class",
                @"
internal enum TestEnum
{
    None = 0
}

internal enum TestEnum2
{
    None = 1
}

internal partial class Class1
{
    private void TestIntegrals(byte b, short s, int i, long l, TestEnum2 e)
    {
        TestEnum res = (TestEnum)b;
        res = (TestEnum)s;
        res = (TestEnum)i;
        res = (TestEnum)l;
        res = (TestEnum)e;
    }

    private void TestNullableIntegrals(byte? b, short? s, int? i, long? l, TestEnum2? e)
    {
        TestEnum res = (TestEnum)b;
        res = (TestEnum)s;
        res = (TestEnum)i;
        res = (TestEnum)l;
        res = (TestEnum)e;
    }

    private void TestUnsignedIntegrals(sbyte b, ushort s, uint i, ulong l)
    {
        TestEnum res = (TestEnum)b;
        res = (TestEnum)s;
        res = (TestEnum)i;
        res = (TestEnum)l;
    }

    private void TestNullableUnsignedIntegrals(sbyte? b, ushort? s, uint? i, ulong? l)
    {
        TestEnum res = (TestEnum)b;
        res = (TestEnum)s;
        res = (TestEnum)i;
        res = (TestEnum)l;
    }
}
");
    }

    [Fact]
    public async Task CastingIntegralTypeToNullableEnumShouldUseExplicitCastAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"
Enum TestEnum
    None = 0
End Enum
Enum TestEnum2
    None = 1
End Enum
Class Class1
    Private Sub TestIntegrals(b as Byte, s as Short, i as Integer, l as Long, e as TestEnum2)
        Dim res = CType(b, TestEnum?)
        res = CType(s, TestEnum?)
        res = CType(i, TestEnum?)
        res = CType(l, TestEnum?)
        res = CType(e, TestEnum?)
    End Sub

    Private Sub TestNullableIntegrals(b as Byte?, s as Short?, i as Integer?, l as Long?, e as TestEnum2?)
        Dim res = CType(b, TestEnum?)
        res = CType(s, TestEnum?)
        res = CType(i, TestEnum?)
        res = CType(l, TestEnum?)
        res = CType(e, TestEnum?)
    End Sub

    Private Sub TestUnsignedIntegrals(b as SByte, s as UShort, i as UInteger, l as ULong)
        Dim res = CType(b, TestEnum?)
        res = CType(s, TestEnum?)
        res = CType(i, TestEnum?)
        res = CType(l, TestEnum?)
    End Sub

    Private Sub TestNullableUnsignedIntegrals(b as SByte?, s as UShort?, i as UInteger?, l as ULong?)
        Dim res = CType(b, TestEnum?)
        res = CType(s, TestEnum?)
        res = CType(i, TestEnum?)
        res = CType(l, TestEnum?)
    End Sub
End Class",
                @"
internal enum TestEnum
{
    None = 0
}

internal enum TestEnum2
{
    None = 1
}

internal partial class Class1
{
    private void TestIntegrals(byte b, short s, int i, long l, TestEnum2 e)
    {
        TestEnum? res = (TestEnum?)b;
        res = (TestEnum?)s;
        res = (TestEnum?)i;
        res = (TestEnum?)l;
        res = (TestEnum?)e;
    }

    private void TestNullableIntegrals(byte? b, short? s, int? i, long? l, TestEnum2? e)
    {
        TestEnum? res = (TestEnum?)b;
        res = (TestEnum?)s;
        res = (TestEnum?)i;
        res = (TestEnum?)l;
        res = (TestEnum?)e;
    }

    private void TestUnsignedIntegrals(sbyte b, ushort s, uint i, ulong l)
    {
        TestEnum? res = (TestEnum?)b;
        res = (TestEnum?)s;
        res = (TestEnum?)i;
        res = (TestEnum?)l;
    }

    private void TestNullableUnsignedIntegrals(sbyte? b, ushort? s, uint? i, ulong? l)
    {
        TestEnum? res = (TestEnum?)b;
        res = (TestEnum?)s;
        res = (TestEnum?)i;
        res = (TestEnum?)l;
    }
}
");
    }

    [Fact]
    public async Task TryCastObjectToGenericListAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Class Class1
    Private Sub Test()
        Dim o As Object = New System.Collections.Generic.List(Of Integer)()
        Dim l As System.Collections.Generic.List(Of Integer) = TryCast(o, System.Collections.Generic.List(Of Integer))
    End Sub
End Class",
            @"using System.Collections.Generic;

internal partial class Class1
{
    private void Test()
    {
        object o = new List<int>();
        List<int> l = o as List<int>;
    }
}");
    }

    [Fact]
    public async Task RetainNullableBoolWhenNeededAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Class Class1
    Function F(a As Net.IPAddress) As Boolean
        Return If(a?.ScopeId = 0, True)
End Function
End Class",
            @"
internal partial class Class1
{
    public bool F(System.Net.IPAddress a)
    {
        return ((a?.ScopeId) is { } arg1 ? arg1 == 0 : (bool?)null) ?? true;
    }
}");
    }

    [Fact]
    public async Task TestNullableBoolConversionsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Class Class1
    Private Function Test1(a as Boolean?) As Boolean
        Return a
    End Function
    Private Function Test2(a as Boolean?) As Boolean?
        Return a
    End Function
    Private Function Test3(a as Boolean) As Boolean?
        Return a
    End Function

    Private Function Test4(a as Integer?) As Boolean
        Return a
    End Function
    Private Function Test5(a as Integer?) As Boolean?
        Return a
    End Function
    Private Function Test6(a as Integer) As Boolean?
        Return a
    End Function

    Private Function Test4(a as Boolean?) As Integer
        Return a
    End Function
    Private Function Test5(a as Boolean?) As Integer?
        Return a
    End Function
    Private Function Test6(a as Boolean) As Integer?
        Return a
    End Function

    Private Function Test7(a as Boolean?) As String
        Return a
    End Function
    Private Function Test8(a as Boolean?) As String
        Return a
    End Function

    Private Function Test9(a as String) As Boolean?
        Return a
    End Function
    Private Function Test10(a as String) As Boolean
        Return a
    End Function
End Class",
            @"using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal partial class Class1
{
    private bool Test1(bool? a)
    {
        return (bool)a;
    }
    private bool? Test2(bool? a)
    {
        return a;
    }
    private bool? Test3(bool a)
    {
        return a;
    }

    private bool Test4(int? a)
    {
        return Conversions.ToBoolean(a.Value);
    }
    private bool? Test5(int? a)
    {
        return a.HasValue ? Conversions.ToBoolean(a.Value) : null;
    }
    private bool? Test6(int a)
    {
        return Conversions.ToBoolean(a);
    }

    private int Test4(bool? a)
    {
        return Conversions.ToInteger(a.Value);
    }
    private int? Test5(bool? a)
    {
        return a.HasValue ? Conversions.ToInteger(a.Value) : null;
    }
    private int? Test6(bool a)
    {
        return Conversions.ToInteger(a);
    }

    private string Test7(bool? a)
    {
        return Conversions.ToString(a.Value);
    }
    private string Test8(bool? a)
    {
        return Conversions.ToString(a.Value);
    }

    private bool? Test9(string a)
    {
        return Conversions.ToBoolean(a);
    }
    private bool Test10(string a)
    {
        return Conversions.ToBoolean(a);
    }
}");
    }

    [Fact]
    public async Task TestNullableEnumConversionsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"
Enum TestEnum
    None = 1
End Enum
Class Class1
    Private Function Test1(a as Integer) As TestEnum?
        Return a
    End Function
    Private Function Test2(a as Integer?) As TestEnum?
        Return a
    End Function
    Private Function Test3(a as Integer?) As TestEnum
        Return a
    End Function

    Private Function Test4(a as TestEnum) As Integer?
        Return a
    End Function
    Private Function Test5(a as TestEnum?) As Integer?
        Return a
    End Function
    Private Function Test6(a as TestEnum?) As TestEnum?
        Return a
    End Function
    Private Function Test7(a as TestEnum?) As Integer
        Return a
    End Function

    Private Function Test8(a as TestEnum?) As String
        Return a
    End Function
    Private Function Test9(a as TestEnum?) As String
        Return a
    End Function
    
    Private Function Test10(a as String) As TestEnum?
        Return a
    End Function
    Private Function Test11(a as String) As TestEnum
        Return a
    End Function
End Class",
            @"using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal enum TestEnum
{
    None = 1
}

internal partial class Class1
{
    private TestEnum? Test1(int a)
    {
        return (TestEnum?)a;
    }
    private TestEnum? Test2(int? a)
    {
        return (TestEnum?)a;
    }
    private TestEnum Test3(int? a)
    {
        return (TestEnum)a;
    }

    private int? Test4(TestEnum a)
    {
        return Conversions.ToInteger(a);
    }
    private int? Test5(TestEnum? a)
    {
        return (int?)a;
    }
    private TestEnum? Test6(TestEnum? a)
    {
        return a;
    }
    private int Test7(TestEnum? a)
    {
        return (int)a;
    }

    private string Test8(TestEnum? a)
    {
        return Conversions.ToString(a.Value);
    }
    private string Test9(TestEnum? a)
    {
        return Conversions.ToString(a.Value);
    }

    private TestEnum? Test10(string a)
    {
        return (TestEnum?)Conversions.ToInteger(a);
    }
    private TestEnum Test11(string a)
    {
        return (TestEnum)Conversions.ToInteger(a);
    }
}");
    }

    [Fact]
    public async Task TestNumbersNullableConversionsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"
Class Class1
    Private Function Test1(a as Integer) As Integer?
        Return a
    End Function
    Private Function Test2(a as Integer?) As Integer?
        Return a
    End Function
    Private Function Test3(a as Integer?) As Integer
        Return a
    End Function
    Private Function Test4(a as Single) As Integer?
        Return a
    End Function
    Private Function Test5(a as Single?) As Integer?
        Return a
    End Function

    Private Function Test6(a as Single) As Single?
        Return a
    End Function
    Private Function Test7(a as Single?) As Single?
        Return a
    End Function
    Private Function Test8(a as Single?) As Single
        Return a
    End Function
    Private Function Test9(a as Integer) As Single?
        Return a
    End Function
    Private Function Test10(a as Integer?) As Single?
        Return a
    End Function

   Private Function Test11(a as Integer?) As String
        Return a
    End Function
    Private Function Test12(a as Integer?) As String
        Return a
    End Function

    Private Function Test13(a as String) As Integer?
        Return a
    End Function
    Private Function Test14(a as String) As Integer
        Return a
    End Function
End Class",
            @"using System;
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal partial class Class1
{
    private int? Test1(int a)
    {
        return a;
    }
    private int? Test2(int? a)
    {
        return a;
    }
    private int Test3(int? a)
    {
        return (int)a;
    }
    private int? Test4(float a)
    {
        return (int?)Math.Round(a);
    }
    private int? Test5(float? a)
    {
        return a.HasValue ? (int?)Math.Round(a.Value) : null;
    }

    private float? Test6(float a)
    {
        return a;
    }
    private float? Test7(float? a)
    {
        return a;
    }
    private float Test8(float? a)
    {
        return (float)a;
    }
    private float? Test9(int a)
    {
        return a;
    }
    private float? Test10(int? a)
    {
        return a;
    }

    private string Test11(int? a)
    {
        return Conversions.ToString(a.Value);
    }
    private string Test12(int? a)
    {
        return Conversions.ToString(a.Value);
    }

    private int? Test13(string a)
    {
        return Conversions.ToInteger(a);
    }
    private int Test14(string a)
    {
        return Conversions.ToInteger(a);
    }
}");
    }

    [Fact]
    public async Task CastConstantNumberToLongAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Class Class1
    Private Sub Test()
        Dim o As Object = 5L
    End Sub
End Class",
            @"
internal partial class Class1
{
    private void Test()
    {
        object o = 5L;
    }
}");
    }

    [Fact]
    public async Task CastConstantNumberToFloatAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Class Class1
    Private Sub Test()
        Dim o As Object = 5F
    End Sub
End Class",
            @"
internal partial class Class1
{
    private void Test()
    {
        object o = 5f;
    }
}");
    }

    [Fact]
    public async Task CastConstantNumberToDecimalAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Class Class1
    Private Sub Test()
        Dim o As Object = 5.0D
    End Sub
End Class" + Environment.NewLine, @"
internal partial class Class1
{
    private void Test()
    {
        object o = 5.0m;
    }
}" + Environment.NewLine);
    }

    [Fact]
    public async Task CastConstantNumberToCharacterWAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Private Sub Test()
    Dim CR = ChrW(&HF)
End Sub
", @"private void Test()
{
    char CR = '\u000f';
}
");
    }

    [Fact]
    public async Task CastConstantNumberToCharacterAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Private Sub Test()
    Dim CR As Char = Chr(&HF)
End Sub
", @"private void Test()
{
    char CR = '\u000f';
}
");
    }

    
    [Fact]
    public async Task CastObjectToNullableAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Class CastTest
    Private Function Test(input as Object) As Integer?
            Return CType(input, Integer?)
    End Function
    Private Function Test2(input as Object) As Decimal?
        Return CType(input, Nullable(Of Double))
    End Function
    Private Function Test2(input as Integer) As Decimal?
        Return CType(input, Nullable(Of Double))
    End Function
End Class", @"
internal partial class CastTest
{
    private int? Test(object input)
    {
        return (int?)input;
    }
    private decimal? Test2(object input)
    {
        return (decimal?)(double?)input;
    }
    private decimal? Test2(int input)
    {
        return (decimal?)(double?)input;
    }
}");
    }


    [Fact]
    public async Task TestSingleCharacterStringLiteralBecomesCharWhenNeededAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Class CharTestClass
    Private Function QuoteSplit(ByVal text As String) As String()
        Return text.Split("""""""")
    End Function
End Class", @"
internal partial class CharTestClass
{
    private string[] QuoteSplit(string text)
    {
        return text.Split('""');
    }
}");
    }


    [Fact]
    public async Task TestSelectCaseComparesCharsAndStringsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"
Class CharTestClass
    Private Sub Q()
        Select Case ""a""
            Case ""x""c To ""y""c
            Case ""b""c
        End Select
    End Sub
End Class", @"
internal partial class CharTestClass
{
    private void Q()
    {
        switch (""a"")
        {
            case var @case when ""x"" <= @case && @case <= ""y"":
                {
                    break;
                }

            case ""b"":
                {
                    break;
                }
        }
    }
}
1 target compilation errors:
CS0825: The contextual keyword 'var' may only appear within a local variable declaration or in script code");
    }

    [Fact]
    public async Task TestSingleCharacterStringLiteralBecomesChar_WhenExplictCastAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Class ExplicitCastClass
    Dim wordArray As String() = 1.ToString().Split(CChar("",""))
End Class", @"
internal partial class ExplicitCastClass
{
    private string[] wordArray = 1.ToString().Split(',');
}");
    }

    [Fact]
    public async Task TestCastHasBracketsWhenElementAccessAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Class TestCastHasBracketsWhenElementAccess
    Private Function Casting(ByVal sender As Object) As Integer
        Return CInt(DirectCast(sender, Object())(0))
    End Function
End Class", @"using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal partial class TestCastHasBracketsWhenElementAccess
{
    private int Casting(object sender)
    {
        return Conversions.ToInteger(((object[])sender)[0]);
    }
}");
    }

    [Fact]
    public async Task MultipleNestedCastsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Public Class MultipleCasts
    Public Shared Function ToGenericParameter(Of T)(Value As Object) As T
        If Value Is Nothing Then
            Return Nothing
        End If
        Dim reflectedType As Global.System.Type = GetType(T)
        If Global.System.Type.Equals(reflectedType, GetType(Global.System.Int16)) Then
            Return DirectCast(CObj(CShort(Value)), T)
        ElseIf Global.System.Type.Equals(reflectedType, GetType(Global.System.UInt64)) Then
            Return DirectCast(CObj(CULng(Value)), T)
        Else
            Return DirectCast(Value, T)
        End If
    End Function
End Class", @"using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

public partial class MultipleCasts
{
    public static T ToGenericParameter<T>(object Value)
    {
        if (Value is null)
        {
            return default;
        }
        var reflectedType = typeof(T);
        if (Equals(reflectedType, typeof(short)))
        {
            return (T)(object)Conversions.ToShort(Value);
        }
        else if (Equals(reflectedType, typeof(ulong)))
        {
            return (T)(object)Conversions.ToULong(Value);
        }
        else
        {
            return (T)Value;
        }
    }
}");
    }

    /// <summary>
    /// We just use ConditionalCompareObjectEqual to make it a bool, but VB emits a late binding call something like this:
    /// array[0] = Operators.CompareObjectEqual(left, right, false);
    /// array[1] = "Identical values stored in objects should be equal";
    /// NewLateBinding.LateCall(this, null, "AssertTrue", array, null, null, null, true);
    /// This will likely be the same in the vast majority of cases
    /// </summary>
    [Fact]
    public async Task ObjectComparisonIsConvertedToBoolRatherThanLateBoundAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Public Class CopiedFromTheSelfVerifyingBooleanTests
    Public Sub VisualBasicEqualityOfNormalObjectsNotSubjectToSpecialStringConversionRules()
        Dim a1 As Object = 3
        Dim a2 As Object = 3
        AssertTrue(a1 = a2, ""Identical values stored in objects should be equal"")
    End Sub

    Private Sub AssertTrue(v1 As Nullable(Of Boolean), v2 As String)
    End Sub

    Private Sub AssertTrue(v1 As Boolean, v2 As String)
    End Sub
End Class", @"using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

public partial class CopiedFromTheSelfVerifyingBooleanTests
{
    public void VisualBasicEqualityOfNormalObjectsNotSubjectToSpecialStringConversionRules()
    {
        object a1 = 3;
        object a2 = 3;
        AssertTrue(Operators.ConditionalCompareObjectEqual(a1, a2, false), ""Identical values stored in objects should be equal"");
    }

    private void AssertTrue(bool? v1, string v2)
    {
    }

    private void AssertTrue(bool v1, string v2)
    {
    }
}");
    }

    [Fact]
    public async Task TestGenericCastAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Class TestGenericCast
    Private Shared Function GenericFunctionWithCTypeCast(Of T)() As T
        Const result = 1
        Dim resultObj As Object = result
        Return CType(resultObj, T)
    End Function
    Private Shared Function GenericFunctionWithCast(Of T)() As T
        Const result = 1
        Dim resultObj As Object = result
        Return resultObj
    End Function
    Private Shared Function GenericFunctionWithCastThatExistsInCsharp(Of T As {TestGenericCast})() As T
        Return New TestGenericCast
    End Function
End Class", @"using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal partial class TestGenericCast
{
    private static T GenericFunctionWithCTypeCast<T>()
    {
        const int result = 1;
        object resultObj = result;
        return Conversions.ToGenericParameter<T>(resultObj);
    }
    private static T GenericFunctionWithCast<T>()
    {
        const int result = 1;
        object resultObj = result;
        return Conversions.ToGenericParameter<T>(resultObj);
    }
    private static T GenericFunctionWithCastThatExistsInCsharp<T>() where T : TestGenericCast
    {
        return (T)new TestGenericCast();
    }
}");
    }

    [Fact]
    public async Task TestInferringImplicitGenericTypesAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"
Imports System
Imports System.Linq

Public Class TestClass
    Public Sub GenerateFromConstants
        Dim floatArr = Enumerable.Repeat(1.0F, 5).ToArray()        
        Dim doubleArr = Enumerable.Repeat(2.0, 5).ToArray()        
        Dim decimalArr = Enumerable.Repeat(3.0D, 5).ToArray()    
        Dim boolArr = Enumerable.Repeat(true, 5).ToArray()
        Dim intArr = Enumerable.Repeat(1, 5).ToArray()        
        Dim uintArr = Enumerable.Repeat(1ui, 5).ToArray()        
        Dim longArr = Enumerable.Repeat(1l, 5).ToArray()        
        Dim ulongArr = Enumerable.Repeat(1ul, 5).ToArray()
        Dim charArr = Enumerable.Repeat(""a""c, 5).ToArray()        
        Dim strArr = Enumerable.Repeat(""a"", 5).ToArray()
        Dim objArr = Enumerable.Repeat(new object(), 5).ToArray()
    End Sub

    Public Sub GenerateFromCasts
        Dim floatArr = Enumerable.Repeat(CSng(1), 5).ToArray()        
        Dim doubleArr = Enumerable.Repeat(CDbl(2), 5).ToArray()        
        Dim decimalArr = Enumerable.Repeat(CDec(3), 5).ToArray()  
        Dim boolArr = Enumerable.Repeat(CBool(1), 5).ToArray()
        Dim intArr = Enumerable.Repeat(CInt(1.0), 5).ToArray()        
        Dim uintArr = Enumerable.Repeat(CUInt(1.0), 5).ToArray()        
        Dim longArr = Enumerable.Repeat(CLng(1.0), 5).ToArray()        
        Dim ulongArr = Enumerable.Repeat(CULng(1.0), 5).ToArray()
        Dim charArr = Enumerable.Repeat(CChar(""a""), 5).ToArray()        
        Dim strArr = Enumerable.Repeat(CStr(""a""c), 5).ToArray()
        Dim objArr1 = Enumerable.Repeat(CObj(""a""), 5).ToArray()        
        Dim objArr2 = Enumerable.Repeat(CType(""a"", object), 5).ToArray()
    End Sub
End Class
", @"");
    }


        [Fact]
    public async Task TestCTypeStringToEnumAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Public Enum TestEnum
    A
    B
End Enum

Public Class VisualBasicClass
    Public Sub Test(s as String)
        Dim x =  CType(s, TestEnum) = TestEnum.A
        Dim y = TestCast(CType(s, TestEnum))
    End Sub

    Public Function TestCast(s as System.Enum) As String
        Return s.ToString()
    End Function
End Class", @"using System;
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

public enum TestEnum
{
    A,
    B
}

public partial class VisualBasicClass
{
    public void Test(string s)
    {
        bool x = (TestEnum)Conversions.ToInteger(s) == TestEnum.A;
        string y = TestCast((TestEnum)Conversions.ToInteger(s));
    }

    public string TestCast(Enum s)
    {
        return s.ToString();
    }
}");
    }

    [Fact]
    public async Task Issue894_LinqQueryWhereClauseIsAlwaysBooleanAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Imports System.Collections.Generic
Imports System.Linq

Public Class C
    Private Shared Sub LinqWithNullable()
        Dim a = New List(Of Integer?) From {1, 2, 3, Nothing}
        Dim result = From x In a Where x = 1
    End Sub
End Class", @"using System.Collections.Generic;
using System.Linq;

public partial class C
{
    private static void LinqWithNullable()
    {
        var a = new List<int?>() { 1, 2, 3, default };
        var result = from x in a
                     where x == 1
                     select x;
    }
}");
    }
}