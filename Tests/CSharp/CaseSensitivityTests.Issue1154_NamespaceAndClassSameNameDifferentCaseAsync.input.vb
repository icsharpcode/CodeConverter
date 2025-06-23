
Imports System

Namespace Issue1154
    <CaseSensitive1.Casesensitive1.TestDummy>
    Public Class UpperLowerCase
    End Class

    <Casesensitive2.CaseSensitive2.TestDummy>
    Public Class LowerUpperCase
    End Class

    <CaseSensitive3.CaseSensitive3.TestDummy>
    Public Class SameCase
    End Class
End Namespace

Namespace CaseSensitive1
    Public Class Casesensitive1
        Public Class TestDummyAttribute
            Inherits Attribute
        End Class
    End Class
End Namespace

Namespace Casesensitive2
    Public Class CaseSensitive2
        Public Class TestDummyAttribute
            Inherits Attribute
        End Class
    End Class
End Namespace

Namespace CaseSensitive3
    Public Class CaseSensitive3
        Public Class TestDummyAttribute
            Inherits Attribute
        End Class
    End Class
End Namespace
