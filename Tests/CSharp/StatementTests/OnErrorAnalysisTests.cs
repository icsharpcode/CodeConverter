using System.Collections.Generic;
using System.Linq;
using ICSharpCode.CodeConverter.Common;
using ICSharpCode.CodeConverter.CSharp.OnError;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Xunit;
using CSharpSyntaxFactory = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace ICSharpCode.CodeConverter.Tests.CSharp.StatementTests;

/// <summary>
/// The analysis classes behind the On Error shapes, tested directly where the conversion tests can't reach a branch.
/// </summary>
public class OnErrorAnalysisTests
{
    [Theory]
    [InlineData("l: return;", false)]
    [InlineData("l: x();", true)]
    [InlineData("if (a) return; else throw new System.Exception();", false)]
    [InlineData("if (a) return; else x();", true)]
    [InlineData("if (a) return;", true)]
    [InlineData("try { return; } catch { throw; }", false)]
    [InlineData("try { return; } catch { x(); }", true)]
    [InlineData("try { x(); } catch { return; }", true)]
    [InlineData("using (a) return;", false)]
    [InlineData("using (a) x();", true)]
    [InlineData("lock (a) return;", false)]
    [InlineData("{ x(); goto l; }", false)]
    [InlineData("l: { x(); return; }", false)]
    [InlineData("lock (a) x();", true)]
    [InlineData("lock (a) { x(); throw new System.Exception(); }", false)]
    [InlineData("using (a) { x(); throw new System.Exception(); }", false)]
    [InlineData("try { x(); } catch { y(); }", true)]
    [InlineData("try { throw new System.Exception(); } catch { return; } finally { z(); }", false)]
    [InlineData("try { return; } catch (System.Exception) { return; }", false)]
    [InlineData("switch (a) { case 1: return; default: return; }", false)]
    [InlineData("switch (a) { case 1: return; default: x(); break; }", true)]
    [InlineData("switch (a) { case 1: return; }", true)]
    [InlineData("switch (a) { case 1: return; default: if (b) break; return; }", true)]
    [InlineData("switch (a) { case 1: return; default: while (b) { break; } return; }", false)]
    [InlineData("switch (a) { case 1: return; default: switch (b) { default: break; } return; }", false)]
    [InlineData("switch (a) { case 1: return; default: foreach (var q in b) { break; } return; }", false)]
    [InlineData("switch (a) { case 1: return; default: x(); y(); }", true)]
    [InlineData("switch (a) { case int n when n > 1: return; }", true)]
    public void EndIsReachableIsDecidedSyntactically(string csharp, bool expected)
    {
        Assert.Equal(expected, StatementReachability.EndIsReachable(new[] { CSharpSyntaxFactory.ParseStatement(csharp) }));
    }

    [Fact]
    public void StatementsAfterTheFirstThatLeavesAreDropped()
    {
        var statements = new[] { "x();", "return;", "y();" }.Select(s => CSharpSyntaxFactory.ParseStatement(s));
        Assert.Equal(new[] { "x();", "return;" }, StatementReachability.UpToFirstLeaving(statements).Select(s => s.ToString()));
    }

