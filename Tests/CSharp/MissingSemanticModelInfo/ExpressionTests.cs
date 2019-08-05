using System.Threading.Tasks;
using CodeConverter.Tests.TestRunners;
using Xunit;

namespace CodeConverter.Tests.CSharp.MissingSemanticModelInfo
{
    public class ExpressionTests : ConverterTestBase
    {
        [Fact]
        public async Task InvokeIndexerOnPropertyValue()
        {
            // Chances of having an unknown delegate stored as a field/property/local seem lower than having an unknown non-delegate
            // type with an indexer stored, so for a standalone identifier err on the side of assuming it's an indexer
            await TestConversionVisualBasicToCSharp(@"Class TestClass
Public Property SomeProperty As System.Some.UnknownType
    Private Sub TestMethod()
        Dim value = SomeProperty(0)
    End Sub
End Class", @"class TestClass
{
    public System.Some.UnknownType SomeProperty { get; set; }
    private void TestMethod()
    {
        var value = SomeProperty[0];
    }
}");
        }
        [Fact]
        public async Task InvokeMethodWithUnknownReturnType()
        {
            await TestConversionVisualBasicToCSharpWithoutComments(@"Public Class Class1
    Sub Foo()
        Bar(Nothing)
    End Sub

    Private Function Bar(x As SomeClass) As SomeClass
        Return x
    End Function

End Class", @"using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic;

public class Class1
{
    public void Foo()
    {
        Bar(null);
    }

    private SomeClass Bar(SomeClass x)
    {
        return x;
    }
}");
        }

        [Fact]
        public async Task ForNextMutatingMissingField()
        {
            // Comment from "Next" gets pushed up to previous line
            await TestConversionVisualBasicToCSharpWithoutComments(@"Public Class Class1
    Sub Foo()
        For Me.Index = 0 To 10

        Next
    End Sub
End Class", @"public class Class1
{
    public void Foo()
    {
        for (this.Index = 0; this.Index <= 10; this.Index++)
        {
        }
    }
}");
        }
    }
}
