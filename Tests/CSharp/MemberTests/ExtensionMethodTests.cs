using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.MemberTests;

public class ExtensionMethodTests : ConverterTestBase
{
    [Fact]
    public async Task TestExtensionMethodAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Module TestClass
    <System.Runtime.CompilerServices.Extension()>
    Sub TestMethod(ByVal str As String)
    End Sub

    <System.Runtime.CompilerServices.Extension()>
    Sub TestMethod2Parameters(ByVal str As String, other As String)
    End Sub
End Module", @"
internal static partial class TestClass
{
    public static void TestMethod(this string str)
    {
    }

    public static void TestMethod2Parameters(this string str, string other)
    {
    }
}");
    }

    [Fact]
    public async Task TestExtensionMethodWithExistingImportAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Imports System.Runtime.CompilerServices ' Removed by simplifier

Module TestClass
    <Extension()>
    Sub TestMethod(ByVal str As String)
    End Sub
End Module", @"
internal static partial class TestClass
{
    public static void TestMethod(this string str)
    {
    }
}");
    }

    [Fact]
    public async Task TestRefExtensionMethodAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Imports System
Imports System.Runtime.CompilerServices ' Removed since the extension attribute is removed

Public Module MyExtensions
    <Extension()>
    Public Sub Add(Of T)(ByRef arr As T(), item As T)
        Array.Resize(arr, arr.Length + 1)
        arr(arr.Length - 1) = item
    End Sub
End Module

Public Module UsagePoint
    Public Sub Main()
        Dim arr = New Integer() {1, 2, 3}
        arr.Add(4)
        System.Console.WriteLine(arr(3))
    End Sub
End Module", @"using System;

public static partial class MyExtensions
{
    public static void Add<T>(ref T[] arr, T item)
    {
        Array.Resize(ref arr, arr.Length + 1);
        arr[arr.Length - 1] = item;
    }
}

public static partial class UsagePoint
{
    public static void Main()
    {
        int[] arr = new int[] { 1, 2, 3 };
        MyExtensions.Add(ref arr, 4);
        Console.WriteLine(arr[3]);
    }
}");
    }

    [Fact]
    public async Task TestExtensionWithinExtendedTypeAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Module Extensions
    <Extension()>
    Sub TestExtension(extendedClass As ExtendedClass)
    End Sub
End Module

Class ExtendedClass
  Sub TestExtensionConsumer()
    TestExtension()
  End Sub
End Class", @"
internal static partial class Extensions
{
    public static void TestExtension(this ExtendedClass extendedClass)
    {
    }
}

internal partial class ExtendedClass
{
    public void TestExtensionConsumer()
    {
        this.TestExtension();
    }
}");
    }

    [Fact]
    public async Task TestExtensionWithinTypeDerivedFromExtendedTypeAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Module Extensions
    <Extension()>
    Sub TestExtension(extendedClass As ExtendedClass)
    End Sub
End Module

Class ExtendedClass
End Class

Class DerivedClass
    Inherits ExtendedClass

  Sub TestExtensionConsumer()
    TestExtension()
  End Sub
End Class", @"
internal static partial class Extensions
{
    public static void TestExtension(this ExtendedClass extendedClass)
    {
    }
}

internal partial class ExtendedClass
{
}

internal partial class DerivedClass : ExtendedClass
{

    public void TestExtensionConsumer()
    {
        this.TestExtension();
    }
}");
    }

    [Fact]
    public async Task TestExtensionWithinNestedExtendedTypeAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Module Extensions
    <Extension()>
    Sub TestExtension(extendedClass As NestingClass.ExtendedClass)
    End Sub
End Module

Class NestingClass
    Class ExtendedClass
      Sub TestExtensionConsumer()
        TestExtension()
      End Sub        
    End Class
End Class", @"
internal static partial class Extensions
{
    public static void TestExtension(this NestingClass.ExtendedClass extendedClass)
    {
    }
}

internal partial class NestingClass
{
    public partial class ExtendedClass
    {
        public void TestExtensionConsumer()
        {
            this.TestExtension();
        }
    }
}");
    }

    [Fact]
    public async Task TestExtensionWithMeWithinExtendedTypeAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Module Extensions
    <Extension()>
    Sub TestExtension(extendedClass As ExtendedClass)
    End Sub
End Module

Class ExtendedClass
  Sub TestExtensionConsumer()
    Me.TestExtension()
  End Sub
End Class", @"
internal static partial class Extensions
{
    public static void TestExtension(this ExtendedClass extendedClass)
    {
    }
}

internal partial class ExtendedClass
{
    public void TestExtensionConsumer()
    {
        this.TestExtension();
    }
}");
    }
}