Option Strict Off
Imports System
Imports Xunit

Public Class NothingCompoundAssignmentTests
    <Fact>
    Public Sub NothingAndParenthesizedNothing()
        Dim d As Double = 1.5
        d += (Nothing)
        Dim e As Double = 2.5
        e *= Nothing
        Assert.Equal("1.5,0", String.Join(",", d.ToString(Globalization.CultureInfo.InvariantCulture), e.ToString(Globalization.CultureInfo.InvariantCulture)))
    End Sub
End Class
