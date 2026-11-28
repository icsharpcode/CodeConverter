Imports System
Imports System.Collections.Generic
Imports Xunit

Public Class CompoundAssignmentEvaluationTests
    Private ReadOnly _log As New List(Of String)
    Private ReadOnly _objects As Object() = {1, 2, 3}
    Private ReadOnly _ints As Integer() = {10, 20, 30}
    Private ReadOnly _points As Point() = {New Point(), New Point()}
    Private ReadOnly _holder As New Holder()

    Public Structure Point
        Public X As Integer
    End Structure

    Public Class Holder
        Public F As Integer
        Public Property P As Integer
        Private ReadOnly _q As Byte() = {0, 0, 0}
        Public Property Q(i As Integer) As Byte
            Get
                Return _q(i)
            End Get
            Set(value As Byte)
                _q(i) = value
            End Set
        End Property
    End Class

    Private Function Idx() As Integer
        _log.Add("Idx")
        Return 1
    End Function

    Private Function Rhs() As Object
        _log.Add("Rhs")
        Return 5
    End Function

    Private Function Objects() As Object()
        _log.Add("Objects")
        Return _objects
    End Function

    Private Function GetH() As Holder
        _log.Add("GetH")
        Return _holder
    End Function

    Private Function Log() As String
        Dim result = String.Join(",", _log)
        _log.Clear()
        Return result
    End Function

    <Fact>
    Public Sub ArrayElementIsEvaluatedOnce()
        Objects()(Idx()) += Rhs()
        Assert.Equal("Objects,Idx,Rhs", Log())
        Assert.Equal(7, CInt(_objects(1)))
        _ints(Idx()) += 2.5
        Assert.Equal("Idx", Log())
        Assert.Equal(22, _ints(1))
        _ints(Idx()) += Rhs()
        Assert.Equal("Idx,Rhs", Log())
        Assert.Equal(27, _ints(1))
        _points(Idx()).X += Rhs()
        Assert.Equal("Idx,Rhs", Log())
        Assert.Equal(5, _points(1).X)
    End Sub

    <Fact>
    Public Sub MemberOfAnInvocationIsEvaluatedOnce()
        GetH().F += Rhs()
        Assert.Equal("GetH,Rhs", Log())
        Assert.Equal(5, _holder.F)
        GetH().P += Rhs()
        Assert.Equal("GetH,Rhs", Log())
        Assert.Equal(5, _holder.P)
        GetH().F += 2.5
        Assert.Equal("GetH", Log())
        Assert.Equal(8, _holder.F)
    End Sub

    <Fact>
    Public Sub ParameterizedPropertyIsEvaluatedOnce()
        GetH().Q(Idx()) += 1
        Assert.Equal("GetH,Idx", Log())
        Assert.Equal(CByte(1), _holder.Q(1))
        GetH().Q(Idx()) += Rhs()
        Assert.Equal("GetH,Idx,Rhs", Log())
        Assert.Equal(CByte(6), _holder.Q(1))
    End Sub
End Class
