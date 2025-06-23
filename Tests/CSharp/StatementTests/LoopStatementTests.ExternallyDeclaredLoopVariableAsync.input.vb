Sub Main()
    Dim foo As Single = 3.5
    Dim index As Integer
    For index = Int(foo) To Int(foo * 3)
        Console.WriteLine(index)
    Next
End Sub