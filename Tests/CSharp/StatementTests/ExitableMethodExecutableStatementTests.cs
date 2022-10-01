using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.StatementTests;

/// <summary>
/// Covers:
/// Exit { Do | For | Select | Try | While } https://docs.microsoft.com/en-us/dotnet/visual-basic/language-reference/statements/exit-statement
/// Continue { Do | For | While } https://docs.microsoft.com/en-us/dotnet/visual-basic/language-reference/statements/continue-statement
///
/// Does not cover:
/// Exit { Function | Property | Sub } since they are not MethodExecutableStatements
/// </summary>
public class ExitableMethodExecutableStatementTests : ConverterTestBase
{
    [Fact]
    public async Task WhileStatementAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        Dim b As Integer
        b = 0

        While b = 0
            If b = 2 Then Continue While
            If b = 3 Then Exit While
            b = 1
        End While
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod()
    {
        int b;
        b = 0;

        while (b == 0)
        {
            if (b == 2)
                continue;
            if (b == 3)
                break;
            b = 1;
        }
    }
}");
    }

    [Fact]
    public async Task SimpleDoStatementAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        Dim b As Integer
        b = 0

        Do
            If b = 2 Then Continue Do
            If b = 3 Then Exit Do
            b = 1
        Loop
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod()
    {
        int b;
        b = 0;

        do
        {
            if (b == 2)
                continue;
            if (b == 3)
                break;
            b = 1;
        }
        while (true);
    }
}");
    }

    [Fact]
    public async Task DoWhileStatementAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        Dim b As Integer
        b = 0

        Do
            If b = 2 Then Continue Do
            If b = 3 Then Exit Do
            b = 1
        Loop While b = 0
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod()
    {
        int b;
        b = 0;

        do
        {
            if (b == 2)
                continue;
            if (b == 3)
                break;
            b = 1;
        }
        while (b == 0);
    }
}");
    }

    [Fact]
    public async Task ForEachStatementWithExplicitTypeAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod(ByVal values As Integer())
        For Each v As Integer In values
            If v = 2 Then Continue For
            If v = 3 Then Exit For
        Next
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod(int[] values)
    {
        foreach (int v in values)
        {
            if (v == 2)
                continue;
            if (v == 3)
                break;
        }
    }
}");
    }

    [Fact]
    public async Task ForEachStatementWithVarAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod(ByVal values As Integer())
        For Each v In values
            If v = 2 Then Continue For
            If v = 3 Then Exit For
        Next
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod(int[] values)
    {
        foreach (var v in values)
        {
            if (v == 2)
                continue;
            if (v == 3)
                break;
        }
    }
}");
    }

    [Fact]
    public async Task ForEachStatementWithUsedOuterDeclarationAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod(ByVal values As Integer())
        Dim val As Integer
        For Each val In values
            If val = 2 Then Continue For
            If val = 3 Then Exit For
        Next

        Console.WriteLine(val)
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    private void TestMethod(int[] values)
    {
        var val = default(int);
        foreach (var currentVal in values)
        {
            val = currentVal;
            if (val == 2)
                continue;
            if (val == 3)
                break;
        }

        Console.WriteLine(val);
    }
}");
    }

    [Fact]
    public async Task ForEachStatementWithFieldVarUsedOuterDeclarationAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Dim val As Integer

    Private Sub TestMethod(ByVal values As Integer())
        For Each val In values
            If val = 2 Then Continue For
            If val = 3 Then Exit For
        Next

        Console.WriteLine(val)
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    private int val;

    private void TestMethod(int[] values)
    {
        foreach (var currentVal in values)
        {
            val = currentVal;
            if (val == 2)
                continue;
            if (val == 3)
                break;
        }

        Console.WriteLine(val);
    }
}");
    }

    [Fact]
    public async Task ForEachStatementWithUnusedOuterDeclarationAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod(ByVal values As Integer())
        Dim val As Integer
        For Each val In values
            If val = 2 Then Continue For
            If val = 3 Then Exit For
        Next
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod(int[] values)
    {
        foreach (var val in values)
        {
            if (val == 2)
                continue;
            if (val == 3)
                break;
        }
    }
}");
    }

    [Fact]
    public async Task ForEachStatementWithFieldVarUnusedOuterDeclarationAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Dim val As Integer
    Private Sub TestMethod(ByVal values As Integer())
        For Each val In values
            If val = 2 Then Continue For
            If val = 3 Then Exit For
        Next
    End Sub
