Public Class Compound
    Public Sub Operators()
        Dim aShort As Short = 123
        Dim anotherShort As Short = 234
        Dim x As Short = aShort * anotherShort
        x *= aShort ' Implicit cast in C# due to compound operator
        x = aShort * x
    End Sub
End Class