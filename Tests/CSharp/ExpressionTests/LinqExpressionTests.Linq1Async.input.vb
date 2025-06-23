Private Shared Sub SimpleQuery()
    Dim numbers = {7, 9, 5, 3, 6}
    Dim res = From n In numbers Where n > 5 Select n
    For Each n In res
        Console.WriteLine(n)
    Next
End Sub