Public Class Issue584RaiseEventByRefDemo
    Public Event ConversionNeeded(ai_OrigID As Integer, ByRef NewID As Integer)

    Public Function TestConversion(ai_ID) As Integer
        Dim i_NewValue As Integer
        RaiseEvent ConversionNeeded(ai_ID, i_NewValue)
        Return i_NewValue
    End Function
End Class