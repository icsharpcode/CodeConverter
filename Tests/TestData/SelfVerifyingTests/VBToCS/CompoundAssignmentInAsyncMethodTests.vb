Imports System
Imports System.Collections.Generic
Imports System.Threading.Tasks
Imports Xunit

Namespace AsyncRef
Public Structure Pt
    Public X As Integer
End Structure
Public Class Probe
    Shared _points As Pt() = {New Pt()}
    Shared Function Idx() As Integer
        Return 0
    End Function
    Shared Async Function RhsAsync() As Task(Of Object)
        Await Task.Yield()
        Return 5
    End Function
    Shared Async Function GoAsync() As Task
        _points(Idx()).X += Await RhsAsync()
        _points(Idx()).X += 2.5
    End Function
    Public Shared Function Run() As String
        GoAsync().Wait()
        Return _points(0).X.ToString()
    End Function
End Class
End Namespace

Namespace IteratorRef
Public Structure Pt
    Public X As Integer
End Structure
Public Class Probe
    Shared _points As Pt() = {New Pt()}
    Shared Function Idx() As Integer
        Return 0
    End Function
    Shared Iterator Function Gen() As IEnumerable(Of Integer)
        _points(Idx()).X += 2.5
        Yield _points(0).X
        _points(Idx()).X += 2.5
        Yield _points(0).X
    End Function
    Shared Async Function GoAsync(o As Object) As System.Threading.Tasks.Task
        _points(Idx()).X += o
        Await System.Threading.Tasks.Task.Yield()
        _points(Idx()).X += o
    End Function
    Public Shared Function Run() As String
        Dim s = String.Join(",", Gen())
        GoAsync(1).Wait()
        Return s & ";" & _points(0).X
    End Function
End Class
End Namespace

Namespace Async2
Public Structure Pt
    Private _q As Integer
    Public Property Q(i As Integer) As Integer
        Get
            Return _q
        End Get
        Set(value As Integer)
            _q = value
        End Set
    End Property
End Structure
Public Class Probe
    Shared _pts As Pt() = {New Pt(), New Pt()}
    Shared Function Idx() As Integer
        Return 1
    End Function
    Shared Async Function IdxAsync() As Task(Of Integer)
        Await Task.Yield()
        Return 0
    End Function
    Shared Async Function GoAsync(o As Object) As Task
        _pts(Idx()).Q(Await IdxAsync()) += o
    End Function
    Public Shared Function Run() As String
        GoAsync(3).Wait()
        Return _pts(1).Q(0).ToString()
    End Function
End Class
End Namespace

Public Class CompoundAssignmentInAsyncMethodTests
    <Fact>
    Public Sub ProbeAsyncRef()
        Assert.Equal("8", AsyncRef.Probe.Run())
    End Sub

    <Fact>
    Public Sub ProbeIteratorRef()
        Assert.Equal("2,4;6", IteratorRef.Probe.Run())
    End Sub

    <Fact>
    Public Sub ProbeAsync2()
        Assert.Equal("3", Async2.Probe.Run())
    End Sub
End Class
