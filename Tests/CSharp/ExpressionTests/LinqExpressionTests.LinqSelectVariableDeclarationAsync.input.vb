Imports System
Imports System.Linq

Public Class Class717
        Sub Main()
        Dim arr(1) as Integer
        arr(0) = 0
        arr(1) = 1

        Dim r = From e In arr
                Select p = $"value: {e}"
                Select l = p.Substring(1)
                Select x = l

        For each m In r
            Console.WriteLine(m)
        Next
    End Sub
End Class