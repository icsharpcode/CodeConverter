
Public Class TestForDates
    Public Shared Sub WriteStatus(adminDate As DateTime?, chartingTimeAllowanceEnd As DateTime)
        If adminDate Is Nothing OrElse adminDate > chartingTimeAllowanceEnd Then
            adminDate = DateTime.Now
        End If
    End Sub
End Class
