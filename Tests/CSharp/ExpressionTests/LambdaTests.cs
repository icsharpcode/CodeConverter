using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.ExpressionTests;

public class LambdaTests : ConverterTestBase
{
    [Fact]
    public async Task Issue1012_MultiLineLambdaWithSingleStatement()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading.Tasks

Public Class ConversionTest3
    Private Class MyEntity
        Property EntityId As Integer
        Property Name As String
    End Class
    Private Sub BugRepro()

        Dim entities As New List(Of MyEntity)

        Parallel.For(1, 3, Sub(i)
                               Dim result As String = (From e In entities
                                                       Where e.EntityId = 123
                                                       Select e.Name).Single
                           End Sub)
    End Sub
End Class", @"using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public partial class ConversionTest3
{
    private partial class MyEntity
    {
        public int EntityId { get; set; }
        public string Name { get; set; }
    }
    private void BugRepro()
    {

        var entities = new List<MyEntity>();

        Parallel.For(1, 3, i =>
        {
            string result = (from e in entities
                             where e.EntityId == 123
                             select e.Name).Single();
        });
    }
}");
    }

    [Fact]
    public async Task SingleLineLambdaBodyIsConvertedToReturnTypeAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod(i As Integer, b As Byte)
        Dim narrowed As Func(Of Byte) = Function() i
        Dim widened As Func(Of Long) = Function() b
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    private void TestMethod(int i, byte b)
    {
        Func<byte> narrowed = () => (byte)i;
        Func<long> widened = () => b;
    }
}");
    }

    [Fact]
    public async Task SingleLineLambdaBodyConversionToEachKindOfReturnTypeAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System.Linq.Expressions
Imports System.Threading.Tasks
Class TestClass
    Private Sub TestMethod(b1 As Byte, b2 As Byte, i As Integer, o As Object, bytes As Byte())
        Dim g As Func(Of Func(Of Integer)) = Function() Function() 1
        Dim g2 As Func(Of Func(Of Integer, Integer)) = Function() Function(x) x
        Dim g3 As Func(Of Action) = Function() Sub() Console.WriteLine()
        Dim g4 As Func(Of Object) = Function() Function() 1
        Dim fo As Func(Of Object) = Function() b1 And b2
        Dim fa As Func(Of Task(Of Byte)) = Async Function() i
        Dim fs As Func(Of String) = Function() i
        Dim fe As Func(Of DayOfWeek) = Function() i
        Dim fd As Func(Of Decimal) = Function() i
        Dim fc As Func(Of Char) = Function() ""a""
        Dim fdt As Func(Of Date) = Function() ""2020-01-01""
        Dim ex As Expression(Of Func(Of Byte)) = Function() i
        Dim ex2 As Expression(Of Func(Of Byte)) = Function() b1 And b2
        Dim ex3 As Expression(Of Func(Of Boolean)) = Function() o = Nothing
        Dim q1 = From x In bytes Select x And b1
        Dim q2 = From x In bytes Where (x And b1) = 0 Select x
        Dim q3 = From x In bytes Group By k = x And b1 Into Group
        Dim q4 = bytes.AsQueryable().Select(Function(x) x And b1)
        Dim q5 = bytes.AsQueryable().Select(Function(x) Not x)
        Dim an = New With {.A = b1 And b2, .B = Not b1}
    End Sub
End Class", @"using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal partial class TestClass
{
    private void TestMethod(byte b1, byte b2, int i, object o, byte[] bytes)
    {
        Func<Func<int>> g = () => () => 1;
        Func<Func<int, int>> g2 = () => x => x;
        Func<Action> g3 = () => () => Console.WriteLine();
        Func<object> g4 = () => new Func<int>(() => 1);
        Func<object> fo = () => (byte)(b1 & b2);
        Func<Task<byte>> fa = async () => (byte)i;
        Func<string> fs = () => i.ToString();
        Func<DayOfWeek> fe = () => (DayOfWeek)i;
        Func<decimal> fd = () => i;
        Func<char> fc = () => 'a';
        Func<DateTime> fdt = () => Conversions.ToDate(""2020-01-01"");
        Expression<Func<byte>> ex = () => (byte)i;
        Expression<Func<byte>> ex2 = () => (byte)(b1 & b2);
        Expression<Func<bool>> ex3 = () => o == default;
        var q1 = from x in bytes
                 select (byte)(x & b1);
        var q2 = from x in bytes
                 where (x & b1) == 0
                 select x;
        var q3 = from x in bytes
                 group x by (byte)(x & b1) into Group
                 select new { k = Group.Key, Group };
        var q4 = bytes.AsQueryable().Select(x => (byte)(x & b1));
        var q5 = bytes.AsQueryable().Select(x => (byte)~x);
        var an = new { A = (byte)(b1 & b2), B = (byte)~b1 };
    }
}");
    }

    [Fact]
    public async Task SingleLineLambdaReturningObjectComparisonIsNotConvertedToBooleanAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System.Linq.Expressions

Class TestClass
    Sub M(rows As Object(), o As Object)
        Dim w = rows.Where(Function(r) r = 5)
        Dim f As Func(Of Boolean) = Function() Not o = 5
        Dim q = rows.AsQueryable().Where(Function(r) r = Nothing AndAlso o = Nothing)
        Dim x = rows.Where(Function(r) (r = 5 OrElse o < 5) AndAlso r IsNot Nothing)
        Dim a = rows.Where(Function(r) r = 5 And o = 5)
        Dim n = rows.Where(Function(r) r)
        Dim e As Expression(Of Func(Of Boolean)) = Function() o = Nothing
    End Sub
End Class", @"using System;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal partial class TestClass
{
    public void M(object[] rows, object o)
    {
        var w = rows.Where(r => Operators.ConditionalCompareObjectEqual(r, 5, false));
        Func<bool> f = () => !Operators.ConditionalCompareObjectEqual(o, 5, false);
        var q = rows.AsQueryable().Where(r => r == default && o == default);
        var x = rows.Where(r => (Operators.ConditionalCompareObjectEqual(r, 5, false) || Operators.ConditionalCompareObjectLess(o, 5, false)) && r is not null);
        var a = rows.Where(r => Conversions.ToBoolean(Operators.AndObject(Operators.ConditionalCompareObjectEqual(r, 5, false), Operators.ConditionalCompareObjectEqual(o, 5, false))));
        var n = rows.Where(r => Conversions.ToBoolean(r));
        Expression<Func<bool>> e = () => o == default;
    }
}");
    }
}
