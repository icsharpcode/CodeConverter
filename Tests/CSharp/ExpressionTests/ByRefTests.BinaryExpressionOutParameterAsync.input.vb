Imports System.Runtime.InteropServices ' BUG: Comment lost because overwritten

Public Class BinaryExpressionOutParameter
    Shared Sub Main()
        Dim wide As Object = 7
        Zero(wide)
        Dim narrow As Short = 3
        Zero(narrow)
        Zero(7 + 3)
    End Sub

    Shared Sub Zero(<Out> ByRef arg As Integer)
        arg = 0
    End Sub
End Class