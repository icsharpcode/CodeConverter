using System.Threading.Tasks;
using CodeConverter.Tests.TestRunners;
using Xunit;

namespace CodeConverter.Tests.VB
{
    public class NamespaceLevelTests : ConverterTestBase
    {
        [Fact]
        public async Task TestNamespace()
        {
            await TestConversionCSharpToVisualBasic(@"namespace Test
{

}", @"Namespace Test
End Namespace");
        }

        [Fact]
        public async Task TestTopLevelAttribute()
        {
            await TestConversionCSharpToVisualBasic(
                @"[assembly: CLSCompliant(true)]",
                @"
<Assembly: CLSCompliant(True)>

2 source compilation errors:
CS0246: The type or namespace name 'CLSCompliantAttribute' could not be found (are you missing a using directive or an assembly reference?)
CS0246: The type or namespace name 'CLSCompliant' could not be found (are you missing a using directive or an assembly reference?)");
        }

        [Fact]
        public async Task NamedImport()
        {
            await TestConversionCSharpToVisualBasic(
                @"using s = System.String;

public class X
{
    s GetStr()
    {
        return s.Empty;
    }
}",
                @"Imports s = System.String

Public Class X
    Private Function GetStr() As s
        Return s.Empty
    End Function
End Class");
        }

        [Fact]
        public async Task TestClass()
        {
            await TestConversionCSharpToVisualBasic(@"namespace Test.@class
{
    class TestClass<T>
    {
    }
}", @"Namespace Test.class
    Friend Class TestClass(Of T)
    End Class
End Namespace");
        }

        [Fact]
        public async Task TestInternalStaticClass()
        {
            await TestConversionCSharpToVisualBasic(@"namespace Test.@class
{
    internal static class TestClass
    {
        public static void Test() {}
        static void Test2() {}
    }
}", @"Namespace Test.class
    Friend Module TestClass
        Public Sub Test()
        End Sub

        Private Sub Test2()
        End Sub
    End Module
End Namespace");
        }

        [Fact]
        public async Task TestAbstractClass()
        {
            await TestConversionCSharpToVisualBasic(
@"namespace Test.@class
{
    public abstract class TestClass
    {
    }
}
namespace Test
{
    public class Test1 : @class.TestClass
    {
    }
}
",
                @"Namespace Test.class
    Public MustInherit Class TestClass
    End Class
End Namespace

Namespace Test
    Public Class Test1
        Inherits [class].TestClass
    End Class
End Namespace");
        }

        [Fact]
        public async Task TestSealedClass()
        {
            await TestConversionCSharpToVisualBasic(@"namespace Test.@class
{
    sealed class TestClass
    {
    }
}", @"Namespace Test.class
    Friend NotInheritable Class TestClass
    End Class
End Namespace");
        }

        [Fact]
        public async Task TestInterface()
        {
            await TestConversionCSharpToVisualBasic(
                @"interface ITest : System.IDisposable
{
    void Test ();
}", @"Friend Interface ITest
    Inherits IDisposable

    Sub Test()
End Interface");
        }

        [Fact]
        public async Task TestInterfaceWithTwoMembers()
        {
            await TestConversionCSharpToVisualBasic(
                @"interface ITest : System.IDisposable
{
    void Test ();
    void Test2 ();
}", @"Friend Interface ITest
    Inherits IDisposable

    Sub Test()
    Sub Test2()
End Interface");
        }

        [Fact]
        public async Task TestEnum()
        {
            await TestConversionCSharpToVisualBasic(
    @"internal enum ExceptionResource
{
    Argument_ImplementIComparable,
    ArgumentOutOfRange_NeedNonNegNum,
    ArgumentOutOfRange_NeedNonNegNumRequired,
    Arg_ArrayPlusOffTooSmall
}", @"Friend Enum ExceptionResource
    Argument_ImplementIComparable
    ArgumentOutOfRange_NeedNonNegNum
    ArgumentOutOfRange_NeedNonNegNumRequired
    Arg_ArrayPlusOffTooSmall
End Enum");
        }

        [Fact]
        public async Task TestEnumWithExplicitBaseType()
        {
            await TestConversionCSharpToVisualBasic(
    @"public enum ExceptionResource : byte
{
    Argument_ImplementIComparable
}", @"Public Enum ExceptionResource As Byte
    Argument_ImplementIComparable
End Enum");
        }

        [Fact]
        public async Task TestClassInheritanceList()
        {
            await TestConversionCSharpToVisualBasic(
    @"abstract class ClassA : System.IDisposable
{
    protected abstract void Test();
}", @"Friend MustInherit Class ClassA
    Implements IDisposable

    Protected MustOverride Sub Test()
End Class

1 source compilation errors:
CS0535: 'ClassA' does not implement interface member 'IDisposable.Dispose()'
1 target compilation errors:
BC30149: Class 'ClassA' must implement 'Sub Dispose()' for interface 'IDisposable'.");

            await TestConversionCSharpToVisualBasic(
                @"abstract class ClassA : System.EventArgs, System.IDisposable
{
    protected abstract void Test();
}", @"Friend MustInherit Class ClassA
    Inherits EventArgs
    Implements IDisposable

    Protected MustOverride Sub Test()
End Class");
        }

        [Fact]
        public async Task TestStruct()
        {
            await TestConversionCSharpToVisualBasic(
    @"struct MyType : System.IComparable<MyType>
{
    void Test() {}
}", @"Friend Structure MyType
    Implements IComparable(Of MyType)

    Private Sub Test()
    End Sub
End Structure

1 source compilation errors:
CS0535: 'MyType' does not implement interface member 'IComparable<MyType>.CompareTo(MyType)'
1 target compilation errors:
BC30149: Structure 'MyType' must implement 'Function CompareTo(other As MyType) As Integer' for interface 'IComparable(Of MyType)'.");
        }

        [Fact]
        public async Task TestDelegate()
        {
            await TestConversionCSharpToVisualBasic(
                @"public delegate void Test();",
                @"Public Delegate Sub Test()");
            await TestConversionCSharpToVisualBasic(
                @"public delegate int Test();",
                @"Public Delegate Function Test() As Integer");
            await TestConversionCSharpToVisualBasic(
                @"public delegate void Test(int x);",
                @"Public Delegate Sub Test(ByVal x As Integer)");
            await TestConversionCSharpToVisualBasic(
                @"public delegate void Test(ref int x);",
                @"Public Delegate Sub Test(ByRef x As Integer)");
        }

        [Fact]
        public async Task GlobalImportsStatement()
        {
               await AssertConvertedCodeResultEquals<CSToVBWithoutSimplifierConversion>(@"using MyAlias = global::System.Runtime.Remoting.Metadata.W3cXsd2001;
using SO = global::System.Runtime.Remoting.Metadata.SoapOption;

class ThisUri
{
    private MyAlias.SoapAnyUri s;
    private SO so;
}",
@"Imports MyAlias = System.Runtime.Remoting.Metadata.W3cXsd2001
Imports SO = System.Runtime.Remoting.Metadata.SoapOption

Friend Class ThisUri
    Private s As System.Runtime.Remoting.Metadata.W3cXsd2001.SoapAnyUri
    Private so As System.Runtime.Remoting.Metadata.SoapOption
End Class");
        }

        [Fact]
        public async Task MoveImportsStatement()
        {
            await TestConversionCSharpToVisualBasic("namespace test { using SomeNamespace; }",
                @"Imports SomeNamespace

Namespace test
End Namespace

1 source compilation errors:
CS0246: The type or namespace name 'SomeNamespace' could not be found (are you missing a using directive or an assembly reference?)");
        }

        [Fact]
        public async Task InnerNamespace_MoveImportsStatement()
        {
            await TestConversionCSharpToVisualBasic(
@"namespace System {
    using Collections; // Moves outside namespace
    public class TestClass {
        public Hashtable Property { get; set; }
    }
}",
                @"Imports System.Collections ' Moves outside namespace
Namespace System
    Public Class TestClass
        Public Property [Property] As Hashtable
    End Class
End Namespace", conversion: EmptyNamespaceOptionStrictOff);
        }
        [Fact]
        public async Task Namespace_Duplicates() {
            await AssertConvertedCodeResultEquals<CSToVBWithoutSimplifierConversion>(
@"using System.Linq;
namespace System {
    using Linq;
}",
@"Imports System.Linq

Namespace System
End Namespace");
        }

        [Fact]
        public async Task ClassImplementsInterface()
        {
            await TestConversionCSharpToVisualBasic(@"public class ToBeDisplayed : iDisplay
{
    public string Name { get; set; }

    public void DisplayName()
    {
    }
}

public interface iDisplay
{
    string Name { get; set; }
    void DisplayName();
}",
                @"Public Class ToBeDisplayed
    Implements iDisplay

    Public Property Name As String Implements iDisplay.Name

    Public Sub DisplayName() Implements iDisplay.DisplayName
    End Sub
End Class

Public Interface iDisplay
    Property Name As String
    Sub DisplayName()
End Interface");
        }

        [Fact]
        public async Task ClassExplicitlyImplementsInterface()
        {
            await TestConversionCSharpToVisualBasic(
@"public class ToBeDisplayed : iDisplay
{
    string iDisplay.Name { get; set; }

    private void iDisplay.DisplayName()
    {
    }
}
public interface iDisplay
{
    string Name { get; set; }
    void DisplayName();
}",
                @"Public Class ToBeDisplayed
    Implements iDisplay

    Private Property Name As String Implements iDisplay.Name

    Private Sub DisplayName() Implements iDisplay.DisplayName
    End Sub
End Class

Public Interface iDisplay
    Property Name As String
    Sub DisplayName()
End Interface

1 source compilation errors:
CS0106: The modifier 'private' is not valid for this item");
        }

        [Fact]
        public async Task ClassExplicitlyImplementsInterface_Indexer()
        {
            await TestConversionCSharpToVisualBasic(
@"public class ToBeDisplayed : iDisplay {
    object iDisplay.this[int i] {
        get { throw new System.NotImplementedException(); }
        set { throw new System.NotImplementedException(); }
    }
}
public interface iDisplay {
    object this[int i] { get; set; }
}",
                @"Public Class ToBeDisplayed
    Implements iDisplay

    Private Property Item(ByVal i As Integer) As Object Implements iDisplay.Item
        Get
            Throw New NotImplementedException()
        End Get
        Set(ByVal value As Object)
            Throw New NotImplementedException()
        End Set
    End Property
End Class

Public Interface iDisplay
    Default Property Item(ByVal i As Integer) As Object
End Interface");
        }

        [Fact]
        public async Task ClassImplementsInterface2()
        {
            await TestConversionCSharpToVisualBasic("class test : System.IComparable { }",
                @"Friend Class test
    Implements IComparable
End Class

1 source compilation errors:
CS0535: 'test' does not implement interface member 'IComparable.CompareTo(object)'
1 target compilation errors:
BC30149: Class 'test' must implement 'Function CompareTo(obj As Object) As Integer' for interface 'IComparable'.");
        }

        [Fact]
        public async Task ClassInheritsClass()
        {
            await TestConversionCSharpToVisualBasic("using System.IO; class test : InvalidDataException { }",
                @"Imports System.IO

Friend Class test
    Inherits InvalidDataException
End Class

1 source compilation errors:
CS0509: 'test': cannot derive from sealed type 'InvalidDataException'
1 target compilation errors:
BC30299: 'test' cannot inherit from class 'InvalidDataException' because 'InvalidDataException' is declared 'NotInheritable'.

1 source compilation errors:
CS0509: 'test': cannot derive from sealed type 'InvalidDataException'
1 target compilation errors:
BC30299: 'test' cannot inherit from class 'InvalidDataException' because 'InvalidDataException' is declared 'NotInheritable'.");
        }

        [Fact]
        public async Task ClassInheritsClass2()
        {
            await TestConversionCSharpToVisualBasic("class test : System.IO.InvalidDataException { }",
                @"Friend Class test
    Inherits InvalidDataException
End Class

1 source compilation errors:
CS0509: 'test': cannot derive from sealed type 'InvalidDataException'
1 target compilation errors:
BC30299: 'test' cannot inherit from class 'InvalidDataException' because 'InvalidDataException' is declared 'NotInheritable'.");
        }

        [Fact]
        public async Task StaticGenericClass() {
            await TestConversionCSharpToVisualBasic(
@"using System.Threading.Tasks;

public abstract class Class1 {
}

public static class TestClass<T> where T : Class1, new() {
        static Task task;
        static TestClass() {
        }
        public static Task Method() {
            return task;
        }
    }",
                @"Imports System.Threading.Tasks

Public MustInherit Class Class1
End Class

Public NotInheritable Class TestClass(Of T As {Class1, New})
    Private Shared task As Task

    Shared Sub New()
    End Sub

    Public Shared Function Method() As Task
        Return task
    End Function
End Class");
        }

        [Fact]
        public async Task NestedStaticClass() {
            await TestConversionCSharpToVisualBasic(
@"public static class Factory {
    static class Generator {
        public static void Initialize() { }
    }
}",
                @"Public Module Factory
    Friend NotInheritable Class Generator
        Public Shared Sub Initialize()
        End Sub
    End Class
End Module");
        }
        [Fact]
        public async Task VisibilityStaticClass() {
            await TestConversionCSharpToVisualBasic(
@"public static class Factory {
    private const string Name = ""a"";
    internal const string Name1 = ""b"";
    public const string Name2 = ""c"";
    public static void Initialize() { }
    internal static void Initialize1() { }
    private static void Initialize2() { }
}",
                @"Public Module Factory
    Private Const Name As String = ""a""
    Friend Const Name1 As String = ""b""
    Public Const Name2 As String = ""c""

    Public Sub Initialize()
    End Sub

    Friend Sub Initialize1()
    End Sub

    Private Sub Initialize2()
    End Sub
End Module");
        }
        [Fact]
        public async Task ImplementsGenericInterface()
        {
            await TestConversionCSharpToVisualBasic(
@"public interface ITestInterface<T> {
    void Method(List<T> list);
}
public class TestClass : ITestInterface<string> {
    public void Method(List<string> list) {
    }
}",
                @"Public Interface ITestInterface(Of T)
    Sub Method(ByVal list As List(Of T))
End Interface

Public Class TestClass
    Implements ITestInterface(Of String)

    Public Sub Method(ByVal list As List(Of String)) Implements ITestInterface(Of String).Method
    End Sub
End Class

1 source compilation errors:
CS0246: The type or namespace name 'List<>' could not be found (are you missing a using directive or an assembly reference?)");
        }
        [Fact]
        public async Task ImplementsEvent() {
            await TestConversionCSharpToVisualBasic(
@"using System.ComponentModel;
public class TestClass : INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;
}",
                @"Imports System.ComponentModel

Public Class TestClass
    Implements INotifyPropertyChanged

    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged
End Class");
        }
        [Fact]
        public async Task FullQualificationInImplements() {
            await TestConversionCSharpToVisualBasic(
@"public class TestClass : System.ComponentModel.INotifyPropertyChanged {
    public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
}",
@"Public Class TestClass
    Implements System.ComponentModel.INotifyPropertyChanged

    Public Event PropertyChanged As System.ComponentModel.PropertyChangedEventHandler Implements System.ComponentModel.INotifyPropertyChanged.PropertyChanged
End Class", conversion: EmptyNamespaceOptionStrictOff);
        }
        [Fact]
        public async Task PartialClass_OmitedModifier() {
            await TestConversionCSharpToVisualBasic(
@"public partial class Entities {
}
partial class Entities {
}",
@"Public Partial Class Entities
End Class

Partial Class Entities
End Class");
        }
    }
}
