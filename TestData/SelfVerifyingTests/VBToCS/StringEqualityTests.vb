Imports System
Imports System.Linq
Imports Xunit


Public Class StringEqualityTests

    <Fact>
    Public Sub TestStringComparison()
        Dim s1 As String = Nothing
        Dim s2 As String = ""
        Assert.True(s1 = s2)
    End Sub

    <Fact>
    Public Sub VisualBasicEqualityOfCharArrays()
        Assert.True(New Char(){} = New Char(){}, "Char arrays should be compared as strings because that's what happens in VB")
    End Sub

    <Fact>
    Public Sub VisualBasicEqualityOfNormalObjectsNotSubjectToSpecialStringConversionRules()
        Dim a1 As Object = 3
        Dim a2 As Object = 3
        Dim b As Object = 4
        Assert.True(a1 = a2, "Identical values stored in objects should be equal")
        Assert.False(a1 = b, "Different values stored in objects should not be equal")
    End Sub

    Private nullObject As Object = Nothing
    Private nullString As String = Nothing
    Private emptyStringObject As Object = ""
    Private emptyString As String = ""
    Private nonEmptyString As String = "a"
    Private emptyCharArray As Char() = New Char(){}
    Private nullCharArray As Char() = Nothing

    <Fact> 'https://github.com/icsharpcode/CodeConverter/issues/105
    Public Sub VisualBasicEqualityOfEmptyStringAndNothingIsPreservedForNullObject()
        Dim record = ""
        If nullObject = nullObject Then record &= "1" Else record &= "0"
        If nullObject = nullString Then record &= "1" Else record &= "0"
        If nullObject = emptyStringObject Then record &= "1" Else record &= "0"
        If nullObject = emptyString Then record &= "1" Else record &= "0"
        If nullObject = nonEmptyString Then record &= "1" Else record &= "0"
        If nullObject = emptyCharArray Then record &= "1" Else record &= "0"
        If nullObject = nullCharArray Then record &= "1" Else record &= "0"
        Assert.Equal("1111011", record)
    End Sub

    <Fact> 'https://github.com/icsharpcode/CodeConverter/issues/105
    Public Sub VisualBasicEqualityOfEmptyStringAndNothingIsPreservedForNullString()
        Dim record = ""
        If nullString = nullObject Then record &= "1" Else record &= "0"
        If nullString = nullString Then record &= "1" Else record &= "0"
        If nullString = emptyStringObject Then record &= "1" Else record &= "0"
        If nullString = emptyString Then record &= "1" Else record &= "0"
        If nullString = nonEmptyString Then record &= "1" Else record &= "0"
        If nullString = emptyCharArray Then record &= "1" Else record &= "0"
        If nullString = nullCharArray Then record &= "1" Else record &= "0"
        Assert.Equal("1111011", record)
    End Sub

    <Fact> 'https://github.com/icsharpcode/CodeConverter/issues/105
    Public Sub VisualBasicEqualityOfEmptyStringAndNothingIsPreservedForEmptyStringObject()
        Dim record = ""
        If emptyStringObject = nullObject Then record &= "1" Else record &= "0"
        If emptyStringObject = nullString Then record &= "1" Else record &= "0"
        If emptyStringObject = emptyStringObject Then record &= "1" Else record &= "0"
        If emptyStringObject = emptyString Then record &= "1" Else record &= "0"
        If emptyStringObject = nonEmptyString Then record &= "1" Else record &= "0"
        If emptyStringObject = emptyCharArray Then record &= "1" Else record &= "0"
        If emptyStringObject = nullCharArray Then record &= "1" Else record &= "0"
        Assert.Equal("1111011", record)
    End Sub

    <Fact> 'https://github.com/icsharpcode/CodeConverter/issues/105
    Public Sub VisualBasicEqualityOfEmptyStringAndNothingIsPreservedForEmptyString()
        Dim record = ""
        If emptyString = nullObject Then record &= "1" Else record &= "0"
        If emptyString = nullString Then record &= "1" Else record &= "0"
        If emptyString = emptyStringObject Then record &= "1" Else record &= "0"
        If emptyString = emptyString Then record &= "1" Else record &= "0"
        If emptyString = nonEmptyString Then record &= "1" Else record &= "0"
        If emptyString = emptyCharArray Then record &= "1" Else record &= "0"
        If emptyString = nullCharArray Then record &= "1" Else record &= "0"
        Assert.Equal("1111011", record)
    End Sub

    <Fact> 'https://github.com/icsharpcode/CodeConverter/issues/105
    Public Sub VisualBasicEqualityOfEmptyStringAndNothingIsPreservedForNonEmptyString()
        Dim record = ""
        If nonEmptyString = nullObject Then record &= "1" Else record &= "0"
        If nonEmptyString = nullString Then record &= "1" Else record &= "0"
        If nonEmptyString = emptyStringObject Then record &= "1" Else record &= "0"
        If nonEmptyString = emptyString Then record &= "1" Else record &= "0"
        If nonEmptyString = nonEmptyString Then record &= "1" Else record &= "0"
        If nonEmptyString = emptyCharArray Then record &= "1" Else record &= "0"
        If nonEmptyString = nullCharArray Then record &= "1" Else record &= "0"
        Assert.Equal("0000100", record)
    End Sub

    <Fact> 'https://github.com/icsharpcode/CodeConverter/issues/105
    Public Sub VisualBasicEqualityOfEmptyStringAndNothingIsPreservedForEmptyCharArray()
        Dim record = ""
        If emptyCharArray = nullObject Then record &= "1" Else record &= "0"
        If emptyCharArray = nullString Then record &= "1" Else record &= "0"
        If emptyCharArray = emptyStringObject Then record &= "1" Else record &= "0"
        If emptyCharArray = emptyString Then record &= "1" Else record &= "0"
        If emptyCharArray = nonEmptyString Then record &= "1" Else record &= "0"
        If emptyCharArray = emptyCharArray Then record &= "1" Else record &= "0"
        If emptyCharArray = nullCharArray Then record &= "1" Else record &= "0"
        Assert.Equal("1111011", record)
    End Sub

    <Fact> 'https://github.com/icsharpcode/CodeConverter/issues/105
    Public Sub VisualBasicEqualityOfEmptyStringAndNothingIsPreservedForNullCharArray()
        Dim record = ""
        If nullCharArray = nullObject Then record &= "1" Else record &= "0"
        If nullCharArray = nullString Then record &= "1" Else record &= "0"
        If nullCharArray = emptyStringObject Then record &= "1" Else record &= "0"
        If nullCharArray = emptyString Then record &= "1" Else record &= "0"
        If nullCharArray = nonEmptyString Then record &= "1" Else record &= "0"
        If nullCharArray = emptyCharArray Then record &= "1" Else record &= "0"
        If nullCharArray = nullCharArray Then record &= "1" Else record &= "0"
        Assert.Equal("1111011", record)
    End Sub
End Class
