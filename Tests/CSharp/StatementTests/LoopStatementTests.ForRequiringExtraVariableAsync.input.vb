Class TestClass
    Private Sub TestMethod()
        Dim stringValue AS string = "42"
        For i As Integer = 1 To 10 - stringValue.Length
           stringValue = stringValue & " " + Cstr(i)
           Console.WriteLine(stringValue)
        Next
    End Sub
End Class