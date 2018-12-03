Imports System
Imports System.Linq
Imports Xunit

Module Program

    Public Class Tests

        <Fact>
        Public Sub TestFloatingPointDivisionOfIntegers()
            Dim x = 7 / 2
            Assert.Equal(x, 3.5)
        End Sub

        <Fact>
        Public Sub TestIntegerDivisionOfIntegers()
            Dim x = 7 \ 2
            Assert.Equal(x, 3)
        End Sub

        <Fact>
        Public Sub TestDecimalDivisionOfDecimals()
            Dim x = 7D / 2D
            Assert.Equal(x, 3.5D)
        End Sub
        
        <Fact>
        Public Sub TestIntegerDivisionOfChars()
            Dim x As Char = 7
            Dim y As Char = 2
            Dim z = x / y
            Assert.Equal(x, 3)
        End Sub

        <Fact(Skip := "https://github.com/icsharpcode/CodeConverter/issues/105")>
        Public Sub EmptyStringEqualityToNull()
            Dim s As String = ""
            Dim areEqual As Boolean = s = Nothing
            Assert.True(areEqual)
        End Sub
    End Class

End Module
