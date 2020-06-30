using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.VB
{
    public class SpecialConversionTests : ConverterTestBase
    {
        [Fact]
        public async Task TestSimpleInlineAssignAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(
                @"class TestClass
{
    void TestMethod()
    {
        int a, b;
        b = a = 5;
    }
}", @"Friend Class TestClass
    Private Sub TestMethod()
        Dim a, b As Integer
        b = CSharpImpl.__Assign(a, 5)
    End Sub

    Private Class CSharpImpl
        <Obsolete(""Please refactor calling code to use normal Visual Basic assignment"")>
        Shared Function __Assign(Of T)(ByRef target As T, value As T) As T
            target = value
            Return value
        End Function
    End Class
End Class

1 target compilation errors:
BC30451: 'CSharpImpl.__Assign' is not declared. It may be inaccessible due to its protection level.");
        }

        [Fact]
        public async Task TestSimplePostIncrementAssignAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(
@"class TestClass{
    void TestMethod()
    {
        int a = 5, b;
        b = a++;
    }
}",
@"Friend Class TestClass
    Private Sub TestMethod()
        Dim b As Integer, a As Integer = 5
        b = System.Math.Min(System.Threading.Interlocked.Increment(a), a - 1)
    End Sub
End Class", conversionOptions: EmptyNamespaceOptionStrictOff);
        }

        [Fact]
        public async Task RaiseEventOneLinersAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(
                @"using System;

class TestClass
{
    event EventHandler MyEvent;

    void TestMethod()
    {
        MyEvent(this, EventArgs.Empty);
        if (MyEvent != null) MyEvent(this, EventArgs.Empty);
        MyEvent.Invoke(this, EventArgs.Empty);
        MyEvent?.Invoke(this, EventArgs.Empty);
    }
}", @"Imports System

Friend Class TestClass
    Private Event MyEvent As EventHandler

    Private Sub TestMethod()
        RaiseEvent MyEvent(Me, EventArgs.Empty)
        RaiseEvent MyEvent(Me, EventArgs.Empty)
        RaiseEvent MyEvent(Me, EventArgs.Empty)
        RaiseEvent MyEvent(Me, EventArgs.Empty)
    End Sub
End Class");
        }

        [Fact]
        public async Task RaiseEventInElseAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(
                @"using System;

public class Foo
{
    public event EventHandler<EventArgs> Bar;

    protected void OnBar(EventArgs e)
    {
        if (Bar == null)
            System.Diagnostics.Debug.WriteLine(""No subscriber"");
        else
            Bar.Invoke(this, e);
    }
}", @"Imports System

Public Class Foo
    Public Event Bar As EventHandler(Of EventArgs)

    Protected Sub OnBar(ByVal e As EventArgs)
        If BarEvent Is Nothing Then
            Debug.WriteLine(""No subscriber"")
        Else
            RaiseEvent Bar(Me, e)
        End If
    End Sub
End Class
");
        }

        [Fact]
        public async Task RaiseEventReversedConditionalAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(
                @"using System;

class TestClass
{
    event EventHandler MyEvent;

    void TestMethod()
    {
        if (null != MyEvent) { MyEvent(this, EventArgs.Empty); }
    }
}", @"Imports System

Friend Class TestClass
    Private Event MyEvent As EventHandler

    Private Sub TestMethod()
        RaiseEvent MyEvent(Me, EventArgs.Empty)
    End Sub
End Class");
        }

        [Fact]
        public async Task RaiseEventQualifiedAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(
                @"using System;

class TestClass
{
    event EventHandler MyEvent;

    void TestMethod()
    {
        if (this.MyEvent != null) this.MyEvent(this, EventArgs.Empty);
    }
}", @"Imports System

Friend Class TestClass
    Private Event MyEvent As EventHandler

    Private Sub TestMethod()
        RaiseEvent MyEvent(Me, EventArgs.Empty)
    End Sub
End Class");
        }

        [Fact]
        public async Task RaiseEventInNestedBracketsAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(
                @"using System;

class TestClass
{
    event EventHandler MyEvent;

    void TestMethod()
    {
        if ((MyEvent != null)) this.MyEvent.Invoke(this, EventArgs.Empty);
    }
}", @"Imports System

Friend Class TestClass
    Private Event MyEvent As EventHandler

    Private Sub TestMethod()
        RaiseEvent MyEvent(Me, EventArgs.Empty)
    End Sub
End Class");
        }

        [Fact]
        public async Task RaiseEventQualifiedWithNestedBracketsAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(
                @"using System;

class TestClass
{
    event EventHandler MyEvent;

    void TestMethod()
    {
        if ((this.MyEvent != null)) { this.MyEvent(this, EventArgs.Empty); }
    }
}", @"Imports System

Friend Class TestClass
    Private Event MyEvent As EventHandler

    Private Sub TestMethod()
        RaiseEvent MyEvent(Me, EventArgs.Empty)
    End Sub
End Class");
        }

        /// <summary>
        /// Intentionally unknown type used to ensure imperfect compilation errs towards common case
        /// </summary>
        [Fact]
        public async Task IfStatementSimilarToRaiseEventAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(
                @"class TestClass
{
    void TestMethod()
    {
        if (FullImage != null) DrawImage();
    }
}", @"Friend Class TestClass
    Private Sub TestMethod()
        If FullImage IsNot Nothing Then DrawImage()
    End Sub
End Class

2 source compilation errors:
CS0103: The name 'FullImage' does not exist in the current context
CS0103: The name 'DrawImage' does not exist in the current context
2 target compilation errors:
BC30451: 'FullImage' is not declared. It may be inaccessible due to its protection level.
BC30451: 'DrawImage' is not declared. It may be inaccessible due to its protection level.", expectCompilationErrors: true);
        }

        /// <summary>
        /// Intentionally unknown type used to ensure imperfect compilation errs towards common case
        /// </summary>
        [Fact]
        public async Task IfStatementSimilarToRaiseEventRegressionTestAsync()
        {
            // regression test:
            await TestConversionCSharpToVisualBasicAsync(
                @"class TestClass
{
    void TestMethod()
    {
        if (FullImage != null) e.DrawImage();
    }
}", @"Friend Class TestClass
    Private Sub TestMethod()
        If FullImage IsNot Nothing Then e.DrawImage()
    End Sub
End Class

2 source compilation errors:
CS0103: The name 'FullImage' does not exist in the current context
CS0103: The name 'e' does not exist in the current context
2 target compilation errors:
BC30451: 'FullImage' is not declared. It may be inaccessible due to its protection level.
BC30451: 'e' is not declared. It may be inaccessible due to its protection level.", expectCompilationErrors: true);
        }

        /// <summary>
        /// Intentionally unknown type used to ensure imperfect compilation errs towards common case
        /// </summary>
        [Fact]
        public async Task IfStatementSimilarToRaiseEventWithBracesAnotherAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(
                @"class TestClass
{
    void TestMethod()
    {
        if (FullImage != null) { DrawImage(); }
    }
}", @"Friend Class TestClass
    Private Sub TestMethod()
        If FullImage IsNot Nothing Then
            DrawImage()
        End If
    End Sub
End Class

2 source compilation errors:
CS0103: The name 'FullImage' does not exist in the current context
CS0103: The name 'DrawImage' does not exist in the current context
2 target compilation errors:
BC30451: 'FullImage' is not declared. It may be inaccessible due to its protection level.
BC30451: 'DrawImage' is not declared. It may be inaccessible due to its protection level.", expectCompilationErrors: true);
        }

        /// <summary>
        /// Intentionally unknown type used to ensure imperfect compilation errs towards common case
        /// </summary>
        [Fact]
        public async Task IfStatementSimilarToRaiseEventAnother2Async()
        {
            await TestConversionCSharpToVisualBasicAsync(
                @"class TestClass
{
    void TestMethod()
    {
        if (Tiles != null) foreach (Tile t in Tiles) this.TileTray.Controls.Remove(t);
    }
}", @"Friend Class TestClass
    Private Sub TestMethod()
        If Tiles IsNot Nothing Then

            For Each t As Tile In Tiles
                Me.TileTray.Controls.Remove(t)
            Next
        End If
    End Sub
End Class

3 source compilation errors:
CS0103: The name 'Tiles' does not exist in the current context
CS0246: The type or namespace name 'Tile' could not be found (are you missing a using directive or an assembly reference?)
CS1061: 'TestClass' does not contain a definition for 'TileTray' and no accessible extension method 'TileTray' accepting a first argument of type 'TestClass' could be found (are you missing a using directive or an assembly reference?)
3 target compilation errors:
BC30451: 'Tiles' is not declared. It may be inaccessible due to its protection level.
BC30002: Type 'Tile' is not defined.
BC30456: 'TileTray' is not a member of 'TestClass'.", expectCompilationErrors: true);
        }

        /// <summary>
        /// VB's overload resolution is much poorer than C#'s in relation to Func/Action (in C# it was improved to support Linq method chaining, but VB has a more extensive query syntax instead)
        /// If there are any overloads (including the extension method version of a method vs its non extension method version), VB needs an exact match (with no narrowing conversions).
        /// This means Funcs/Actions need to be wrapped in a typed constructor such as New Action(Of String)
        /// </summary>
        [Fact]
        public async Task AddressOfWhereVbTypeInferenceIsWeakerAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(@"using System;

static class TestClass
{
    private static object TypeSwitch(this object obj, Func<string, object> matchFunc1, Func<int, object> matchFunc2, Func<object, object> defaultFunc)
    {
        return null;
    }

    private static object ConvertInt(int node)
    {
        return node;
    }

    private static object ConvertString(string node)
    {
        return node;
    }

    public static object Convert(object node)
    {
        return node.TypeSwitch(ConvertString, ConvertInt, _ => throw new NotImplementedException($""Conversion for '{node.GetType()}' not implemented""));
    }
}", @"Imports System
Imports System.Runtime.CompilerServices

Friend Module TestClass
    <Extension()>
    Private Function TypeSwitch(ByVal obj As Object, ByVal matchFunc1 As Func(Of String, Object), ByVal matchFunc2 As Func(Of Integer, Object), ByVal defaultFunc As Func(Of Object, Object)) As Object
        Return Nothing
    End Function

    Private Function ConvertInt(ByVal node As Integer) As Object
        Return node
    End Function

    Private Function ConvertString(ByVal node As String) As Object
        Return node
    End Function

    Public Function Convert(ByVal node As Object) As Object
        Return node.TypeSwitch(New Func(Of String, Object)(AddressOf ConvertString), New Func(Of Integer, Object)(AddressOf ConvertInt), Function(__)
                                                                                                                                             Throw New NotImplementedException($""Conversion for '{node.GetType()}' not implemented"")
                                                                                                                                         End Function)
    End Function
End Module");
        }

        [Fact]
        public async Task HexAndBinaryLiteralsAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(
                @"class Test
{
    public int CR = 0x0D * 0b1;
}", @"Friend Class Test
    Public CR As Integer = &H0D * &B1
End Class");
        }

        [Fact]
        public async Task CaseConflict_LocalWithLocalAsync() {
            await TestConversionCSharpToVisualBasicAsync(
@"void Test() {
    object aB = 5;
    int Ab = (int) o;
}",
                @"Private Sub Test()
    Dim lAB As Object = 5
    Dim Ab As Integer = CInt(o)
End Sub

1 source compilation errors:
CS0103: The name 'o' does not exist in the current context
1 target compilation errors:
BC30451: 'o' is not declared. It may be inaccessible due to its protection level.");
        }
        [Fact]
        public async Task CaseConflict_LocalWithLocalInMethodAsync() {
            await TestConversionCSharpToVisualBasicAsync(
@"void Test() {
    object test = 5;
    int tesT = (int) o;
}",
                @"Private Sub Test()
    Dim lTest1 As Object = 5
    Dim lTesT As Integer = CInt(o)
End Sub

1 source compilation errors:
CS0103: The name 'o' does not exist in the current context
1 target compilation errors:
BC30451: 'o' is not declared. It may be inaccessible due to its protection level.");
        }
        [Fact]
        public async Task CaseConflict_LocalWithLocalInPropertyAsync() {
            await TestConversionCSharpToVisualBasicAsync(
@"public int Test {
    get {
        object test = 5;
        int tesT = (int) o;
        return test;
    }
}",
                @"Public ReadOnly Property Test As Integer
    Get
        Dim lTest1 As Object = 5
        Dim lTesT As Integer = CInt(o)
        Return lTest1
    End Get
End Property

2 source compilation errors:
CS0103: The name 'o' does not exist in the current context
CS0266: Cannot implicitly convert type 'object' to 'int'. An explicit conversion exists (are you missing a cast?)
1 target compilation errors:
BC30451: 'o' is not declared. It may be inaccessible due to its protection level.");
        }

        [Fact]
        public async Task CaseConflict_LocalWithLocalInEventAsync() {
            await TestConversionCSharpToVisualBasicAsync(
@"class TestClass {
    System.EventHandler test;

    public event System.EventHandler Test {
        add {
            object teSt = 5;
            int tesT = (int)o;
            test += value;
        }
        remove {
            object teSt = 5;
            int tesT = (int)o;
            test -= value;
        }
    }
}",
                @"Friend Class TestClass
    Private testField As EventHandler

    Public Custom Event Test As EventHandler
        AddHandler(ByVal value As EventHandler)
            Dim lTeSt1 As Object = 5
            Dim lTesT As Integer = CInt(o)
            testField = [Delegate].Combine(testField, value)
        End AddHandler
        RemoveHandler(ByVal value As EventHandler)
            Dim lTeSt1 As Object = 5
            Dim lTesT As Integer = CInt(o)
            testField = [Delegate].Remove(testField, value)
        End RemoveHandler
        RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
            testField?(sender, e)
        End RaiseEvent
    End Event
End Class

1 source compilation errors:
CS0103: The name 'o' does not exist in the current context
3 target compilation errors:
BC36637: The '?' character cannot be used here.
BC30451: 'o' is not declared. It may be inaccessible due to its protection level.
BC30451: '[Delegate]' is not declared. It may be inaccessible due to its protection level.");
        }
        [Fact]
        public async Task CaseConflict_LocalWithArgumentMethodAsync() {
            await TestConversionCSharpToVisualBasicAsync(
@"int Method(object test) {
    int tesT = (int)test;
    return tesT;
}",
                @"Private Function Method(ByVal test As Object) As Integer
    Dim lTesT As Integer = test
    Return lTesT
End Function");
        }
        [Fact]
        public async Task NonConflictingArgument_PropertyAsync() {
            await TestConversionCSharpToVisualBasicAsync(
@"public class TestClass {
    public int Value {
        get { return GetValue(); }
        set { SetValue(value) }
    }
    int GetValue() { return 0; }
    void SetValue(int value) { }
}",
@"Public Class TestClass
    Public Property Value As Integer
        Get
            Return GetValue()
        End Get
        Set(ByVal value As Integer)
            SetValue(value)
        End Set
    End Property

    Private Function GetValue() As Integer
        Return 0
    End Function

    Private Sub SetValue(ByVal value As Integer)
    End Sub
End Class

1 source compilation errors:
CS1002: ; expected");
        }
        [Fact]
        public async Task NonConflictingArgument_EventAsync() {
            await TestConversionCSharpToVisualBasicAsync(
@"using System;

public class TestClass {
    EventHandler value;
    public event EventHandler Value {
        add { this.value += value; }
        remove { this.value -= value; }
    }
}",
@"Imports System

Public Class TestClass
    Private valueField As EventHandler

    Public Custom Event Value As EventHandler
        AddHandler(ByVal value As EventHandler)
            valueField = [Delegate].Combine(valueField, value)
        End AddHandler
        RemoveHandler(ByVal value As EventHandler)
            valueField = [Delegate].Remove(valueField, value)
        End RemoveHandler
        RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
            valueField?(sender, e)
        End RaiseEvent
    End Event
End Class

2 target compilation errors:
BC36637: The '?' character cannot be used here.
BC30451: '[Delegate]' is not declared. It may be inaccessible due to its protection level.");
        }
        [Fact]
        public async Task CaseConflict_FieldAndInterfacePropertyAsync() {
            await TestConversionCSharpToVisualBasicAsync(
@"public interface IInterface {
    int Prop { get; set; }
}
public class TestClass : IInterface {
    int prop;
    int IInterface.Prop {
        get { return prop; }
        set { prop = value;}
    }
}",
@"Public Interface IInterface
    Property Prop As Integer
End Interface

Public Class TestClass
    Implements IInterface

    Private propField As Integer

    Private Property Prop As Integer Implements IInterface.Prop
        Get
            Return propField
        End Get
        Set(ByVal value As Integer)
            propField = value
        End Set
    End Property
End Class");
        }
        [Fact]
        public async Task CaseConflict_ForeignNamespaceAsync() {
            await TestConversionCSharpToVisualBasicAsync(
@"namespace System {
    public class TestClass {
        int test;
        public int Test { get { return test; } }
    }
}",
                @"Namespace System
    Public Class TestClass
        Private test As Integer

        Public ReadOnly Property Test As Integer
            Get
                Return Me.test
            End Get
        End Property
    End Class
End Namespace

2 target compilation errors:
BC30260: 'Test' is already declared as 'Private test As Integer' in this class.
BC31429: 'test' is ambiguous because multiple kinds of members with this name exist in class 'TestClass'.");
        }

        [Fact]
        public async Task ConstantsShouldBeQualifiedAsync() {
            await TestConversionCSharpToVisualBasicAsync(
@"public class TestClass {
    public void Method() {
        string vbLf = ""\n"";
        string vbCrLf = ""\r\n"";
    }
}",
@"Public Class TestClass
    Public Sub Method()
        Dim vbLf As String = Microsoft.VisualBasic.vbLf
        Dim vbCrLf As String = Microsoft.VisualBasic.vbCrLf
    End Sub
End Class", conversionOptions: EmptyNamespaceOptionStrictOff);
        }

        [Fact]
        public async Task ExplicitImplementationsMustNotDifferOnlyByReturnTypeAsync() {
            await TestConversionCSharpToVisualBasicAsync(
@"using System.Collections;
using System.Collections.Generic;

public class AdditionalLocals : IEnumerable<KeyValuePair<string, int>>
{
    private readonly Stack<Dictionary<string, int>> _additionalLocals = new Stack<Dictionary<string, int>>();

    public IEnumerator<KeyValuePair<string, int>> GetEnumerator()
    {
        return _additionalLocals.Peek().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _additionalLocals.Peek().GetEnumerator();
    }
}",
@"Imports System.Collections
Imports System.Collections.Generic

Public Class AdditionalLocals
    Implements IEnumerable(Of KeyValuePair(Of String, Integer))

    Private ReadOnly _additionalLocals As Stack(Of Dictionary(Of String, Integer)) = New Stack(Of Dictionary(Of String, Integer))()

    Public Function GetEnumerator() As IEnumerator(Of KeyValuePair(Of String, Integer)) Implements IEnumerable(Of KeyValuePair(Of String, Integer)).GetEnumerator
        Return _additionalLocals.Peek().GetEnumerator()
    End Function

    Private Function GetEnumerator1() As IEnumerator Implements IEnumerable.GetEnumerator
        Return _additionalLocals.Peek().GetEnumerator()
    End Function
End Class", conversionOptions: EmptyNamespaceOptionStrictOff);
        }
    }
}
