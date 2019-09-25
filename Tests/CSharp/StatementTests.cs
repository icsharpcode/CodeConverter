using System.Threading.Tasks;
using CodeConverter.Tests.TestRunners;
using Xunit;

namespace CodeConverter.Tests.CSharp
{
    public class StatementTests : ConverterTestBase
    {
        [Fact]
        public async Task EmptyStatement()
        {
            await TestConversionVisualBasicToCSharpWithoutComments(@"Class TestClass
    Private Sub TestMethod()
        If True Then
        End If

        While True
        End While

        Do
        Loop While True
    End Sub
End Class", @"internal partial class TestClass
{
    private void TestMethod()
    {
        if (true)
        {
        }

        while (true)
        {
        }

        do
        {
        }
        while (true);
    }
}");
        }

        [Fact]
        public async Task AssignmentStatement()
        {
            await TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Dim b As Integer
        b = 0
    End Sub
End Class", @"internal partial class TestClass
{
    private void TestMethod()
    {
        int b;
        b = 0;
    }
}");
        }

        [Fact]
        public async Task EnumAssignmentStatement()
        {
            await TestConversionVisualBasicToCSharp(@"Enum MyEnum
    AMember
End Enum

Class TestClass
    Private Sub TestMethod(v as String)
        Dim b As MyEnum = MyEnum.Parse(GetType(MyEnum), v)
        b = MyEnum.Parse(GetType(MyEnum), v)
    End Sub
End Class", @"using System;

internal enum MyEnum
{
    AMember
}

internal partial class TestClass
{
    private void TestMethod(string v)
    {
        MyEnum b = (MyEnum)Enum.Parse(typeof(MyEnum), v);
        b = (MyEnum)Enum.Parse(typeof(MyEnum), v);
    }
}");
        }

        [Fact]
        public async Task AssignmentStatementInDeclaration()
        {
            await TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Dim b As Integer = 0
    End Sub
End Class", @"internal partial class TestClass
{
    private void TestMethod()
    {
        int b = 0;
    }
}");
        }

        [Fact]
        public async Task AssignmentStatementInVarDeclaration()
        {
            await TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Dim b = 0
    End Sub
End Class", @"internal partial class TestClass
{
    private void TestMethod()
    {
        int b = 0;
    }
}");
        }

        [Fact]
        public async Task AssignmentStatementWithXmlElement()
        {
            await TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Dim b = <someXmlTag></someXmlTag>
        Dim c = <someXmlTag><bla anAttribute=""itsValue"">tata</bla><someContent>tata</someContent></someXmlTag>
    End Sub
End Class", @"using System.Xml.Linq;

internal partial class TestClass
{
    private void TestMethod()
    {
        var b = XElement.Parse(""<someXmlTag></someXmlTag>"");
        var c = XElement.Parse(""<someXmlTag><bla anAttribute=\""itsValue\"">tata</bla><someContent>tata</someContent></someXmlTag>"");
    }
}");
        }

        /// <summary>
        /// Implicitly typed lambdas exist in vb but are not happening in C#. See discussion on https://github.com/dotnet/roslyn/issues/14
        /// * For VB local declarations, inference happens. The closest equivalent in C# is a local function since Func/Action would be overly restrictive for some cases
        /// * For VB field declarations, inference doesn't happen, it just uses "Object", but in C# lambdas can't be assigned to object so we have to settle for Func/Action for externally visible methods to maintain assignability.
        /// </summary>
        [Fact]
        public async Task AssignmentStatementWithFunc()
        {
            // BUG: pubWrite's body is missing a return statement
            // pubWrite is an example of when the LambdaConverter could analyze ConvertedType at usages, realize the return type is never used, and convert it to an Action.
            // Number of lines changes so can't auto test comments
            await TestConversionVisualBasicToCSharpWithoutComments(@"Public Class TestFunc
    Public pubIdent = Function(row As Integer) row
    Public pubWrite = Function(row As Integer) Console.WriteLine(row)
    Dim isFalse = Function(row As Integer) False
    Dim write0 = Sub()
        Console.WriteLine(0)
    End Sub

    Private Sub TestMethod()
        Dim index = (Function(pList As List(Of String)) pList.All(Function(x) True)),
            index2 = (Function(pList As List(Of String)) pList.All(Function(x) False)),
            index3 = (Function(pList As List(Of Integer)) pList.All(Function(x) True))
        Dim isTrue = Function(pList As List(Of String))
                            Return pList.All(Function(x) True)
                     End Function
        Dim isTrueWithNoStatement = (Function(pList As List(Of String)) pList.All(Function(x) True))
        Dim write = Sub() Console.WriteLine(1)
    End Sub
End Class", @"using System;
using System.Collections.Generic;
using System.Linq;

public partial class TestFunc
{
    public Func<int, int> pubIdent = (row) => row;
    public Func<int, object> pubWrite = (row) => Console.WriteLine(row);
    private bool isFalse(int row) => false;
    private void write0() => Console.WriteLine(0);

    private void TestMethod()
    {
        bool index(List<string> pList) => pList.All(x => true);
        bool index2(List<string> pList) => pList.All(x => false);
        bool index3(List<int> pList) => pList.All(x => true);
        bool isTrue(List<string> pList) => pList.All(x => true);
        bool isTrueWithNoStatement(List<string> pList) => pList.All(x => true);
        void write() => Console.WriteLine(1);
    }
}");
        }

        /// <summary>
        /// Technically it's possible to use a type-inferred lambda within a for loop
        /// Other than the above field/local declarations, candidates would be other things using <see cref="SplitVariableDeclarations"/>,
        /// e.g. ForEach (no assignment involved), Using block (can't have a disposable lambda)
        /// </summary>
        [Fact]
        public async Task ContrivedFuncInferenceExample()
        {
            //BUG: Comments on operators first line get ported to last line
            await TestConversionVisualBasicToCSharpWithoutComments(@"Friend Class ContrivedFuncInferenceExample
    Private Sub TestMethod()
        For index = (Function(pList As List(Of String)) pList.All(Function(x) True)) To New Blah() Step New Blah()
            Dim buffer = index.Check(New List(Of String))
            Console.WriteLine($""{buffer}"")
        Next
    End Sub

    Class Blah
        Public ReadOnly Check As Func(Of List(Of String), Boolean)

        Public Sub New(Optional check As Func(Of List(Of String), Boolean) = Nothing)
            check = check
        End Sub

        Public Shared Widening Operator CType(ByVal p1 As Func(Of List(Of String), Boolean)) As Blah
            Return New Blah(p1)
        End Operator
        Public Shared Widening Operator CType(ByVal p1 As Blah) As Func(Of List(Of String), Boolean)
            Return p1.Check
        End Operator
        Public Shared Operator -(ByVal p1 As Blah, ByVal p2 As Blah) As Blah
            Return New Blah()
        End Operator
        Public Shared Operator +(ByVal p1 As Blah, ByVal p2 As Blah) As Blah
            Return New Blah()
        End Operator
        Public Shared Operator <=(ByVal p1 As Blah, ByVal p2 As Blah) As Boolean
            Return p1.Check(New List(Of String))
        End Operator
        Public Shared Operator >=(ByVal p1 As Blah, ByVal p2 As Blah) As Boolean
            Return p2.Check(New List(Of String))
        End Operator
    End Class
End Class", @"using System;
using System.Collections.Generic;
using System.Linq;

internal partial class ContrivedFuncInferenceExample
{
    private void TestMethod()
    {
        for (Blah index = (List<string> pList) => pList.All(x => true), loopTo = new Blah(); index <= loopTo; index += new Blah())
        {
            bool buffer = index.Check(new List<string>());
            Console.WriteLine($""{buffer}"");
        }
    }

    public partial class Blah
    {
        public readonly Func<List<string>, bool> Check;

        public Blah(Func<List<string>, bool> check = null)
        {
            check = check;
        }

        public static implicit operator Blah(Func<List<string>, bool> p1)
        {
            return new Blah(p1);
        }
        public static implicit operator Func<List<string>, bool>(Blah p1)
        {
            return p1.Check;
        }
        public static Blah operator -(Blah p1, Blah p2)
        {
            return new Blah();
        }
        public static Blah operator +(Blah p1, Blah p2)
        {
            return new Blah();
        }
        public static bool operator <=(Blah p1, Blah p2)
        {
            return p1.Check(new List<string>());
        }
        public static bool operator >=(Blah p1, Blah p2)
        {
            return p2.Check(new List<string>());
        }
    }
}");
        }

        [Fact]
        public async Task ObjectInitializationStatement()
        {
            await TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Dim b As String
        b = New String(""test"")
    End Sub
End Class", @"internal partial class TestClass
{
    private void TestMethod()
    {
        string b;
        b = new string(""test"");
    }
}");
        }

        [Fact]
        public async Task TupleInitializationStatement()
        {
            await TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Dim totales As (fics As Integer, dirs As Integer) = (0, 0)
    End Sub
End Class", @"internal partial class TestClass
{
    private void TestMethod()
    {
        (int fics, int dirs) totales = (0, 0);
    }
}");
        }

        [Fact]
        public async Task ObjectInitializationStatementInDeclaration()
        {
            await TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Dim b As String = New String(""test"")
    End Sub
End Class", @"internal partial class TestClass
{
    private void TestMethod()
    {
        string b = new string(""test"");
    }
}");
        }

        [Fact]
        public async Task ObjectInitializationStatementInVarDeclaration()
        {
            await TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Dim b = New String(""test"")
    End Sub
End Class", @"internal partial class TestClass
{
    private void TestMethod()
    {
        string b = new string(""test"");
    }
}");
        }

        [Fact]
        public async Task ValuesOfArrayAssignmentWithSurroundingClass()
        {
            await TestConversionVisualBasicToCSharp(
@"Class SurroundingClass
    Public Arr() As String
End Class

Class UseClass
    Public Sub DoStuff()
        Dim surrounding As SurroundingClass = New SurroundingClass()
        surrounding.Arr(1) = ""bla""
    End Sub
End Class", @"internal partial class SurroundingClass
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
        public async Task ArrayDeclarationStatement()
        {
            await TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Dim b As Integer()
    End Sub
End Class", @"internal partial class TestClass
{
    private void TestMethod()
    {
        int[] b;
    }
}");
        }

        [Fact]
        public async Task ArrayDeclarationWithRangeStatement()
        {
            await TestConversionVisualBasicToCSharp(@"Imports System.Collections.Generic

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
        var a = new string[colFics.Count - 1 + 1];
    }
}");
        }

        [Fact]
        public async Task ArrayEraseAndRedimStatement()
        {
            // One statement turns into two, so can't auto-test comments
            await TestConversionVisualBasicToCSharpWithoutComments(@"Public Class TestClass
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
        var oldNumArray = numArray;
        numArray = new int[6];
        if (oldNumArray != null)
            Array.Copy(oldNumArray, numArray, Math.Min(6, oldNumArray.Length));
        var oldNumArray2 = numArray2;
        numArray2 = new int[6];
        if (oldNumArray2 != null)
            Array.Copy(oldNumArray2, numArray2, Math.Min(6, oldNumArray2.Length));
        var y = new int[7, 6];
        y[2, 3] = 1;
        var oldY = y;
        y = new int[7, 9];
        if (oldY != null)
            for (var i = 0; i <= oldY.Length / oldY.GetLength(1) - 1; ++i)
                Array.Copy(oldY, i * oldY.GetLength(1), y, i * y.GetLength(1), Math.Min(oldY.GetLength(1), y.GetLength(1)));
        return numArray2;
    }
}");
        }

        [Fact]
        public async Task EndStatement()
        {
            await TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        End
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    private void TestMethod()
    {
        Environment.Exit(0);
    }
}");
        }

        [Fact]
        public async Task StopStatement()
        {
            await TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Stop
    End Sub
End Class", @"using System.Diagnostics;

internal partial class TestClass
{
    private void TestMethod()
    {
        Debugger.Break();
    }
}");
        }

        [Fact]
        public async Task WithBlock()
        {
            await TestConversionVisualBasicToCSharpWithoutComments(@"Class TestClass
    Private Sub TestMethod()
        With New System.Text.StringBuilder
            .Capacity = 20
            ?.Length = 0
        End With
    End Sub
End Class", @"using System.Text;

internal partial class TestClass
{
    private void TestMethod()
    {
        {
            var withBlock = new StringBuilder();
            withBlock.Capacity = 20;
            withBlock?.Length = 0;
        }
    }
}");
        }

        [Fact]
        public async Task WithBlock2()
        {
            await TestConversionVisualBasicToCSharpWithoutComments(@"Imports System.Data.SqlClient

Class TestClass
    Private Sub Save()
        Using cmd As SqlCommand = new SqlCommand()
            With cmd
            .ExecuteNonQuery()
            ?.ExecuteNonQuery()
            .ExecuteNonQuery
            ?.ExecuteNonQuery
            End With
        End Using
    End Sub
End Class", @"using System.Data.SqlClient;

internal partial class TestClass
{
    private void Save()
    {
        using (var cmd = new SqlCommand())
        {
            {
                var withBlock = cmd;
                withBlock.ExecuteNonQuery();
                withBlock?.ExecuteNonQuery();
                withBlock.ExecuteNonQuery();
                withBlock?.ExecuteNonQuery();
            }
        }
    }
}");
        }

        [Fact]
        public async Task WithBlockValue()
        {
            //Whitespace trivia bug on first statement in with block
            await TestConversionVisualBasicToCSharpWithoutComments(@"Public Class VisualBasicClass
    Public Sub Stuff()
        Dim str As SomeStruct
        With Str
            ReDim .ArrField(1)
            ReDim .ArrProp(2)
        End With
    End Sub
End Class

Public Structure SomeStruct
    Public ArrField As String()
    Public Property ArrProp As String()
End Structure", @"public partial class VisualBasicClass
{
    public void Stuff()
    {
        var str = default(SomeStruct);
        str
.ArrField = new string[2];
        str.ArrProp = new string[3];
    }
}

public partial struct SomeStruct
{
    public string[] ArrField;
    public string[] ArrProp { get; set; }
}");
        }

        [Fact]
        public async Task NestedWithBlock()
        {
            await TestConversionVisualBasicToCSharpWithoutComments(@"Class TestClass
    Private Sub TestMethod()
        With New System.Text.StringBuilder
            Dim withBlock as Integer = 3
            With New System.Text.StringBuilder
                Dim withBlock1 as Integer = 4
                .Capacity = withBlock1
            End With

            .Length = withBlock
        End With
    End Sub
End Class", @"using System.Text;

internal partial class TestClass
{
    private void TestMethod()
    {
        {
            var withBlock2 = new StringBuilder();
            int withBlock = 3;
            {
                var withBlock3 = new StringBuilder();
                int withBlock1 = 4;
                withBlock3.Capacity = withBlock1;
            }

            withBlock2.Length = withBlock;
        }
    }
}");
        }
        [Fact]
        public async Task ArrayInitializationStatement()
        {
            await TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Dim b As Integer() = {1, 2, 3}
    End Sub
End Class", @"internal partial class TestClass
{
    private void TestMethod()
    {
        var b = new[] { 1, 2, 3 };
    }
}");
        }

        [Fact]
        public async Task ArrayInitializationStatementInVarDeclaration()
        {
            await TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Dim b = {1, 2, 3}
    End Sub
End Class", @"internal partial class TestClass
{
    private void TestMethod()
    {
        var b = new[] { 1, 2, 3 };
    }
}");
        }

        [Fact]
        public async Task ArrayInitializationStatementWithType()
        {
            await TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Dim b As Integer() = New Integer() {1, 2, 3}
    End Sub
End Class", @"internal partial class TestClass
{
    private void TestMethod()
    {
        var b = new int[] { 1, 2, 3 };
    }
}");
        }

        [Fact]
        public async Task ArrayInitializationStatementWithLength()
        {
            await TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Dim b As Integer() = New Integer(2) {1, 2, 3}
    End Sub
End Class", @"internal partial class TestClass
{
    private void TestMethod()
    {
        var b = new int[3] { 1, 2, 3 };
    }
}");
        }

        [Fact]
        public async Task ArrayInitializationStatementWithLengthAndNoValues()
        {
            await TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Dim b As Integer() = New Integer(2) { }
    End Sub
End Class", @"internal partial class TestClass
{
    private void TestMethod()
    {
        var b = new int[3];
    }
}");
        }

        /// <summary>
        /// Inspired by: https://docs.microsoft.com/en-us/dotnet/visual-basic/programming-guide/language-features/arrays/
        /// </summary>
        [Fact]
        public async Task LotsOfArrayInitialization()
        {
            await TestConversionVisualBasicToCSharpWithoutComments(@"Class TestClass
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
End Class", @"internal partial class TestClass
{
    private void TestMethod()
    {
        // Declare a single-dimension array of 5 numbers.
        var numbers1 = new int[5];

        // Declare a single-dimension array and set its 4 values.
        var numbers2 = new int[] { 1, 2, 4, 8 };

        // Declare a 6 x 6 multidimensional array.
        var matrix1 = new double[6, 6];

        // Declare a 4 x 3 multidimensional array and set array element values.
        var matrix2 = new int[4, 3] { { 1, 2, 3 }, { 2, 3, 4 }, { 3, 4, 5 }, { 4, 5, 6 } };

        // Combine rank specifiers with initializers of various kinds
        var rankSpecifiers = new double[2, 2] { { 1.0, 2.0 }, { 3.0, 4.0 } };
        var rankSpecifiers2 = new double[2, 2];

        // Declare a jagged array
        var sales = new double[12][] { };
    }
}");
        }

        [Fact]
        public async Task MultidimensionalArrayDeclarationStatement()
        {
            await TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Dim b As Integer(,)
    End Sub
End Class", @"internal partial class TestClass
{
    private void TestMethod()
    {
        int[,] b;
    }
}");
        }

        [Fact]
        public async Task MultidimensionalArrayInitializationStatement()
        {
            await TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Dim b As Integer(,) = {{1, 2}, {3, 4}}
    End Sub
End Class", @"internal partial class TestClass
{
    private void TestMethod()
    {
        var b = new[] { { 1, 2 }, { 3, 4 } };
    }
}");
        }

        [Fact]
        public async Task MultidimensionalArrayInitializationStatementWithType()
        {
            await TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Dim b As Integer(,) = New Integer(,) {{1, 2}, {3, 4}}
    End Sub
End Class", @"internal partial class TestClass
{
    private void TestMethod()
    {
        var b = new int[,] { { 1, 2 }, { 3, 4 } };
    }
}");
        }

        [Fact]
        public async Task MultidimensionalArrayInitializationStatementWithAndWithoutLengths()
        {
            await TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Dim a As Integer(,) = New Integer(,) {{1, 2}, {3, 4}}
        Dim b As Integer(,) = New Integer(1, 1) {{1, 2}, {3, 4}}
        Dim c as Integer(,,) = New Integer(,,) {{{1}}}
        Dim d as Integer(,,) = New Integer(0, 0, 0) {{{1}}}
        Dim e As Integer()(,) = New Integer()(,) {}
        Dim f As Integer()(,) = New Integer(-1)(,) {}
    End Sub
End Class", @"internal partial class TestClass
{
    private void TestMethod()
    {
        var a = new int[,] { { 1, 2 }, { 3, 4 } };
        var b = new int[2, 2] { { 1, 2 }, { 3, 4 } };
        var c = new int[,,] { { { 1 } } };
        var d = new int[1, 1, 1] { { { 1 } } };
        var e = new int[][,] { };
        var f = new int[0][,] { };
    }
}");
        }

        [Fact]
        public async Task JaggedArrayDeclarationStatement()
        {
            await TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Dim b As Integer()()
    End Sub
End Class", @"internal partial class TestClass
{
    private void TestMethod()
    {
        int[][] b;
    }
}");
        }

        [Fact]
        public async Task JaggedArrayInitializationStatement()
        {
            await TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Dim b As Integer()() = {New Integer() {1, 2}, New Integer() {3, 4}}
    End Sub
End Class", @"internal partial class TestClass
{
    private void TestMethod()
    {
        var b = new[] { new int[] { 1, 2 }, new int[] { 3, 4 } };
    }
}");
        }

        [Fact]
        public async Task JaggedArrayInitializationStatementWithType()
        {
            await TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Dim b = New Integer()() {New Integer() {1}}
    End Sub
End Class", @"internal partial class TestClass
{
    private void TestMethod()
    {
        var b = new int[][] { new int[] { 1 } };
    }
}");
        }

        [Fact]
        public async Task JaggedArrayInitializationStatementWithLength()
        {
            await TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Dim b As Integer()() = New Integer(1)() {New Integer() {1, 2}, New Integer() {3, 4}}
    End Sub
End Class", @"internal partial class TestClass
{
    private void TestMethod()
    {
        var b = new int[2][] { new int[] { 1, 2 }, new int[] { 3, 4 } };
    }
}");
        }

        [Fact]
        public async Task DeclarationStatements()
        {
            await TestConversionVisualBasicToCSharpWithoutComments(
@"Class Test
    Private Sub TestMethod()
the_beginning:
        Dim value As Integer = 1
        Const myPIe As Double = System.Math.PI
        Dim text = ""This is my text!""
        GoTo the_beginning
    End Sub
End Class", @"using System;

internal partial class Test
{
    private void TestMethod()
    {
    the_beginning:
        ;
        int value = 1;
        const double myPIe = Math.PI;
        string text = ""This is my text!"";
        goto the_beginning;
    }
}");
        }

        [Theory]
        [InlineData("Sub", "", "void")]
        [InlineData("Function", " As Long", "long")]
        public async Task DeclareStatement(string vbMethodDecl,string vbType, string csType)
        {
            // Intentionally uses a type name with a different casing as the loop variable, i.e. "process" to test name resolution
            await TestConversionVisualBasicToCSharp($@"Imports System.Diagnostics
Imports System.Threading

Public Class AcmeClass
    Private Declare {vbMethodDecl} SetForegroundWindow Lib ""user32"" (ByVal hwnd As Int32){vbType}

    Public Shared Sub Main()
        For Each proc In Process.GetProcesses().Where(Function(p) Not String.IsNullOrEmpty(p.MainWindowTitle))
            SetForegroundWindow(proc.MainWindowHandle.ToInt32())
            Thread.Sleep(1000)
        Next
    End Sub
End Class"
                , $@"using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

public partial class AcmeClass
{{
    [DllImport(""user32"")]
    private static extern {csType} SetForegroundWindow(int hwnd);

    public static void Main()
    {{
        foreach (var proc in Process.GetProcesses().Where(p => !string.IsNullOrEmpty(p.MainWindowTitle)))
        {{
            SetForegroundWindow(proc.MainWindowHandle.ToInt32());
            Thread.Sleep(1000);
        }}
    }}
}}");
        }


        [Fact]
        public async Task DeclareStatementWithAttributes()
        {
            await TestConversionVisualBasicToCSharp(@"Public Class AcmeClass
    Friend Declare Ansi Function GetNumDevices Lib ""CP210xManufacturing.dll"" Alias ""CP210x_GetNumDevices"" (ByRef NumDevices As String) As Integer
End Class"
                , @"using System.Runtime.InteropServices;

public partial class AcmeClass
{
    [DllImport(""CP210xManufacturing.dll"", EntryPoint = ""CP210x_GetNumDevices"", CharSet = CharSet.Ansi)]
    internal static extern int GetNumDevices(ref string NumDevices);
}");
        }

        [Fact]
        public async Task IfStatement()
        {
            await TestConversionVisualBasicToCSharpWithoutComments(@"Class TestClass
    Private Sub TestMethod(ByVal a As Integer)
        Dim b As Integer

        If a = 0 Then
            b = 0
        ElseIf a = 1 Then
            b = 1
        ElseIf a = 2 OrElse a = 3 Then
            b = 2
        Else
            b = 3
        End If
    End Sub
End Class", @"internal partial class TestClass
{
    private void TestMethod(int a)
    {
        int b;

        if (a == 0)
            b = 0;
        else if (a == 1)
            b = 1;
        else if (a == 2 || a == 3)
            b = 2;
        else
            b = 3;
    }
}");
        }

        [Fact]
        public async Task NestedBlockStatementsKeepSameNesting()
        {
            await TestConversionVisualBasicToCSharpWithoutComments(@"Class TestClass
    Shared Function FindTextInCol(w As String, pTitleRow As Integer, startCol As Integer, needle As String) As Integer

        For c As Integer = startCol To w.Length
            If needle = """" Then
                If String.IsNullOrWhiteSpace(w(c).ToString) Then
                    Return c
                End If
            Else
                If w(c).ToString = needle Then
                    Return c
                End If
            End If
        Next
        Return -1
    End Function
End Class", @"internal partial class TestClass
{
    public static int FindTextInCol(string w, int pTitleRow, int startCol, string needle)
    {
        for (int c = startCol, loopTo = w.Length; c <= loopTo; c++)
        {
            if (string.IsNullOrEmpty(needle))
            {
                if (string.IsNullOrWhiteSpace(w[c].ToString()))
                    return c;
            }
            else if ((w[c].ToString() ?? """") == (needle ?? """"))
                return c;
        }
        return -1;
    }
}");
        }

        [Fact]
        public async Task WhileStatement()
        {
            await TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Dim b As Integer
        b = 0

        While b = 0
            If b = 2 Then Continue While
            If b = 3 Then Exit While
            b = 1
        End While
    End Sub
End Class", @"internal partial class TestClass
{
    private void TestMethod()
    {
        int b;
        b = 0;

        while (b == 0)
        {
            if (b == 2)
                continue;
            if (b == 3)
                break;
            b = 1;
        }
    }
}");
        }

        [Fact]
        public async Task UntilStatement()
        {
            //Bug: comment on statement in do loop gets moved to end of conditional
            await TestConversionVisualBasicToCSharpWithoutComments(@"Class TestClass
    Private Sub TestMethod()
        Dim charIndex As Integer
        ' allow only digits and letters
        Do
            charIndex = rand.Next(48, 123)
        Loop Until (charIndex >= 48 AndAlso charIndex <= 57) OrElse (charIndex >= 65 AndAlso charIndex <= 90) OrElse (charIndex >= 97 AndAlso charIndex <= 122)
    End Sub
End Class", @"internal partial class TestClass
{
    private void TestMethod()
    {
        int charIndex;
        // allow only digits and letters
        do
            charIndex = rand.Next(48, 123);
        while ((charIndex < 48 || charIndex > 57) && (charIndex < 65 || charIndex > 90) && (charIndex < 97 || charIndex > 122));
    }
}");
        }

        [Fact]
        public async Task SimpleDoStatement()
        {
            await TestConversionVisualBasicToCSharpWithoutComments(@"Class TestClass
    Private Sub TestMethod()
        Dim b As Integer
        b = 0

        Do
            If b = 2 Then Continue Do
            If b = 3 Then Exit Do
            b = 1
        Loop
    End Sub
End Class", @"internal partial class TestClass
{
    private void TestMethod()
    {
        int b;
        b = 0;

        do
        {
            if (b == 2)
                continue;
            if (b == 3)
                break;
            b = 1;
        }
        while (true);
    }
}");
        }

        [Fact]
        public async Task DoWhileStatement()
        {
            await TestConversionVisualBasicToCSharpWithoutComments(@"Class TestClass
    Private Sub TestMethod()
        Dim b As Integer
        b = 0

        Do
            If b = 2 Then Continue Do
            If b = 3 Then Exit Do
            b = 1
        Loop While b = 0
    End Sub
End Class", @"internal partial class TestClass
{
    private void TestMethod()
    {
        int b;
        b = 0;

        do
        {
            if (b == 2)
                continue;
            if (b == 3)
                break;
            b = 1;
        }
        while (b == 0);
    }
}");
        }

        [Fact]
        public async Task IncompleteStatement()
        {
            await TestConversionVisualBasicToCSharpWithoutComments(@"Class TestClass
    Private Sub TestMethod()
        Dim b As Integer
        b = 0

        Do
            If b = 2 Then Continue Do
            If b = 3 Then Exit Do
            b = 1
        Loop While b = 0
    End Sub
End Class", @"internal partial class TestClass
{
    private void TestMethod()
    {
        int b;
        b = 0;

        do
        {
            if (b == 2)
                continue;
            if (b == 3)
                break;
            b = 1;
        }
        while (b == 0);
    }
}");
        }

        [Fact]
        public async Task ForEachStatementWithExplicitType()
        {
            await TestConversionVisualBasicToCSharpWithoutComments(@"Class TestClass
    Private Sub TestMethod(ByVal values As Integer())
        For Each val As Integer In values
            If val = 2 Then Continue For
            If val = 3 Then Exit For
        Next
    End Sub
End Class", @"internal partial class TestClass
{
    private void TestMethod(int[] values)
    {
        foreach (int val in values)
        {
            if (val == 2)
                continue;
            if (val == 3)
                break;
        }
    }
}");
        }

        [Fact]
        public async Task ForEachStatementWithVar()
        {
            await TestConversionVisualBasicToCSharpWithoutComments(@"Class TestClass
    Private Sub TestMethod(ByVal values As Integer())
        For Each val In values
            If val = 2 Then Continue For
            If val = 3 Then Exit For
        Next
    End Sub
End Class", @"internal partial class TestClass
{
    private void TestMethod(int[] values)
    {
        foreach (var val in values)
        {
            if (val == 2)
                continue;
            if (val == 3)
                break;
        }
    }
}");
        }

        [Fact]
        public async Task TwoForEachStatementsWithImplicitVariableCreation()
        {
            await TestConversionVisualBasicToCSharpWithoutComments(@"Friend Class Program
    Public Shared Sub Main(ByVal args As String())
        For idx = 0 To 10
        Next

        For idx = 0 To 10
        Next
    End Sub
End Class", @"internal partial class Program
{
    public static void Main(string[] args)
    {
        for (int idx = 0; idx <= 10; idx++)
        {
        }

        for (int idx = 0; idx <= 10; idx++)
        {
        }
    }
}");
        }

        [Fact]
        public async Task SyncLockStatement()
        {
            await TestConversionVisualBasicToCSharpWithoutComments(@"Class TestClass
    Private Sub TestMethod(ByVal nullObject As Object)
        If nullObject Is Nothing Then Throw New ArgumentNullException(NameOf(nullObject))

        SyncLock nullObject
            Console.WriteLine(nullObject)
        End SyncLock
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    private void TestMethod(object nullObject)
    {
        if (nullObject == null)
            throw new ArgumentNullException(nameof(nullObject));

        lock (nullObject)
            Console.WriteLine(nullObject);
    }
}");
        }

        [Fact]
        public async Task ForWithSingleStatement()
        {
            // Comment from "Next" gets pushed up to previous line
            await TestConversionVisualBasicToCSharpWithoutComments(@"Class TestClass
    Private Sub TestMethod(end As Integer)
        Dim b, s As Integer()
        For i = 0 To [end]
            b(i) = s(i)
        Next
    End Sub
End Class", @"internal partial class TestClass
{
    private void TestMethod(int end)
    {
        var b = default(int[]), s = default(int[]);
        for (int i = 0, loopTo = end; i <= loopTo; i++)
            b[i] = s[i];
    }
}");
        }

        [Fact]
        public async Task ForNextMutatingField()
        {
            // Comment from "Next" gets pushed up to previous line
            await TestConversionVisualBasicToCSharpWithoutComments(@"Public Class Class1
    Private Index As Integer

    Sub Foo()
        For Me.Index = 0 To 10

        Next
    End Sub
End Class", @"public partial class Class1
{
    private int Index;

    public void Foo()
    {
        for (Index = 0; Index <= 10; Index++)
        {
        }
    }
}");
        }

        [Fact]
        public async Task ForRequiringExtraVariable()
        {
            // Comment from "Next" gets pushed up to previous line
            await TestConversionVisualBasicToCSharpWithoutComments(@"Class TestClass
    Private Sub TestMethod()
        Dim stringValue AS string = ""42""
        For i As Integer = 1 To 10 - stringValue.Length
           stringValue = stringValue & "" "" + Cstr(i)
           Console.WriteLine(stringValue)
        Next
    End Sub
End Class", @"using System;
using Microsoft.VisualBasic.CompilerServices;

internal partial class TestClass
{
    private void TestMethod()
    {
        string stringValue = ""42"";
        for (int i = 1, loopTo = 10 - stringValue.Length; i <= loopTo; i++)
        {
            stringValue = stringValue + "" "" + Conversions.ToString(i);
            Console.WriteLine(stringValue);
        }
    }
}");
        }

        [Fact]
        public async Task ForWithBlock()
        {
            // Comment from "Next" gets pushed up to previous line
            await TestConversionVisualBasicToCSharpWithoutComments(@"Class TestClass
    Private Sub TestMethod([end] As Integer)
        Dim b, s As Integer()
        For i = 0 To [end] - 1
            b(i) = s(i)
        Next
    End Sub
End Class", @"internal partial class TestClass
{
    private void TestMethod(int end)
    {
        var b = default(int[]), s = default(int[]);
        for (int i = 0, loopTo = end - 1; i <= loopTo; i++)
            b[i] = s[i];
    }
}");
        }

        [Fact]
        public async Task LabeledAndForStatement()
        {
            await TestConversionVisualBasicToCSharpWithoutComments(@"Class GotoTest1
    Private Shared Sub Main()
        Dim x As Integer = 200, y As Integer = 4
        Dim count As Integer = 0
        Dim array As String(,) = New String(x - 1, y - 1) {}

        For i As Integer = 0 To x - 1

            For j As Integer = 0 To y - 1
                array(i, j) = (System.Threading.Interlocked.Increment(count)).ToString()
            Next
        Next

        Console.Write(""Enter the number to search for: "")
        Dim myNumber As String = Console.ReadLine()

        For i As Integer = 0 To x - 1

            For j As Integer = 0 To y - 1

                If array(i, j).Equals(myNumber) Then
                    GoTo Found
                End If
            Next
        Next

        Console.WriteLine(""The number {0} was not found."", myNumber)
        GoTo Finish
Found:
        Console.WriteLine(""The number {0} is found."", myNumber)
Finish:
        Console.WriteLine(""End of search."")
        Console.WriteLine(""Press any key to exit."")
        Console.ReadKey()
    End Sub
End Class", @"using System;

internal partial class GotoTest1
{
    private static void Main()
    {
        int x = 200;
        int y = 4;
        int count = 0;
        var array = new string[x - 1 + 1, y - 1 + 1];

        for (int i = 0, loopTo = x - 1; i <= loopTo; i++)
        {
            for (int j = 0, loopTo1 = y - 1; j <= loopTo1; j++)
                array[i, j] = System.Threading.Interlocked.Increment(ref count).ToString();
        }

        Console.Write(""Enter the number to search for: "");
        string myNumber = Console.ReadLine();

        for (int i = 0, loopTo2 = x - 1; i <= loopTo2; i++)
        {
            for (int j = 0, loopTo3 = y - 1; j <= loopTo3; j++)
            {
                if (array[i, j].Equals(myNumber))
                    goto Found;
            }
        }

        Console.WriteLine(""The number {0} was not found."", myNumber);
        goto Finish;
    Found:
        ;
        Console.WriteLine(""The number {0} is found."", myNumber);
    Finish:
        ;
        Console.WriteLine(""End of search."");
        Console.WriteLine(""Press any key to exit."");
        Console.ReadKey();
    }
}");
        }

        [Fact]
        public async Task ThrowStatement()
        {
            await TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod(ByVal nullObject As Object)
        If nullObject Is Nothing Then Throw New ArgumentNullException(NameOf(nullObject))
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    private void TestMethod(object nullObject)
    {
        if (nullObject == null)
            throw new ArgumentNullException(nameof(nullObject));
    }
}");
        }

        [Fact]
        public async Task CallStatement()
        {
            await TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Call (Sub() Console.Write(""Hello""))
        Call (Sub() Console.Write(""Hello""))()
        Call TestMethod
        Call TestMethod()
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    private void TestMethod()
    {
        (() => Console.Write(""Hello""))();
        (() => Console.Write(""Hello""))();
        TestMethod();
        TestMethod();
    }
}");
        }

        [Fact]
        public async Task AddRemoveHandler()
        {
            await TestConversionVisualBasicToCSharp(@"Class TestClass
    Public Event MyEvent As EventHandler

    Private Sub TestMethod(ByVal e As EventHandler)
        AddHandler Me.MyEvent, e
        AddHandler Me.MyEvent, AddressOf MyHandler
    End Sub

    Private Sub TestMethod2(ByVal e As EventHandler)
        RemoveHandler Me.MyEvent, e
        RemoveHandler Me.MyEvent, AddressOf MyHandler
    End Sub

    Private Sub MyHandler(ByVal sender As Object, ByVal e As EventArgs)
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    public event EventHandler MyEvent;

    private void TestMethod(EventHandler e)
    {
        MyEvent += e;
        MyEvent += MyHandler;
    }

    private void TestMethod2(EventHandler e)
    {
        MyEvent -= e;
        MyEvent -= MyHandler;
    }

    private void MyHandler(object sender, EventArgs e)
    {
    }
}");
        }

        [Fact]
        public async Task SelectCase1()
        {
            await TestConversionVisualBasicToCSharpWithoutComments(@"Class TestClass
    Private Sub TestMethod(ByVal number As Integer)
        Select Case number
            Case 0, 1, 2
                Console.Write(""number is 0, 1, 2"")
            Case 5
                Console.Write(""section 5"")
            Case Else
                Console.Write(""default section"")
        End Select
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    private void TestMethod(int number)
    {
        switch (number)
        {
            case 0:
            case 1:
            case 2:
                {
                    Console.Write(""number is 0, 1, 2"");
                    break;
                }

            case 5:
                {
                    Console.Write(""section 5"");
                    break;
                }

            default:
                {
                    Console.Write(""default section"");
                    break;
                }
        }
    }
}");
        }

        [Fact]
        public async Task SelectCaseWithExpression()
        {
            await TestConversionVisualBasicToCSharpWithoutComments(@"Public Class TestClass
    Shared Function TimeAgo(daysAgo As Integer) As String
        Select Case daysAgo
            Case 0 To 3, 4, Is >= 5, Is < 6, Is <= 7
                Return ""this week""
            Case Is > 0
                Return daysAgo \ 7 & "" weeks ago""
            Case Else
                Return ""in the future""
        End Select
    End Function
End Class", @"using Microsoft.VisualBasic.CompilerServices;

public partial class TestClass
{
    public static string TimeAgo(int daysAgo)
    {
        switch (daysAgo)
        {
            case object _ when 0 <= daysAgo && daysAgo <= 3:
            case 4:
            case object _ when daysAgo >= 5:
            case object _ when daysAgo < 6:
            case object _ when daysAgo <= 7:
                {
                    return ""this week"";
                }

            case object _ when daysAgo > 0:
                {
                    return Conversions.ToString(daysAgo / 7) + "" weeks ago"";
                }

            default:
                {
                    return ""in the future"";
                }
        }
    }
}");
        }

        [Fact]
        public async Task SelectCaseWithString()
        {
            await TestConversionVisualBasicToCSharpWithoutComments(@"Public Class TestClass
    Shared Function TimeAgo(x As String) As String
        Select Case UCase(x)
            Case UCase(""a""), UCase(""b"")
                Return ""ab""
            Case UCase(""c"")
                Return ""c""
            Case ""d""
                Return ""d""
            Case Else
                Return ""e""
        End Select
    End Function
End Class", @"using Microsoft.VisualBasic;

public partial class TestClass
{
    public static string TimeAgo(string x)
    {
        switch (Strings.UCase(x))
        {
            case var @case when @case == Strings.UCase(""a""):
            case var case1 when case1 == Strings.UCase(""b""):
                {
                    return ""ab"";
                }

            case var case2 when case2 == Strings.UCase(""c""):
                {
                    return ""c"";
                }

            case ""d"":
                {
                    return ""d"";
                }

            default:
                {
                    return ""e"";
                }
        }
    }
}");
        }

        [Fact]
        public async Task SelectCaseWithExpression2()
        {
            await TestConversionVisualBasicToCSharpWithoutComments(@"Public Class TestClass2
    Function CanDoWork(Something As Object) As Boolean
        Select Case True
            Case Today.DayOfWeek = DayOfWeek.Saturday Or Today.DayOfWeek = DayOfWeek.Sunday
                ' we do not work on weekends
                Return False
            Case Not IsSqlAlive()
                ' Database unavailable
                Return False
            Case TypeOf Something Is Integer
                ' Do something with the Integer
                Return True
            Case Else
                ' Do something else
                Return False
        End Select
    End Function

    Private Function IsSqlAlive() As Boolean
        ' Do something to test SQL Server
        Return True
    End Function
End Class", @"using System;
using Microsoft.VisualBasic;

public partial class TestClass2
{
    public bool CanDoWork(object Something)
    {
        switch (true)
        {
            case object _ when (int)DateAndTime.Today.DayOfWeek == (int)DayOfWeek.Saturday | DateAndTime.Today.DayOfWeek == (int)DayOfWeek.Sunday:
                {
                    // we do not work on weekends
                    return false;
                }

            case object _ when !IsSqlAlive():
                {
                    // Database unavailable
                    return false;
                }

            case object _ when Something is int:
                {
                    // Do something with the Integer
                    return true;
                }

            default:
                {
                    // Do something else
                    return false;
                }
        }
    }

    private bool IsSqlAlive()
    {
        // Do something to test SQL Server
        return true;
    }
}");
        }

        [Fact]
        public async Task TryCatch()
        {
            await TestConversionVisualBasicToCSharpWithoutComments(@"Class TestClass
    Private Shared Function Log(ByVal message As String) As Boolean
        Console.WriteLine(message)
        Return False
    End Function

    Private Sub TestMethod(ByVal number As Integer)
        Try
            Console.WriteLine(""try"")
        Catch e As Exception
            Console.WriteLine(""catch1"")
        Catch
            Console.WriteLine(""catch all"")
        Finally
            Console.WriteLine(""finally"")
        End Try

        Try
            Console.WriteLine(""try"")
        Catch e2 As NotImplementedException
            Console.WriteLine(""catch1"")
        Catch e As Exception When Log(e.Message)
            Console.WriteLine(""catch2"")
        End Try

        Try
            Console.WriteLine(""try"")
        Finally
            Console.WriteLine(""finally"")
        End Try
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    private static bool Log(string message)
    {
        Console.WriteLine(message);
        return false;
    }

    private void TestMethod(int number)
    {
        try
        {
            Console.WriteLine(""try"");
        }
        catch (Exception e)
        {
            Console.WriteLine(""catch1"");
        }
        catch
        {
            Console.WriteLine(""catch all"");
        }
        finally
        {
            Console.WriteLine(""finally"");
        }

        try
        {
            Console.WriteLine(""try"");
        }
        catch (NotImplementedException e2)
        {
            Console.WriteLine(""catch1"");
        }
        catch (Exception e) when (Log(e.Message))
        {
            Console.WriteLine(""catch2"");
        }

        try
        {
            Console.WriteLine(""try"");
        }
        finally
        {
            Console.WriteLine(""finally"");
        }
    }
}");
        }

        [Fact]
        public async Task Yield()
        {
            // Comment from "Next" gets pushed up to previous line
            await TestConversionVisualBasicToCSharpWithoutComments(@"Class TestClass
    Private Iterator Function TestMethod(ByVal number As Integer) As IEnumerable(Of Integer)
        If number < 0 Then Return
        For i As Integer = 0 To number - 1
            Yield i
        Next
    End Function
End Class", @"using System.Collections.Generic;

internal partial class TestClass
{
    private IEnumerable<int> TestMethod(int number)
    {
        if (number < 0)
            yield break;
        for (int i = 0, loopTo = number - 1; i <= loopTo; i++)
            yield return i;
    }
}");
        }
    }
}
