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

    MustInherit Class Base
        MustOverride Sub UPPER()
        MustOverride Property FOO As Boolean
    End Class
    Class NotBase
        Inherits Base

        Public Overrides Sub upper()
        End Sub
        Public Overrides Property foo As Boolean
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
    End Sub
End Class
