    Sub DummyMethod()
        Dim someArray = New Integer() { 1, 2, 3}
        For index As Int16 = 0 To someArray.Length - 1
            Console.WriteLine(index)
        Next
    End Sub