Public Class EnumTests
    Private Enum RankEnum As SByte
        First = 1
        Second = 2
    End Enum

    Public Sub TestEnumConcat()
        Console.Write(RankEnum.First & RankEnum.Second)
    End Sub
End Class