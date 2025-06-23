Imports System

Public Class VisualBasicClass779
    Public Property SomeCase As Integer = 1
    Public Property ComboBox_CostCenter As Object()
    Public Property The_Cost_Center As Object

    Public Sub Test
        Try
            If Not To_Show_Cost() Then
                SomeCase *= 2
            End If

            SomeCase *= 3
                
            If The_Cost_Center = 0 Then
                    SomeCase *=5
                Exit Try
            End If

            For i = 0 To ComboBox_CostCenter.Length - 1
                If 7 = The_Cost_Center Then
                    SomeCase *=7
                    Exit Try
                End If
            Next
        Finally
        End Try
    End Sub

    Private Function To_Show_Cost() As Boolean
        Throw New NotImplementedException()
    End Function
End Class