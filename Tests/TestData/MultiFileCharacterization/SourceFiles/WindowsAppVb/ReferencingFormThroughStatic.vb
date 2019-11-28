Public Module ReferencingFormThroughStatic
    Public Function GetFormTitle() As String
        If WinformsDesignerTest IsNot Nothing AndAlso WinformsDesignerTest.Text IsNot Nothing Then
            Return WinformsDesignerTest.Text
        End If
        Return ""
    End Function
End Module
