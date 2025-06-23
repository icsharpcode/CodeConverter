using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using VerifyXunit;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp;

public class NamespaceLevelTests : ConverterTestBase
{
    [Fact]
    public async Task TestNamespaceAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
namespace Test
{

}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestLongNamespaceAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
namespace Test1.Test2.Test3
{
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestGlobalNamespaceAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
namespace Test
{
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestGenericInheritanceInGlobalNamespaceAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
internal partial class A<T>
{
}
internal partial class B : A<string>
{
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestTopLevelAttributeAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

[assembly: CLSCompliant(true)]", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task AliasedImportsAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using tr = System.IO.TextReader;

public partial class Test
{
    private tr aliased;
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task UnaliasedImportsAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using UnrecognizedNamespace;


1 target compilation errors:
CS0246: The type or namespace name 'UnrecognizedNamespace' could not be found (are you missing a using directive or an assembly reference?)", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestClassAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
namespace Test.@class
{
    internal partial class TestClass<T>
    {
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestMixedCaseNamespaceAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
namespace Aaa
{
    internal partial class A
    {
        public static void Foo()
        {
        }
    }

    internal partial class Z
    {
    }
    internal partial class Z
    {
    }

    internal abstract partial class Base
    {
        public abstract void UPPER();
        public abstract bool FOO { get; set; }
    }
    internal partial class NotBase : Base
    {

