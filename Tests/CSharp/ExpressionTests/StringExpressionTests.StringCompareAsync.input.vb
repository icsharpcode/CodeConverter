Public Class Class1
    Sub Foo()
        Dim s1 As String = Nothing
        Dim s2 As String = ""
        If s1 <> s2 Then
            Throw New Exception()
        End If
        If s1 = "something" Then
            Throw New Exception()
        End If
        If "something" = s1 Then
            Throw New Exception()
        End If
        If s1 = Nothing Then
            '
        End If
        If s1 = "" Then
            '
        End If
    End Sub
End Class