using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.StatementTests;

public class ArrayStatementTests : ConverterTestBase
{

    [Fact]
    public async Task ValuesOfArrayAssignmentWithSurroundingClassAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Class SurroundingClass
    Public Arr() As String
End Class

Class UseClass
    Public Sub DoStuff()
        Dim surrounding As SurroundingClass = New SurroundingClass()
        surrounding.Arr(1) = ""bla""
    End Sub
End Class", @"
internal partial class SurroundingClass
{
    public string[] Arr;
}

internal partial class UseClass
{
    public void DoStuff()
    {
        var surrounding = new SurroundingClass();
        surrounding.Arr[1] = ""bla"";
    }
}");
    }

    [Fact]
    public async Task ArrayDeclarationStatementAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        Dim b As Integer()
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod()
    {
        int[] b;
    }
}");
    }

    [Fact]
    public async Task NonDefaultTypedArrayDeclarationStatementAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Dim o As Object() = {""a""}", @"{
    object[] o = new[] { ""a"" };
}");
    }

    [Fact]
    public async Task ArrayDeclarationWithRangeStatementAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System.Collections.Generic

Class TestClass
    Private Sub TestMethod()
        Dim colFics = New List(Of Integer)
        Dim a(0 To colFics.Count - 1) As String
    End Sub
End Class", @"using System.Collections.Generic;

internal partial class TestClass
{
    private void TestMethod()
    {
        var colFics = new List<int>();
        var a = new string[colFics.Count];
    }
}");
    }

    [Fact]
    public async Task InitializeArrayOfArraysAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Module Module1
    Sub Main()
        Dim ls As New ArrayList(5)
        Dim s(ls.Count - 1)() As String
    End Sub
End Module", @"using System.Collections;

internal static partial class Module1
{
    public static void Main()
    {
        var ls = new ArrayList(5);
        var s = new string[ls.Count][];
    }
}");
    }

    [Fact]
    public async Task ArrayEraseAndRedimStatementAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class TestClass
    Shared Function TestMethod(numArray As Integer(), numArray2 As Integer()) As Integer()
        ReDim numArray(3)
        Erase numArray
        numArray2(1) = 1
        ReDim Preserve numArray(5), numArray2(5)
        Dim y(6, 5) As Integer
        y(2,3) = 1
        ReDim Preserve y(6,8)
        Return numArray2
    End Function
End Class", @"using System;

public partial class TestClass
{
    public static int[] TestMethod(int[] numArray, int[] numArray2)
    {
        numArray = new int[4];
        numArray = null;
        numArray2[1] = 1;
        Array.Resize(ref numArray, 6);
        Array.Resize(ref numArray2, 6);
        var y = new int[7, 6];
        y[2, 3] = 1;
        var oldY = y;
        y = new int[7, 9];
        if (oldY is not null)
            for (var i = 0; i <= oldY.Length / oldY.GetLength(1) - 1; ++i)
                Array.Copy(oldY, i * oldY.GetLength(1), y, i * y.GetLength(1), Math.Min(oldY.GetLength(1), y.GetLength(1)));
        return numArray2;
    }
}");
    }

    [Fact]
    public async Task Redim2dArrayAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Friend Class Program
    Private Shared My2darray As Integer()()
    Public Shared Sub Main(ByVal args As String())
        ReDim Me.My2darray(6)
    End Sub
End Class", @"
internal partial class Program
{
    private static int[][] My2darray;
    public static void Main(string[] args)
    {
        My2darray = new int[7][];
    }
}
1 source compilation errors:
BC30043: 'Me' is valid only within an instance method.
");
    }

    [Fact]
    public async Task RedimArrayOfGenericsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class Class1
    Dim test() As List(Of Integer)

    Private Sub test123(sender As Object, e As EventArgs)
        ReDim Me.test(42)

        Dim test1() As Tuple(Of Integer, Integer)
        ReDim test1(42)
    End Sub
End Class", @"using System;
using System.Collections.Generic;

