Class Product
    Public Category As String
    Public ProductName As String
End Class

Class Test

    Public Function GetProductList As Product()
        Return Nothing
    End Function

    Public Sub Linq102()
        Dim categories As String() = New String() {"Beverages", "Condiments", "Vegetables", "Dairy Products", "Seafood"}
        Dim products As Product() = GetProductList()
        Dim q = From c In categories Join p In products On c Equals p.Category Select New With {Key .Category = c, p.ProductName}

        For Each v In q
            Console.WriteLine($"{v.ProductName}: {v.Category}")
        Next
    End Sub
End Class