using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.StatementTests;

/// <summary>
/// The simplifier honours the user's "Prefer braces" code style, but must never let an else bind to a different if.
/// </summary>
public class PreferNoBracesTests : ConverterTestBase
{
    [Fact]
    public async Task SingleLineIfAsLastStatementBeforeElseKeepsBracesIssue823Async()
    {
        await TestPreferNoBracesConversionAsync(@"Class TestClass
    Private Sub TestMethod(OUTER As Boolean, FOO1 As Boolean, FOO2 As Boolean)
        If OUTER Then
          If FOO1 Then Bar1() : Exit Sub
        Else 'NOT OUTER
          If FOO2 Then
            Bar2()
            Exit Sub
          End If
        End If 'OUTER
    End Sub
    Private Sub Bar1()
    End Sub
    Private Sub Bar2()
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod(bool OUTER, bool FOO1, bool FOO2)
    {
        if (OUTER)
        {
            if (FOO1)
            {
                Bar1();
                return;
            }
        }
        else if (FOO2) // NOT OUTER
        {
            Bar2();
            return;
        } // OUTER
    }
    private void Bar1()
    {
    }
    private void Bar2()
    {
    }
}");
    }

    [Fact]
    public async Task BracesRemovedWhenOuterIfHasNoElseAsync()
    {
        await TestPreferNoBracesConversionAsync(@"Class TestClass
    Private Sub TestMethod(a As Boolean, b As Boolean)
        If a Then
            If b Then Bar()
        End If
    End Sub
    Private Sub Bar()
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod(bool a, bool b)
    {
        if (a)
            if (b)
                Bar();
    }
    private void Bar()
    {
    }
}");
    }

    [Fact]
    public async Task BracesRemovedWhenInnerIfHasElseAsync()
    {
        await TestPreferNoBracesConversionAsync(@"Class TestClass
    Private Sub TestMethod(a As Boolean, b As Boolean)
        If a Then
            If b Then Bar() Else Baz()
        Else
            Baz()
        End If
    End Sub
    Private Sub Bar()
    End Sub
    Private Sub Baz()
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod(bool a, bool b)
    {
        if (a)
            if (b)
                Bar();
            else
                Baz();
        else
            Baz();
    }
    private void Bar()
    {
    }
    private void Baz()
    {
    }
}");
    }

    [Fact]
    public async Task OnlyOutermostBracesKeptForLoopEndingWithIfBeforeElseAsync()
    {
        await TestPreferNoBracesConversionAsync(@"Class TestClass
    Private Sub TestMethod(a As Boolean, b As Boolean)
        If a Then
            For Each x In {1}
                If b Then Bar()
            Next
        Else
            Bar()
        End If
    End Sub
    Private Sub Bar()
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod(bool a, bool b)
    {
        if (a)
        {
            foreach (var x in new[] { 1 })
                if (b)
                    Bar();
        }
        else
            Bar();
    }
    private void Bar()
    {
    }
}");
    }

    [Fact]
    public async Task ElseIfChainEndingWithoutElseKeepsBracesBeforeElseAsync()
    {
        await TestPreferNoBracesConversionAsync(@"Class TestClass
    Private Sub TestMethod(a As Boolean, b As Boolean, c As Boolean)
        If a Then
            If b Then
                Bar()
            ElseIf c Then
                Bar()
            End If
        Else
            Bar()
        End If
    End Sub
    Private Sub Bar()
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod(bool a, bool b, bool c)
    {
        if (a)
        {
            if (b)
                Bar();
            else if (c)
                Bar();
        }
        else
            Bar();
    }
    private void Bar()
    {
    }
}");
    }

    [Fact]
    public async Task BracesKeptAroundStatementsThatCannotBeEmbeddedAsync()
    {
        await TestPreferNoBracesConversionAsync(@"Class TestClass
    Private Sub TestMethod(a As Boolean)
        If a Then
            Dim x = F()
        Else
            Dim y = F()
        End If
        If a Then
label1:
        End If
    End Sub
    Private Function F() As Integer
        Return 1
    End Function
End Class", @"
internal partial class TestClass
{
    private void TestMethod(bool a)
    {
        if (a)
        {
            int x = F();
        }
        else
        {
            int y = F();
        }
        if (a)
        {
        label1:
            ;

        }
    }
    private int F()
    {
        return 1;
    }
}");
    }

    private Task TestPreferNoBracesConversionAsync(string visualBasicCode, string expectedCsharpCode) =>
        AssertConvertedCodeResultEqualsAsync<VBToCSPreferNoBracesConversion>(visualBasicCode, expectedCsharpCode);
}
