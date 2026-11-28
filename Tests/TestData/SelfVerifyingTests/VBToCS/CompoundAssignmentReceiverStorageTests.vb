Imports System
Imports System.Collections.Generic
Imports System.Threading.Tasks
Imports Xunit

Public Interface IHasX
    Property X As Integer
End Interface

Public Class CX
    Implements IHasX
    Public Property X As Integer Implements IHasX.X
End Class

Public Structure SX
    Implements IHasX
    Public Property X As Integer Implements IHasX.X
End Structure

Public Structure SD
    Public X As Integer
End Structure

Public Class NamedArgumentsHolder
    Public Shared Log As New List(Of String)
    Public Property Q(i As Integer, j As Integer) As Integer
        Get
            Log.Add("get" & i & j)
            Return 10
        End Get
        Set(value As Integer)
            Log.Add("set" & i & j & "=" & value)
        End Set
    End Property
End Class

Public Class CompoundAssignmentReceiverStorageTests
    Private ReadOnly _log As New List(Of String)
    Private _calls As Integer

    Private Function NextI() As Integer
        _log.Add("NextI")
        _calls += 1
        Return _calls Mod 2
    End Function

    Private Function V(x As Object) As Object
        _log.Add("V")
        Return x
    End Function

    Private Async Function VAsync(x As Object) As Task(Of Object)
        Await Task.Yield()
        _log.Add("VAsync")
        Return x
    End Function

    Private Function Log() As String
        Dim result = String.Join(",", _log)
        _log.Clear()
        _calls = 0
        Return result
    End Function

    Private Sub Bump(Of T As IHasX)(items As T())
        items(NextI()).X ^= V(2)
    End Sub

    Private Async Function BumpAsync(Of T As IHasX)(items As T()) As Task
        items(NextI()).X ^= V(2)
        items(NextI()).X += Await VAsync(1)
    End Function

    Private Iterator Function BumpIterator(Of T As IHasX)(items As T()) As IEnumerable(Of Integer)
        items(NextI()).X ^= V(2)
        Yield items(1).X
    End Function

    Private Async Function BumpStructuresAsync(sd As SD()) As Task
        sd(NextI()).X ^= 2
        Await Task.Yield()
    End Function

    <Fact>
    Public Sub CovariantArrayElementIsNotTakenByReference()
        Dim cxs As CX() = {New CX() With {.X = 3}, New CX() With {.X = 4}}
        Dim items As IHasX() = cxs
        Bump(items)
        Assert.Equal("NextI,V", Log())
        Assert.Equal("3,16", cxs(0).X & "," & cxs(1).X)
    End Sub

    <Fact>
    Public Sub TypeParameterStructureElementIsChangedInPlace()
        Dim sxs As SX() = {New SX() With {.X = 3}, New SX() With {.X = 4}}
        Bump(sxs)
        Assert.Equal("NextI,V", Log())
        Assert.Equal("3,16", sxs(0).X & "," & sxs(1).X)
    End Sub

    <Fact>
    Public Sub TypeParameterElementInAsyncMethod()
        Dim sxs As SX() = {New SX() With {.X = 3}, New SX() With {.X = 4}}
        BumpAsync(sxs).Wait()
        Assert.Equal("NextI,V,NextI,VAsync", Log())
        Assert.Equal("4,16", sxs(0).X & "," & sxs(1).X)
    End Sub

    <Fact>
    Public Sub TypeParameterElementInIterator()
        Dim sxs As SX() = {New SX() With {.X = 3}, New SX() With {.X = 4}}
        Assert.Equal("16", String.Join(",", BumpIterator(sxs)))
        Assert.Equal("NextI,V", Log())
    End Sub

    <Fact>
    Public Sub StructureElementInAsyncMethodWithoutAwaitInTheStatement()
        Dim sd As SD() = {New SD() With {.X = 2}, New SD() With {.X = 3}}
        BumpStructuresAsync(sd).Wait()
        Assert.Equal("NextI", Log())
        Assert.Equal("2,9", sd(0).X & "," & sd(1).X)
    End Sub

    Private Function GetHolder() As NamedArgumentsHolder
        NamedArgumentsHolder.Log.Add("GetHolder")
        Return New NamedArgumentsHolder()
    End Function

    Private Function Idx(n As Integer) As Integer
        NamedArgumentsHolder.Log.Add("Idx" & n)
        Return n
    End Function

    <Fact>
    Public Sub NamedArgumentsAreEvaluatedInParameterOrder()
        Dim three As Object = 3
        GetHolder().Q(j:=Idx(0), i:=Idx(1)) \= three
        Assert.Equal("GetHolder,Idx1,Idx0,get10,set10=3", String.Join(",", NamedArgumentsHolder.Log))
    End Sub
End Class
