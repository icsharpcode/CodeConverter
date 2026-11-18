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

    [Fact]
    public async Task CommentsOnRemovableBracesKeepBracesAsync()
    {
        await TestPreferNoBracesConversionAsync(@"Class TestClass
    Private Sub TestMethod(a As Boolean, b As Boolean)
        If a Then
            If b Then X() ' c1
        Else ' c2
            Y() ' c3
        End If ' c4
        If a Then
            If b Then ' e0
                X() ' e1
            End If ' e2
        Else
            Y()
        End If
    End Sub
    Private Sub X()
    End Sub
    Private Sub Y()
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod(bool a, bool b)
    {
        if (a)
        {
            if (b)
                X(); // c1
        }
        else // c2
        {
            Y();
        } // c3
          // c4
        if (a)
        {
            if (b) // e0
            {
                X(); // e1
            } // e2
        }
        else
            Y();
    }
    private void X()
    {
    }
    private void Y()
    {
    }
}");
    }

    [Fact]
    public async Task LoopLockAndUsingEndingWithIfKeepBracesBeforeElseAsync()
    {
        await TestPreferNoBracesConversionAsync(@"Class TestClass
    Private Sub TestMethod(a As Boolean, b As Boolean)
        If a Then
            For i = 1 To 2
                If b Then X()
            Next
        Else
            X()
        End If
        If a Then
            SyncLock Me
                If b Then X()
            End SyncLock
        Else
            X()
        End If
        If a Then
            Using New System.IO.MemoryStream()
                If b Then X()
            End Using
        Else
            X()
        End If
    End Sub
    Private Sub X()
    End Sub
End Class", @"using System.IO;

internal partial class TestClass
{
    private void TestMethod(bool a, bool b)
    {
        if (a)
        {
            for (int i = 1; i <= 2; i++)
                if (b)
                    X();
        }
        else
            X();
        if (a)
        {
            lock (this)
                if (b)
                    X();
        }
        else
            X();
        if (a)
        {
            using (new MemoryStream())
                if (b)
                    X();
        }
        else
            X();
    }
    private void X()
    {
    }
}");
    }

    private Task TestPreferNoBracesConversionAsync(string visualBasicCode, string expectedCsharpCode) =>
        AssertConvertedCodeResultEqualsAsync<VBToCSPreferNoBracesConversion>(visualBasicCode, expectedCsharpCode);
}
