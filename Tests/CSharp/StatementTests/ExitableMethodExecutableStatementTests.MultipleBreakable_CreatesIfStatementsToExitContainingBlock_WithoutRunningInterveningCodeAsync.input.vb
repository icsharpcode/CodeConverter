Imports System.Collections.Generic

Public Class VisualBasicClass
    Public Sub Test
        Dim LstTmp As New List(Of Integer)
        LstTmp.Add(5)
        LstTmp.Add(6)
        LstTmp.Add(7)
        Dim i_Total As Integer
        For Each CurVal As Integer In LstTmp
            i_Total += CurVal
            Select Case CurVal
                Case 6
                    Exit For
                Case 7
                    Exit For
            End Select
            Console.WriteLine()
        Next
    system.Console.WriteLine(i_Total.ToString())
    End Sub
End Class