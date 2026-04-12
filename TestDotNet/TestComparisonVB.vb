Option Compare Text
Imports System

Module Program
    Sub Main()
        Dim testChar As Char = Nothing
        Console.WriteLine(testChar = "")
        Console.WriteLine(testChar = "a")
        Console.WriteLine("a" = testChar)
        Console.WriteLine("ab" = testChar)

        Dim c As Char = "a"c
        Console.WriteLine(c = "a")
        Console.WriteLine(c = "ab")

        Dim s As String = ""
        Console.WriteLine(c = s)
        Console.WriteLine(testChar = s)
    End Sub
End Module
