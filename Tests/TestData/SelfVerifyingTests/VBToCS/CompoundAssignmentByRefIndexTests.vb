Imports System
Imports Xunit

Namespace ProbeByrefIdx
Public Structure S
    Public X As Integer
End Structure
Public Module M
    Function R(ByRef a() As S, b() As S) As Integer
        a = b
        Return 1
    End Function
    Function Inc(ByRef i As Integer) As Integer
        i += 1
        Return 0
    End Function
    Public Function ProbeArrSwap() As String
        Dim a(2) As S
        Dim orig = a
        Dim b(2) As S
        a(R(a, b)).X += 0.4 + 5
        Return orig(1).X & "," & b(1).X
    End Function
    Public Function ProbeIdxSwap() As String
        Dim a(2, 2) As S
        Dim i = 1
        a(i, Inc(i)).X += 0.4 + 5
        Return a(1, 0).X & "," & a(2, 0).X
    End Function
    Public Function ProbeIdxSwapInt() As String
        Dim a(2, 2) As Integer
        Dim i = 1
        a(i, Inc(i)) += 0.4 + 5
        Return a(1, 0) & "," & a(2, 0)
    End Function
    Public Function ProbeRhsLambda() As String
        Dim a(2) As S
        Dim orig = a
        Dim i = 1
        Dim f As Func(Of Integer) = Function()
                                        a = New S(2) {}
                                        Return 5
                                    End Function
        a(Inc(i) + 1).X += 0.4 + f()
        Return orig(1).X & "," & a(1).X
    End Function
End Module
End Namespace

Public Class CompoundAssignmentByRefIndexTests
    <Fact>
    Public Sub ProbeByrefIdx_ProbeArrSwap()
        Assert.Equal("5,0", ProbeByrefIdx.M.ProbeArrSwap())
    End Sub

    <Fact>
    Public Sub ProbeByrefIdx_ProbeIdxSwap()
        Assert.Equal("5,0", ProbeByrefIdx.M.ProbeIdxSwap())
    End Sub

    <Fact>
    Public Sub ProbeByrefIdx_ProbeIdxSwapInt()
        Assert.Equal("5,0", ProbeByrefIdx.M.ProbeIdxSwapInt())
    End Sub

    <Fact>
    Public Sub ProbeByrefIdx_ProbeRhsLambda()
        Assert.Equal("5,0", ProbeByrefIdx.M.ProbeRhsLambda())
    End Sub
End Class
