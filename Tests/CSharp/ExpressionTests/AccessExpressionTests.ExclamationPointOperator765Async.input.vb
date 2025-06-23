Imports System.Data

Public Class Issue765
    Public Sub GetByName(dataReader As IDataReader)
        Dim foo As Object
        foo = dataReader!foo
    End Sub
End Class