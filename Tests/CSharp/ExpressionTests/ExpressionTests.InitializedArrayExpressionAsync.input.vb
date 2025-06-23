
Public Class Issue713
    Public Function Empty() As Integer()
        Dim initializedSingle As IEnumerable(Of Integer) = {1}
        Dim initialized2d As Integer(,) = {{1}}
        Return {}
    End Function
End Class