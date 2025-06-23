Public Class BinaryExpressionRefParameter
    Shared Sub Main()
        Dim wide As Object = 7
        LogAndReset(wide)
        Dim wideArray() As Object = {3,4,4}
        LogAndReset(wideArray(1))
        Dim narrow As Short = 3
        LogAndReset(narrow)
        LogAndReset(7 + 3)
    End Sub

    Shared Sub LogAndReset(ByRef arg As Integer)
        System.Console.WriteLine(arg)
        arg = 0
    End Sub
End Class