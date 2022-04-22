Imports System
Imports System.Linq
Imports Xunit

Module Program

    Public Class ParametrizedPropertiesInheritanceTests
        <Fact()>
        Public Sub MyClassInheritance()
            Dim x As A = New B
            Assert.True(x.TestMethod())
        End Sub
    End Class

    Public Interface IA
        Property P1(Optional str As String = "") As Integer
        Property P2(Optional str As String = "") As Integer
    End Interface

    Public Class A
        Implements IA

        Private _p1 As Integer = 1
        Property P1(Optional str As String = "") As Integer Implements IA.P1
            Get
                Return _p1
            End Get
            Set
                _p1 = Value
            End Set
        End Property

        Private _p2 As Integer = 2
        Protected Overridable Property P2(Optional str As String = "") As Integer Implements IA.P2
            Get
                Return _p2
            End Get
            Set
                _p2 = Value
            End Set
        End Property

        Public Function TestMethod() As Boolean
            Dim nonVirtualPropertyMe = Me.P1() = 1
            Dim interfaceProperty1 = CType(Me, IA).P1() = 1
            Me.P1() = 11
            Dim nonVirtualPropertyMeAfterSet = Me.P1() = 11
            Dim interfaceProperty1AfterSet = CType(Me, IA).P1() = 11

            Dim virtualPropertyMe = Me.P2() = 22
            Dim interfaceProperty2 = CType(Me, IA).P2() = 22
            Me.P2 = 33
            Dim virtualPropertyMeAfterSet = Me.P2 = 33
            Dim interfaceProperty2AfterSet = CType(Me, IA).P2() = 33

            Dim P1Good = nonVirtualPropertyMe AndAlso interfaceProperty1 AndAlso 
                         nonVirtualPropertyMeAfterSet AndAlso interfaceProperty1AfterSet

            Dim P2Good = virtualPropertyMe AndAlso interfaceProperty2 AndAlso 
                         virtualPropertyMeAfterSet AndAlso interfaceProperty2AfterSet

            Return P1Good AndAlso P2Good
        End Function

    End Class


    Public Class B
        Inherits A

        Private _p2 As Integer = 22
        Protected Overrides Property P2(Optional str As String = "") As Integer
            Get
                Return _p2
            End Get
            Set
                _p2 = Value
            End Set
        End Property
    End Class
End Module
