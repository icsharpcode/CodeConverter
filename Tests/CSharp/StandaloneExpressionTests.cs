using Xunit;

namespace CodeConverter.Tests.CSharp
{
    public class StandaloneExpressionTests : ConverterTestBase
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
@"Dim obj as New NameValue With
{
    .Name = ""Hello"",
    .Value = ""World""
}",
@"NameValue obj = new NameValue
{
    Name = ""Hello"",
    Value = ""World""
};",
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
