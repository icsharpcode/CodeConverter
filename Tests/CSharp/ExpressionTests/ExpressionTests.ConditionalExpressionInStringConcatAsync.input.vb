Class ConditionalExpressionInStringConcat
    Private Sub TestMethod(ByVal str As String)
        Dim appleCount as integer = 42
        Console.WriteLine("I have " & appleCount & If(appleCount = 1, " apple", " apples"))
    End Sub
End Class