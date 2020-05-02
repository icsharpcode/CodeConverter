using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using ICSharpCode.CodeConverter.CSharp;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.MemberTests
{
    public class OperatorMemberTests : ConverterTestBase
    {
        [Fact]
        public async Task TestNarrowingWideningConversionOperatorAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Public Class MyInt
    Public Shared Narrowing Operator CType(i As Integer) As MyInt
        Return New MyInt()
    End Operator
    Public Shared Widening Operator CType(myInt As MyInt) As Integer
        Return 1
    End Operator
End Class"
                , @"
public partial class MyInt
{
    public static explicit operator MyInt(int i)
    {
        return new MyInt();
    }

    public static implicit operator int(MyInt myInt)
    {
        return 1;
    }
}");
        }

        [Fact]
        public async Task OperatorOverloadsAsync()
        {
            // Note a couple map to the same thing in C# so occasionally the result won't compile. The user can manually decide what to do in such scenarios.
            await TestConversionVisualBasicToCSharpAsync(@"Public Class AcmeClass
    Public Shared Operator +(i As Integer, ac As AcmeClass) As AcmeClass
        Return ac
    End Operator
    Public Shared Operator &(s As String, ac As AcmeClass) As AcmeClass
        Return ac
    End Operator
    Public Shared Operator -(i As Integer, ac As AcmeClass) As AcmeClass
        Return ac
    End Operator
    Public Shared Operator Not(ac As AcmeClass) As AcmeClass
        Return ac
    End Operator
    Public Shared Operator *(i As Integer, ac As AcmeClass) As AcmeClass
        Return ac
    End Operator
    Public Shared Operator /(i As Integer, ac As AcmeClass) As AcmeClass
        Return ac
    End Operator
    Public Shared Operator \(i As Integer, ac As AcmeClass) As AcmeClass
        Return ac
    End Operator
    Public Shared Operator Mod(s As String, ac As AcmeClass) As AcmeClass
        Return ac
    End Operator
    Public Shared Operator <<(ac As AcmeClass, i As Integer) As AcmeClass
        Return ac
    End Operator
    Public Shared Operator >>(ac As AcmeClass, i As Integer) As AcmeClass
        Return ac
    End Operator
    Public Shared Operator =(s As String, ac As AcmeClass) As AcmeClass
        Return ac
    End Operator
    Public Shared Operator <>(s As String, ac As AcmeClass) As AcmeClass
        Return ac
    End Operator
    Public Shared Operator <(s As String, ac As AcmeClass) As AcmeClass
        Return ac
    End Operator
    Public Shared Operator >(s As String, ac As AcmeClass) As AcmeClass
        Return ac
    End Operator
    Public Shared Operator <=(s As String, ac As AcmeClass) As AcmeClass
        Return ac
    End Operator
    Public Shared Operator >=(s As String, ac As AcmeClass) As AcmeClass
        Return ac
    End Operator
    Public Shared Operator And(s As String, ac As AcmeClass) As AcmeClass
        Return ac
    End Operator
    Public Shared Operator Or(s As String, ac As AcmeClass) As AcmeClass
        Return ac
    End Operator
End Class", @"
public partial class AcmeClass
{
    public static AcmeClass operator +(int i, AcmeClass ac)
    {
        return ac;
    }

    public static AcmeClass operator +(string s, AcmeClass ac)
    {
        return ac;
    }

    public static AcmeClass operator -(int i, AcmeClass ac)
    {
        return ac;
    }

    public static AcmeClass operator !(AcmeClass ac)
    {
        return ac;
    }

    public static AcmeClass operator *(int i, AcmeClass ac)
    {
        return ac;
    }

    public static AcmeClass operator /(int i, AcmeClass ac)
    {
        return ac;
    }

    public static AcmeClass operator /(int i, AcmeClass ac)
    {
        return ac;
    }

    public static AcmeClass operator %(string s, AcmeClass ac)
    {
        return ac;
    }

    public static AcmeClass operator <<(AcmeClass ac, int i)
    {
        return ac;
    }

    public static AcmeClass operator >>(AcmeClass ac, int i)
    {
        return ac;
    }

    public static AcmeClass operator ==(string s, AcmeClass ac)
    {
        return ac;
    }

    public static AcmeClass operator !=(string s, AcmeClass ac)
    {
        return ac;
    }

    public static AcmeClass operator <(string s, AcmeClass ac)
    {
        return ac;
    }

    public static AcmeClass operator >(string s, AcmeClass ac)
    {
        return ac;
    }

    public static AcmeClass operator <=(string s, AcmeClass ac)
    {
        return ac;
    }

    public static AcmeClass operator >=(string s, AcmeClass ac)
    {
        return ac;
    }

    public static AcmeClass operator &(string s, AcmeClass ac)
    {
        return ac;
    }

    public static AcmeClass operator |(string s, AcmeClass ac)
    {
        return ac;
    }
}
1 target compilation errors:
CS0111: Type 'AcmeClass' already defines a member called 'op_Division' with the same parameter types");
        }

        [Fact]// The stack trace displayed will change from time to time. Feel free to update this characterization test appropriately.
        public async Task OperatorOverloadsWithNoCSharpEquivalentShowErrorInlineCharacterizationAsync()
        {
            // No valid conversion to C# - to implement this you'd need to create a new method, and convert all callers to use it.
            var convertedCode = await ConvertAsync<VBToCSConversion>(@"Public Class AcmeClass
    Public Shared Operator ^(i As Integer, ac As AcmeClass) As AcmeClass
        Return ac
    End Operator
    Public Shared Operator Like(s As String, ac As AcmeClass) As AcmeClass
        Return ac
    End Operator
End Class");

            Assert.Contains("Cannot convert", convertedCode);
            Assert.Contains("#error", convertedCode);
            Assert.Contains("_failedMemberConversionMarker1", convertedCode);
            Assert.Contains("Public Shared Operator ^(i As Integer,", convertedCode);
            Assert.Contains("_failedMemberConversionMarker2", convertedCode);
            Assert.Contains("Public Shared Operator Like(s As String,", convertedCode);
        }
    }
}
