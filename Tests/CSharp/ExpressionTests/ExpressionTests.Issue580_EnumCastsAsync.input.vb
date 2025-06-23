
Public Class EnumToString
    Enum Tes As Short
        None = 0
        TEST2 = 2
    End Enum
    Private Sub TEest2(aEnum As Tes)
        Dim sxtr_Tmp As String = "Use" & CShort(aEnum).ToString
        Dim si_Txt As Short = CShort(2 ^ Tes.TEST2)
    End Sub
End Class