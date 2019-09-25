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
End Class", @"internal partial class TestClass
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
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic;

public partial class Class1
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
End Class", @"public partial class Class1
{
    public void Foo()
    {
        for (this.Index = 0; this.Index <= 10; this.Index++)
        {
        }
    }
}");
        }

        [Fact]
        public async Task OutParameterNonCompilingType()
        {
            await TestConversionVisualBasicToCSharp(@"Public Class OutParameterWithMissingType
    Private Shared Sub AddToDict(ByVal pDict As Dictionary(Of Integer, MissingType), ByVal pKey As Integer)
        Dim anInstance As MissingType = Nothing
        If Not pDict.TryGetValue(pKey, anInstance) Then
            anInstance = New MissingType
            pDict.Add(pKey, anInstance)
        End If
    End Sub
End Class

Public Class OutParameterWithNonCompilingType
    Private Shared Sub AddToDict(ByVal pDict As Dictionary(Of OutParameterWithMissingType, MissingType), ByVal pKey As OutParameterWithMissingType)
        Dim anInstance As MissingType = Nothing
        If Not pDict.TryGetValue(pKey, anInstance) Then
            anInstance = New MissingType
            pDict.Add(pKey, anInstance)
        End If
    End Sub
End Class", @"using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic;

public partial class OutParameterWithMissingType
{
    private static void AddToDict(Dictionary<int, MissingType> pDict, int pKey)
    {
        MissingType anInstance = null;
        if (!pDict.TryGetValue(pKey, out anInstance))
        {
            anInstance = new MissingType();
            pDict.Add(pKey, anInstance);
        }
    }
}

public partial class OutParameterWithNonCompilingType
{
    private static void AddToDict(Dictionary<OutParameterWithMissingType, MissingType> pDict, OutParameterWithMissingType pKey)
    {
        MissingType anInstance = null;
        if (!pDict.TryGetValue(pKey, out anInstance))
        {
            anInstance = new MissingType();
            pDict.Add(pKey, anInstance);
        }
    }
}");
        }
        [Fact]
        public async Task EnumSwitchAndValWithUnusedMissingType()
        {
            // BUG: Stop comments appearing before colon in case statement
            await TestConversionVisualBasicToCSharpWithoutComments(@"Public Class EnumAndValTest
    Public Enum PositionEnum As Integer
        None = 0
        LeftTop = 1
    End Enum

    Public TitlePosition As PositionEnum = PositionEnum.LeftTop
    Public TitleAlign As PositionEnum = 2
    Public Ratio As Single = 0

    Function PositionEnumFromString(ByVal pS As String, missing As MissingType) As PositionEnum
        Dim tPos As PositionEnum
        Select Case pS.ToUpper
            Case ""NONE"", ""0""
                tPos = 0
            Case ""LEFTTOP"", ""1""
                tPos = 1
            Case Else
                Ratio = Val(pS)
        End Select
        Return tPos
    End Function
    Function PositionEnumStringFromConstant(ByVal pS As PositionEnum) As String
        Dim tS As String
        Select Case pS
            Case 0
                tS = ""NONE""
            Case 1
                tS = ""LEFTTOP""
            Case Else
                tS = pS
        End Select
        Return tS
    End Function
End Class",
@"using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

public partial class EnumAndValTest
{
    public enum PositionEnum : int
    {
        None = 0,
        LeftTop = 1
    }

    public PositionEnum TitlePosition = PositionEnum.LeftTop;
    public PositionEnum TitleAlign = (PositionEnum)2;
    public float Ratio = 0;

    public PositionEnum PositionEnumFromString(string pS, MissingType missing)
    {
        var tPos = default(PositionEnum);
        switch (pS.ToUpper())
        {
            case ""NONE"":
            case ""0"":
                {
                    tPos = 0;
                    break;
                }

            case ""LEFTTOP"":
            case ""1"":
                {
                    tPos = (PositionEnum)1;
                    break;
                }

            default:
                {
                    Ratio = Conversions.ToSingle(Conversion.Val(pS));
                    break;
                }
        }
        return tPos;
    }
    public string PositionEnumStringFromConstant(PositionEnum pS)
    {
        string tS;
        switch (pS)
        {
            case 0:
                {
                    tS = ""NONE"";
                    break;
                }

            case (PositionEnum)1:
                {
                    tS = ""LEFTTOP"";
                    break;
                }

            default:
                {
                    tS = Conversions.ToString(pS);
                    break;
                }
        }
        return tS;
    }
}");
        }

        [Fact]
        public async Task UnknownTypeInvocation()
        {
            await TestConversionVisualBasicToCSharp(@"Class TestClass
    Private property DefaultDate as System.SomeUnknownType
    private sub TestMethod()
        Dim a = DefaultDate(1, 2, 3).Blawer(1, 2, 3)
    End Sub
End Class", @"internal partial class TestClass
{
    private System.SomeUnknownType DefaultDate { get; set; }
    private void TestMethod()
    {
        var a = DefaultDate[1, 2, 3].Blawer(1, 2, 3);
    }
}");
        }

    [Fact]
    public async Task CharacterizeRaiseEventWithMissingDefinitionActsLikeFunc()
    {
    await TestConversionCSharpToVisualBasic(
        @"using System;

class TestClass
{
    void TestMethod()
    {
        if (MyEvent != null) MyEvent(this, EventArgs.Empty);
    }
}", @"Imports System

Friend Class TestClass
    Private Sub TestMethod()
        If MyEvent IsNot Nothing Then MyEvent(Me, EventArgs.Empty)
    End Sub
End Class");
        }
    }
}
