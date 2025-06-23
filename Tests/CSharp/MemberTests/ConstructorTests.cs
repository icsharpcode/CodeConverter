using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using VerifyXunit;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.MemberTests;

public class ConstructorTests : ConverterTestBase
{
    [Fact]
    public async Task TestConstructorVisibilityAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
internal partial class Class1
{
    public Class1(bool x)
    {
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestModuleConstructorAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
internal static partial class Module1
{
    static Module1()
    {
        int someValue = 0;
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestHoistedOutParameterAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System.Collections.Generic;
using System.Diagnostics;

public partial class ClassWithProperties
{
    public string Property1 { get; set; }
}

public partial class VisualBasicClass
{
    public VisualBasicClass()
    {
        var x = new Dictionary<string, string>();
        var y = new ClassWithProperties();

        bool localTryGetValue() { string argvalue = y.Property1; var ret = x.TryGetValue(""x"", out argvalue); y.Property1 = argvalue; return ret; }

        if (localTryGetValue())
        {
            Debug.Print(y.Property1);
        }
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestHoistedOutParameterLambdaUsingByRefParameterAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System.Runtime.InteropServices;
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

public partial class SomeClass
{
    public void S([Optional, DefaultParameterValue(-1)] ref int x)
    {
        int i = 0;
        bool localF1(ref int x) { object argo = i; var ret = F1(x, ref argo); i = Conversions.ToInteger(argo); return ret; }
        bool localF2(ref int x) { object argo1 = i; var ret = F2(ref x, ref argo1); i = Conversions.ToInteger(argo1); return ret; }
        bool localF3(ref int x) { object argx = x; object argo2 = i; var ret = F3(ref argx, ref argo2); x = Conversions.ToInteger(argx); i = Conversions.ToInteger(argo2); return ret; }

        if (localF1(ref x))
        {
        }
        else if (localF2(ref x))
        {
        }
        else if (localF3(ref x))
        {
        }
    }

    public bool F1(int x, ref object o)
    {
        return default;
    }
    public bool F2(ref int x, ref object o)
    {
        return default;
    }
    public bool F3(ref object x, ref object o)
    {
        return default;
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestConstructorAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
internal partial class TestClass<T, T2, T3>
    where T : class, new()
    where T2 : struct
{
    public TestClass(out T argument, ref T2 argument2, T3 argument3)
    {
    }
}
1 target compilation errors:
CS0177: The out parameter 'argument' must be assigned to before control leaves the current method", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestConstructorWithImplicitPublicAccessibilityAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"public SurroundingClass()
{
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestStaticConstructorAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"static SurroundingClass()
{
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestConstructorStaticLocalConvertedToFieldAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

internal partial class StaticLocalConvertedToField
{
    private int _sPrevPosition = 7; // Comment moves with declaration
    public StaticLocalConvertedToField(bool x)
    {
        Console.WriteLine(_sPrevPosition);
    }

    private int _sPrevPosition1 = default;
    public StaticLocalConvertedToField(int x)
    {
        Console.WriteLine(_sPrevPosition1);
    }
}", extension: "cs")
            );
        }
    }
}