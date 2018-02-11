using Xunit;

namespace CodeConverter.Tests.CSharp
{
    public class StandaloneMultiStatementTests : ConverterTestBase
    {
        [Fact]
        public void Reassignment()
        {
            TestConversionVisualBasicToCSharp(
@"Dim num as Integer = 4
num = 5",
@"int num = 4;
num = 5;",
standaloneStatements: true);
        }

        [Fact]
        public void ObjectMemberInitializerSyntax()
        {
            TestConversionVisualBasicToCSharp(
@"Dim obj as New AttributeUsageAttribute With
{
    .AllowMultiple = True,
    .Inherited = False
}
obj = Nothing",
@"AttributeUsageAttribute obj = new AttributeUsageAttribute()
{
    AllowMultiple = true,
    Inherited = false
};
obj = null;",
                standaloneStatements: true);
        }


        [Fact]
        public void AnonymousObjectCreationExpressionSyntax()
        {
            TestConversionVisualBasicToCSharp(
@"Dim obj = New With
{
    .Name = ""Hello"",
    .Value = ""World""
}
obj = Nothing",
@"var obj = new
{
    Name = ""Hello"",
    Value = ""World""
};
obj = null;",
                standaloneStatements: true);
        }
    }
}
