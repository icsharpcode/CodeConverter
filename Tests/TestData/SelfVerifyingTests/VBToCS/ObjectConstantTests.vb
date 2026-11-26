Imports System
Imports System.Reflection
Imports Xunit

<AttributeUsage(AttributeTargets.Class)>
Public Class ObjectConstantAttribute
    Inherits Attribute
    Public O As Object
End Class

<ObjectConstant(O:="a"c & "b")>
Public Class ObjectConstantTests
    Private Shared Function F(Optional o As Object = "a"c & "b") As Object
        Return o
    End Function

    <Fact>
    Public Sub CharConcatenationIsVbsValue()
        Assert.Equal("ab", CStr(F()))
        Assert.Equal("ab", CStr(GetType(ObjectConstantTests).GetCustomAttribute(Of ObjectConstantAttribute)().O))
    End Sub
End Class
