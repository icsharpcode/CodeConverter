Imports System
Imports System.Linq
Imports Xunit

Module Program

    Public Class InheritanceTests
    <Fact()>
        Public Sub MyClassInheritance()
            Dim x As A = New B
            Assert.True(x.TestMethod())
        End Sub
    End Class

    Public Class A
        Overridable Function F1() As Integer
            Return 1
        End Function

        Function F2() As Integer
            Return 2
        End Function

        Shared Function F3() As Integer
            Return 3
        End Function

        Overridable ReadOnly Property P1 As Integer = 1
        Property P2 As Integer = 2
        Shared Property P3 As Integer = 3

        Public Function TestMethod() As Boolean
            Dim virtualMethodGood1 = MyClass.F1() = 1
            Dim virtualMethodGood2 = Me.F1() = 11
            Dim nonVirtualMethodGood1 = MyClass.F2() = 2
            Dim nonVirtualMethodGood2 = Me.F2() = 2
            Dim sharedMethodGood1 = MyClass.F3() = 3
            Dim sharedMethodGood2 = A.F3() = 3

            Dim virtualPropertyGood1 = MyClass.P1 = 1
            Dim virtualPropertyGood2 = Me.P1 = 11
            Dim nonVirtualPropertyGood1 = MyClass.P2 = 2
            Dim nonVirtualPropertyGood2 = Me.P2 = 2
            Dim sharedPropertyGood1 = MyClass.P3 = 3
            Dim sharedPropertyGood2 = A.P3 = 3

            Dim methodsGood = virtualMethodGood1 AndAlso virtualMethodGood2 AndAlso
                              nonVirtualMethodGood1 AndAlso nonVirtualMethodGood2 AndAlso
                              sharedMethodGood1 AndAlso sharedMethodGood2

            Dim propertiesGood = virtualPropertyGood1 AndAlso virtualPropertyGood2 AndAlso
                                 nonVirtualPropertyGood1 AndAlso nonVirtualPropertyGood2 AndAlso
                                 sharedPropertyGood1 AndAlso sharedPropertyGood2

            Return methodsGood AndAlso propertiesGood
        End Function
    End Class


    Public Class B
        Inherits A
        Public Overrides Function F1() As Integer
            Return 11
        End Function

        Public Overrides ReadOnly Property P1 As Integer
            Get
                Return 11
            End Get
        End Property
    End Class
End Module
