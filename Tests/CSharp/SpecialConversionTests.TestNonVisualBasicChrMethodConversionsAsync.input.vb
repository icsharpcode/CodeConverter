
Class TestConversions
    Sub Test()
        Dim a As String
        a = Chr(2)
        a = Me.Chr(2)
        a = Strings.Chr(2)
        a = Microsoft.VisualBasic.Strings.Chr(2)
        a = Microsoft.VisualBasic.Chr(2)
    End Sub

    Sub TestW()
        Dim a As String
        a = ChrW(2)
        a = Me.ChrW(2)
        a = Strings.ChrW(2)
        a = Microsoft.VisualBasic.Strings.ChrW(2)
        a = Microsoft.VisualBasic.ChrW(2)
    End Sub

    Function Chr(o As Object) As Char
        Return Microsoft.VisualBasic.Chr(o)
    End Function

    Function ChrW(o As Object) As Char
        Return Microsoft.VisualBasic.ChrW(o)
    End Function
End Class