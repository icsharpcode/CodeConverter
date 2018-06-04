using Xunit;

namespace CodeConverter.Tests.VB
{
    public class StandaloneMultiStatementTests : ConverterTestBase
    {
        [Fact]
        public void Reassignment()
        {
            TestConversionCSharpToVisualBasic(
@"int num = 4;
num = 5;",
@"Dim num As Integer = 4
num = 5",
expectSurroundingMethodBlock: true);
        }

        [Fact]
        public void ObjectMemberInitializerSyntax()
        {
            TestConversionCSharpToVisualBasic(
@"AttributeUsageAttribute obj = new AttributeUsageAttribute()
{
    AllowMultiple = true,
    Inherited = false
};
obj = null;",
@"Dim obj As AttributeUsageAttribute = New AttributeUsageAttribute() With {
    .AllowMultiple = True,
    .Inherited = False
}
obj = Nothing",
                expectSurroundingMethodBlock: true);
        }

        [Fact]
        public void AnonymousObjectCreationExpressionSyntax()
        {
            TestConversionCSharpToVisualBasic(
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
        public void SingleAssigment()
        {
            TestConversionCSharpToVisualBasic(
@"var x = 3;",
@"Dim x = 3");
        }

        [Fact]
        public void SingleFieldDeclaration()
        {
            TestConversionCSharpToVisualBasic(
@"private int x = 3;",
@"Private x As Integer = 3");
        }

        [Fact]
        public void SingleEmptyClass()
        {
            TestConversionCSharpToVisualBasic(
@"public class Test
{
}",
@"Public Class Test
End Class");
        }

        [Fact]
        public void SingleAbstractMethod()
        {
            TestConversionCSharpToVisualBasic(
@"protected abstract void abs();",
@"Protected MustOverride Sub abs()");
        }

        [Fact]
        public void SingleEmptyNamespace()
        {
            TestConversionCSharpToVisualBasic(
@"namespace nam
{
}",
@"Namespace nam
End Namespace");
        }

        [Fact]
        public void SingleUsing()
        {
            TestConversionCSharpToVisualBasic(
@"using s = System.String;",
@"Imports s = System.String");
        }
    }
}
