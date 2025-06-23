Imports System.Data

Public Class NonStringSelect
    Private Function Test3(CurRow As DataRow)
        For Each CurCol As DataColumn In CurRow.GetColumnsInError
            Select Case CurCol.DataType
                Case GetType(String)
                    Return False
                Case Else
                    Return True
            End Select
        Next
    End Function
End Class