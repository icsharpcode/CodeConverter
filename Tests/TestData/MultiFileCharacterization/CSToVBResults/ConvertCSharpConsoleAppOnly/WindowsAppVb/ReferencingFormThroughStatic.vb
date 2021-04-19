Public Module ReferencingFormThroughStatic
    Public Function GetFormTitle() As String
        ' This used to cause a bug in the expander for maes.Expression leading to another bug later on
        With New System.Text.StringBuilder()
            .Capacity = 4
        End With

        If WinformsDesignerTest IsNot Nothing AndAlso WinformsDesignerTest.Text IsNot Nothing Then
            Return WinformsDesignerTest.Text
        End If
        Return ""
    End Function
End Module
