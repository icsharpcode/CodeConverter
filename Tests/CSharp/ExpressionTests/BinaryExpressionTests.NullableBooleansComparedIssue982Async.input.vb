Dim newDays As Integer? = 1
Dim oldDays As Integer? = Nothing

If (newDays.HasValue AndAlso Not oldDays.HasValue) _
                OrElse (newDays.HasValue AndAlso oldDays.HasValue AndAlso newDays <> oldDays) _
                OrElse (Not newDays.HasValue AndAlso oldDays.HasValue) Then

'Some code
End If