Imports System.Data

Class TestClass
    Private ReadOnly _myTable As DataTable

    Sub TestMethod()
      Dim dataRow = _myTable(0)
    End Sub
End Class