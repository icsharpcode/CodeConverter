
Private Function TestMethod(newDays As Integer?, oldDays As Integer?) As Boolean
    Return (newDays.HasValue OrElse oldDays.HasValue) AndAlso newDays <> oldDays
End Function
