Option Strict Off
Imports System
Imports System.Linq
Imports Xunit

Public Class CompoundAssignmentEarlierLambdaTests
    Private ReadOnly _p(3, 3) As Integer
    Public S(3, 3) As Single
    Public B(3, 3) As Byte?

    Public Property Item2(i As Integer, j As Integer) As Integer
        Get
            Return _p(i, j)
        End Get
        Set(value As Integer)
            _p(i, j) = value
        End Set
    End Property

    <Fact>
    Public Sub LocalChangedByALambdaCreatedEarlier()
        Dim i = 0
        Dim f As Func(Of Integer) = Function()
                                        i = 2
                                        Return 1
                                    End Function
        Item2(i, f()) += 10
        i = 0
        S(0, 1) = 9 : S(2, 1) = 9
        S(i, f()) \= 2
        i = 0
        B(0, 1) = 9 : B(2, 1) = 9
        B(i, f()) += 2
        Assert.Equal("10,0,4,9,11,9", String.Join(",", _p(0, 1), _p(2, 1), S(0, 1), S(2, 1), B(0, 1), B(2, 1)))
    End Sub

    <Fact>
    Public Sub ParameterChangedByALambdaCreatedEarlier()
        Assert.Equal("10,0", ChangeParameter(0))
    End Sub

    Private Function ChangeParameter(i As Integer) As String
        Dim f As Func(Of Integer) = Function()
                                        i = 2
                                        Return 1
                                    End Function
        Item2(i, f()) += 10
        Return String.Join(",", _p(0, 1), _p(2, 1))
    End Function

    Private Shared Function Bump(ByRef i As Integer) As Boolean
        i = 2
        Return True
    End Function

    <Fact>
    Public Sub LocalChangedByAQueryCreatedEarlier()
        Dim i = 0
        Dim q = From x In {1} Where Bump(i) Select x
        Item2(i, q.Count()) += 10
        Assert.Equal("10,0,2", String.Join(",", _p(0, 1), _p(2, 1), i))
    End Sub

    <Fact>
    Public Sub StaticLocalChangedByAReentrantCall()
        Assert.Equal("3,10,0", ChangeStatic(True) & "," & _p(0, 1) & "," & _p(3, 1))
    End Sub

    Private Function ChangeStatic(top As Boolean) As String
        Static s As Integer = 0
        If Not top Then
            s = 3
            Return ""
        End If
        s = 0
        Item2(s, Reenter()) += 10
        Return s.ToString()
    End Function

    Private Function Reenter() As Integer
        ChangeStatic(False)
        Return 1
    End Function
End Class
