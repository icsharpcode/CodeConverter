Public Class Issue483
    Private Function numstr(ByVal aDouble As Double) As String
        Dim str_Txt As String = Format(aDouble, "0.000000")
        Mid(str_Txt, Len(str_Txt) - 6, 1) = "."
        Mid(str_Txt, Len(str_Txt) - 6) = "."
        Mid(str_Txt, Len(str_Txt) - 6) = aDouble
        Console.WriteLine(aDouble)
        If aDouble > 5.0 Then Mid(str_Txt, Len(str_Txt) - 6) = numstr(aDouble - 1.0)
        Return str_Txt
    End Function
End Class