Imports System
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
            While CurVal < 3
                Select Case CurVal
                    Case 6
                        Continue For
                End Select
            End While
            While CurVal < 4
                Select Case CurVal
                    Case 7
                        Continue For
                    Case 8
                        Exit For
                End Select
            End While
            Console.WriteLine()
        Next
        System.Console.WriteLine(i_Total.ToString())
    End Sub
End Class