    [Theory]
    [InlineData("a < b", false)]
    [InlineData("(a < b)", false)]
    [InlineData("Not (a = b)", false)]
    [InlineData("Not flag", false)]
    [InlineData("Not (Not flag)", false)]
    [InlineData("(a < b) And Not flag", false)]
    [InlineData("a < Foo()", true)]
    [InlineData("Not (Foo() = 1)", true)]
    [InlineData("s = \"x\"", true)]
    public void ConditionsOverLocalsAndParametersCantFail(string condition, bool canThrow)
    {
        var method = Parse($@"Class C
    Function Foo() As Integer
        Return 1
    End Function
    Sub M(a As Integer, b As Integer, flag As Boolean, s As String)
        If {condition} Then Foo()
    End Sub
End Class", out var model).Single(m => m.SubOrFunctionStatement.Identifier.Text == "M");
        var ifStatement = method.DescendantNodes().OfType<SingleLineIfStatementSyntax>().Single();
        Assert.Equal(canThrow, ThrowAnalysis.CanThrow(ifStatement.Condition, model));
    }

    /// <summary>The generated C# is unchecked, so integer addition, subtraction and multiplication of locals and constants can't throw.</summary>
    [Theory]
    [InlineData("i += 1", false)]
    [InlineData("i -= j", false)]
    [InlineData("i *= 2", false)]
    [InlineData("i = i + 1", false)]
    [InlineData("i = j - 1", false)]
    [InlineData("i = (i + j) * 2", false)]
    [InlineData("l += 1", false)]
    [InlineData("l = l + 1", false)]
    [InlineData("ui += 1UI", false)]
    [InlineData("i = (i + (j * 2)) - 1", false)]
    [InlineData("i = (i + (j * Foo())) - 1", true)]
    [InlineData("i = (i + (j * l)) - 1", true)]
    [InlineData("i = (i + fieldI) - 1", true)]
    [InlineData("l = i * j", true)]
    [InlineData("l = i + 1", true)]
    [InlineData("l = (i + j) * 2", true)]
    [InlineData("i = l + 1", true)]
    [InlineData("e += 1", true)]
    [InlineData("e = e + 1", true)]
    [InlineData("e = CType(i + 1, System.DayOfWeek)", true)]
    [InlineData("fieldL = fieldL + 1", true)]
    [InlineData("i = Foo()", true)]
    [InlineData("i = Foo() + 1", true)]
    [InlineData("i = (Foo())", true)]
    [InlineData("i /= 2", true)]
    [InlineData("i \\= 2", true)]
    [InlineData("i = i \\ j", true)]
    [InlineData("i = i Mod j", true)]
    [InlineData("i = i ^ 2", true)]
    [InlineData("i = i / j", true)]
    [InlineData("d += 1", true)]
    [InlineData("d = d + 1", true)]
    [InlineData("m += 1", true)]
    [InlineData("sg += 1", true)]
    [InlineData("s += \"a\"", true)]
    [InlineData("i += Foo()", true)]
    [InlineData("i = i + Foo()", true)]
    [InlineData("i += l", true)]
    [InlineData("b += 1", true)]
    [InlineData("i += o", true)]
    [InlineData("fieldI += 1", true)]
    [InlineData("i += fieldI", true)]
    public void IntegerArithmeticOnLocalsCantFailBecauseTheOutputIsUnchecked(string statement, bool canThrow)
    {
        var method = Parse($@"Class C
    Private fieldI As Integer
    Private fieldL As Long
    Function Foo() As Integer
        Return 1
    End Function
    Sub M(o As Object)
        Dim i, j As Integer
        Dim l As Long
        Dim ui As UInteger
        Dim e As System.DayOfWeek
        Dim b As Byte
        Dim d As Double
        Dim sg As Single
        Dim m As Decimal
        Dim s As String
        {statement}
    End Sub
End Class", out var model).Single(m => m.SubOrFunctionStatement.Identifier.Text == "M");
        Assert.Equal(canThrow, ThrowAnalysis.CanThrow(method.Statements.Last(), model));
    }

    [Fact]
    public void HandlerFunctionIsNotPlannedWhenSomethingJumpsToTheHandlerLabel()
    {
        var statements = MethodBody(@"Sub M(flag As Boolean)
        On Error GoTo H
        Foo()
        If flag Then GoTo H
        Exit Sub
H:
        Resume Next
    End Sub", out var model);
        var onErrorModel = OnErrorModel.Create(statements, model);
        Assert.False(onErrorModel.IsDynamic);
        Assert.Null(HandlerFunctionPlanner.TryPlan(onErrorModel, model));
    }

    [Fact]
    public void HandlerFunctionIsPlannedWithoutTheJump()
    {
        var statements = MethodBody(@"Sub M(flag As Boolean)
        On Error GoTo H
        Foo()
        Exit Sub
H:
        Resume Next
    End Sub", out var model);
        Assert.NotNull(HandlerFunctionPlanner.TryPlan(OnErrorModel.Create(statements, model), model));
    }

    [Fact]
    public void StructuredPlanIsNotMadeWhenTheHandlerResumesToALabelBeforeOnError()
    {
        var statements = MethodBody(@"Sub M()
Start:
        On Error GoTo H
        Foo()
        Exit Sub
H:
        Resume Start
    End Sub", out var model);
        Assert.Null(StructuredPlanner.TryPlan(OnErrorModel.Create(statements, model), model));
    }

    [Fact]
    public void StructuredPlanIsMadeWhenTheHandlerResumesToALabelAfterOnError()
    {
        var statements = MethodBody(@"Sub M()
        On Error GoTo H
        Foo()
Done:
        Exit Sub
H:
        Resume Done
    End Sub", out var model);
        Assert.NotNull(StructuredPlanner.TryPlan(OnErrorModel.Create(statements, model), model));
    }

    [Theory]
    [InlineData("On Error GoTo 0", true)]
    [InlineData("On Error Resume Next", true)]
    [InlineData("Exit For", true)]
    [InlineData("Lbl:", true)]
    [InlineData("Resume Next", false)]
    [InlineData("Resume", false)]
    [InlineData("GoTo Lbl", false)]
    [InlineData("Return", false)]
    [InlineData("Exit Sub", false)]
    [InlineData("Throw New System.Exception()", false)]
    [InlineData("End", false)]
    public void HandlerEndingInThisStatementRunsOffItsEndOnlyIfTheStatementDoesntLeave(string statement, bool reachable)
    {
        Parse($@"Class C
    Sub M()
        For i = 1 To 2
            {statement}
Lbl:
        Next
    End Sub
End Class", out var model);
        var first = model.SyntaxTree.GetRoot().DescendantNodes().OfType<ForBlockSyntax>().Single().Statements.First();
        Assert.Equal(reachable, OnErrorModel.EndPointIsReachable(first, model));
    }

    private static IReadOnlyList<StatementSyntax> MethodBody(string method, out SemanticModel model)
    {
        var methods = Parse($@"Class C
    Sub Foo()
    End Sub
    {method}
End Class", out model);
        return methods.Single(x => x.SubOrFunctionStatement.Identifier.Text == "M").Statements.ToArray();
    }

    private static IEnumerable<MethodBlockSyntax> Parse(string code, out SemanticModel model)
    {
        var tree = VisualBasicSyntaxTree.ParseText(code);
        var compilation = VisualBasicCompilation.Create("t", new[] { tree }, DefaultReferences.With(), new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithOptionStrict(OptionStrict.Off));
        model = compilation.GetSemanticModel(tree);
        return tree.GetRoot().DescendantNodes().OfType<MethodBlockSyntax>().ToArray();
    }
}
