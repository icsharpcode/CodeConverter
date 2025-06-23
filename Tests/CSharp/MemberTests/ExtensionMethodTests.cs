using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using VerifyXunit;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.MemberTests;

public class ExtensionMethodTests : ConverterTestBase
{
    [Fact]
    public async Task TestExtensionMethodAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
internal static partial class TestClass
{
    public static void TestMethod(this string str)
    {
    }

    public static void TestMethod2Parameters(this string str, string other)
    {
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestExtensionMethodWithExistingImportAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
internal static partial class TestClass
{
    public static void TestMethod(this string str)
    {
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestRefExtensionMethodAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

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
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestExtensionWithinExtendedTypeAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
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
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestExtensionWithinTypeDerivedFromExtendedTypeAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
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
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestExtensionWithinNestedExtendedTypeAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
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
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestExtensionWithMeWithinExtendedTypeAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
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
}", extension: "cs")
            );
        }
    }
}