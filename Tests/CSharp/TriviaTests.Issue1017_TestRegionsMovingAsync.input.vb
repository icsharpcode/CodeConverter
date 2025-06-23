Public Class ConversionTest8
    Private x As Integer = 5

    Public Sub New()

        'Constructor Comment 1
        Dim constructorVar1 As Boolean = True

        'Constructor Comment 2
        Dim constructorVar2 As Boolean = True

    End Sub

#Region "Region1"
    Private Sub Method1()
    End Sub
#End Region
#Region "Region2"
    'Class Comment 3
    Private ReadOnly ClassVariable1 As New ParallelOptions With {.MaxDegreeOfParallelism = x}
#End Region
End Class