End Class", @"
internal partial class TestClass
{
    private int val;
    private void TestMethod(int[] values)
    {
        foreach (var currentVal in values)
        {
            val = currentVal;
            if (val == 2)
                continue;
            if (val == 3)
                break;
        }
    }
}");
    }

    [Fact]
    public async Task ForEachStatementWithUnusedNestedDeclarationAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod(ByVal values As Integer())
        Dim inline1, inline2, keep1, keep2 As Integer
        For Each inline1 In values
            For Each keep1 In values
                For Each inline2 In values
                    If inline2 = 2 Then Continue For
                    If inline2 = 3 Then Exit For
                Next
            Next
            Console.WriteLine(keep1)
        Next
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    private void TestMethod(int[] values)
    {
        int keep1 = default, keep2;
        foreach (var inline1 in values)
        {
            foreach (var currentKeep1 in values)
            {
                keep1 = currentKeep1;
                foreach (var inline2 in values)
                {
                    if (inline2 == 2)
                        continue;
                    if (inline2 == 3)
                        break;
                }
            }
            Console.WriteLine(keep1);
        }
    }
}");
    }

    [Fact]
    public async Task SelectCaseWithExplicitExitAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class A
    Public Function Add(ByVal x As Integer) As Integer
        Select Case x
            Case 1
                Exit Select
        End Select
        Return 3
    End Function
End Class", @"
internal partial class A
{
    public int Add(int x)
    {
        switch (x)
        {
            case 1:
                {
                    break;
                }
        }
        return 3;
    }
}");
    }

    [Fact]
    public async Task MultipleBreakable_CreatesIfStatementsToExitContainingBlock_Issue690Async()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System.Collections.Generic

Public Class VisualBasicClass
    Public Sub Test
        Dim LstTmp As New List(Of Integer)
        LstTmp.Add(5)
        LstTmp.Add(6)
        LstTmp.Add(7)
        Dim i_Total As Integer
        For Each CurVal As Integer In LstTmp
            i_Total += CurVal
            Select Case CurVal
                Case 6
                    Exit For
            End Select
        Next
    system.Console.WriteLine(i_Total.ToString())
    End Sub
End Class", @"using System;
using System.Collections.Generic;

public partial class VisualBasicClass
{
    public void Test()
    {
        var LstTmp = new List<int>();
        LstTmp.Add(5);
        LstTmp.Add(6);
        LstTmp.Add(7);
        var i_Total = default(int);
        foreach (int CurVal in LstTmp)
        {
            i_Total += CurVal;
            bool exitFor = false;
            switch (CurVal)
            {
                case 6:
                    {
                        exitFor = true;
                        break;
                    }
            }

            if (exitFor)
            {
                break;
            }
        }
        Console.WriteLine(i_Total.ToString());
    }
}");
    }

    [Fact]
    public async Task MultipleBreakable_CreatesIfStatementsToExitContainingBlock_WithoutRunningInterveningCodeAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System.Collections.Generic

Public Class VisualBasicClass
    Public Sub Test
        Dim LstTmp As New List(Of Integer)
        LstTmp.Add(5)
        LstTmp.Add(6)
        LstTmp.Add(7)
        Dim i_Total As Integer
        For Each CurVal As Integer In LstTmp
            i_Total += CurVal
            Select Case CurVal
                Case 6
                    Exit For
                Case 7
                    Exit For
            End Select
            Console.WriteLine()
        Next
    system.Console.WriteLine(i_Total.ToString())
    End Sub
End Class", @"using System;
using System.Collections.Generic;

