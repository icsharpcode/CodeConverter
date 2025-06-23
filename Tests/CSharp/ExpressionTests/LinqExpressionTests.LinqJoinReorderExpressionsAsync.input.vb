Class Customer
    Public CustomerID As String
    Public CompanyName As String
End Class

Class Order
    Public CustomerID As String
    Public Total As String
End Class

Class Test
Private Shared Sub ASub()
    Dim customers = New List(Of Customer)
    Dim orders = New List(Of Order)
    Dim customerList = From cust In customers
                       Join ord In orders On ord.CustomerID Equals cust.CustomerID
                       Select cust.CompanyName, ord.Total
End Sub
End Class