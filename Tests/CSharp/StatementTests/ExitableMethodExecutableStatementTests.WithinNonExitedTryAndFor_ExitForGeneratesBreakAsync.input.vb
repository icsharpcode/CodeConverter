Imports System

Public Class VisualBasicClass779
    Public Property SomeCase As Integer = 1
    Public Property ComboBox_CostCenter As Object()
    Public Property The_Cost_Center As Object

    Public Sub Test
        Try
            For i = 0 To ComboBox_CostCenter.Length - 1
                If 7 = The_Cost_Center Then
                    SomeCase *=7
                    Exit For
                End If
            Next
        Finally
        End Try
    End Sub

    Private Function To_Show_Cost() As Boolean
        Throw New NotImplementedException()
    End Function
End Class