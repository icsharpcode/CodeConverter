Imports System
Imports System.Linq
Imports Xunit

Public Class ArithmeticTests

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
    Public Sub TestIntFunctionFloorsDecimal() 'https://github.com/icsharpcode/CodeConverter/issues/238
        Dim a = 30.4
        Dim b = 20.5
        Dim c = 10.6
        Dim d = -10.4
        Dim e = -20.5
        Dim f = -30.6
        Assert.Equal(30, Int(a))
        Assert.Equal(20, Int(b))
        Assert.Equal(10, Int(c))
        Assert.Equal(-11, Int(d))
        Assert.Equal(-21, Int(e))
        Assert.Equal(-31, Int(f))
    End Sub

    <Fact>
    Public Sub TestNoCompileErrorWhenAssigningLiterals() 'https://github.com/icsharpcode/CodeConverter/issues/548
        Dim TestSingle As Single
        TestSingle = 29.7
        TestSingle = 29
        Dim TestDouble As Double
        TestDouble = 29.7
        TestDouble = 29
        Dim TestDecimal As Decimal
        TestDecimal = 29.7
        TestDecimal = 29
        Assert.Equal(TestSingle.ToString(), TestDouble.ToString())
        Assert.Equal(TestDecimal.ToString(), TestDouble.ToString())
    End Sub
End Class
