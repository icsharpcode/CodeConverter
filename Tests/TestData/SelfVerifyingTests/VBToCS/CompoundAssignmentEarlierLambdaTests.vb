Option Strict Off
Imports System
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
End Class
