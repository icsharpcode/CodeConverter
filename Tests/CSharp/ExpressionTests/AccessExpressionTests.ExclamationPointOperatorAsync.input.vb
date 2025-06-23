Public Class Issue479
  Default Public ReadOnly Property index(ByVal s As String) As Integer
    Get
      Return 32768 + AscW(s)
    End Get
  End Property
End Class

Public Class TestIssue479
  Public Sub compareAccess()
    Dim hD As Issue479 = New Issue479()
    System.Console.WriteLine("Traditional access returns " & hD.index("X") & vbCrLf & 
      "Default property access returns " & hD("X") & vbCrLf &
      "Dictionary access returns " & hD!X)
  End Sub
End Class