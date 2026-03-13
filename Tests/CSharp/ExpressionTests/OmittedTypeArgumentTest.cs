using System;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.ExpressionTests;

public class OmittedTypeArgumentTest : ConverterTestBase
{
    [Fact]
    public async Task TestGetTypeOmittedArgument()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class Test
    Public Function IsNullable(ByVal type As Type) As Boolean
        Return type.IsGenericType AndAlso type.GetGenericTypeDefinition() Is GetType(Nullable(Of))
    End Function
End Class", @"using System;

public partial class Test
{
    public bool IsNullable(Type @type)
    {
        return type.IsGenericType && ReferenceEquals(type.GetGenericTypeDefinition(), typeof(Nullable<>));
    }
}");
    }
}
