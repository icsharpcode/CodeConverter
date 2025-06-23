Imports System.Data

Public Class AsEnumerableTest
    Public Sub FillImgColor()
        Dim dtsMain As New DataSet
        For Each i_ColCode As Integer In 
            From CurRow In dtsMain.Tables("tb_Color") Select CInt(CurRow.Item("i_ColCode"))
        Next
    End Sub
End Class