
Public Class TestSimpleMethodReplacements
    Sub TestMethod()
        Dim str1 As String
        Dim str2 As String
        Dim x As Object
        Dim dt As DateTime
        x = Microsoft.VisualBasic.DateAndTime.Now
        x = Microsoft.VisualBasic.DateAndTime.Today
        x = Microsoft.VisualBasic.DateAndTime.Year(dt)
        x = Microsoft.VisualBasic.DateAndTime.Month(dt)
        x = Microsoft.VisualBasic.DateAndTime.Day(dt)
        x = Microsoft.VisualBasic.DateAndTime.Hour(dt)
        x = Microsoft.VisualBasic.DateAndTime.Minute(dt)
        x = Microsoft.VisualBasic.DateAndTime.Second(dt)
    End Sub
End Class