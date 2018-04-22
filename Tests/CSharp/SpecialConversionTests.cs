using Xunit;

namespace CodeConverter.Tests.CSharp
{
    public class SpecialConversionTests : ConverterTestBase
    {
        [Fact]
        public void RaiseEvent()
        {
            TestConversionVisualBasicToCSharp(
@"Class TestClass
    Private Event MyEvent As EventHandler

    Private Sub TestMethod()
        RaiseEvent MyEvent(Me, EventArgs.Empty)
    End Sub
End Class", @"using System;

class TestClass
{
    private event EventHandler MyEvent;

    private void TestMethod()
    {
        MyEvent?.Invoke(this, EventArgs.Empty);
    }
}");
        }

        [Fact]
        public void TestCustomEvent()
        {
            // Can't be automatically tested for comments since an extra method is generated
            TestConversionVisualBasicToCSharpWithoutComments(
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

class TestClass45
{
    private event EventHandler backingField;

    public event EventHandler MyEvent
    {
        add
        {
            this.backingField += value;
        }
        remove
        {
            this.backingField -= value;
        }
    }
    void OnMyEvent(object sender, System.EventArgs e)
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
        public void HexAndBinaryLiterals()
        {
        TestConversionVisualBasicToCSharp(
        @"Class Test
    Public CR As Integer = &HD * &B1
End Class", @"class Test
{
    public int CR = 0xD * 0b1;
}");
        }
    }
}
