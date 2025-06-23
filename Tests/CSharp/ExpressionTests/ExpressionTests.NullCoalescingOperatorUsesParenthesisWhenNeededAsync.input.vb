Public Class VisualBasicClass
    Public Sub TestMethod(ByVal x As String, ByVal y As Func(Of Integer))
        Dim a As String = If(x, "x")
        Dim b As String = If(x, "x").ToUpper()
        Dim c As String = $"{If(x, "x")}"
        Dim d As String = $"{If(x, "x").ToUpper()}"
        Dim e =  If(y, Function() 5)
        Dim f =  If(y, (Function() 6))
    End Sub
End Class