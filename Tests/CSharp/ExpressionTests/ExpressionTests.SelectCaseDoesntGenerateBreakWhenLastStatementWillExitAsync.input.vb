Public Class Test
    Public Function OnLoad() As Integer
        Dim x = 5
        While True
            Select Case x
                Case 0
                    Continue While
                Case 1
                    x = 1
                Case 2
                    Return 2
                Case 3
                    Throw New Exception()
                Case 4
                    If True Then
                        x = 4
                    Else
                        Return x
                    End If
                Case 5
                    If True Then
                        Return x
                    Else
                        x = 5
                    End If
                Case 6
                    If True Then
                        Return x
                    Else If False Then
                        x = 6
                    Else
                        Return x
                    End If
                Case 7
                    If True Then
                        Return x
                    End If
                Case 8
                    If True Then Return x
                Case 9
                    If True Then x = 9
                Case 10
                    If True Then Return x Else x = 10
                Case 11
                    If True Then x = 11 Else Return x
                Case 12
                    If True Then Return x Else Return x
                Case 13
                    If True Then
                        Return x
                    Else If False Then
                        Continue While
                    Else If False Then
                        Throw New Exception()
                    Else If False Then
                        Exit Select
                    Else
                        Return x
                    End If
                Case 14
                    If True Then
                        Return x
                    Else If False Then
                        Return x
                    Else If False Then
                        Exit Select
                    End If
                Case Else
                    If True Then
                        Return x
                    Else
                        Return x
                    End If
            End Select
        End While
        Return x
    End Function
End Class