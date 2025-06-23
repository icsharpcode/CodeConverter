Class Product
    Public Category As String
    Public ProductName As String
End Class

Class Test
    Public Function GetProductList As Product()
        Return Nothing
    End Function

    Public Sub Linq103()
        Dim categories As String() = New String() {"Beverages", "Condiments", "Vegetables", "Dairy Products", "Seafood"}
        Dim products = GetProductList()
        Dim q = From c In categories Group Join p In products On c Equals p.Category Into ps = Group Select New With {Key .Category = c, Key .Products = ps}

        For Each v In q
            Console.WriteLine(v.Category & ":")

            For Each p In v.Products
                Console.WriteLine("   " & p.ProductName)
            Next
        Next
    End Sub
End Class