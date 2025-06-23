Friend Class Program
    Public Shared Sub Main(ByVal args As String())
        Dim zs As Object = { 1, 2, 3 }
        For Each z in zs
            Console.WriteLine(z)
        Next
    End Sub
End Class