        public override void UPPER()
        {
        }
        public override bool FOO { get; set; }
    }
}

namespace aaa
{
    internal partial class B
    {
        public static void Bar()
        {
        }
    }
}

internal static partial class C
{
    public static void Main()
    {
        var x = new Aaa.A();
        var y = new aaa.B();
        var z = new Aaa.A();
        var a = new aaa.B();
        var b = new Aaa.A();
        var c = new aaa.B();
        var d = new Aaa.A();
        var e = new aaa.B();
        var f = new Aaa.Z();
        var g = new Aaa.Z();
        Aaa.A.Foo();
        Aaa.A.Foo();
        aaa.B.Bar();
        aaa.B.Bar();
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestInternalStaticClassAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
namespace Test.@class
{
    internal static partial class TestClass
    {
        public static void Test()
        {
        }

        private static void Test2()
        {
        }
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestAbstractClassAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
namespace Test.@class
{
    internal abstract partial class TestClass
    {
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestSealedClassAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
namespace Test.@class
{
    internal sealed partial class TestClass
    {
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestInterfaceAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

internal partial interface ITest : IDisposable
{

    void Test();
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestEnumAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
internal enum ExceptionResource
{
    Argument_ImplementIComparable,
    ArgumentOutOfRange_NeedNonNegNum,
    ArgumentOutOfRange_NeedNonNegNumRequired,
    Arg_ArrayPlusOffTooSmall
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestClassInheritanceList1Async()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

internal abstract partial class ClassA : IDisposable
{

    protected abstract void Test();
    public abstract void Dispose();
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestClassInheritanceList2Async()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

internal abstract partial class ClassA : EventArgs, IDisposable
{

    protected abstract void Test();
    public abstract void Dispose();
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestStructAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

internal partial struct MyType : IComparable<MyType>
{

    private void Test()
    {
    }
}
1 source compilation errors:
BC30149: Structure 'MyType' must implement 'Function CompareTo(other As MyType) As Integer' for interface 'IComparable(Of MyType)'.
1 target compilation errors:
CS0535: 'MyType' does not implement interface member 'IComparable<MyType>.CompareTo(MyType)'", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestDelegateAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"public delegate void Test();", extension: "cs")
            );
        }
        {
            await Task.WhenAll(
                Verifier.Verify(@"public delegate int Test();", extension: "cs")
            );
        }
        {
            await Task.WhenAll(
                Verifier.Verify(@"public delegate void Test(int x);", extension: "cs")
            );
        }
        {
            await Task.WhenAll(
                Verifier.Verify(@"public delegate void Test(ref int x);", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestGenericDelegate771Async()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"public delegate T Operation<T>();", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestDelegateWithOmittedParameterTypeAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"public delegate void Test(object x);", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task ClassImplementsInterfaceAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

internal partial class test : IComparable
{
}
1 source compilation errors:
BC30149: Class 'test' must implement 'Function CompareTo(obj As Object) As Integer' for interface 'IComparable'.
1 target compilation errors:
CS0535: 'test' does not implement interface member 'IComparable.CompareTo(object)'", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task ClassImplementsInterface2Async()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

internal partial class ClassImplementsInterface2 : IComparable
{
}
1 source compilation errors:
BC30149: Class 'ClassImplementsInterface2' must implement 'Function CompareTo(obj As Object) As Integer' for interface 'IComparable'.
1 target compilation errors:
CS0535: 'ClassImplementsInterface2' does not implement interface member 'IComparable.CompareTo(object)'", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task ClassInheritsClassAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System.IO;

internal partial class ClassInheritsClass : InvalidDataException
{
}
1 source compilation errors:
BC30299: 'ClassInheritsClass' cannot inherit from class 'InvalidDataException' because 'InvalidDataException' is declared 'NotInheritable'.
1 target compilation errors:
CS0509: 'ClassInheritsClass': cannot derive from sealed type 'InvalidDataException'", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task ClassInheritsClass2Async()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System.IO;

internal partial class ClassInheritsClass2 : InvalidDataException
{
}
1 source compilation errors:
BC30299: 'ClassInheritsClass2' cannot inherit from class 'InvalidDataException' because 'InvalidDataException' is declared 'NotInheritable'.
1 target compilation errors:
CS0509: 'ClassInheritsClass2': cannot derive from sealed type 'InvalidDataException'", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task ClassInheritsClassWithNoParenthesesOnBaseCallAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"public partial class DataSet1 : System.Data.DataSet
{
    public DataSet1() : base()
    {
    }
}
", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task MultilineDocCommentAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial class MyTestClass
{
    /// <summary>
    /// Returns empty
    /// </summary>
    private string MyFunc3()
    {
        return """";
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task MultilineCommentRootOfFileAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
/// <summary>
/// Class xml doc
/// </summary>
public partial class MyTestClass
{
    private string MyFunc4()
    {
        return """";
    }
}

/// <summary>
/// Issue334
/// </summary>
internal partial class Program
{
}", extension: "cs")
            );
        }
    }

    [Fact (Skip ="This test currently fails.  The initial line is trimmed. Not sure of importance")]
    public async Task MultilineCommentRootOfFileLeadingSpacesAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"    /// <summary>
    /// Class xml doc with leading spaces
    /// </summary>
public partial class MyTestClass
{
    private string MyFunc5()
    {
        return """";
    }
}", extension: "cs")
            );
        }
    }
    [Fact]
    public async Task EnumConversionAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal enum ESByte : sbyte
{
    M1 = 0
}

internal enum EByte : byte
{
    M1 = 0
}

internal enum EShort : short
{
    M1 = 0
}

internal enum EUShort : ushort
{
    M1 = 0
}

internal enum EInteger : int
{
    M1 = 0
}

internal enum EUInteger : uint
{
    M1 = 0U
}

internal enum ELong : long
{
    M1 = 0L
}

internal enum EULong : ulong
{
    M1 = 0UL
}

internal static partial class Module1
{
    public static void Main()
    {
        bool vBooleanSByte = Conversions.ToBoolean(ESByte.M1);
        bool vBooleanByte = Conversions.ToBoolean(EByte.M1);
        bool vBooleanShort = Conversions.ToBoolean(EShort.M1);
        bool vBooleanUShort = Conversions.ToBoolean(EUShort.M1);
        bool vBooleanInteger = Conversions.ToBoolean(EInteger.M1);
        bool vBooleanUInteger = Conversions.ToBoolean(EUInteger.M1);
        bool vBooleanLong = Conversions.ToBoolean(ELong.M1);
        bool vBooleanULong = Conversions.ToBoolean(EULong.M1);
        sbyte vSByteSByte = (sbyte)ESByte.M1;
        sbyte vSByteByte = (sbyte)EByte.M1;
        sbyte vSByteShort = (sbyte)EShort.M1;
        sbyte vSByteUShort = (sbyte)EUShort.M1;
        sbyte vSByteInteger = (sbyte)EInteger.M1;
        sbyte vSByteUInteger = (sbyte)EUInteger.M1;
        sbyte vSByteLong = (sbyte)ELong.M1;
        sbyte vSByteULong = (sbyte)EULong.M1;
        byte vByteSByte = (byte)ESByte.M1;
        byte vByteByte = (byte)EByte.M1;
        byte vByteShort = (byte)EShort.M1;
        byte vByteUShort = (byte)EUShort.M1;
        byte vByteInteger = (byte)EInteger.M1;
        byte vByteUInteger = (byte)EUInteger.M1;
        byte vByteLong = (byte)ELong.M1;
        byte vByteULong = (byte)EULong.M1;
        short vShortSByte = (short)ESByte.M1;
        short vShortByte = (short)EByte.M1;
        short vShortShort = (short)EShort.M1;
        short vShortUShort = (short)EUShort.M1;
        short vShortInteger = (short)EInteger.M1;
        short vShortUInteger = (short)EUInteger.M1;
        short vShortLong = (short)ELong.M1;
        short vShortULong = (short)EULong.M1;
        ushort vUShortSByte = (ushort)ESByte.M1;
        ushort vUShortByte = (ushort)EByte.M1;
        ushort vUShortShort = (ushort)EShort.M1;
        ushort vUShortUShort = (ushort)EUShort.M1;
        ushort vUShortInteger = (ushort)EInteger.M1;
        ushort vUShortUInteger = (ushort)EUInteger.M1;
        ushort vUShortLong = (ushort)ELong.M1;
        ushort vUShortULong = (ushort)EULong.M1;
        int vIntegerSByte = (int)ESByte.M1;
        int vIntegerByte = (int)EByte.M1;
        int vIntegerShort = (int)EShort.M1;
        int vIntegerUShort = (int)EUShort.M1;
        int vIntegerInteger = (int)EInteger.M1;
        int vIntegerUInteger = (int)EUInteger.M1;
        int vIntegerLong = (int)ELong.M1;
        int vIntegerULong = (int)EULong.M1;
        uint vUIntegerSByte = (uint)ESByte.M1;
        uint vUIntegerByte = (uint)EByte.M1;
        uint vUIntegerShort = (uint)EShort.M1;
        uint vUIntegerUShort = (uint)EUShort.M1;
        uint vUIntegerInteger = (uint)EInteger.M1;
        uint vUIntegerUInteger = (uint)EUInteger.M1;
        uint vUIntegerLong = (uint)ELong.M1;
        uint vUIntegerULong = (uint)EULong.M1;
        long vLongSByte = (long)ESByte.M1;
        long vLongByte = (long)EByte.M1;
        long vLongShort = (long)EShort.M1;
        long vLongUShort = (long)EUShort.M1;
        long vLongInteger = (long)EInteger.M1;
        long vLongUInteger = (long)EUInteger.M1;
        long vLongLong = (long)ELong.M1;
        long vLongULong = (long)EULong.M1;
        ulong vULongSByte = (ulong)ESByte.M1;
        ulong vULongByte = (ulong)EByte.M1;
        ulong vULongShort = (ulong)EShort.M1;
        ulong vULongUShort = (ulong)EUShort.M1;
        ulong vULongInteger = (ulong)EInteger.M1;
        ulong vULongUInteger = (ulong)EUInteger.M1;
        ulong vULongLong = (ulong)ELong.M1;
        ulong vULongULong = (ulong)EULong.M1;
        decimal vDecimalSByte = (decimal)ESByte.M1;
        decimal vDecimalByte = (decimal)EByte.M1;
        decimal vDecimalShort = (decimal)EShort.M1;
        decimal vDecimalUShort = (decimal)EUShort.M1;
        decimal vDecimalInteger = (decimal)EInteger.M1;
        decimal vDecimalUInteger = (decimal)EUInteger.M1;
        decimal vDecimalLong = (decimal)ELong.M1;
        decimal vDecimalULong = (decimal)EULong.M1;
        float vSingleSByte = (float)ESByte.M1;
        float vSingleByte = (float)EByte.M1;
        float vSingleShort = (float)EShort.M1;
        float vSingleUShort = (float)EUShort.M1;
        float vSingleInteger = (float)EInteger.M1;
        float vSingleUInteger = (float)EUInteger.M1;
        float vSingleLong = (float)ELong.M1;
        float vSingleULong = (float)EULong.M1;
        double vDoubleSByte = (double)ESByte.M1;
        double vDoubleByte = (double)EByte.M1;
        double vDoubleShort = (double)EShort.M1;
        double vDoubleUShort = (double)EUShort.M1;
        double vDoubleInteger = (double)EInteger.M1;
        double vDoubleUInteger = (double)EUInteger.M1;
        double vDoubleLong = (double)ELong.M1;
        double vDoubleULong = (double)EULong.M1;
        string vStringSByte = ((sbyte)ESByte.M1).ToString();
        string vStringByte = ((byte)EByte.M1).ToString();
        string vStringShort = ((short)EShort.M1).ToString();
        string vStringUShort = ((ushort)EUShort.M1).ToString();
        string vStringInteger = ((int)EInteger.M1).ToString();
        string vStringUInteger = ((uint)EUInteger.M1).ToString();
        string vStringLong = ((long)ELong.M1).ToString();
        string vStringULong = ((ulong)EULong.M1).ToString();
        object vObjectSByte = ESByte.M1;
        object vObjectByte = EByte.M1;
        object vObjectShort = EShort.M1;
        object vObjectUShort = EUShort.M1;
        object vObjectInteger = EInteger.M1;
        object vObjectUInteger = EUInteger.M1;
        object vObjectLong = ELong.M1;
        object vObjectULong = EULong.M1;
    }

}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task NewTypeConstraintLastAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial interface Foo
{
}

public partial class Bar<x> where x : Foo, new()
{

}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task MyClassVirtualCallMethodAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public abstract partial class A
{
    public int MyClassF1(int x)
    {
        return 1;
    }

    public virtual int F1(int x) => MyClassF1(x); // Comment ends up out of order, but attached to correct method
    public abstract int F2();
    public void TestMethod()
    {
        int w = MyClassF1(1);
        int x = F1(2);
        int y = F2();
        int z = F2();
    }
}
1 source compilation errors:
BC30614: 'MustOverride' method 'Public MustOverride Function F2() As Integer' cannot be called with 'MyClass'.", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task MyClassVirtualCallPropertyAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public abstract partial class A
{
    public int MyClassP1 { get; set; } = 1;

    public virtual int P1
    {
        get
        {
            return MyClassP1;
        }

        set
        {
            MyClassP1 = value;
        }
    }
    public abstract int P2 { get; set; }
    public void TestMethod()
    {
        int w = MyClassP1;
        int x = P1;
        int y = P2;
        int z = P2;
    }
}
1 source compilation errors:
BC30614: 'MustOverride' method 'Public MustOverride Property P2 As Integer' cannot be called with 'MyClass'.", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task OverridenMemberCallAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

internal static partial class Module1
{
    public partial class BaseImpl
    {
        protected virtual string GetImplName()
        {
            return nameof(BaseImpl);
        }
    }

    /// <summary>
    /// The fact that this class doesn't contain a definition for GetImplName is crucial to the repro
    /// </summary>
    public partial class ErrorSite : BaseImpl
    {
        public object PublicGetImplName()
        {
            // This must not be qualified with MyBase since the method is overridable
            return GetImplName();
        }
    }

    public partial class OverrideImpl : ErrorSite
    {
        protected override string GetImplName()
        {
            return nameof(OverrideImpl);
        }
    }

    public static void Main()
    {
        var c = new OverrideImpl();
        Console.WriteLine(c.PublicGetImplName());
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task Issue1019_ImportsClassUsingStaticAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using static System.String;

public partial class Class1
{
    private object x = IsNullOrEmpty(""test"");
}
", extension: "cs")
            );
        }
    }
}