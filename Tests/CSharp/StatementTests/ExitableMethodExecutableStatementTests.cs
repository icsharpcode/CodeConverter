using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using VerifyXunit;
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
        {
            await Task.WhenAll(
                Verifier.Verify(@"
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
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task SimpleDoStatementAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
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
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task DoWhileStatementAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
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
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task ForEachStatementWithExplicitTypeAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
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
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task ForEachStatementWithVarAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
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
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task ForEachStatementWithUsedOuterDeclarationAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

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
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task ForEachStatementWithFieldVarUsedOuterDeclarationAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

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
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task ForEachStatementWithUnusedOuterDeclarationAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
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
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task ForEachStatementWithFieldVarUnusedOuterDeclarationAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
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
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task ForEachStatementWithUnusedNestedDeclarationAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

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
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task SelectCaseWithExplicitExitAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
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
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task MultipleBreakable_CreatesIfStatementsToExitContainingBlock_Issue690Async()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;
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
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task MultipleBreakable_CreatesIfStatementsToExitContainingBlock_WithoutRunningInterveningCodeAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;
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
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task MultipleBreakable_CreatesIfStatementsToExitContainingBlockIssue946Async()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System.Collections;
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
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task BreakableThenContinuable_CreatesIfStatementsToExitContainingBlock_WithoutRunningInterveningCodeAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;
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
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task MultipleContinuable_CreatesIfStatementsToExitContainingBlock_WithoutRunningInterveningCodeAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;
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
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task ExitTry_CreatesBreakableLoop_Issue779Async()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;
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
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task WithinNonExitedTryAndFor_ExitForGeneratesBreakAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;
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
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task WithinForAndNonExitedTry_ExitForGeneratesBreakAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;
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
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task WithinForAndExitedTry_ExitForGeneratesIfStatementsAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;
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
}", extension: "cs")
            );
        }
    }
}