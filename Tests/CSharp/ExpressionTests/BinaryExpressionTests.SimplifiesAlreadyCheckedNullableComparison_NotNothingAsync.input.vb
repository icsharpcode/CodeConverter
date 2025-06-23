
Private Function TestMethod(newDays As Integer?, oldDays As Integer?) As Boolean
    Return newDays IsNot Nothing AndAlso oldDays IsNot Nothing AndAlso newDays = oldDays
End Function
