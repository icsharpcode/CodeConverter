Class TestClass
    Private Shared Function Log(ByVal message As String) As Boolean
        Console.WriteLine(message)
        Return False
    End Function

    Private Sub TestMethod(ByVal number As Integer)
        Try
            Console.WriteLine("try")
        Catch e As Exception
            Console.WriteLine("catch1")
        Catch
            Console.WriteLine("catch all")
        Finally
            Console.WriteLine("finally")
        End Try

        Try
            Console.WriteLine("try")
        Catch e2 As NotImplementedException
            Console.WriteLine("catch1")
        Catch e As Exception When Log(e.Message)
            Console.WriteLine("catch2")
        End Try

        Try
            Console.WriteLine("try")
        Finally
            Console.WriteLine("finally")
        End Try
    End Sub
End Class