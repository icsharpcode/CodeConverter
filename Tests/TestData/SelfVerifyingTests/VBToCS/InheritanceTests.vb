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

        <Fact()>
        Public Sub ConstructorInitializersWithNonCanonicalCasing()
            Assert.Equal("Base(lower)", New NonCanonicalCtorCalls("lower").Log)
            Assert.Equal("Base(upper);Derived(Integer)", New NonCanonicalCtorCalls(1).Log)
            Assert.Equal("Base(myclass);Derived(Long)", New NonCanonicalCtorCalls(1L).Log)
            Assert.Equal("Base();Derived(Short)", New NonCanonicalCtorCalls(1S).Log)
            Assert.Equal("Base(upper);Derived(Integer);Derived()", New NonCanonicalCtorCalls().Log)
            Assert.Equal("Base(call);Derived(Double)", New NonCanonicalCtorCalls(1.0).Log)
            Assert.Equal("Base(upper);Derived(Integer);Derived(Decimal)", New NonCanonicalCtorCalls(1D).Log)
            Assert.Equal("Base(myclass);Derived(Long);Derived(Byte)", New NonCanonicalCtorCalls(CByte(1)).Log)
        End Sub
    End Class

    Public Class CtorBase
        Public Log As String
        Public Sub New()
            Log = "Base()"
        End Sub
        Public Sub New(value As String)
            Log = "Base(" & value & ")"
        End Sub
    End Class

    Public Class NonCanonicalCtorCalls
        Inherits CtorBase
        Public Sub New(value As String)
            MyBase.new(value)
        End Sub
        Public Sub New(value As Integer)
            MYBASE.NEW("upper")
            Log &= ";Derived(Integer)"
        End Sub
        Public Sub New(value As Long)
            myclass.New("myclass")
            Log &= ";Derived(Long)"
        End Sub
        Public Sub New(value As Short)
            mybase.nEw
            Log &= ";Derived(Short)"
        End Sub
        Public Sub New()
            Me.new(1)
            Log &= ";Derived()"
        End Sub
        Public Sub New(value As Double)
            Call MyBase.new("call")
            Log &= ";Derived(Double)"
        End Sub
        Public Sub New(value As Decimal)
            CALL ME.NEW(1)
            Log &= ";Derived(Decimal)"
        End Sub
        Public Sub New(value As Byte)
            call myclass.New(1L)
            Log &= ";Derived(Byte)"
        End Sub
    End Class

    Public Interface IA
        Function F4() As Integer
        Property P4() As Integer
        ReadOnly Property P5() As Integer
    End Interface

    Public Class A
        Implements IA

        Overridable Function F1() As Integer
            Return 1
        End Function

        Function F2() As Integer
            Return 2
        End Function

        Shared Function F3() As Integer
            Return 3
        End Function

        Protected Overridable Function F4() As Integer Implements IA.F4
            Return 1
        End Function

        Overridable ReadOnly Property P1 As Integer = 1
        Property P2 As Integer = 2
        Shared Property P3 As Integer = 3
        Protected Overridable Property P4 As Integer = 4 Implements IA.P4
        Protected Friend Overridable ReadOnly Property P5Renamed As Integer = 5 Implements IA.P5

        Sub ChangeP5ViaDirectAccess()
            MyClass._P5Renamed = 555
        End Sub

        Public Function TestMethod() As Boolean
            Dim virtualMethodGood1 = MyClass.F1() = 1
            Dim virtualMethodGood2 = Me.F1() = 11
            Dim nonVirtualMethodGood1 = MyClass.F2() = 2
            Dim nonVirtualMethodGood2 = Me.F2() = 2
            Dim sharedMethodGood1 = MyClass.F3() = 3
            Dim sharedMethodGood2 = A.F3() = 3
            Dim interfaceMethodGood = CType(Me, IA).F4() = 44

            Dim virtualPropertyGood1 = MyClass.P1 = 1
            Dim virtualPropertyGood2 = Me.P1 = 11
            Dim virtualPropertyGood3 = Me.P5Renamed = 55
            Dim virtualPropertyGood4 = MyClass.P5Renamed = 5
            Dim nonVirtualPropertyGood1 = MyClass.P2 = 2
            Dim nonVirtualPropertyGood2 = Me.P2 = 2
            Dim sharedPropertyGood1 = MyClass.P3 = 3
            Dim sharedPropertyGood2 = A.P3 = 3
            Dim interfacePropertyGood1 = CType(Me, IA).P4 = 44
            Dim interfacePropertyGood2 = CType(Me, IA).P5 = 55

            ChangeP5ViaDirectAccess()
            Dim p5MyClassGood = MyClass.P5Renamed = 555
            Dim p5MeGood = Me.P5Renamed = 55
            Dim p5InterfaceGood =  CType(Me, IA).P5 = 55

            Dim methodsGood = virtualMethodGood1 AndAlso virtualMethodGood2 AndAlso
                              nonVirtualMethodGood1 AndAlso nonVirtualMethodGood2 AndAlso
                              sharedMethodGood1 AndAlso sharedMethodGood2 AndAlso interfaceMethodGood

            Dim propertiesGood = virtualPropertyGood1 AndAlso virtualPropertyGood2 AndAlso
                                 nonVirtualPropertyGood1 AndAlso nonVirtualPropertyGood2 AndAlso
                                 sharedPropertyGood1 AndAlso sharedPropertyGood2 AndAlso interfacePropertyGood1 AndAlso 
                                 virtualPropertyGood3 AndAlso virtualPropertyGood4 AndAlso interfacePropertyGood2 AndAlso 
                                 p5MyClassGood AndAlso p5MeGood AndAlso p5InterfaceGood

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

        Protected Overrides Function F4() As Integer
            Return 44
        End Function

        Protected Overrides Property P4 As Integer = 44
        Protected Friend Overrides ReadOnly Property P5Renamed As Integer = 55
    End Class
End Module
