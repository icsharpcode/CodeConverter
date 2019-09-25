using System.Threading.Tasks;
using CodeConverter.Tests.TestRunners;
using Xunit;

namespace CodeConverter.Tests.CSharp
{
    public class SpecialConversionTests : ConverterTestBase
    {
        [Fact]
        public async Task RaiseEvent()
        {
            await TestConversionVisualBasicToCSharp(
@"Class TestClass
    Private Event MyEvent As EventHandler

    Private Sub TestMethod()
        RaiseEvent MyEvent(Me, EventArgs.Empty)
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    private event EventHandler MyEvent;

    private void TestMethod()
    {
        MyEvent?.Invoke(this, EventArgs.Empty);
    }
}");
        }

        [Fact]
        public async Task TestCustomEvent()
        {
            // Can't be automatically tested for comments since an extra method is generated
            await TestConversionVisualBasicToCSharpWithoutComments(
                @"Class TestClass45
    Private Event backingField As EventHandler

    Public Custom Event MyEvent As EventHandler
        AddHandler(ByVal value As EventHandler)
            AddHandler Me.backingField, value
        End AddHandler
        RemoveHandler(ByVal value As EventHandler)
            RemoveHandler Me.backingField, value
        End RemoveHandler
        RaiseEvent(ByVal sender As Object, ByVal e As System.EventArgs)
            Console.WriteLine(""Event Raised"")
        End RaiseEvent
    End Event

    Public Sub RaiseCustomEvent()
        RaiseEvent MyEvent(Me, EventArgs.Empty)
    End Sub
End Class", @"using System;

internal partial class TestClass45
{
    private event EventHandler backingField;

    public event EventHandler MyEvent
    {
        add
        {
            backingField += value;
        }
        remove
        {
            backingField -= value;
        }
    }
    void OnMyEvent(object sender, EventArgs e)
    {
        Console.WriteLine(""Event Raised"");
    }

    public void RaiseCustomEvent()
    {
        OnMyEvent(this, EventArgs.Empty);
    }
}");
        }

        [Fact]
        public async Task HexAndBinaryLiterals()
        {
        await TestConversionVisualBasicToCSharp(
        @"Class Test
    Public CR As Integer = &HD * &B1
End Class", @"internal partial class Test
{
    public int CR = 0xD * 0b1;
}");
        }
    }
}
