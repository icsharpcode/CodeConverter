Public Class ClassWithProperties
   Public Property Property1 As String
End Class

Public Class VisualBasicClass
   Public Sub New()
       Dim x As New Dictionary(Of String, String)()
       Dim y As New ClassWithProperties()
       
       If (x.TryGetValue("x", y.Property1)) Then
          Debug.Print(y.Property1)
       End If
   End Sub
End Class