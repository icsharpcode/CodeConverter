Public Class VisualBasicClass
    Public Sub s()
        If Validate({}) Then
        End If
    End Sub
    Private Function Validate(w As IEnumerable(Of Int16)) As Boolean
        Return True
    End Function
End Class