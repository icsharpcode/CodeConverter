
Private Function TestMethod(newDays As Integer?, oldDays As Integer?) As Boolean
    Return newDays.HasValue AndAlso newDays < oldDays
End Function
