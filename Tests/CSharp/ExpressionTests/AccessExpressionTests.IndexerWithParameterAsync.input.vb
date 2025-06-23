Imports System.Data

Public Class A
    Public Function ReadDataSet(myData As DataSet) As String
        With myData.Tables(0).Rows(0)
            Return .Item("MY_COLUMN_NAME").ToString()
        End With
    End Function
End Class