public partial class Class1
{
    private List<int>[] test;

    private void test123(object sender, EventArgs e)
    {
        test = new List<int>[43];

        Tuple<int, int>[] test1;
        test1 = new Tuple<int, int>[43];
    }
}");
    }
    [Fact]
    public async Task ArrayInitializationStatementAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        Dim b As Integer() = {1, 2, 4}
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod()
    {
        int[] b = new[] { 1, 2, 4 };
    }
}");
    }

    [Fact]
    public async Task ArrayInitializationStatementInVarDeclarationAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        Dim b = {1, 2, 3}
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod()
    {
        int[] b = new[] { 1, 2, 3 };
    }
}");
    }

    [Fact]
    public async Task ArrayInitializationStatementWithTypeAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        Dim b As Integer() = New Integer() {1, 2, 3}
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod()
    {
        int[] b = new int[] { 1, 2, 3 };
    }
}");
    }

    [Fact]
    public async Task Issue554_AvoidImplicitArrayTypeAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System.Net
Imports System.Net.Sockets

Public Class Issue554_ImplicitArrayType
    Public Shared Sub Main()
        Dim msg() As Byte = {2}
        Dim ep As IPEndPoint = New IPEndPoint(IPAddress.Loopback, 1434)
        Dim l_socket As Socket = New Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
        Dim i_Test, i_Tab(), bearb(,) As Integer
        l_socket.SendTo(msg, ep)
    End Sub
End Class"
            , @"using System.Net;
using System.Net.Sockets;

