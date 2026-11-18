Imports System
Imports System.Linq.Expressions
Imports Xunit

Public Class RelaxedDelegateConversionTests
    Private _log As String = ""

    Private Sub Invoke(Of T)(value As T, Optional callback As Action(Of T) = Nothing)
        callback(value)
    End Sub

    <Fact>
    Public Sub SubWithOmittedParameterIsCalled()
        Invoke(1, Sub()
                      _log &= "called"
                  End Sub)
        Assert.Equal("called", _log)
    End Sub

    <Fact>
    Public Sub OmittedParametersOfNamedDelegates()
        Dim twoParams As Action(Of Integer, String) = Sub() _log &= "two"
        twoParams(1, "a")
        Dim handler As EventHandler = Sub() _log &= ",handler"
        handler(Me, EventArgs.Empty)
        Dim func As Func(Of Integer, Integer) = Function() 4
        Assert.Equal("two,handler", _log)
        Assert.Equal(4, func(3))
    End Sub

    <Fact>
    Public Sub OmittedParameterOfExpressionTree()
        Dim expr As Expression(Of Func(Of Integer, Integer)) = Function() 5
        Assert.Equal(5, expr.Compile()(3))
    End Sub
End Class
