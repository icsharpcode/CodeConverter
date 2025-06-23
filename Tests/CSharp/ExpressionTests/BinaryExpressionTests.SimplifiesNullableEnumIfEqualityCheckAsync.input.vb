
Public Enum PasswordStatus
    Expired
    Locked    
End Enum
Public Class TestForEnums
    Public Shared Sub WriteStatus(status As PasswordStatus?)
      If status = PasswordStatus.Locked Then
          Console.Write("Locked")
      End If
    End Sub
End Class
