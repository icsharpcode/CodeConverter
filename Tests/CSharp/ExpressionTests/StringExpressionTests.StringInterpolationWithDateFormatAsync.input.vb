Imports System

Namespace Global.InnerNamespace
    Public Class Test
           public function InterStringDateFormat(dt As DateTime) As String
            Dim a As String = $"Soak: {dt: d\.h\:mm\:ss\.f}"
            return a 
            End function
    End Class
End Namespace