public partial class Issue554_ImplicitArrayType
{
    public static void Main()
    {
        byte[] msg = new byte[] { 2 };
        var ep = new IPEndPoint(IPAddress.Loopback, 1434);
        var l_socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        int i_Test;
        int[] i_Tab;
        int[,] bearb;
        l_socket.SendTo(msg, ep);
    }
}");
    }

    [Fact]
    public async Task ArrayInitializationStatementWithLengthAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        Dim b As Integer() = New Integer(2) {1, 2, 3}
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod()
    {
        int[] b = new int[3] { 1, 2, 3 };
    }
}");
    }

    [Fact]
    public async Task ArrayInitializationStatementWithLengthAndNoValuesAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        Dim b As Integer() = New Integer(2) { }
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod()
    {
        int[] b = new int[3];
    }
}");
    }

    /// <summary>
    /// Inspired by: https://docs.microsoft.com/en-us/dotnet/visual-basic/programming-guide/language-features/arrays/
    /// </summary>
    [Fact]
    public async Task LotsOfArrayInitializationAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        ' Declare a single-dimension array of 5 numbers.
        Dim numbers1(4) As Integer

        ' Declare a single-dimension array and set its 4 values.
        Dim numbers2 = New Integer() {1, 2, 4, 8}

        ' Declare a 6 x 6 multidimensional array.
        Dim matrix1(5, 5) As Double

        ' Declare a 4 x 3 multidimensional array and set array element values.
        Dim matrix2 = New Integer(3, 2) {{1, 2, 3}, {2, 3, 4}, {3, 4, 5}, {4, 5, 6}}

        ' Combine rank specifiers with initializers of various kinds
        Dim rankSpecifiers(,) As Double = New Double(1,1) {{1.0, 2.0}, {3.0, 4.0}}
        Dim rankSpecifiers2(,) As Double = New Double(1,1) {}

        ' Declare a jagged array
        Dim sales()() As Double = New Double(11)() {}
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod()
    {
        // Declare a single-dimension array of 5 numbers.
        var numbers1 = new int[5];

        // Declare a single-dimension array and set its 4 values.
        int[] numbers2 = new int[] { 1, 2, 4, 8 };

        // Declare a 6 x 6 multidimensional array.
        var matrix1 = new double[6, 6];

        // Declare a 4 x 3 multidimensional array and set array element values.
        int[,] matrix2 = new int[4, 3] { { 1, 2, 3 }, { 2, 3, 4 }, { 3, 4, 5 }, { 4, 5, 6 } };

        // Combine rank specifiers with initializers of various kinds
        double[,] rankSpecifiers = new double[2, 2] { { 1.0d, 2.0d }, { 3.0d, 4.0d } };
        double[,] rankSpecifiers2 = new double[2, 2];

        // Declare a jagged array
        double[][] sales = new double[12][];
    }
}");
    }

    [Fact]
    public async Task MultidimensionalArrayDeclarationStatementAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        Dim b As Integer(,)
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod()
    {
        int[,] b;
    }
}");
    }

    [Fact]
    public async Task MultidimensionalArrayInitializationStatementAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        Dim b As Integer(,) = {{1, 2}, {3, 4}}
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod()
    {
        int[,] b = new[,] { { 1, 2 }, { 3, 4 } };
    }
}");
    }

    [Fact]
    public async Task MultidimensionalArrayInitializationStatementWithTypeAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        Dim b As Integer(,) = New Integer(,) {{1, 3}, {2, 4}}
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod()
    {
        int[,] b = new int[,] { { 1, 3 }, { 2, 4 } };
    }
}");
    }

    [Fact]
    public async Task MultidimensionalArrayInitializationStatementWithAndWithoutLengthsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        Dim a As Integer(,) = New Integer(,) {{1, 2}, {3, 4}}
        Dim b As Integer(,) = New Integer(1, 1) {{1, 2}, {3, 4}}
        Dim c as Integer(,,) = New Integer(,,) {{{1}}}
        Dim d as Integer(,,) = New Integer(0, 0, 0) {{{1}}}
        Dim e As Integer()(,) = New Integer()(,) {}
        Dim f As Integer()(,) = New Integer(-1)(,) {}
        Dim g As Integer()(,) = New Integer(0)(,) {}
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod()
    {
        int[,] a = new int[,] { { 1, 2 }, { 3, 4 } };
        int[,] b = new int[2, 2] { { 1, 2 }, { 3, 4 } };
        int[,,] c = new int[,,] { { { 1 } } };
        int[,,] d = new int[1, 1, 1] { { { 1 } } };
        int[][,] e = new int[][,] { };
        int[][,] f = new int[0][,];
        int[][,] g = new int[1][,];
    }
}");
    }

    [Fact]
    public async Task JaggedArrayDeclarationStatementAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        Dim b As Integer()()
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod()
    {
        int[][] b;
    }
}");
    }

    [Fact]
    public async Task JaggedArrayInitializationStatementAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        Dim b As Integer()() = {New Integer() {1, 2}, New Integer() {3, 4}}
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod()
    {
        int[][] b = new[] { new int[] { 1, 2 }, new int[] { 3, 4 } };
    }
}");
    }

    [Fact]
    public async Task JaggedArrayInitializationStatementWithTypeAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        Dim b = New Integer()() {New Integer() {1}}
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod()
    {
        int[][] b = new int[][] { new int[] { 1 } };
    }
}");
    }

    [Fact]
    public async Task JaggedArrayInitializationStatementWithLengthAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        Dim b As Integer()() = New Integer(1)() {New Integer() {1, 2}, New Integer() {3, 4}}
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod()
    {
        int[][] b = new int[2][] { new int[] { 1, 2 }, new int[] { 3, 4 } };
    }
}");
    }

    [Fact]
    public async Task SplitArrayDeclarationsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class SplitArrayDeclarations
    Public Shared Sub Main()
        Dim i_Test, i_Tab(), bearb(,) As Integer
    End Sub
End Class"
            , @"
public partial class SplitArrayDeclarations
{
    public static void Main()
    {
        int i_Test;
        int[] i_Tab;
        int[,] bearb;
    }
}");
    }

    [Fact]
    public async Task ConditionalArrayBoundsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Class TestClass
    Private Sub TestMethod()
        dim i as integer = 0
        dim a(If(i = 1, 2, 3)) as string
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod()
    {
        int i = 0;
        var a = new string[(i == 1 ? 2 : 3) + 1];
    }
}");
    }
}