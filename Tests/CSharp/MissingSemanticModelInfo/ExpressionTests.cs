using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.MissingSemanticModelInfo
{
    public class ExpressionTests : ConverterTestBase
    {
        [Fact]
        public async Task InvokeIndexerOnPropertyValueAsync()
        {
            // Chances of having an unknown delegate stored as a field/property/local seem lower than having an unknown non-delegate
            // type with an indexer stored, so for a standalone identifier err on the side of assuming it's an indexer
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Public Property SomeProperty As System.Some.UnknownType
    Private Sub TestMethod()
        Dim num = 0
        Dim value = SomeProperty(num)
        value = SomeProperty(0)
    End Sub
End Class", @"
internal partial class TestClass
{
    public System.Some.UnknownType SomeProperty { get; set; }

    private void TestMethod()
    {
        int num = 0;
        var value = SomeProperty[num];
        value = SomeProperty[0];
    }
}
2 source compilation errors:
BC30002: Type 'System.Some.UnknownType' is not defined.
BC32016: 'Public Property SomeProperty As System.Some.UnknownType' has no parameters and its return type cannot be indexed.
1 target compilation errors:
CS0234: The type or namespace name 'Some' does not exist in the namespace 'System' (are you missing an assembly reference?)");
        }
        [Fact]
        public async Task InvokeMethodOnPropertyValueAsync()
        {
            // Chances of having an unknown delegate stored as a field/property/local seem lower than having an unknown non-delegate
            // type with an indexer stored, so for a standalone identifier err on the side of assuming it's an indexer
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Public Property SomeProperty As System.Some.UnknownType
    Private Sub TestMethod()
        Dim value = SomeProperty(New Object())
    End Sub
End Class", @"
internal partial class TestClass
{
    public System.Some.UnknownType SomeProperty { get; set; }

    private void TestMethod()
    {
        var value = SomeProperty(new object());
    }
}
2 source compilation errors:
BC30002: Type 'System.Some.UnknownType' is not defined.
BC32016: 'Public Property SomeProperty As System.Some.UnknownType' has no parameters and its return type cannot be indexed.
2 target compilation errors:
CS0234: The type or namespace name 'Some' does not exist in the namespace 'System' (are you missing an assembly reference?)
CS1955: Non-invocable member 'TestClass.SomeProperty' cannot be used like a method.");
        }

        [Fact]
        public async Task InvokeMethodWithUnknownReturnTypeAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Public Class Class1
    Sub Foo()
        Bar(Nothing)
    End Sub

    Private Function Bar(x As SomeClass) As SomeClass
        Return x
    End Function

End Class", @"
public partial class Class1
{
    public void Foo()
    {
        Bar(default);
    }

    private SomeClass Bar(SomeClass x)
    {
        return x;
    }
}
1 source compilation errors:
BC30002: Type 'SomeClass' is not defined.
1 target compilation errors:
CS0246: The type or namespace name 'SomeClass' could not be found (are you missing a using directive or an assembly reference?)");
        }

        [Fact]
        public async Task ForNextMutatingMissingFieldAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Public Class Class1
    Sub Foo()
        For Me.Index = 0 To 10

        Next
    End Sub
End Class", @"
public partial class Class1
{
    public void Foo()
    {
        for (this.Index = 0; this.Index <= 10; this.Index++)
        {
        }
    }
}
1 source compilation errors:
BC30456: 'Index' is not a member of 'Class1'.
1 target compilation errors:
CS1061: 'Class1' does not contain a definition for 'Index' and no accessible extension method 'Index' accepting a first argument of type 'Class1' could be found (are you missing a using directive or an assembly reference?)");
        }

        [Fact]
        public async Task OutParameterNonCompilingTypeAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Public Class OutParameterWithMissingType
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
End Class", @"using System.Collections.Generic;

