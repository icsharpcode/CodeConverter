Public MustInherit Class A
    Overridable Function F1(x As Integer) As Integer ' Comment ends up out of order, but attached to correct method
        Return 1
    End Function
    MustOverride Function F2() As Integer
    Public Sub TestMethod()
        Dim w = MyClass.f1(1)
        Dim x = Me.F1(2)
        Dim y = MyClass.F2()
        Dim z = Me.F2()
    End Sub
End Class