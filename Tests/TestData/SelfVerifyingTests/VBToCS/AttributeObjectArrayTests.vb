Option Strict Off
Imports System
Imports System.Linq
Imports Xunit

Public Class AttributeObjectArrayTests
    Public Enum E As Byte
        A = 1
    End Enum

    <AttributeUsage(AttributeTargets.All)>
    Public Class ValuesAttribute
        Inherits Attribute
        Public ReadOnly Values As Object()
        Public Sub New(values As Object())
            Me.Values = values
        End Sub
    End Class

    <Values(New Object() {CByte(True), CType(2.5, E), CShort(-1), 3, "s"})>
    Public Class Target
    End Class

    <Fact>
    Public Sub ObjectElementsKeepVbsType()
        Dim attribute = DirectCast(GetType(Target).GetCustomAttributes(GetType(ValuesAttribute), False).Single(), ValuesAttribute)
        Assert.Equal("Byte:255,E:2,Int16:-1,Int32:3,String:s", String.Join(",", attribute.Values.Select(Function(v) v.GetType().Name & ":" & v.ToString())))
    End Sub
End Class
