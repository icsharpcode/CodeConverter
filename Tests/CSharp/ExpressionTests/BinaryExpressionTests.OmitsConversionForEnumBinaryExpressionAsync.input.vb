Friend Enum RankEnum As SByte
    First = 1
    Second = 2
End Enum

Public Class TestClass
    Sub TestMethod()
        Dim eEnum = RankEnum.Second
        Dim enumEnumEquality As Boolean = eEnum = RankEnum.First
    End Sub
End Class