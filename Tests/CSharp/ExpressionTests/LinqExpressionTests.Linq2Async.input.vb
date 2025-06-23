Public Shared Sub Linq40()
    Dim numbers As Integer() = {5, 4, 1, 3, 9, 8, 6, 7, 2, 0}
    Dim numberGroups = From n In numbers Group n By __groupByKey1__ = n Mod 5 Into g = Group Select New With {Key .Remainder = __groupByKey1__, Key .Numbers = g}
    
    For Each g In numberGroups
        Console.WriteLine($"Numbers with a remainder of { g.Remainder} when divided by 5:")

        For Each n In g.Numbers
            Console.WriteLine(n)
        Next
    Next
End Sub