Imports System

Public Class VisualBasicClass779
    Public Property SomeCase As Integer = 1
    Public Property ComboBox_CostCenter As Object()
    Public Property The_Cost_Center As Object

    Public Sub Test
        For i = 0 To ComboBox_CostCenter.Length - 1
            Try
                If 7 = The_Cost_Center Then
                    SomeCase *=7
                    Exit For
                Else
                    Exit Try
                End If
            Finally
            End Try
        Next
    End Sub

    Private Function To_Show_Cost() As Boolean
        Throw New NotImplementedException()
    End Function
End Class