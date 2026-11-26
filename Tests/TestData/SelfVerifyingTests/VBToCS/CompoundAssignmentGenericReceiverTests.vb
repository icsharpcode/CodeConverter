Imports System
Imports Xunit

Namespace GenericReceiver
Public Interface IHasValue
    Property Value As Integer
End Interface
Public Structure SV
    Implements IHasValue
    Public Property Value As Integer Implements IHasValue.Value
End Structure
Public Class Probe
    Shared Function Idx() As Integer
        Return 0
    End Function
    Shared Sub Bump(Of T As IHasValue)(items As T(), o As Object)
        items(Idx()).Value += o
    End Sub
    Shared Sub Bump2(Of T As IHasValue)(items As T())
        items(Idx()).Value += 2.5
    End Sub
    Public Shared Function Run() As String
        Dim arr = {New SV()}
        Bump(arr, 5)
        Bump2(arr)
        Return arr(0).Value.ToString()
    End Function
End Class
End Namespace

Namespace GenericClassConstraint
Public Interface IHasValue
    Property Value As Integer
End Interface
Public Structure SV
    Implements IHasValue
    Public Property Value As Integer Implements IHasValue.Value
End Structure
Public Class Box(Of T As IHasValue)
    Public Item As T
    Public Items As T() = New T(1) {}
    Shared Function Idx() As Integer
        Return 0
    End Function
    Public Sub Go(o As Object)
        Me.Item.Value += o
        Items(Idx()).Value += o
    End Sub
End Class
Public Class Probe
    Public Shared Function Run() As String
        Dim b As New Box(Of SV)()
        b.Go(3)
        Return b.Item.Value & "," & b.Items(0).Value
    End Function
End Class
End Namespace

Public Class CompoundAssignmentGenericReceiverTests
    <Fact>
    Public Sub ProbeGenericReceiver()
        Assert.Equal("8", GenericReceiver.Probe.Run())
    End Sub

    <Fact>
    Public Sub ProbeGenericClassConstraint()
        Assert.Equal("3,3", GenericClassConstraint.Probe.Run())
    End Sub
End Class
