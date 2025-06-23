Public Class VisualBasicClass

    Private Function TestMethod(testArray As Object()) As Boolean
        Return Not String.IsNullOrWhiteSpace(testArray?(0)?.ToString())
    End Function
    
End Class