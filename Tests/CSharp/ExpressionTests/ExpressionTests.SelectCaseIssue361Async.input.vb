Module Module1
    Enum E
        A = 1
    End Enum

    Sub Main()
        Dim x = 1
        Select Case x
            Case E.A
                Console.WriteLine("z")
        End Select
    End Sub
End Module