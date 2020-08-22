using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using ICSharpCode.CodeConverter.VB;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.VB
{
    public class MemberTests : ConverterTestBase
    {
        [Fact]
        public async Task TestPropertyWithModifierAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(
@"class TestClass {
    public string Text { get; private set; }
}", @"Friend Class TestClass
    Private _Text As String

    Public Property Text As String
        Get
            Return _Text
        End Get
        Private Set(ByVal value As String)
            _Text = value
        End Set
    End Property
End Class

1 target compilation errors:
BC30451: '_Text' is not declared. It may be inaccessible due to its protection level.");
        }

        [Fact]
        public async Task TestInferredPropertyInnerClassAsync(){
            await TestConversionCSharpToVisualBasicAsync(
@"class TestClass {
    class InnerClass {
        public string Text { get; private set; }
    }
}",
@"Friend Class TestClass
    Friend Class InnerClass
        Private _Text As String

        Public Property Text As String
            Get
                Return _Text
            End Get
            Private Set(ByVal value As String)
                _Text = value
            End Set
        End Property
    End Class
End Class

1 target compilation errors:
BC30451: '_Text' is not declared. It may be inaccessible due to its protection level.");
        }

        [Fact]
        public async Task TestProperty_StaticInferredAsync() {
            await TestConversionCSharpToVisualBasicAsync(
@"class TestClass {
    public static string Text { get; private set; };
    public int Count { get; private set; };
}",
                @"Friend Class TestClass
    Private Shared _Text As String
    Private _Count As Integer

    Public Shared Property Text As String
        Get
            Return _Text
        End Get
        Private Set(ByVal value As String)
            _Text = value
        End Set
    End Property

    Public Property Count As Integer
        Get
            Return _Count
        End Get
        Private Set(ByVal value As Integer)
            _Count = value
        End Set
    End Property
End Class

1 source compilation errors:
CS1597: Semicolon after method or accessor block is not valid
2 target compilation errors:
BC30451: '_Text' is not declared. It may be inaccessible due to its protection level.
BC30451: '_Count' is not declared. It may be inaccessible due to its protection level.");
        }

        [Fact]
        public async Task TestProperty_StaticInferredInModuleAsync() {
            await TestConversionCSharpToVisualBasicAsync(
@"static class TestClass {
    public static string Text { get; private set; }
}",
                @"Friend Module TestClass
    Private _Text As String

    Public Property Text As String
        Get
            Return _Text
        End Get
        Private Set(ByVal value As String)
            _Text = value
        End Set
    End Property
End Module

1 target compilation errors:
BC30451: '_Text' is not declared. It may be inaccessible due to its protection level.");
        }

        [Fact]
        public async Task TestFieldAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(
                @"class TestClass
{
    const int answer = 42;
    int value = 10;
    readonly int v = 15;
}", @"Friend Class TestClass
    Const answer As Integer = 42
    Private value As Integer = 10
    Private ReadOnly v As Integer = 15
End Class");
        }
        [Fact]
        public async Task DoNotSimplifyArrayTypeInFieldDeclarationsAsync() {
            await TestConversionCSharpToVisualBasicAsync(
@"class TestClass {
    int[] answer = { 1, 2 };
}",
@"Friend Class TestClass
    Private answer As Integer() = {1, 2}
End Class");
        }

        [Fact]
        public async Task TestMethodWithCommentsAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(
                @"class TestClass
{
    public void TestMethod<T, T2, T3>(out T argument, ref T2 argument2, T3 argument3) where T : class, new where T2 : struct
    {
        argument = null; //1
        argument2 = default(T2); //2
        argument3 = default(T3); //3
    }
}", @"Imports System.Runtime.InteropServices

Friend Class TestClass
    Public Sub TestMethod(Of T As {Class, New}, T2 As Structure, T3)(<Out> ByRef argument As T, ByRef argument2 As T2, ByVal argument3 As T3)
        argument = Nothing '1
        argument2 = Nothing '2
        argument3 = Nothing '3
    End Sub
End Class

2 source compilation errors:
CS1003: Syntax error, '(' expected
CS1026: ) expected");
        }

        [Fact]
        public async Task TestMethodWithReturnTypeAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(
                @"class TestClass
{
    public int TestMethod<T, T2, T3>(out T argument, ref T2 argument2, T3 argument3) where T : class, new where T2 : struct
    {
        return 0;
    }
}", @"Imports System.Runtime.InteropServices

Friend Class TestClass
    Public Function TestMethod(Of T As {Class, New}, T2 As Structure, T3)(<Out> ByRef argument As T, ByRef argument2 As T2, ByVal argument3 As T3) As Integer
        Return 0
    End Function
End Class

3 source compilation errors:
CS1003: Syntax error, '(' expected
CS1026: ) expected
CS0177: The out parameter 'argument' must be assigned to before control leaves the current method");
        }

        [Fact]
        public async Task TestStaticMethodAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(
                @"class TestClass
{
    public static void TestMethod<T, T2, T3>(out T argument, ref T2 argument2, T3 argument3) where T : class, new where T2 : struct
    {
        argument = null;
        argument2 = default(T2);
        argument3 = default(T3);
    }
}", @"Imports System.Runtime.InteropServices

Friend Class TestClass
    Public Shared Sub TestMethod(Of T As {Class, New}, T2 As Structure, T3)(<Out> ByRef argument As T, ByRef argument2 As T2, ByVal argument3 As T3)
        argument = Nothing
        argument2 = Nothing
        argument3 = Nothing
    End Sub
End Class

2 source compilation errors:
CS1003: Syntax error, '(' expected
CS1026: ) expected");
        }

        [Fact]
        public async Task TestAbstractMethodAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(
                @"abstract class TestClass
{
    public abstract void TestMethod();
}", @"Friend MustInherit Class TestClass
    Public MustOverride Sub TestMethod()
End Class");
        }

        [Fact]
        public async Task TestNewMethodIsOverloadsNotShadowsAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(
                @"class TestClass
{
    public void TestMethod()
    {
    }

    public void TestMethod(int i)
    {
    }
}

class TestSubclass : TestClass
{
    public new void TestMethod()
    {
        TestMethod(3);
        System.Console.WriteLine(""Shadowed implementation"");
    }
}", @"Friend Class TestClass
    Public Sub TestMethod()
    End Sub

    Public Sub TestMethod(ByVal i As Integer)
    End Sub
End Class

Friend Class TestSubclass
    Inherits TestClass

    Public Overloads Sub TestMethod()
        TestMethod(3)
        System.Console.WriteLine(""Shadowed implementation"")
    End Sub
End Class", conversionOptions: EmptyNamespaceOptionStrictOff);
        }


        [Fact]
        public async Task OperatorOverloadsAsync()
        {
            // Note a couple map to the same thing in C# so occasionally the result won't compile. The user can manually decide what to do in such scenarios.
            await TestConversionCSharpToVisualBasicAsync(@"public class AcmeClass
{
    public static AcmeClass operator +(int i, AcmeClass ac)
    {
        return ac;
    }
    public static AcmeClass operator +(string s, AcmeClass ac)
    {
        return ac;
    }
    public static AcmeClass operator -(int i, AcmeClass ac)
    {
        return ac;
    }
    public static AcmeClass operator !(AcmeClass ac)
    {
        return ac;
    }
    public static AcmeClass operator *(int i, AcmeClass ac)
    {
        return ac;
    }
    public static AcmeClass operator /(int i, AcmeClass ac)
    {
        return ac;
    }
    public static AcmeClass operator %(string s, AcmeClass ac)
    {
        return ac;
    }
    public static AcmeClass operator <<(AcmeClass ac, int i)
    {
        return ac;
    }
    public static AcmeClass operator >>(AcmeClass ac, int i)
    {
        return ac;
    }
    public static AcmeClass operator ==(string s, AcmeClass ac)
    {
        return ac;
    }
    public static AcmeClass operator !=(string s, AcmeClass ac)
    {
        return ac;
    }
    public static AcmeClass operator <(string s, AcmeClass ac)
    {
        return ac;
    }
    public static AcmeClass operator >(string s, AcmeClass ac)
    {
        return ac;
    }
    public static AcmeClass operator <=(string s, AcmeClass ac)
    {
        return ac;
    }
    public static AcmeClass operator >=(string s, AcmeClass ac)
    {
        return ac;
    }
    public static AcmeClass operator &(string s, AcmeClass ac)
    {
        return ac;
    }
    public static AcmeClass operator |(string s, AcmeClass ac)
    {
        return ac;
    }
}", @"Public Class AcmeClass
    Public Shared Operator +(ByVal i As Integer, ByVal ac As AcmeClass) As AcmeClass
        Return ac
    End Operator

    Public Shared Operator &(ByVal s As String, ByVal ac As AcmeClass) As AcmeClass
        Return ac
    End Operator

    Public Shared Operator -(ByVal i As Integer, ByVal ac As AcmeClass) As AcmeClass
        Return ac
    End Operator

    Public Shared Operator Not(ByVal ac As AcmeClass) As AcmeClass
        Return ac
    End Operator

    Public Shared Operator *(ByVal i As Integer, ByVal ac As AcmeClass) As AcmeClass
        Return ac
    End Operator

    Public Shared Operator /(ByVal i As Integer, ByVal ac As AcmeClass) As AcmeClass
        Return ac
    End Operator

    Public Shared Operator Mod(ByVal s As String, ByVal ac As AcmeClass) As AcmeClass
        Return ac
    End Operator

    Public Shared Operator <<(ByVal ac As AcmeClass, ByVal i As Integer) As AcmeClass
        Return ac
    End Operator

    Public Shared Operator >>(ByVal ac As AcmeClass, ByVal i As Integer) As AcmeClass
        Return ac
    End Operator

    Public Shared Operator =(ByVal s As String, ByVal ac As AcmeClass) As AcmeClass
        Return ac
    End Operator

    Public Shared Operator <>(ByVal s As String, ByVal ac As AcmeClass) As AcmeClass
        Return ac
    End Operator

    Public Shared Operator <(ByVal s As String, ByVal ac As AcmeClass) As AcmeClass
        Return ac
    End Operator

    Public Shared Operator >(ByVal s As String, ByVal ac As AcmeClass) As AcmeClass
        Return ac
    End Operator

    Public Shared Operator <=(ByVal s As String, ByVal ac As AcmeClass) As AcmeClass
        Return ac
    End Operator

    Public Shared Operator >=(ByVal s As String, ByVal ac As AcmeClass) As AcmeClass
        Return ac
    End Operator

    Public Shared Operator And(ByVal s As String, ByVal ac As AcmeClass) As AcmeClass
        Return ac
    End Operator

    Public Shared Operator Or(ByVal s As String, ByVal ac As AcmeClass) As AcmeClass
        Return ac
    End Operator
End Class");
        }

        [Fact]
        public async Task TestSealedMethodAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(
                @"class TestClass
{
    public sealed void TestMethod<T, T2, T3>(out T argument, ref T2 argument2, T3 argument3) where T : class, new where T2 : struct
    {
        argument = null;
        argument2 = default(T2);
        argument3 = default(T3);
    }
}", @"Imports System.Runtime.InteropServices

Friend Class TestClass
    Public NotOverridable Sub TestMethod(Of T As {Class, New}, T2 As Structure, T3)(<Out> ByRef argument As T, ByRef argument2 As T2, ByVal argument3 As T3)
        argument = Nothing
        argument2 = Nothing
        argument3 = Nothing
    End Sub
End Class

3 source compilation errors:
CS1003: Syntax error, '(' expected
CS1026: ) expected
CS0238: 'TestClass.TestMethod<T, T2, T3>(out T, ref T2, T3)' cannot be sealed because it is not an override
1 target compilation errors:
BC31088: 'NotOverridable' cannot be specified for methods that do not override another method.");
        }

        [Fact]
        public async Task TestExtensionMethodAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(
@"using System;
static class TestClass
{
    public static void TestMethod(this String str)
    {
    }

    public static void TestMethod2Parameters(this String str, Action<string> _)
    {
    }
}", @"Imports System
Imports System.Runtime.CompilerServices

Friend Module TestClass
    <Extension()>
    Public Sub TestMethod(ByVal str As String)
    End Sub

    <Extension()>
    Public Sub TestMethod2Parameters(ByVal str As String, ByVal __ As Action(Of String))
    End Sub
End Module", conversionOptions: EmptyNamespaceOptionStrictOff);
        }

        [Fact]
        public async Task TestExtensionMethodWithExistingImportAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(
@"using System; //Gets simplified away
using System.Runtime.CompilerServices;

static class TestClass
{
    public static void TestMethod(this String str)
    {
    }
}", @"Imports System.Runtime.CompilerServices

Friend Module TestClass
    <Extension()>
    Public Sub TestMethod(ByVal str As String)
    End Sub
End Module");
        }

        [Fact]
        public async Task TestPropertyAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(
                @"class TestClass
{
    public int Test { get; set; }
    public int Test2 {
        get { return 0; }
    }
    int m_test3;
    public int Test3 {
        get { return this.m_test3; }
        set { this.m_test3 = value; }
    }
}", @"Friend Class TestClass
    Public Property Test As Integer

    Public ReadOnly Property Test2 As Integer
        Get
            Return 0
        End Get
    End Property

    Private m_test3 As Integer

    Public Property Test3 As Integer
        Get
            Return m_test3
        End Get
        Set(ByVal value As Integer)
            m_test3 = value
        End Set
    End Property
End Class");
        }

        [Fact]
        public async Task CaseConflict_PropertyAndField_EnsureOtherClassNotAffectedAsync() {
            await TestConversionCSharpToVisualBasicAsync(
@"public class HasConflictingPropertyAndField {
    int test;
    public int Test {
        get { return test; }
        set { test = value; }
    }
}
public class ShouldNotChange {
    int test;
    public int Test1 {
        get { return test; }
        set { test = value; }
    }
}
",
                @"Public Class HasConflictingPropertyAndField
    Private testField As Integer

    Public Property Test As Integer
        Get
            Return testField
        End Get
        Set(ByVal value As Integer)
            testField = value
        End Set
    End Property
End Class

Public Class ShouldNotChange
    Private test As Integer

    Public Property Test1 As Integer
        Get
            Return test
        End Get
        Set(ByVal value As Integer)
            test = value
        End Set
    End Property
End Class");
        }

        [Fact]
        public async Task CaseConflict_ArgumentFieldAndMethodWithOverloadsAsync() {
            await TestConversionCSharpToVisualBasicAsync(
                @"public class HasConflictingMethodAndField {

    public int HasConflictingParam(int test) {
        this.teSt = test;
        return test;
    }

    int teSt;

    private int test() {
        return 1;
    }

    public int Test() {
        return test();
    }

    private int test(int arg) {
        return arg;
    }

    public int Test(int arg) {
        return test(arg);
    }
}",
                @"Public Class HasConflictingMethodAndField
    Public Function HasConflictingParam(ByVal test As Integer) As Integer
        teStField = test
        Return test
    End Function

    Private teStField As Integer

    Private Function testMethod() As Integer
        Return 1
    End Function

    Public Function Test() As Integer
        Return testMethod()
    End Function

    Private Function testMethod(ByVal arg As Integer) As Integer
        Return arg
    End Function

    Public Function Test(ByVal arg As Integer) As Integer
        Return testMethod(arg)
    End Function
End Class");
        }

        [Fact]
        public async Task CaseConflict_ArgumentFieldAndPropertyAsync() {
            await TestConversionCSharpToVisualBasicAsync(
@"public class HasConflictingPropertyAndField {

    public int HasConflictingParam(int test) {
        this.test = test;
        return test;
    }

    int test;

    public int Test {
        get { return test; }
        set { test = value; }
    }
}",
                @"Public Class HasConflictingPropertyAndField
    Public Function HasConflictingParam(ByVal test As Integer) As Integer
        testField = test
        Return test
    End Function

    Private testField As Integer

    Public Property Test As Integer
        Get
            Return testField
        End Get
        Set(ByVal value As Integer)
            testField = value
        End Set
    End Property
End Class");
        }

        [Fact]
        public async Task CaseConflict_ArgumentPropertyAndFieldAsync() {
            await TestConversionCSharpToVisualBasicAsync(
@"public class HasConflictingPropertyAndField {
    int test;
    public int Test {
        get { return test; }
        set { test = value; }
    }
    public int HasConflictingParam(int test) {
        Test = test;
        return test;
    }
}",
                @"Public Class HasConflictingPropertyAndField
    Private testField As Integer

    Public Property Test As Integer
        Get
            Return testField
        End Get
        Set(ByVal value As Integer)
            testField = value
        End Set
    End Property

    Public Function HasConflictingParam(ByVal test As Integer) As Integer
        Me.Test = test
        Return test
    End Function
End Class");
        }

        [Fact]
        public async Task CaseConflict_PartialClass_ArgumentFieldPropertyAndLocalInBothPartsAsync() {
            await TestConversionCSharpToVisualBasicAsync(
@"public partial class HasConflictingPropertyAndField {
    public int HasConflictingParam(int test) {
        int TEST = 0;
        this.test = test + TEST;
        return test;
    }
}

public partial class HasConflictingPropertyAndField {
    int test;
    public int Test {
        get 
        {
            int TEST = 0;
            return test + TEST;
        }
        set { test = value; }
    }
}",
                @"Public Partial Class HasConflictingPropertyAndField
    Public Function HasConflictingParam(ByVal test As Integer) As Integer
        Dim lTEST As Integer = 0
        testField = test + lTEST
        Return test
    End Function
End Class

Public Partial Class HasConflictingPropertyAndField
    Private testField As Integer

    Public Property Test As Integer
        Get
            Dim lTEST As Integer = 0
            Return testField + lTEST
        End Get
        Set(ByVal value As Integer)
            testField = value
        End Set
    End Property
End Class");
        }

        [Fact]
        public async Task TestPropertyWithExpressionBodyAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(
                @"public class ConversionResult
{
    private string _sourcePathOrNull;

    public string SourcePathOrNull {
        get => _sourcePathOrNull;
        set => _sourcePathOrNull = string.IsNullOrWhiteSpace(value) ? null : value;
    }
}", @"Public Class ConversionResult
    Private _sourcePathOrNull As String

    Public Property SourcePathOrNull As String
        Get
            Return _sourcePathOrNull
        End Get
        Set(ByVal value As String)
            _sourcePathOrNull = If(String.IsNullOrWhiteSpace(value), Nothing, value)
        End Set
    End Property
End Class");
        }

        [Fact]
        public async Task TestOmmittedAccessorsReplacedWithExpressionBodyAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(
                @"class MyFavColor
{
    private string[] favColor => new string[] {""Red"", ""Green""};
    public string this[int index] => favColor[index];
}
", @"Friend Class MyFavColor
    Private ReadOnly Property favColor As String()
        Get
            Return New String() {""Red"", ""Green""}
        End Get
    End Property

    Default Public ReadOnly Property Item(ByVal index As Integer) As String
        Get
            Return favColor(index)
        End Get
    End Property
End Class");
        }

        [Fact]
        public async Task TestPropertyWithExpressionBodyThatCanBeStatementAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(
                @"using System;

public class ConversionResult
{
    private int _num;

    public string Num {
        set => _num++;
    }

    public string Blanket {
        set => throw new Exception();
    }
}", @"Imports System

Public Class ConversionResult
    Private _num As Integer

    Public WriteOnly Property Num As String
        Set(ByVal value As String)
            _num += 1
        End Set
    End Property

    Public WriteOnly Property Blanket As String
        Set(ByVal value As String)
            Throw New Exception()
        End Set
    End Property
End Class");
        }

        [Fact]
        public async Task TestPropertyWithAttributeAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(
                @"class TestClass
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    int value { get; set; }
}", @"Friend Class TestClass
    <DatabaseGenerated(DatabaseGeneratedOption.None)>
    Private Property value As Integer