public partial class VisualBasicClass
{
    public void Test()
    {
        var LstTmp = new List<int>();
        LstTmp.Add(5);
        LstTmp.Add(6);
        LstTmp.Add(7);
        var i_Total = default(int);
        foreach (int CurVal in LstTmp)
        {
            i_Total += CurVal;
            bool exitFor = false;
            switch (CurVal)
            {
                case 6:
                    {
                        exitFor = true;
                        break;
                    }
                case 7:
                    {
                        exitFor = true;
                        break;
                    }
            }

            if (exitFor)
            {
                break;
            }
            Console.WriteLine();
        }
        Console.WriteLine(i_Total.ToString());
    }
}");
    }

    [Fact]
    public async Task MultipleBreakable_CreatesIfStatementsToExitContainingBlockIssue946Async()
    {
        await TestConversionVisualBasicToCSharpAsync(@"
Public Class VisualBasicClass
    Public Function Test(applicationRoles)
        For Each appRole In applicationRoles
            Dim objectUnit = appRole
            While objectUnit IsNot Nothing
                If appRole < 10 Then
                    If appRole < 3 Then
                        Return True
                    Else If appRole < 4 Then
                        Continue While ' Continue While
                    Else If appRole < 5 Then
                        Exit For ' Exit For
                    Else If appRole < 6 Then
                        Continue For ' Continue For
                    Else If appRole < 7 Then
                        Exit For ' Exit For
                    Else If appRole < 8 Then
                        Exit While ' Exit While
                    Else If appRole < 9 Then
                        Continue While ' Continue While
                    Else
                        Continue For ' Continue For
                    End If
                End IF
                objectUnit = objectUnit.ToString
            End While
        Next
    End Function
End Class", @"using System.Collections;
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

public partial class VisualBasicClass
{
    public object Test(object applicationRoles)
    {
        foreach (var appRole in (IEnumerable)applicationRoles)
        {
            var objectUnit = appRole;
            bool continueFor = false;
            bool exitFor = false;
            while (objectUnit is not null)
            {
                if (Conversions.ToBoolean(Operators.ConditionalCompareObjectLess(appRole, 10, false)))
                {
                    if (Conversions.ToBoolean(Operators.ConditionalCompareObjectLess(appRole, 3, false)))
                    {
                        return true;
                    }
                    else if (Conversions.ToBoolean(Operators.ConditionalCompareObjectLess(appRole, 4, false)))
                    {
                        continue; // Continue While
                    }
                    else if (Conversions.ToBoolean(Operators.ConditionalCompareObjectLess(appRole, 5, false)))
                    {
                        exitFor = true;
                        break; // Exit For
                    }
                    else if (Conversions.ToBoolean(Operators.ConditionalCompareObjectLess(appRole, 6, false)))
                    {
                        continueFor = true;
                        break; // Continue For
                    }
                    else if (Conversions.ToBoolean(Operators.ConditionalCompareObjectLess(appRole, 7, false)))
                    {
                        exitFor = true;
                        break; // Exit For
                    }
                    else if (Conversions.ToBoolean(Operators.ConditionalCompareObjectLess(appRole, 8, false)))
                    {
                        break; // Exit While
                    }
                    else if (Conversions.ToBoolean(Operators.ConditionalCompareObjectLess(appRole, 9, false)))
                    {
                        continue; // Continue While
                    }
                    else
                    {
                        continueFor = true;
                        break;
                    } // Continue For
                }
                objectUnit = objectUnit.ToString();
            }

            if (continueFor)
            {
                continue;
            }

            if (exitFor)
            {
                break;
            }
        }

        return default;
    }
}");
    }

    [Fact]
    public async Task BreakableThenContinuable_CreatesIfStatementsToExitContainingBlock_WithoutRunningInterveningCodeAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System.Collections.Generic

Public Class VisualBasicClass
    Public Sub Test
        Dim LstTmp As New List(Of Integer)
        LstTmp.Add(5)
        LstTmp.Add(6)
        LstTmp.Add(7)
        Dim i_Total As Integer
        For Each CurVal As Integer In LstTmp
            i_Total += CurVal
            Select Case CurVal
                Case 6
                    Continue For
            End Select
            Console.WriteLine()
        Next
    system.Console.WriteLine(i_Total.ToString())
    End Sub
End Class", @"using System;
using System.Collections.Generic;

public partial class VisualBasicClass
{
    public void Test()
    {
        var LstTmp = new List<int>();
        LstTmp.Add(5);
        LstTmp.Add(6);
        LstTmp.Add(7);
        var i_Total = default(int);
        foreach (int CurVal in LstTmp)
        {
            i_Total += CurVal;
            switch (CurVal)
            {
                case 6:
                    {
                        continue;
                    }
            }
            Console.WriteLine();
        }
        Console.WriteLine(i_Total.ToString());
    }
}");
    }

    [Fact]
    public async Task MultipleContinuable_CreatesIfStatementsToExitContainingBlock_WithoutRunningInterveningCodeAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System