public partial class OutParameterWithMissingType
{
    private static void AddToDict(Dictionary<int, MissingType> pDict, int pKey)
    {
        MissingType anInstance = default;
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
        MissingType anInstance = default;
        if (!pDict.TryGetValue(pKey, out anInstance))
        {
            anInstance = new MissingType();
            pDict.Add(pKey, anInstance);
        }
    }
}
1 source compilation errors:
BC30002: Type 'MissingType' is not defined.
1 target compilation errors:
CS0246: The type or namespace name 'MissingType' could not be found (are you missing a using directive or an assembly reference?)");
        }
        [Fact]
        public async Task EnumSwitchAndValWithUnusedMissingTypeAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Public Class EnumAndValTest
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
@"using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

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
        switch (pS.ToUpper() ?? """")
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
                    tS = ((int)pS).ToString();
                    break;
                }
        }

        return tS;
    }
}
1 source compilation errors:
BC30002: Type 'MissingType' is not defined.
1 target compilation errors:
CS0246: The type or namespace name 'MissingType' could not be found (are you missing a using directive or an assembly reference?)");
        }

        [Fact]
        public async Task CastToSameTypeAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Public Class CastToSameTypeTest

    Sub PositionEnumFromString(ByVal c As Char)
        Select Case c
            Case CChar(""."")
                Console.WriteLine(1)
            Case CChar("","")
                Console.WriteLine(2)
        End Select
    End Sub
End Class",
    @"using System;

public partial class CastToSameTypeTest
{
    public void PositionEnumFromString(char c)
    {
        switch (c)
        {
            case '.':
                {
                    Console.WriteLine(1);
                    break;
                }

            case ',':
                {
                    Console.WriteLine(2);
                    break;
                }
        }
    }
}") ;
        }

        [Fact]
        public async Task UnknownTypeInvocationAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private property DefaultDate as System.SomeUnknownType
    private sub TestMethod()
        Dim a = DefaultDate(1, 2, 3).Blawer(1, 2, 3)
    End Sub
End Class", @"
internal partial class TestClass
{
    private System.SomeUnknownType DefaultDate { get; set; }

    private void TestMethod()
    {
        var a = DefaultDate(1, 2, 3).Blawer(1, 2, 3);
    }
}
2 source compilation errors:
BC30002: Type 'System.SomeUnknownType' is not defined.
BC32016: 'Private Property DefaultDate As System.SomeUnknownType' has no parameters and its return type cannot be indexed.
2 target compilation errors:
CS0234: The type or namespace name 'SomeUnknownType' does not exist in the namespace 'System' (are you missing an assembly reference?)
CS1955: Non-invocable member 'TestClass.DefaultDate' cannot be used like a method.");
        }

        [Fact]
        public async Task CharacterizeRaiseEventWithMissingDefinitionActsLikeMultiIndexerAsync()
        {
        await TestConversionVisualBasicToCSharpAsync(
            @"Imports System

    Friend Class TestClass
        Private Sub TestMethod()
            If MyEvent IsNot Nothing Then MyEvent(Me, EventArgs.Empty)
        End Sub
    End Class", @"using System;

internal partial class TestClass
{
    private void TestMethod()
    {
        if (MyEvent is object)
            MyEvent(this, EventArgs.Empty);
    }
}
1 source compilation errors:
BC30451: 'MyEvent' is not declared. It may be inaccessible due to its protection level.
1 target compilation errors:
CS0103: The name 'MyEvent' does not exist in the current context");
        }

        [Fact]
        public async Task ConvertBuiltInMethodWithUnknownArgumentTypeAsync()
        {
        await TestConversionVisualBasicToCSharpAsync(
            @"Class A
    Public Sub Test()
        Dim x As SomeUnknownType = Nothing
        Dim y As Integer = 3
        If IsNothing(x) OrElse IsNothing(y) Then

        End If
    End Sub
End Class", @"using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic

internal partial class A
{
    public void Test()
    {
        SomeUnknownType x = default;
        int y = 3;
        if (Information.IsNothing(x) || Information.IsNothing(y))
        {
        }
    }
}
1 source compilation errors:
BC30002: Type 'SomeUnknownType' is not defined.
1 target compilation errors:
CS0246: The type or namespace name 'SomeUnknownType' could not be found (are you missing a using directive or an assembly reference?)");
        }

        [Fact]
        public async Task CallShouldAlwaysBecomeInvocationAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"Call mySuperFunction(strSomething, , optionalSomething)",
                @"mySuperFunction(strSomething, default, optionalSomething);",
                expectSurroundingBlock: true, missingSemanticInfo: true
            );
        }

    }
}
