Imports System.Data

Class TestClass
    Function GetItem(dr As DataRow) As Object
        Return dr.Item("col1")
    End Function
End Class