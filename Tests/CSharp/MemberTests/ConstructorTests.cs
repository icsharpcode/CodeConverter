using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.MemberTests;

public class ConstructorTests : ConverterTestBase
{
    [Fact]
    public async Task TestConstructorVisibilityAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class Class1
    Sub New(x As Boolean)
    End Sub
End Class", @"
internal partial class Class1
{
    public Class1(bool x)
    {
    }
}");
    }

    [Fact]
    public async Task TestModuleConstructorAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Module Module1
    Sub New()
        Dim someValue As Integer = 0
    End Sub
End Module", @"
internal static partial class Module1
{
    static Module1()
    {
        int someValue = 0;
    }
}");
    }

    [Fact]
    public async Task TestHoistedOutParameterAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Public Class ClassWithProperties
   Public Property Property1 As String
End Class

Public Class VisualBasicClass
   Public Sub New()
       Dim x As New Dictionary(Of String, String)()
       Dim y As New ClassWithProperties()
       
       If (x.TryGetValue(""x"", y.Property1)) Then
          Debug.Print(y.Property1)
       End If
   End Sub
End Class", @"using System.Collections.Generic;
using System.Diagnostics;

public partial class ClassWithProperties
{
    public string Property1 { get; set; }
}

public partial class VisualBasicClass
{
    public VisualBasicClass()
    {
        var x = new Dictionary<string, string>();
        var y = new ClassWithProperties();

        bool localTryGetValue() { string argvalue = y.Property1; var ret = x.TryGetValue(""x"", out argvalue); y.Property1 = argvalue; return ret; }

        if (localTryGetValue())
        {
            Debug.Print(y.Property1);
        }
    }
}");
    }

    [Fact]
    public async Task TestHoistedOutParameterLambdaUsingByRefParameterAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Public Class SomeClass
    Sub S(Optional ByRef x As Integer = -1)
        Dim i As Integer = 0
        If F1(x, i) Then
        ElseIf F2(x, i) Then
        ElseIf F3(x, i) Then
        End If
    End Sub

    Function F1(x As Integer, ByRef o As Object) As Boolean : End Function
    Function F2(ByRef x As Integer, ByRef o As Object) As Boolean : End Function
    Function F3(ByRef x As Object, ByRef o As Object) As Boolean : End Function
End Class", @"using System.Runtime.InteropServices;
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

public partial class SomeClass
{
    public void S([Optional, DefaultParameterValue(-1)] ref int x)
    {
        int i = 0;
        bool localF1(ref int x) { object argo = i; var ret = F1(x, ref argo); i = Conversions.ToInteger(argo); return ret; }
        bool localF2(ref int x) { object argo1 = i; var ret = F2(ref x, ref argo1); i = Conversions.ToInteger(argo1); return ret; }
        bool localF3(ref int x) { object argx = x; object argo2 = i; var ret = F3(ref argx, ref argo2); x = Conversions.ToInteger(argx); i = Conversions.ToInteger(argo2); return ret; }

        if (localF1(ref x))
        {
        }
        else if (localF2(ref x))
        {
        }
        else if (localF3(ref x))
        {
        }
    }

    public bool F1(int x, ref object o)
    {
        return default;
    }
    public bool F2(ref int x, ref object o)
    {
        return default;
    }
    public bool F3(ref object x, ref object o)
    {
        return default;
    }
}");
    }

    [Fact]
    public async Task TestConstructorAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Class TestClass(Of T As {Class, New}, T2 As Structure, T3)
    Public Sub New(<Out> ByRef argument As T, ByRef argument2 As T2, ByVal argument3 As T3)
    End Sub
End Class", @"
internal partial class TestClass<T, T2, T3>
    where T : class, new()
    where T2 : struct
{
    public TestClass(out T argument, ref T2 argument2, T3 argument3)
    {
    }
}
1 target compilation errors:
CS0177: The out parameter 'argument' must be assigned to before control leaves the current method");
    }

    [Fact]
    public async Task TestConstructorWithImplicitPublicAccessibilityAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Sub New()
End Sub", @"public SurroundingClass()
{
}");
    }

    [Fact]
    public async Task TestStaticConstructorAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Shared Sub New()
End Sub", @"static SurroundingClass()
{
}");
    }

    [Fact]
    public async Task TestConstructorStaticLocalConvertedToFieldAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Class StaticLocalConvertedToField
    Sub New(x As Boolean)
        Static sPrevPosition As Integer = 7 ' Comment moves with declaration
        Console.WriteLine(sPrevPosition)
    End Sub
    Sub New(x As Integer)
        Static sPrevPosition As Integer
        Console.WriteLine(sPrevPosition)
    End Sub
End Class", @"using System;

internal partial class StaticLocalConvertedToField
{
    private int _sPrevPosition = 7; // Comment moves with declaration
    public StaticLocalConvertedToField(bool x)
    {
        Console.WriteLine(_sPrevPosition);
    }

    private int _sPrevPosition1 = default;
    public StaticLocalConvertedToField(int x)
    {
        Console.WriteLine(_sPrevPosition1);
    }
}");
    }
}