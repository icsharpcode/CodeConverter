Option Strict Off
Imports System
Imports System.Linq
Imports Xunit

Namespace GeneratedNames
Public Class Box
  Public Property V As Integer
End Class
Public Module P
  Sub Inc(ByRef x As Integer)
    x += 1
  End Sub
  Public Function ExprLambdaSibling() As String
    Dim b As New Box
    Inc(b.V)
    Dim f = Function(argx As Integer) argx + 1
    Return b.V & f(1)
  End Function
  Public Function ExprLambdaSameStatement() As String
    Dim b As New Box
    Dim n = {1, 2}.Count(Function(argx) argx > 1) + Bump(b.V)
    Return b.V & n
  End Function
  Function Bump(ByRef x As Integer) As Integer
    x += 1
    Return 0
  End Function
  Public Function ExprLambdaNested() As String
    Dim b As New Box
    If True Then
      Inc(b.V)
    End If
    Dim f = Function(argx As Integer) argx + 1
    Return b.V & f(1)
  End Function
  Public Function LinqSibling() As String
    Dim b As New Box
    Inc(b.V)
    Dim q = From argx In {1, 2} Select argx
    Return b.V & q.Sum()
  End Function
  Public Function LinqNested() As String
    Dim b As New Box
    If True Then
      Inc(b.V)
    End If
    Dim q = From argx In {1, 2} Select argx
    Return b.V & q.Sum()
  End Function
End Module
End Namespace

Public Class GeneratedNameScopeTests
    <Fact>
    Public Sub ExprLambdaNested()
        Assert.Equal("12", GeneratedNames.P.ExprLambdaNested())
    End Sub

    <Fact>
    Public Sub ExprLambdaSameStatement()
        Assert.Equal("11", GeneratedNames.P.ExprLambdaSameStatement())
    End Sub

    <Fact>
    Public Sub ExprLambdaSibling()
        Assert.Equal("12", GeneratedNames.P.ExprLambdaSibling())
    End Sub

    <Fact>
    Public Sub LinqNested()
        Assert.Equal("13", GeneratedNames.P.LinqNested())
    End Sub

    <Fact>
    Public Sub LinqSibling()
        Assert.Equal("13", GeneratedNames.P.LinqSibling())
    End Sub
End Class
