using System.Threading.Tasks;
using CodeConverter.Tests.TestRunners;
using Xunit;

namespace CodeConverter.Tests.VB
{
    public class StandaloneStatementTests : ConverterTestBase
    {
        [Fact]
        public async Task Reassignment()
        {
            await TestConversionCSharpToVisualBasic(
@"int num = 4;
num = 5;",
@"Dim num = 4
num = 5",
expectSurroundingMethodBlock: true);
        }

        [Fact]
        public async Task ObjectMemberInitializerSyntax()
        {
            await TestConversionCSharpToVisualBasic(
@"AttributeUsageAttribute obj = new AttributeUsageAttribute()
{
    AllowMultiple = true,
    Inherited = false
};
obj = null;",
@"Dim obj = New AttributeUsageAttribute With {
    .AllowMultiple = True,
    .Inherited = False
}
obj = Nothing",
                expectSurroundingMethodBlock: true);
        }

        [Fact]
        public async Task AnonymousObjectCreationExpressionSyntax()
        {
            await TestConversionCSharpToVisualBasic(
@"var obj = new
{
    Name = ""Hello"",
    Value = ""World""
};
obj = null;",
@"Dim obj = New With {
    .Name = ""Hello"",
    .Value = ""World""
}
obj = Nothing",
                expectSurroundingMethodBlock: true);
        }

        [Fact]
        public async Task SingleAssigment()
        {
            await TestConversionCSharpToVisualBasic(
@"var x = 3;",
@"Dim x = 3");
        }

        [Fact]
        public async Task SingleFieldDeclaration()
        {
            await TestConversionCSharpToVisualBasic(
@"private int x = 3;",
@"Private x = 3");
        }

        [Fact]
        public async Task SingleEmptyClass()
        {
            await TestConversionCSharpToVisualBasic(
@"public class Test
{
}",
@"Public Class Test
End Class");
        }

        [Fact]
        public async Task SingleAbstractMethod()
        {
            await TestConversionCSharpToVisualBasic(
@"protected abstract void abs();",
@"Protected MustOverride Sub abs()");
        }

        [Fact]
        public async Task SingleEmptyNamespace()
        {
            await TestConversionCSharpToVisualBasic(
@"namespace nam
{
}",
@"Namespace nam
End Namespace");
        }

        [Fact]
        public async Task SingleFieldAssignment()
        {
            await TestConversionCSharpToVisualBasic(
@"this.DataContext = from task in tasks
    where task.Priority == pri
    select task;",
@"Me.DataContext = From task In tasks
                                                                      Where task.Priority Is pri
                                                                      Select task");
        }
    }
}
