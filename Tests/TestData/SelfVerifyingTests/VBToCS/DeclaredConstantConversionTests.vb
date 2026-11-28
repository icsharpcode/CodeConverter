Imports System
Imports System.Reflection
Imports Xunit

Public Enum DeclaredColour
    Red
    Green
End Enum

<Flags>
Public Enum DeclaredFlags
    A = 1
    X = A Or 2.5
    K = DeclaredConstants.L
    H = 1.5
    C = DeclaredConstants.D
    B = A + 1
End Enum

Public Module DeclaredConstants
    Public Const L As Long = 7
    Public Const D As Double = 2.5
    Public Const X As Integer = 5
End Module

<AttributeUsage(AttributeTargets.Class)>
Public Class DeclaredValuesAttribute
    Inherits Attribute
    Public Sub New(i As Integer)
        Value = i
    End Sub
    Public ReadOnly Property Value As Integer
    Public Property P As Integer
    Public F As Short
End Class

<DeclaredValues(2.5, P:=3.5, F:=7)>
Public Class DeclaredConstantConversionTests

    Private Function Defaults(Optional a1 As Integer = L, Optional c As Integer = DeclaredColour.Green, Optional b As Single = D,
                              Optional n As DeclaredColour = 1, Optional h As Integer? = 2.5, Optional ls As Long? = D,
                              Optional z As Short = True << 3, Optional c2 As DeclaredColour = 1.5, Optional bx As Byte = X + 1,
                              Optional a As Integer = 1.5, Optional o As Object = 1.5, Optional ni As Integer? = Nothing) As String
        Return String.Join(",", a1, c, b, n, h, ls, z, c2, bx, a, o, If(ni.HasValue, "x", "Nothing"))
    End Function

    Private Function ByRefDefaults(Optional ByRef b As Short = 3, Optional ByRef r As Integer = 2.5, Optional ByRef s As Integer = X) As String
        Return String.Join(",", b, r, s)
    End Function

    Private Function DateFirst(Optional d As Date = #1/2/2000#, Optional i As Integer = 3, Optional s As String = Nothing) As String
        Return String.Join(",", d.Day, i, If(s Is Nothing, "Nothing", s))
    End Function

    <Fact>
    Public Sub DefaultsAreConvertedToTheParameterType()
        Assert.Equal("7,1,2.5,Green,2,2,-8,2,6,2,1.5,Nothing", Defaults())
        Assert.Equal("3,2,5", ByRefDefaults())
        Assert.Equal("2,3,Nothing", DateFirst())
    End Sub

    <Fact>
    Public Sub EnumMembersAreConvertedToTheUnderlyingType()
        Assert.Equal("3,7,2,2,2", String.Join(",", CInt(DeclaredFlags.X), CInt(DeclaredFlags.K), CInt(DeclaredFlags.H), CInt(DeclaredFlags.C), CInt(DeclaredFlags.B)))
    End Sub

    <Fact>
    Public Sub AttributeArgumentsAreConvertedToTheirParameterOrMemberType()
        Dim attribute = GetType(DeclaredConstantConversionTests).GetCustomAttribute(Of DeclaredValuesAttribute)()
        Assert.Equal("2,4,7", String.Join(",", attribute.Value, attribute.P, attribute.F))
    End Sub
End Class