Imports System.Collections.Generic

Public Class VisualBasicClass
    Public Sub Test
        Dim LstTmp As New List(Of Integer)
        LstTmp.Add(5)
        LstTmp.Add(6)
        LstTmp.Add(7)
        Dim i_Total As Integer
        For Each CurVal As Integer In LstTmp
            i_Total += CurVal
            While CurVal < 3
                Select Case CurVal
                    Case 6
                        Continue For
                End Select
            End While
            While CurVal < 4
                Select Case CurVal
                    Case 7
                        Continue For
                    Case 8
                        Exit For
                End Select
            End While
            Console.WriteLine()
        Next
        System.Console.WriteLine(i_Total.ToString())
    End Sub
End Class", @"using System;
using System.Collections.Generic;

public partial class VisualBasicClass
{
    public void Test()
    {
        var LstTmp = new List<int>();
        LstTmp.Add(5);
        LstTmp.Add(6);
        LstTmp.Add(7);
        var i_Total = default(int);
        foreach (int CurVal in LstTmp)
        {
            i_Total += CurVal;
            bool continueFor = false;
            while (CurVal < 3)
            {
                bool breakFor = false;
                switch (CurVal)
                {
                    case 6:
                        {
                            continueFor = breakFor = true;
                            break;
                        }
                }

                if (breakFor)
                {
                    break;
                }
            }

            if (continueFor)
            {
                continue;
            }
            bool continueFor1 = false;
            bool exitFor1 = false;
            while (CurVal < 4)
            {
                bool breakFor1 = false;
                bool exitFor = false;
                switch (CurVal)
                {
                    case 7:
                        {
                            continueFor1 = breakFor1 = true;
                            break;
                        }
                    case 8:
                        {
                            exitFor1 = exitFor = true;
                            break;
                        }
                }

                if (breakFor1)
                {
                    break;
                }

                if (exitFor)
                {
                    break;
                }
            }

            if (continueFor1)
            {
                continue;
            }

            if (exitFor1)
            {
                break;
            }
            Console.WriteLine();
        }
        Console.WriteLine(i_Total.ToString());
    }
}");
    }

    [Fact]
    public async Task ExitTry_CreatesBreakableLoop_Issue779Async()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System

Public Class VisualBasicClass779
    Public Property SomeCase As Integer = 1
    Public Property ComboBox_CostCenter As Object()
    Public Property The_Cost_Center As Object

    Public Sub Test
        Try
            If Not To_Show_Cost() Then
                SomeCase *= 2
            End If

            SomeCase *= 3
                
            If The_Cost_Center = 0 Then
                    SomeCase *=5
                Exit Try
            End If

            For i = 0 To ComboBox_CostCenter.Length - 1
                If 7 = The_Cost_Center Then
                    SomeCase *=7
                    Exit Try
                End If
            Next
        Finally
        End Try
    End Sub

    Private Function To_Show_Cost() As Boolean
        Throw New NotImplementedException()
    End Function
End Class", @"using System;
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

public partial class VisualBasicClass779
{
    public int SomeCase { get; set; } = 1;
    public object[] ComboBox_CostCenter { get; set; }
    public object The_Cost_Center { get; set; }

    public void Test()
    {
        do
        {
            try
            {
                if (!To_Show_Cost())
                {
                    SomeCase *= 2;
                }

                SomeCase *= 3;

                if (Conversions.ToBoolean(Operators.ConditionalCompareObjectEqual(The_Cost_Center, 0, false)))
                {
                    SomeCase *= 5;
                    break;
                }

                bool exitTry = false;
                for (int i = 0, loopTo = ComboBox_CostCenter.Length - 1; i <= loopTo; i++)
                {
                    if (Conversions.ToBoolean(Operators.ConditionalCompareObjectEqual(7, The_Cost_Center, false)))
                    {
                        SomeCase *= 7;
                        exitTry = true;
                        break;
                    }
                }

                if (exitTry)
                {
                    break;
                }
            }
            finally
            {
            }
        }
        while (false);
    }

