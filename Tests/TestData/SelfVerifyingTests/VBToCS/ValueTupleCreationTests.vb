Imports System
Imports System.Collections.Generic
Imports System.Linq.Expressions
Imports Xunit

Public Class ValueTupleCreationTests
    Private ReadOnly _log As New List(Of String)

    Private Function Logged(Of T)(name As String, value As T) As T
        _log.Add(name)
        Return value
    End Function

    <Fact>
    Sub ElementsKeepTheTypeArgumentsWithoutATargetType()
        Dim x As Integer = 1
        Dim s As Short = 2
        Dim widened = New ValueTuple(Of Long, Short)(x, s)
        Assert.Equal(GetType(ValueTuple(Of Long, Short)), widened.GetType())
        Assert.Equal((1L, CShort(2)), widened)

        Dim literals = New ValueTuple(Of Double, Long, Byte, Decimal, Single, Short)(1, 2, 3, 4, 5, 6)
        Assert.Equal(GetType(ValueTuple(Of Double, Long, Byte, Decimal, Single, Short)), literals.GetType())

        Dim shortArithmetic = New ValueTuple(Of Short, Integer)(s * s, s * s)
        Assert.Equal(GetType(ValueTuple(Of Short, Integer)), shortArithmetic.GetType())
        Assert.Equal((CShort(4), 4), shortArithmetic)

        Dim stringAndChar = New ValueTuple(Of String, Char)("a", "b"c)
        Assert.Equal(("a", "b"c), stringAndChar)
    End Sub

    Private Enum Colour
        Red
        Green
    End Enum

    <Fact>
    Sub ConvertedElementsKeepTheTypeArguments()
        Dim o As Object = "3"
        Dim narrowed = New ValueTuple(Of Integer, String)(CInt(o), CStr(o))
        Assert.Equal(GetType(ValueTuple(Of Integer, String)), narrowed.GetType())
        Assert.Equal((3, "3"), narrowed)

        Dim c = Colour.Green
        Dim enums = New ValueTuple(Of Colour, Integer)(c, c)
        Assert.Equal(GetType(ValueTuple(Of Colour, Integer)), enums.GetType())
        Assert.Equal((Colour.Green, 1), enums)
    End Sub

    <Fact>
    Sub ElementsNeedingATargetTypeKeepTheTypeArguments()
        Dim x As Integer = 1
        Dim boxed = New ValueTuple(Of Object, Object)("a", x)
        Assert.Equal(GetType(ValueTuple(Of Object, Object)), boxed.GetType())

        Dim nullable = New ValueTuple(Of Integer?, Integer?)(x, 2)
        Assert.Equal(GetType(ValueTuple(Of Integer?, Integer?)), nullable.GetType())

        Dim nothings = New ValueTuple(Of String, Integer)(Nothing, Nothing)
        Assert.Equal(GetType(ValueTuple(Of String, Integer)), nothings.GetType())
        Assert.Equal((CStr(Nothing), 0), nothings)

        Dim lambda = New ValueTuple(Of Func(Of Integer), Integer)(Function() 3, 4)
        Assert.Equal(3, lambda.Item1())
    End Sub

    <Fact>
    Sub TargetTypedCreationKeepsValues()
        Dim declared As (A As Double, B As Long) = New ValueTuple(Of Double, Long)(1, 2)
        Assert.Equal(1.0, declared.A)
        Assert.Equal(2L, declared.B)
        Assert.Equal((1.0, 2L), ReturnTuple())
    End Sub

    Private Function ReturnTuple() As ValueTuple(Of Double, Long)
        Return New ValueTuple(Of Double, Long)(1, 2)
    End Function

    <Fact>
    Sub ElementsAreEvaluatedInOrder()
        Dim positional = New ValueTuple(Of Integer, Integer)(Logged("first", 1), Logged("second", 2))
        Assert.Equal({"first", "second"}, _log.ToArray())
        Assert.Equal((1, 2), positional)
    End Sub

    <Fact>
    Sub NamedArgumentsKeepValues()
        Dim named = New ValueTuple(Of Integer, Integer)(item2:=2, item1:=1)
        Assert.Equal((1, 2), named)
    End Sub

    <Fact>
    Sub OneElementAndInitializedTuples()
        Dim one = New ValueTuple(Of Integer)(5)
        Assert.Equal(5, one.Item1)

        Dim initialized = New ValueTuple(Of Integer, String) With {.Item2 = "b"}
        Assert.Equal((0, "b"), initialized)
    End Sub

    <Fact>
    Sub CreationWithoutArgumentsIsDefault()
        Dim pair = New ValueTuple(Of Integer, String)()
        Assert.Equal(GetType(ValueTuple(Of Integer, String)), pair.GetType())
        Assert.Equal((0, CStr(Nothing)), pair)
        Dim noParentheses = New ValueTuple(Of Integer, String)
        Assert.Equal(pair, noParentheses)
        Assert.Equal(0, New ValueTuple(Of Integer)().Item1)
    End Sub

    <Fact>
    Sub LongTuplesKeepAllElements()
        Dim eight = New ValueTuple(Of Integer, Integer, Integer, Integer, Integer, Integer, Integer, ValueTuple(Of Integer))(1, 2, 3, 4, 5, 6, 7, New ValueTuple(Of Integer)(8))
        Assert.Equal(GetType(ValueTuple(Of Integer, Integer, Integer, Integer, Integer, Integer, Integer, ValueTuple(Of Integer))), eight.GetType())
        Assert.Equal(8, eight.Rest.Item1)

        Dim nine = New ValueTuple(Of Integer, Integer, Integer, Integer, Integer, Integer, Integer, ValueTuple(Of Integer, Long))(1, 2, 3, 4, 5, 6, 7, New ValueTuple(Of Integer, Long)(8, 9))
        Assert.Equal(GetType(ValueTuple(Of Integer, Integer, Integer, Integer, Integer, Integer, Integer, ValueTuple(Of Integer, Long))), nine.GetType())
        Assert.Equal(9L, nine.Rest.Item2)

        Dim rest = New ValueTuple(Of Integer)(8)
        Dim restVariable = New ValueTuple(Of Integer, Integer, Integer, Integer, Integer, Integer, Integer, ValueTuple(Of Integer))(1, 2, 3, 4, 5, 6, 7, rest)
        Assert.Equal(eight, restVariable)

        Dim restWidened = New ValueTuple(Of Integer, Integer, Integer, Integer, Integer, Integer, Integer, ValueTuple(Of Long))(1, 2, 3, 4, 5, 6, 7, New ValueTuple(Of Integer)(8))
        Assert.Equal(GetType(ValueTuple(Of Integer, Integer, Integer, Integer, Integer, Integer, Integer, ValueTuple(Of Long))), restWidened.GetType())

        Dim restDefault = New ValueTuple(Of Integer, Integer, Integer, Integer, Integer, Integer, Integer, ValueTuple(Of Integer))(1, 2, 3, 4, 5, 6, 7, New ValueTuple(Of Integer)())
        Assert.Equal(GetType(ValueTuple(Of Integer, Integer, Integer, Integer, Integer, Integer, Integer, ValueTuple(Of Integer))), restDefault.GetType())
    End Sub

    <Fact>
    Sub ExpressionTreeCreatesTuple()
        Dim create As Expression(Of Func(Of Integer, (Integer, Integer))) = Function(x) New ValueTuple(Of Integer, Integer)(x, 2)
        Assert.Equal((1, 2), create.Compile()(1))
    End Sub
End Class
