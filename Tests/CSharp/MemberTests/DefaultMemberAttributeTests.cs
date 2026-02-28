using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.MemberTests;

public class DefaultMemberAttributeTests : ConverterTestBase
{
    [Fact]
    public async Task TestDefaultMemberAttributeConversionAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"
<System.Reflection.DefaultMember(""Caption"")>
Public Class ClassWithReflectionDefaultMember
    Public Property Caption As String
End Class

<System.Reflection.DefaultMember(NameOf(LoosingProperties.Caption))>
Public Class LoosingProperties
    Public Property Caption As String

    Sub S()
        Dim x = New LoosingProperties()
        x.Caption = ""Hello""

        Dim y = New ClassWithReflectionDefaultMember() 'from C#
        y.Caption = ""World""
    End Sub
End Class", @"using System.Reflection;

[DefaultMember(""Caption"")]
public partial class ClassWithReflectionDefaultMember
{
    public string Caption { get; set; }
}

[DefaultMember(nameof(Caption))]
public partial class LoosingProperties
{
    public string Caption { get; set; }

    public void S()
    {
        var x = new LoosingProperties();
        x.Caption = ""Hello"";

        var y = new ClassWithReflectionDefaultMember(); // from C#
        y.Caption = ""World"";
    }
}
");
    }
}
