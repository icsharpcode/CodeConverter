Public MustInherit Class A
    Overridable Property P1() As Integer = 1
    MustOverride Property P2() As Integer
    Public Sub TestMethod()
        Dim w = MyClass.p1
        Dim x = Me.P1
        Dim y = MyClass.P2
        Dim z = Me.P2
    End Sub
End Class