Imports System
Imports System.Reflection
Imports Xunit

Public Class DecimalConstantDefaultValueTests
    Public Shared Function F(Optional ByRef f1 As Decimal = 0.1F, Optional ByRef f2 As Decimal = True, Optional o As Object = 1.5D) As String
        Return f1 & "," & f2 & "," & o.GetType().Name & o.ToString()
    End Function

    Public Shared Function G(Optional m As Decimal = 0.1F) As Decimal
        Return m
    End Function

    <Fact>
    Public Sub DecimalDefaultWithoutAttributesIsVbsConvertedValue()
        Assert.Equal("0.100000001490116", G().ToString(System.Globalization.CultureInfo.InvariantCulture))
        Assert.Equal("0.100000001490116", Convert.ToString(GetType(DecimalConstantDefaultValueTests).GetMethod("G").GetParameters()(0).DefaultValue, System.Globalization.CultureInfo.InvariantCulture))
    End Sub

    <Fact>
    Public Sub DecimalDefaultsInMetadataAreVbsConvertedValue()
        Dim parameters = GetType(DecimalConstantDefaultValueTests).GetMethod("F").GetParameters()
        Assert.Equal("0.100000001490116,-1,1.5,Decimal", String.Join(",", parameters(0).DefaultValue, parameters(1).DefaultValue, parameters(2).DefaultValue, parameters(2).DefaultValue.GetType().Name))
        Assert.Equal("0.100000001490116,-1,Decimal1.5", F())
    End Sub
End Class
