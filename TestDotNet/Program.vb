Imports System

Module Program
    Sub Main()
        Dim testChar As Char = Nothing
        Dim testResult = testChar = ""
        Console.WriteLine(testResult)

        Dim testResult2 = "" = testChar
        Console.WriteLine(testResult2)
    End Sub
End Module
