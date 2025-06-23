     Sub DummyMethod(target As String)
        If target < "Z"c OrElse New Char(){} <= target OrElse target = "" OrElse target <> "" OrElse target >= New Char(){} OrElse target > "" Then
            Console.WriteLine("It must be one of those")
        End If
    End Sub