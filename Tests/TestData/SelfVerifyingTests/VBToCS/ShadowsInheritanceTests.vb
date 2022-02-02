Imports System
Imports System.Linq
Imports Xunit

Module Program

    Public Class ShadowsInheritanceTests
        <Fact()>
        Public Sub MyClassInheritance()
            Dim x As A = New B
            Assert.True(x.TestMethod())
        End Sub
    End Class

    Public Interface IA
        Function F1() As Integer
        Property P1() As Integer

        Function F2() As Integer
        Property P2() As Integer
    End Interface

    Public Class A
        Implements IA

        Overridable Function F1Renamed() As Integer Implements IA.F1
            Return 1
        End Function

        Overridable Function F2() As Integer Implements IA.F2
            Return 2
        End Function

        Overridable Property P1Renamed As Integer = 1 Implements IA.P1
        Overridable Property P2 As Integer = 2 Implements IA.P2

        Public Function TestMethod() As Boolean

            Dim overridedPropertyGood1 = P1Renamed = 1
            Dim overridedPropertyGood2 = CType(Me, IA).P1 = 1
            Dim overridedPropertyGood3 = CType(Me, B).P1Renamed = 11
            Dim shadowsPropertyGood1 = P2 = 2
            Dim shadowsPropertyGood2 = CType(Me, IA).P2 = 2
            Dim shadowsPropertyGood3 = CType(Me, B).P2 = 22

            Dim overridedMethodGood1 = F1Renamed() = 1
            Dim overridedMethodGood2 = CType(Me, IA).F1 = 1
            Dim overridedMethodGood3 = CType(Me, B).F1Renamed = 11
            Dim shadowsMethodGood1 = F2() = 2
            Dim shadowsMethodGood2 = CType(Me, IA).F2 = 2
            Dim shadowsMethodGood3 = CType(Me, B).F2 = 22

            Dim methodsGood = overridedMethodGood1 AndAlso overridedMethodGood2 AndAlso overridedMethodGood3 AndAlso
                              shadowsMethodGood1 AndAlso shadowsMethodGood2 AndAlso shadowsMethodGood3

            Dim propertiesGood = overridedPropertyGood1 AndAlso overridedPropertyGood2 AndAlso overridedPropertyGood3 AndAlso
                                 shadowsPropertyGood1 AndAlso shadowsPropertyGood2 AndAlso shadowsPropertyGood3

            Return methodsGood AndAlso propertiesGood
        End Function

    End Class


    Public Class B
        Inherits A
        Shadows Function F1Renamed() As Integer
            Return 11
        End Function

        Shadows Function F2() As Integer
            Return 22
        End Function


        Shadows Property P1Renamed As Integer = 11
        Shadows Property P2 As Integer = 22
    End Class
End Module
