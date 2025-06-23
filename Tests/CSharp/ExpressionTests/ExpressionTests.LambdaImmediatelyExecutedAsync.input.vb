Public Class Issue869
    Sub Main
        Dim i As Integer = Function() 
                                Return 2 
                        End Function() 
        Console.WriteLine(i)
    End Sub
End Class