
Private Function TestMethod(newDays As Integer?, oldDays As Integer?) As Boolean
    Return newDays.HasValue AndAlso oldDays.HasValue AndAlso newDays <> oldDays
End Function
