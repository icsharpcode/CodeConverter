
Public Class Issue495AndIssue713
    Public Function Empty() As Integer()
        Dim emptySingle As IEnumerable(Of Integer) = {}
        Dim initializedSingle As IEnumerable(Of Integer) = {1}
        Dim emptyNested As Integer()() = {}
        Dim initializedNested(1)() As Integer
        Dim empty2d As Integer(,) = {{}}
        Dim initialized2d As Integer(,) = {{1}}
        Return {}
    End Function
End Class