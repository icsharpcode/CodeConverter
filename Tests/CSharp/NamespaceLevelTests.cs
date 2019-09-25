using System;
using System.Threading.Tasks;
using CodeConverter.Tests.TestRunners;
using Xunit;

namespace CodeConverter.Tests.CSharp
{
    public class NamespaceLevelTests : ConverterTestBase
    {

        [Fact]
        public async Task TestNamespace()
        {
            await TestConversionVisualBasicToCSharp(@"Namespace Test
End Namespace", @"namespace Test
{
}");
        }

        [Fact]
        public async Task TestLongNamespace()
        {
            await TestConversionVisualBasicToCSharp(@"Namespace Test1.Test2.Test3
End Namespace", @"namespace Test1.Test2.Test3
{
}");
        }

        [Fact]
        public async Task TestGlobalNamespace()
        {
            await TestConversionVisualBasicToCSharp(@"Namespace Global.Test
End Namespace", @"namespace Test
{
}");
        }

        [Fact]
        public async Task TestTopLevelAttribute()
        {
            await TestConversionVisualBasicToCSharp(
                @"<Assembly: CLSCompliant(True)>",
                @"using System;

[assembly: CLSCompliant(true)]");
        }

        [Fact]
        public async Task AliasedImports()
        {
            await TestConversionVisualBasicToCSharp(
                @"Imports tr = System.IO.TextReader

Public Class Test
    Private aliased As tr
End Class",
                @"using tr = System.IO.TextReader;

public partial class Test
{
    private tr aliased;
}");
        }

        [Fact]
        public async Task UnaliasedImports()
        {
            await TestConversionVisualBasicToCSharp(
                @"Imports UnrecognizedNamespace",
                @"using UnrecognizedNamespace;");
        }

        [Fact]
        public async Task TestClass()
        {
            await TestConversionVisualBasicToCSharp(@"Namespace Test.[class]
    Class TestClass(Of T)
    End Class
End Namespace", @"namespace Test.@class
{
    internal partial class TestClass<T>
    {
    }
}");
        }

        [Fact]
        public async Task TestInternalStaticClass()
        {
            await TestConversionVisualBasicToCSharp(@"Namespace Test.[class]
    Friend Module TestClass
        Sub Test()
        End Sub

        Private Sub Test2()
        End Sub
    End Module
End Namespace", @"namespace Test.@class
{
    internal partial static class TestClass
    {
        public static void Test()
        {
        }

        private static void Test2()
        {
        }
    }
}");
        }

        [Fact]
        public async Task TestAbstractClass()
        {
            await TestConversionVisualBasicToCSharp(@"Namespace Test.[class]
    MustInherit Class TestClass
    End Class
End Namespace", @"namespace Test.@class
{
    internal abstract partial class TestClass
    {
    }
}");
        }

        [Fact]
        public async Task TestSealedClass()
        {
            await TestConversionVisualBasicToCSharp(@"Namespace Test.[class]
    NotInheritable Class TestClass
    End Class
End Namespace", @"namespace Test.@class
{
    internal sealed partial class TestClass
    {
    }
}");
        }

        [Fact]
        public async Task TestInterface()
        {
            await TestConversionVisualBasicToCSharp(
@"Interface ITest
    Inherits System.IDisposable

    Sub Test()
End Interface", @"using System;

internal partial interface ITest : IDisposable
{
    void Test();
}");
        }

        [Fact]
        public async Task TestEnum()
        {
            await TestConversionVisualBasicToCSharp(
@"Friend Enum ExceptionResource
    Argument_ImplementIComparable
    ArgumentOutOfRange_NeedNonNegNum
    ArgumentOutOfRange_NeedNonNegNumRequired
    Arg_ArrayPlusOffTooSmall
End Enum", @"internal enum ExceptionResource
{
    Argument_ImplementIComparable,
    ArgumentOutOfRange_NeedNonNegNum,
    ArgumentOutOfRange_NeedNonNegNumRequired,
    Arg_ArrayPlusOffTooSmall
}");
        }

        [Fact]
        public async Task TestClassInheritanceList()
        {
            await TestConversionVisualBasicToCSharp(
@"MustInherit Class ClassA
    Implements System.IDisposable

    Protected MustOverride Sub Test()
End Class", @"using System;

internal abstract partial class ClassA : IDisposable
{
    protected abstract void Test();
}");

            await TestConversionVisualBasicToCSharp(
@"MustInherit Class ClassA
    Inherits System.EventArgs
    Implements System.IDisposable

    Protected MustOverride Sub Test()
End Class", @"using System;

internal abstract partial class ClassA : EventArgs, IDisposable
{
    protected abstract void Test();
}");
        }

        [Fact]
        public async Task TestStruct()
        {
            await TestConversionVisualBasicToCSharp(
@"Structure MyType
    Implements System.IComparable(Of MyType)

    Private Sub Test()
    End Sub
End Structure", @"using System;

internal partial struct MyType : IComparable<MyType>
{
    private void Test()
    {
    }
}");
        }

        [Fact]
        public async Task TestDelegate()
        {
            await TestConversionVisualBasicToCSharp(
                @"Public Delegate Sub Test()",
                @"public delegate void Test();");
            await TestConversionVisualBasicToCSharp(
                @"Public Delegate Function Test() As Integer",
                @"public delegate int Test();");
            await TestConversionVisualBasicToCSharp(
                @"Public Delegate Sub Test(ByVal x As Integer)",
                @"public delegate void Test(int x);");
            await TestConversionVisualBasicToCSharp(
                @"Public Delegate Sub Test(ByRef x As Integer)",
                @"public delegate void Test(ref int x);");
        }

        [Fact]
        public async Task ClassImplementsInterface()
        {
            await TestConversionVisualBasicToCSharp(@"Class test
    Implements IComparable
End Class",
                @"using System;

internal partial class test : IComparable
{
}");
        }

        [Fact]
        public async Task ClassImplementsInterface2()
        {
            await TestConversionVisualBasicToCSharp(@"Class test
    Implements System.IComparable
End Class",
                @"using System;

internal partial class test : IComparable
{
}");
        }

        [Fact]
        public async Task ClassInheritsClass()
        {
            await TestConversionVisualBasicToCSharp(@"Imports System.IO

Class test
    Inherits InvalidDataException
End Class",
                @"using System.IO;

internal partial class test : InvalidDataException
{
}");
        }

        [Fact]
        public async Task ClassInheritsClass2()
        {
            await TestConversionVisualBasicToCSharp(@"Class test
    Inherits System.IO.InvalidDataException
End Class",
                @"using System.IO;

internal partial class test : InvalidDataException
{
}");
        }

        [Fact]
        public async Task ClassInheritsClassWithNoParenthesesOnBaseCall()
        {
            // Moving where the base call appears confuses the auto comment tester
            await TestConversionVisualBasicToCSharpWithoutComments(@"Public Class DataSet1
    Inherits Global.System.Data.DataSet
    Public Sub New()
        MyBase.New
    End Sub
End Class",
                @"public partial class DataSet1 : System.Data.DataSet
{
    public DataSet1() : base()
    {
    }
}
");
        }

        [Fact]
        public async Task MultilineDocComment()
        {
            await TestConversionVisualBasicToCSharp(@"Public Class MyTestClass
    ''' <summary>
    ''' Returns empty
    ''' </summary>
    Private Function MyFunc() As String
        Return """"
    End Function
End Class",
                @"public partial class MyTestClass
{
    /// <summary>
    /// Returns empty
    /// </summary>
    private string MyFunc()
    {
        return """";
    }
}");
        }

        [Fact]
        public async Task EnumConversion()
        {
            await TestConversionVisualBasicToCSharpWithoutComments(@"Enum ESByte As SByte
    M1 = 0
End Enum
Enum EByte As Byte
    M1 = 0
End Enum
Enum EShort As Short
    M1 = 0
End Enum
Enum EUShort As UShort
    M1 = 0
End Enum
Enum EInteger As Integer
    M1 = 0
End Enum
Enum EUInteger As UInteger
    M1 = 0
End Enum
Enum ELong As Long
    M1 = 0
End Enum
Enum EULong As ULong
    M1 = 0
End Enum

Module Module1
    Sub Main()
        Dim vBooleanSByte As Boolean = ESByte.M1
        Dim vBooleanByte As Boolean = EByte.M1
        Dim vBooleanShort As Boolean = EShort.M1
        Dim vBooleanUShort As Boolean = EUShort.M1
        Dim vBooleanInteger As Boolean = EInteger.M1
        Dim vBooleanUInteger As Boolean = EUInteger.M1
        Dim vBooleanLong As Boolean = ELong.M1
        Dim vBooleanULong As Boolean = EULong.M1
        Dim vSByteSByte As SByte = ESByte.M1
        Dim vSByteByte As SByte = EByte.M1
        Dim vSByteShort As SByte = EShort.M1
        Dim vSByteUShort As SByte = EUShort.M1
        Dim vSByteInteger As SByte = EInteger.M1
        Dim vSByteUInteger As SByte = EUInteger.M1
        Dim vSByteLong As SByte = ELong.M1
        Dim vSByteULong As SByte = EULong.M1
        Dim vByteSByte As Byte = ESByte.M1
        Dim vByteByte As Byte = EByte.M1
        Dim vByteShort As Byte = EShort.M1
        Dim vByteUShort As Byte = EUShort.M1
        Dim vByteInteger As Byte = EInteger.M1
        Dim vByteUInteger As Byte = EUInteger.M1
        Dim vByteLong As Byte = ELong.M1
        Dim vByteULong As Byte = EULong.M1
        Dim vShortSByte As Short = ESByte.M1
        Dim vShortByte As Short = EByte.M1
        Dim vShortShort As Short = EShort.M1
        Dim vShortUShort As Short = EUShort.M1
        Dim vShortInteger As Short = EInteger.M1
        Dim vShortUInteger As Short = EUInteger.M1
        Dim vShortLong As Short = ELong.M1
        Dim vShortULong As Short = EULong.M1
        Dim vUShortSByte As UShort = ESByte.M1
        Dim vUShortByte As UShort = EByte.M1
        Dim vUShortShort As UShort = EShort.M1
        Dim vUShortUShort As UShort = EUShort.M1
        Dim vUShortInteger As UShort = EInteger.M1
        Dim vUShortUInteger As UShort = EUInteger.M1
        Dim vUShortLong As UShort = ELong.M1
        Dim vUShortULong As UShort = EULong.M1
        Dim vIntegerSByte As Integer = ESByte.M1
        Dim vIntegerByte As Integer = EByte.M1
        Dim vIntegerShort As Integer = EShort.M1
        Dim vIntegerUShort As Integer = EUShort.M1
        Dim vIntegerInteger As Integer = EInteger.M1
        Dim vIntegerUInteger As Integer = EUInteger.M1
        Dim vIntegerLong As Integer = ELong.M1
        Dim vIntegerULong As Integer = EULong.M1
        Dim vUIntegerSByte As UInteger = ESByte.M1
        Dim vUIntegerByte As UInteger = EByte.M1
        Dim vUIntegerShort As UInteger = EShort.M1
        Dim vUIntegerUShort As UInteger = EUShort.M1
        Dim vUIntegerInteger As UInteger = EInteger.M1
        Dim vUIntegerUInteger As UInteger = EUInteger.M1
        Dim vUIntegerLong As UInteger = ELong.M1
        Dim vUIntegerULong As UInteger = EULong.M1
        Dim vLongSByte As Long = ESByte.M1
        Dim vLongByte As Long = EByte.M1
        Dim vLongShort As Long = EShort.M1
        Dim vLongUShort As Long = EUShort.M1
        Dim vLongInteger As Long = EInteger.M1
        Dim vLongUInteger As Long = EUInteger.M1
        Dim vLongLong As Long = ELong.M1
        Dim vLongULong As Long = EULong.M1
        Dim vULongSByte As ULong = ESByte.M1
        Dim vULongByte As ULong = EByte.M1
        Dim vULongShort As ULong = EShort.M1
        Dim vULongUShort As ULong = EUShort.M1
        Dim vULongInteger As ULong = EInteger.M1
        Dim vULongUInteger As ULong = EUInteger.M1
        Dim vULongLong As ULong = ELong.M1
        Dim vULongULong As ULong = EULong.M1
        Dim vDecimalSByte As Decimal = ESByte.M1
        Dim vDecimalByte As Decimal = EByte.M1
        Dim vDecimalShort As Decimal = EShort.M1
        Dim vDecimalUShort As Decimal = EUShort.M1
        Dim vDecimalInteger As Decimal = EInteger.M1
        Dim vDecimalUInteger As Decimal = EUInteger.M1
        Dim vDecimalLong As Decimal = ELong.M1
        Dim vDecimalULong As Decimal = EULong.M1
        Dim vSingleSByte As Single = ESByte.M1
        Dim vSingleByte As Single = EByte.M1
        Dim vSingleShort As Single = EShort.M1
        Dim vSingleUShort As Single = EUShort.M1
        Dim vSingleInteger As Single = EInteger.M1
        Dim vSingleUInteger As Single = EUInteger.M1
        Dim vSingleLong As Single = ELong.M1
        Dim vSingleULong As Single = EULong.M1
        Dim vDoubleSByte As Double = ESByte.M1
        Dim vDoubleByte As Double = EByte.M1
        Dim vDoubleShort As Double = EShort.M1
        Dim vDoubleUShort As Double = EUShort.M1
        Dim vDoubleInteger As Double = EInteger.M1
        Dim vDoubleUInteger As Double = EUInteger.M1
        Dim vDoubleLong As Double = ELong.M1
        Dim vDoubleULong As Double = EULong.M1
        Dim vStringSByte As String = ESByte.M1
        Dim vStringByte As String = EByte.M1
        Dim vStringShort As String = EShort.M1
        Dim vStringUShort As String = EUShort.M1
        Dim vStringInteger As String = EInteger.M1
        Dim vStringUInteger As String = EUInteger.M1
        Dim vStringLong As String = ELong.M1
        Dim vStringULong As String = EULong.M1
        Dim vObjectSByte As Object = ESByte.M1
        Dim vObjectByte As Object = EByte.M1
        Dim vObjectShort As Object = EShort.M1
        Dim vObjectUShort As Object = EUShort.M1
        Dim vObjectInteger As Object = EInteger.M1
        Dim vObjectUInteger As Object = EUInteger.M1
        Dim vObjectLong As Object = ELong.M1
        Dim vObjectULong As Object = EULong.M1
    End Sub

End Module", @"using Microsoft.VisualBasic.CompilerServices;

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
    M1 = 0
}

internal enum ELong : long
{
    M1 = 0
}

internal enum EULong : ulong
{
    M1 = 0
}

internal partial static class Module1
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
        string vStringSByte = Conversions.ToString(ESByte.M1);
        string vStringByte = Conversions.ToString(EByte.M1);
        string vStringShort = Conversions.ToString(EShort.M1);
        string vStringUShort = Conversions.ToString(EUShort.M1);
        string vStringInteger = Conversions.ToString(EInteger.M1);
        string vStringUInteger = Conversions.ToString(EUInteger.M1);
        string vStringLong = Conversions.ToString(ELong.M1);
        string vStringULong = Conversions.ToString(EULong.M1);
        object vObjectSByte = ESByte.M1;
        object vObjectByte = EByte.M1;
        object vObjectShort = EShort.M1;
        object vObjectUShort = EUShort.M1;
        object vObjectInteger = EInteger.M1;
        object vObjectUInteger = EUInteger.M1;
        object vObjectLong = ELong.M1;
        object vObjectULong = EULong.M1;
    }
}");

        }

        [Fact]
        public async Task NewConstraintLast()
        {
            await TestConversionVisualBasicToCSharpWithoutComments(@"Public Interface Foo
End Interface

Public Class Bar(Of x As {New, Foo})

End Class",
                @"public partial interface Foo
{
}

public partial class Bar<x> where x : Foo, new()
{
}");
        }

        [Fact]
        public async Task MyClassVirtualCallMethod()
        {
            await TestConversionVisualBasicToCSharpWithoutComments(@"Public Class A
    Overridable Function F1() As Integer
        Return 1
    End Function
    MustOverride Function F2() As Integer
    Public Sub TestMethod() 
        Dim w = MyClass.f1()"/* Intentionally access with the wrong case which is valid VB */ + @"
        Dim x = Me.F1()
        Dim y = MyClass.F2()
        Dim z = Me.F2()
    End Sub
End Class",
                @"public partial class A
{
    public int MyClassF1()
    {
        return 1;
    }

    public virtual int F1() => MyClassF1();
    public abstract int F2();
    public void TestMethod()
    {
        int w = MyClassF1();
        int x = F1();
        int y = F2();
        int z = F2();
    }
}");
        }

        [Fact]
        public async Task MyClassVirtualCallProperty()
        {
            await TestConversionVisualBasicToCSharpWithoutComments(@"Public Class A
    Overridable Property P1() As Integer = 1
    MustOverride Property P2() As Integer
    Public Sub TestMethod() 
        Dim w = MyClass.p1"/* Intentionally access with the wrong case which is valid VB */ + @"
        Dim x = Me.P1
        Dim y = MyClass.P2
        Dim z = Me.P2
    End Sub
End Class",
                @"public partial class A
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
}");
        }

    }
}
