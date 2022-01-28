Imports Xunit

Namespace [Aaa]
    Friend Class A
        Shared Sub Foo()
        End Sub
    End Class

    Partial Class Z
    End Class
    Partial Class z
    End Class
	
	Public Interface IBase
		Sub UPper()
		Property Foo As Boolean
		
		Function Fun() As Integer
		Property Bar As Integer
	End Interface

    MustInherit Class Base
		Implements IBase
		
        MustOverride Sub UPPER() Implements IBase.UPPER
        MustOverride Property FOO As Boolean Implements IBase.FOO
		
		MustOverride Function FunRenamed() As Integer Implements IBase.Fun
		MustOverride Property BarRenamed As Integer Implements IBase.Bar
    End Class
    Class NotBase
        Inherits Base

        Public Overrides Sub upper()
        End Sub
        Public Overrides Property foo As Boolean
		
		Public Overrides Function funRENAMED() As Integer
		End Function
		Public Overrides Property barRENAMED As Integer
    End Class
End Namespace

Namespace Global.aaa
    Friend Class B
        Shared Sub Bar()
        End Sub
    End Class
End Namespace

Public Class NamespaceCasing
    <Fact>
    Sub TestThisCompiles()
        ' Visual Studio likes to fix casing automatically,
        ' so if editting this, do it in a separate editor
        Dim x = New aaa.A
        Dim y = New Aaa.B
        Dim z = New Aaa.A
        Dim a = New aaa.B
        Dim b = New aaa.a
        Dim c = New aaa.b
        Dim d = New AAA.A
        Dim e = New AAA.B
        Dim f = New Aaa.Z
        Dim g = New Aaa.z
        aaa.a.foo()
        Aaa.A.Foo()
        aaa.b.bar()
        Aaa.B.Bar()

        Dim notBase As New Aaa.NotBase
        Dim base As Aaa.Base = notBase
		Dim interf As Aaa.IBase = notBase
        notBase.upper()
        base.UPPER()
		interf.UPper()
        notBase.foo = True
        base.FOO = True
		interf.Foo = True
		
		base.FunRenamed()
		base.BarRenamed = 5
		notBase.funRENAMED()
		notBase.barRENAMED = 5
		interf.Fun()
		interf.Bar = 5
    End Sub
End Class
