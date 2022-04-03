﻿using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.VB;

public class StandaloneStatementTests : ConverterTestBase
{
    [Fact]
    public async Task ReassignmentAsync()
    {
        await TestConversionCSharpToVisualBasicAsync(
            @"int num = 4;
num = 5;",
            @"Dim num As Integer = 4
num = 5",
            expectSurroundingMethodBlock: true);
    }

    /// <summary>
    /// Tests guessing common using statements for fragments
    /// </summary>
    [Fact]
    public async Task SubLambdaAsync()
    {
        await TestConversionCSharpToVisualBasicAsync(
            @"list.ForEach(i => Console.Write(""{0}\t"", i));",
            @"list.ForEach(Sub(i) Console.Write(""{0}"" & vbTab, i))

1 source compilation errors:
CS0103: The name 'list' does not exist in the current context
1 target compilation errors:
BC32042: Too few type arguments to 'List(Of T)'.");
    }

    [Fact]
    public async Task ObjectMemberInitializerSyntaxAsync()
    {
        await TestConversionCSharpToVisualBasicAsync(
            @"AttributeUsageAttribute obj = new AttributeUsageAttribute()
{
    AllowMultiple = true,
    Inherited = false
};
obj = null;",
            @"Dim obj As AttributeUsageAttribute = New AttributeUsageAttribute() With {
    .AllowMultiple = True,
    .Inherited = False
}
obj = Nothing",
            expectSurroundingMethodBlock: true);
    }

    [Fact]
    public async Task AnonymousObjectCreationExpressionSyntaxAsync()
    {
        await TestConversionCSharpToVisualBasicAsync(
            @"var obj = new
{
    Name = ""Hello"",
    Value = ""World""
};
obj = null;",
            @"Dim obj = New With {
    .Name = ""Hello"",
    .Value = ""World""
}
obj = Nothing",
            expectSurroundingMethodBlock: true);
    }

    [Fact]
    public async Task SingleAssigmentAsync()
    {
        await TestConversionCSharpToVisualBasicAsync(
            @"var x = 3;",
            @"Dim x = 3");
    }

    [Fact]
    public async Task SingleFieldDeclarationAsync()
    {
        await TestConversionCSharpToVisualBasicAsync(
            @"private int x = 3;",
            @"Private x As Integer = 3");
    }

    [Fact]
    public async Task SingleEmptyClassAsync()
    {
        await TestConversionCSharpToVisualBasicAsync(
            @"public class Test
{
}",
            @"Public Class Test
End Class");
    }

    [Fact]
    public async Task SingleAbstractMethodAsync()
    {
        await TestConversionCSharpToVisualBasicAsync(
            @"protected abstract void abs();",
            @"Protected MustOverride Sub abs()");
    }

    [Fact]
    public async Task SingleEmptyNamespaceAsync()
    {
        await TestConversionCSharpToVisualBasicAsync(
            @"namespace nam
{
}",
            @"Namespace nam
End Namespace");
    }

    [Fact]
    public async Task SingleFieldAssignmentAsync()
    {
        await TestConversionCSharpToVisualBasicAsync(
            @"this.DataContext = from task in tasks
    where task.Priority == pri
    select task;",
            @"Me.DataContext = From task In tasks Where task.Priority Is pri Select task

3 source compilation errors:
CS1061: 'SurroundingClass' does not contain a definition for 'DataContext' and no accessible extension method 'DataContext' accepting a first argument of type 'SurroundingClass' could be found (are you missing a using directive or an assembly reference?)
CS0103: The name 'tasks' does not exist in the current context
CS0103: The name 'pri' does not exist in the current context
3 target compilation errors:
BC30456: 'DataContext' is not a member of 'SurroundingClass'.
BC30112: 'System.Threading.Tasks' is a namespace and cannot be used as an expression.
BC36610: Name 'pri' is either not declared or not in the current scope.");
    }
}