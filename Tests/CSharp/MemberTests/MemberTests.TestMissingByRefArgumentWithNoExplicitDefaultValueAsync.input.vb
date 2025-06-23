Imports System.Runtime.InteropServices

Class MissingByRefArgumentWithNoExplicitDefaultValue
    Sub S()
        ByRefNoDefault()
        OptionalByRefNoDefault()
        OptionalByRefWithDefault()
    End Sub

    Private Sub ByRefNoDefault(ByRef str1 As String) : End Sub
    Private Sub OptionalByRefNoDefault(<[Optional]> ByRef str2 As String) : End Sub
    Private Sub OptionalByRefWithDefault(<[Optional], DefaultParameterValue("a")> ByRef str3 As String) : End Sub
End Class