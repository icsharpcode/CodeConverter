Public Class A
    Public Sub Test()
        Dim str1 = Me.GetStringFromNone(0)
        str1 = GetStringFromNone(0)
        Dim str2 = GetStringFromNone()(1)
        Dim str3 = Me.GetStringsFromString("abc")
        str3 = GetStringsFromString("abc")
        Dim str4 = GetStringsFromString("abc")(1)
        Dim fromStr3 = GetMoreStringsFromString("bc")(1)(0)
        Dim explicitNoParameter = GetStringsFromAmbiguous()(0)(1)
        Dim usesParameter1 = GetStringsFromAmbiguous(0)(1)(2)
    End Sub

    Function GetStringFromNone() As String()
        Return New String() { "A", "B", "C"}
    End Function

    Function GetStringsFromString(parm As String) As String()
        Return New String() { "1", "2", "3"}
    End Function

    Function GetMoreStringsFromString(parm As String) As String()()
        Return New String()() { New String() { "1" }}
    End Function

    Function GetStringsFromAmbiguous() As String()()
        Return New String()() { New String() { "1" }}
    End Function

    Function GetStringsFromAmbiguous(amb As Integer) As String()()
        Return New String()() { New String() { "1" }}
    End Function
End Class