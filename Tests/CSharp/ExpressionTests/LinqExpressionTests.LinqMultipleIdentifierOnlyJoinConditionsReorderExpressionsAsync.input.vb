Class Customer
    Public CustomerID As String
    Public CompanyName As String
End Class

Class Order
    Public Customer As Customer
    Public Total As String
End Class

Class Test
Private Shared Sub ASub()
    Dim customers = New List(Of Customer)
    Dim orders = New List(Of Order)
    Dim customerList = From cust In customers
                       Join ord In orders On ord.Customer Equals cust And cust.CompanyName Equals ord.Total
                       Select cust.CompanyName, ord.Total
End Sub
End Class