Imports System
Imports System.Linq
Imports Xunit

Module Program

    Public Class Tests

        <Fact>
        Public Sub TestFloatingPointDivision()
            Dim x = 7 / 2
            Assert.Equal(x, 3.5)
        End Sub

        <Fact>
        Public Sub TestIntegerDivision()
            Dim x = 7 \ 2
            Assert.Equal(x, 3)
        End Sub

        <Fact(Skip := "https://github.com/icsharpcode/CodeConverter/issues/105")>
        Public Sub EmptyStringEqualityToNull()
            Dim s As String = ""
            Dim areEqual As Boolean = s = Nothing
            Assert.True(areEqual)
        End Sub

        <Fact>
        Public Sub TestDecimalDivision()
            Dim x = 7D / 2D
            Assert.Equal(x, 3.5D)
        End Sub

    End Class

End Module
