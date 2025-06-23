Public Class Class1
    Private Sub LoadValues(ByVal strPlainKey As String)
        Dim xmlFile As XDocument = XDocument.Parse(strPlainKey)
        Dim objActivationInfo As XElement = xmlFile.<ActivationKey>.First
    End Sub
End Class