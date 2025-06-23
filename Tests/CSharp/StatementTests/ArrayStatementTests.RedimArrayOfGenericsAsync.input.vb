Public Class Class1
    Dim test() As List(Of Integer)

    Private Sub test123(sender As Object, e As EventArgs)
        ReDim Me.test(42)

        Dim test1() As Tuple(Of Integer, Integer)
        ReDim test1(42)
    End Sub
End Class