    private bool To_Show_Cost()
    {
        throw new NotImplementedException();
    }
}");
    }

    [Fact]
    public async Task WithinNonExitedTryAndFor_ExitForGeneratesBreakAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System

Public Class VisualBasicClass779
    Public Property SomeCase As Integer = 1
    Public Property ComboBox_CostCenter As Object()
    Public Property The_Cost_Center As Object

    Public Sub Test
        Try
            For i = 0 To ComboBox_CostCenter.Length - 1
                If 7 = The_Cost_Center Then
                    SomeCase *=7
                    Exit For
                End If
            Next
        Finally
        End Try
    End Sub

    Private Function To_Show_Cost() As Boolean
        Throw New NotImplementedException()
    End Function
End Class", @"using System;
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

public partial class VisualBasicClass779
{
    public int SomeCase { get; set; } = 1;
    public object[] ComboBox_CostCenter { get; set; }
    public object The_Cost_Center { get; set; }

    public void Test()
    {
        try
        {
            for (int i = 0, loopTo = ComboBox_CostCenter.Length - 1; i <= loopTo; i++)
            {
                if (Conversions.ToBoolean(Operators.ConditionalCompareObjectEqual(7, The_Cost_Center, false)))
                {
                    SomeCase *= 7;
                    break;
                }
            }
        }
        finally
        {
        }
    }

    private bool To_Show_Cost()
    {
        throw new NotImplementedException();
    }
}");
    }

    [Fact]
    public async Task WithinForAndNonExitedTry_ExitForGeneratesBreakAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System

Public Class VisualBasicClass779
    Public Property SomeCase As Integer = 1
    Public Property ComboBox_CostCenter As Object()
    Public Property The_Cost_Center As Object

    Public Sub Test
        For i = 0 To ComboBox_CostCenter.Length - 1
            Try
                If 7 = The_Cost_Center Then
                    SomeCase *=7
                    Exit For
                End If
            Finally
            End Try
        Next
    End Sub

    Private Function To_Show_Cost() As Boolean
        Throw New NotImplementedException()
    End Function
End Class", @"using System;
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

public partial class VisualBasicClass779
{
    public int SomeCase { get; set; } = 1;
    public object[] ComboBox_CostCenter { get; set; }
    public object The_Cost_Center { get; set; }

    public void Test()
    {
        for (int i = 0, loopTo = ComboBox_CostCenter.Length - 1; i <= loopTo; i++)
        {
            try
            {
                if (Conversions.ToBoolean(Operators.ConditionalCompareObjectEqual(7, The_Cost_Center, false)))
                {
                    SomeCase *= 7;
                    break;
                }
            }
            finally
            {
            }
        }
    }

    private bool To_Show_Cost()
    {
        throw new NotImplementedException();
    }
}");
    }

    [Fact]
    public async Task WithinForAndExitedTry_ExitForGeneratesIfStatementsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System

Public Class VisualBasicClass779
    Public Property SomeCase As Integer = 1
    Public Property ComboBox_CostCenter As Object()
    Public Property The_Cost_Center As Object

    Public Sub Test
        For i = 0 To ComboBox_CostCenter.Length - 1
            Try
                If 7 = The_Cost_Center Then
                    SomeCase *=7
                    Exit For
                Else
                    Exit Try
                End If
            Finally
            End Try
        Next
    End Sub

    Private Function To_Show_Cost() As Boolean
        Throw New NotImplementedException()
    End Function
End Class", @"using System;
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

public partial class VisualBasicClass779
{
    public int SomeCase { get; set; } = 1;
    public object[] ComboBox_CostCenter { get; set; }
    public object The_Cost_Center { get; set; }

    public void Test()
    {
        for (int i = 0, loopTo = ComboBox_CostCenter.Length - 1; i <= loopTo; i++)
        {
            do
            {
                bool exitFor = false;
                try
                {
                    if (Conversions.ToBoolean(Operators.ConditionalCompareObjectEqual(7, The_Cost_Center, false)))
                    {
                        SomeCase *= 7;
                        exitFor = true;
                        break;
                    }
                    else
                    {
                        break;
                    }
                }
                finally
                {
                }

                if (exitFor)
                {
                    break;
                }
            }
            while (false);
        }
    }

    private bool To_Show_Cost()
    {
        throw new NotImplementedException();
    }
}");
    }
}