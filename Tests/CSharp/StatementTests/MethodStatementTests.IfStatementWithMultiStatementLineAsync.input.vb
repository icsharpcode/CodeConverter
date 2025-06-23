Class TestClass
    Public Shared Sub MultiStatement(a As Integer)
        If a = 0 Then Console.WriteLine(1) : Console.WriteLine(2) : Return
        Console.WriteLine(3)
    End Sub
End Class