End Class

3 source compilation errors:
CS0246: The type or namespace name 'DatabaseGeneratedAttribute' could not be found (are you missing a using directive or an assembly reference?)
CS0246: The type or namespace name 'DatabaseGenerated' could not be found (are you missing a using directive or an assembly reference?)
CS0103: The name 'DatabaseGeneratedOption' does not exist in the current context
2 target compilation errors:
BC30002: Type 'DatabaseGenerated' is not defined.
BC30451: 'DatabaseGeneratedOption' is not declared. It may be inaccessible due to its protection level.
");
        }

        [Fact]
        public async Task TestClassWithGlobalAttributeAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(
                @"[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
internal class Resources
{
}", @"
<Global.System.Runtime.CompilerServices.CompilerGeneratedAttribute()>
Friend Class Resources
End Class

1 target compilation errors:
BC30002: Type 'Global.System.Runtime.CompilerServices.CompilerGeneratedAttribute' is not defined.
");
        }

        [Fact]
        public async Task TestConstructorAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(
                @"class TestClass<T, T2, T3> where T : class, new where T2 : struct
{
    public TestClass(out T argument, ref T2 argument2, T3 argument3)
    {
    }
}", @"Imports System.Runtime.InteropServices

Friend Class TestClass(Of T As {Class, New}, T2 As Structure, T3)
    Public Sub New(<Out> ByRef argument As T, ByRef argument2 As T2, ByVal argument3 As T3)
    End Sub
End Class

3 source compilation errors:
CS1003: Syntax error, '(' expected
CS1026: ) expected
CS0177: The out parameter 'argument' must be assigned to before control leaves the current method");
        }


        [Fact]
        public async Task TestStaticConstructorAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(
                @"static SurroundingClass()
{
}", @"Shared Sub New()
End Sub");
        }

        [Fact]
        public async Task TestConstructorCallingBaseAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(
                @"public class MyBaseClass
{
    public MyBaseClass(object o)
    {
    }
}

public sealed class MyClass
 : MyBaseClass
{
	 public MyClass(object o)
	  : base(o)
	{
	}
}", @"Public Class MyBaseClass
    Public Sub New(ByVal o As Object)
    End Sub
End Class

Public NotInheritable Class [MyClass]
    Inherits MyBaseClass

    Public Sub New(ByVal o As Object)
        MyBase.New(o)
    End Sub
End Class");
        }

        [Fact]
        public async Task TestDestructorAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(
                @"class TestClass
{
    ~TestClass()
    {
    }
}", @"Friend Class TestClass
    Protected Overrides Sub Finalize()
    End Sub
End Class");
        }

        [Fact]
        public async Task TestExternDllImportAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(
                @"[DllImport(""kernel32.dll"", SetLastError = true)]
static extern IntPtr OpenProcess(AccessMask dwDesiredAccess, bool bInheritHandle, uint dwProcessId);", @"<DllImport(""kernel32.dll"", SetLastError:=True)>
Private Shared Function OpenProcess(ByVal dwDesiredAccess As AccessMask, ByVal bInheritHandle As Boolean, ByVal dwProcessId As UInteger) As IntPtr
End Function

5 source compilation errors:
CS0246: The type or namespace name 'AccessMask' could not be found (are you missing a using directive or an assembly reference?)
CS0246: The type or namespace name 'IntPtr' could not be found (are you missing a using directive or an assembly reference?)
CS0246: The type or namespace name 'DllImportAttribute' could not be found (are you missing a using directive or an assembly reference?)
CS0246: The type or namespace name 'DllImport' could not be found (are you missing a using directive or an assembly reference?)
CS0246: The type or namespace name 'SetLastError' could not be found (are you missing a using directive or an assembly reference?)
1 target compilation errors:
BC30002: Type 'AccessMask' is not defined.");
        }

        [Fact]
        public async Task TestEventAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(
                @"class TestClass
{
    public event EventHandler MyEvent;
}", @"Friend Class TestClass
    Public Event MyEvent As EventHandler
End Class

1 source compilation errors:
CS0246: The type or namespace name 'EventHandler' could not be found (are you missing a using directive or an assembly reference?)");
        }

        [Fact]
        public async Task TestCustomEventAsync() {
            await TestConversionCSharpToVisualBasicAsync(
@"using System;

class TestClass {
    EventHandler backingField;

    public event EventHandler MyEvent {
        add {
            this.backingField += value;
        }
        remove {
            this.backingField -= value;
        }
    }
    public void Reset() {
        backingField = null;
    }
}",
                @"Imports System

Friend Class TestClass
    Private backingField As EventHandler

    Public Custom Event MyEvent As EventHandler
        AddHandler(ByVal value As EventHandler)
            backingField = [Delegate].Combine(backingField, value)
        End AddHandler
        RemoveHandler(ByVal value As EventHandler)
            backingField = [Delegate].Remove(backingField, value)
        End RemoveHandler
        RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
            backingField?(sender, e)
        End RaiseEvent
    End Event

    Public Sub Reset()
        backingField = Nothing
    End Sub
End Class

2 target compilation errors:
BC36637: The '?' character cannot be used here.
BC30451: '[Delegate]' is not declared. It may be inaccessible due to its protection level.");
        }
        [Fact]
        public async Task TestCustomEvent_TrivialExpressionAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(
@"using System;
class TestClass {
    EventHandler _backingField;

    public event EventHandler MyEvent {
        add { _backingField += value; }
        remove { _backingField -= value; }
    }
}",
                @"Imports System

Friend Class TestClass
    Private _backingField As EventHandler

    Public Custom Event MyEvent As EventHandler
        AddHandler(ByVal value As EventHandler)
            _backingField = [Delegate].Combine(_backingField, value)
        End AddHandler
        RemoveHandler(ByVal value As EventHandler)
            _backingField = [Delegate].Remove(_backingField, value)
        End RemoveHandler
        RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
            _backingField?(sender, e)
        End RaiseEvent
    End Event
End Class

2 target compilation errors:
BC36637: The '?' character cannot be used here.
BC30451: '[Delegate]' is not declared. It may be inaccessible due to its protection level.");
        }
        [Fact]
        public async Task TestCustomEventUsingFieldEventAsync() {
            await TestConversionCSharpToVisualBasicAsync(
@"using System;

class TestClass {
    event EventHandler backingField;

    public event EventHandler MyEvent {
        add { backingField += value; }
        remove { backingField -= value; }
    }
}",
@"Imports System

Friend Class TestClass
    Private Event backingField As EventHandler

    Public Custom Event MyEvent As EventHandler
        AddHandler(ByVal value As EventHandler)
            AddHandler backingField, value
        End AddHandler
        RemoveHandler(ByVal value As EventHandler)
            RemoveHandler backingField, value
        End RemoveHandler
        RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
            RaiseEvent backingField(sender, e)
        End RaiseEvent
    End Event
End Class");
        }
        [Fact]
        public async Task SubscribeEventInPropertySetterAsync() {
            await TestConversionCSharpToVisualBasicAsync(
@"using System.ComponentModel;

class TestClass {
    OwnerClass owner;

    public OwnerClass Owner {
        get { return owner; }
        set {
            owner = value;
            ((INotifyPropertyChanged)owner).PropertyChanged += OnOwnerChanged;
        }
    }
    void OnOwnerChanged(object sender, PropertyChangedEventArgs args) { }
}
class OwnerClass : INotifyPropertyChanged {
}",
@"Imports System.ComponentModel

Friend Class TestClass
    Private ownerField As OwnerClass

    Public Property Owner As OwnerClass
        Get
            Return ownerField
        End Get
        Set(ByVal value As OwnerClass)
            ownerField = value
            AddHandler CType(ownerField, INotifyPropertyChanged).PropertyChanged, AddressOf OnOwnerChanged
        End Set
    End Property

    Private Sub OnOwnerChanged(ByVal sender As Object, ByVal args As PropertyChangedEventArgs)
    End Sub
End Class

Friend Class OwnerClass
    Implements INotifyPropertyChanged
End Class

1 source compilation errors:
CS0535: 'OwnerClass' does not implement interface member 'INotifyPropertyChanged.PropertyChanged'
1 target compilation errors:
BC30149: Class 'OwnerClass' must implement 'Event PropertyChanged As PropertyChangedEventHandler' for interface 'INotifyPropertyChanged'.");
        }
        [Fact]
        public async Task TestIndexerAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(
                @"class TestClass
{
    private int[] _Items;

    public int this[int index]
    {
        get
        {
            return _Items[index];
        }
        set
        {
            _Items[index] = value;
        }
    }

    public int this[string index]
    {
        get
        {
            return 0;
        }
    }

    private int m_test3;

    public int this[double index]
    {
        get
        {
            return this.m_test3;
        }
        set
        {
            this.m_test3 = value;
        }
    }
}", @"Friend Class TestClass
    Private _Items As Integer()

    Default Public Property Item(ByVal index As Integer) As Integer
        Get
            Return _Items(index)
        End Get
        Set(ByVal value As Integer)
            _Items(index) = value
        End Set
    End Property

    Default Public ReadOnly Property Item(ByVal index As String) As Integer
        Get
            Return 0
        End Get
    End Property

    Private m_test3 As Integer

    Default Public Property Item(ByVal index As Double) As Integer
        Get
            Return m_test3
        End Get
        Set(ByVal value As Integer)
            m_test3 = value
        End Set
    End Property
End Class");
        }
        [Fact]
        public async Task Interface_IndexerAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(
@"public interface iDisplay {
    object this[int i] { get; set; }
}",
                @"Public Interface iDisplay
    Default Property Item(ByVal i As Integer) As Object
End Interface");
        }
        [Fact]
        public async Task Indexer_EmptySetterAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(
@"using System.Collections

class TestClass : IList {

    public object this[int index] {
        get { return index; }
        set { }
    }
}",
                @"Imports System.Collections

Friend Class TestClass
    Implements IList

    Default Public Property Item(ByVal index As Integer) As Object Implements IList.Item
        Get
            Return index
        End Get
        Set(ByVal value As Object)
        End Set
    End Property
End Class

15 source compilation errors:
CS1002: ; expected
CS0535: 'TestClass' does not implement interface member 'IList.Add(object)'
CS0535: 'TestClass' does not implement interface member 'IList.Contains(object)'
CS0535: 'TestClass' does not implement interface member 'IList.Clear()'
CS0535: 'TestClass' does not implement interface member 'IList.IndexOf(object)'
CS0535: 'TestClass' does not implement interface member 'IList.Insert(int, object)'
CS0535: 'TestClass' does not implement interface member 'IList.Remove(object)'
CS0535: 'TestClass' does not implement interface member 'IList.RemoveAt(int)'
CS0535: 'TestClass' does not implement interface member 'IList.IsReadOnly'
CS0535: 'TestClass' does not implement interface member 'IList.IsFixedSize'
CS0535: 'TestClass' does not implement interface member 'ICollection.CopyTo(Array, int)'
CS0535: 'TestClass' does not implement interface member 'ICollection.Count'
CS0535: 'TestClass' does not implement interface member 'ICollection.SyncRoot'
CS0535: 'TestClass' does not implement interface member 'ICollection.IsSynchronized'
CS0535: 'TestClass' does not implement interface member 'IEnumerable.GetEnumerator()'
14 target compilation errors:
BC30149: Class 'TestClass' must implement 'Function Add(value As Object) As Integer' for interface 'IList'.
BC30149: Class 'TestClass' must implement 'Function Contains(value As Object) As Boolean' for interface 'IList'.
BC30149: Class 'TestClass' must implement 'Sub Clear()' for interface 'IList'.
BC30149: Class 'TestClass' must implement 'Function IndexOf(value As Object) As Integer' for interface 'IList'.
BC30149: Class 'TestClass' must implement 'Sub Insert(index As Integer, value As Object)' for interface 'IList'.
BC30149: Class 'TestClass' must implement 'Sub Remove(value As Object)' for interface 'IList'.
BC30149: Class 'TestClass' must implement 'Sub RemoveAt(index As Integer)' for interface 'IList'.
BC30149: Class 'TestClass' must implement 'ReadOnly Property IsReadOnly As Boolean' for interface 'IList'.
BC30149: Class 'TestClass' must implement 'ReadOnly Property IsFixedSize As Boolean' for interface 'IList'.
BC30149: Class 'TestClass' must implement 'Sub CopyTo(array As Array, index As Integer)' for interface 'ICollection'.
BC30149: Class 'TestClass' must implement 'ReadOnly Property Count As Integer' for interface 'ICollection'.
BC30149: Class 'TestClass' must implement 'ReadOnly Property SyncRoot As Object' for interface 'ICollection'.
BC30149: Class 'TestClass' must implement 'ReadOnly Property IsSynchronized As Boolean' for interface 'ICollection'.
BC30149: Class 'TestClass' must implement 'Function GetEnumerator() As IEnumerator' for interface 'IEnumerable'.");
        }
        [Fact]
        public async Task Indexer_BadCaseAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(
@"class TestClass {
    public object this[int index] {
        get { }
        set { }
    }
}",
                @"Friend Class TestClass
    Default Public Property Item(ByVal index As Integer) As Object
        Get
        End Get
        Set(ByVal value As Object)
        End Set
    End Property
End Class

1 source compilation errors:
CS0161: 'TestClass.this[int].get': not all code paths return a value");
        }
        [Fact]
        public async Task NameMatchesWithTypeDateAsync() {
            await TestConversionCSharpToVisualBasicAsync(
@"using System; //Gets simplified away

class TestClass {
    private DateTime date;
}",
                @"Friend Class TestClass
    Private [date] As Date
End Class");
        }
        [Fact]
        public async Task ParameterWithNamespaceTestAsync() {
            await TestConversionCSharpToVisualBasicAsync(
@"public class TestClass {
    public object TestMethod(System.Type param1, System.Globalization.CultureInfo param2) {
        return null;
    }
}",
                @"Public Class TestClass
    Public Function TestMethod(ByVal param1 As System.Type, ByVal param2 As System.Globalization.CultureInfo) As Object
        Return Nothing
    End Function
End Class", conversionOptions: EmptyNamespaceOptionStrictOff);
    }

        [Fact]// The stack trace displayed will change from time to time. Feel free to update this characterization test appropriately.
        public async Task InvalidOperatorOverloadsShowErrorInlineCharacterizationAsync()
        {
            // No valid conversion to C# - to implement this you'd need to create a new method, and convert all callers to use it.
            var convertedCode = await ConvertAsync<CSToVBConversion>(@"public class AcmeClass
{
    public static AcmeClass operator ++(int i, AcmeClass ac)
    {
        return ac;
    }
    public static AcmeClass operator --(string s, AcmeClass ac)
    {
        return ac;
    }
}");

            Assert.Contains("Cannot convert", convertedCode);
            Assert.Contains("public static AcmeClass operator ++(int i, AcmeClass ac)", convertedCode);
            Assert.Contains("public static AcmeClass operator --(string s, AcmeClass ac)", convertedCode);
        }
        [Fact]
        public async Task MethodOverloadsAsync() {
            await TestConversionCSharpToVisualBasicAsync(
@"public class MailEmployee {
    public string Email { get; set; }
    protected bool Equals(MailEmployee other) {
        return Email == other.Email;
    }
    public override bool Equals(object obj) {
        return Equals((MailEmployee)obj);
    }
}",
                @"Public Class MailEmployee
    Public Property Email As String

    Protected Overloads Function Equals(ByVal other As MailEmployee) As Boolean
        Return Equals(Email, other.Email)
    End Function

    Public Overrides Function Equals(ByVal obj As Object) As Boolean
        Return Equals(CType(obj, MailEmployee))
    End Function
End Class");
        }
        [Fact]
        public async Task Interface_GetAsync() {
            await TestConversionCSharpToVisualBasicAsync(
@"public interface IParametersProvider {
    IEnumerable<object> Parameters { get; }
}",
                @"Public Interface IParametersProvider
    ReadOnly Property Parameters As IEnumerable(Of Object)
End Interface

1 source compilation errors:
CS0246: The type or namespace name 'IEnumerable<>' could not be found (are you missing a using directive or an assembly reference?)");
        }
        [Fact]
        public async Task Interface_SetAsync() {
            await TestConversionCSharpToVisualBasicAsync(
@"public interface IParametersProvider {
    IEnumerable<object> Parameters { set; }
}",
                @"Public Interface IParametersProvider
    WriteOnly Property Parameters As IEnumerable(Of Object)
End Interface

1 source compilation errors:
CS0246: The type or namespace name 'IEnumerable<>' could not be found (are you missing a using directive or an assembly reference?)");
        }
        [Fact]
        public async Task PartialMethodAsync() {
            await TestConversionCSharpToVisualBasicAsync(
@"public partial class Entities {
    partial void OnContextCreated();
}",
@"Public Partial Class Entities
    Partial Private Sub OnContextCreated()
    End Sub
End Class");
        }

    }
}
