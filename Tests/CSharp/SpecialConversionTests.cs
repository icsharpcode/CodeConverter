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
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualBasic;

class TestClass
{
    private event EventHandler MyEvent;

    private void TestMethod()
    {
        MyEvent?.Invoke(this, EventArgs.Empty);
    }
}");
		}
	}
}
