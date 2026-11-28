Option Strict Off
Imports System
Imports System.Linq
Imports Xunit

Namespace ProbeObjtype
Public Enum E
    A = 1
    B = 2
End Enum
<AttributeUsage(AttributeTargets.All, AllowMultiple:=True)>
Public Class AAttribute
    Inherits Attribute
    Public O As Object
    Public Sub New(o As Object)
        Me.O = o
    End Sub
End Class
<A(CByte(True))>
<A(CShort(True))>
<A(CType(2.5, E))>
<A(CUShort(AscW("A"c)))>
<A(CType(CByte(1), E))>
Public Class Target
End Class
Public Class C
    Public Shared Function D1(Optional x As Object = CByte(True)) As Object
        Return x
    End Function
    Public Shared Function D2(Optional x As Object = CType(2.5, E)) As Object
        Return x
    End Function
    Public Shared Function D3(Optional x As Object = CShort(AscW("A"c))) As Object
        Return x
    End Function
End Class
Public Module M
    Function Show(o As Object) As String
        If o Is Nothing Then Return "Nothing"
        Return o.GetType().Name & ":" & Convert.ToString(o, Globalization.CultureInfo.InvariantCulture)
    End Function
    Public Function ProbeAttrs() As String
        Return String.Join(" ; ", GetType(Target).GetCustomAttributes(GetType(AAttribute), False).Cast(Of AAttribute)().Select(Function(a) Show(a.O)))
    End Function
    Public Function ProbeCalls() As String
        Return Show(C.D1()) & " ; " & Show(C.D2()) & " ; " & Show(C.D3())
    End Function
    Public Function ProbeUnbox() As String
        Try
            Dim b As Byte = DirectCast(C.D1(), Byte)
            Dim s As Short = DirectCast(C.D3(), Short)
            Return b & "," & s
        Catch ex As InvalidCastException
            Return "InvalidCast"
        End Try
    End Function
End Module
End Namespace

Public Class ObjectConstantTypeTests
    <Fact>
    Public Sub ProbeObjtype_ProbeAttrs()
        Assert.Equal("Byte:255 ; Int16:-1 ; E:B ; UInt16:65 ; E:A", ProbeObjtype.M.ProbeAttrs())
    End Sub

    <Fact>
    Public Sub ProbeObjtype_ProbeCalls()
        Assert.Equal("Byte:255 ; Int32:2 ; Int16:65", ProbeObjtype.M.ProbeCalls())
    End Sub

    <Fact>
    Public Sub ProbeObjtype_ProbeUnbox()
        Assert.Equal("255,65", ProbeObjtype.M.ProbeUnbox())
    End Sub
End Class
