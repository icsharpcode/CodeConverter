Imports System
Imports Xunit

Public Enum LateBoundFlags As Byte
    Top = 128
End Enum

Public Class LateBoundCompoundAssignmentTests
    Private _q As Byte
    Private Property Q(i As Integer) As Byte
        Get
            Return _q
        End Get
        Set(value As Byte)
            _q = value
        End Set
    End Property

    Private Shared Function Attempt(f As Func(Of Object)) As String
        Try
            Dim v = f()
            Return If(v Is Nothing, "Nothing", v.GetType().Name & ":" & String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}", v))
        Catch ex As Exception
            Return ex.GetType().Name
        End Try
    End Function

    <Fact>
    Public Sub EveryCompoundOperatorWithAnObjectOperand()
        Dim originalCulture = System.Globalization.CultureInfo.CurrentCulture
        System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture
        Try
            Dim ob As Object = 3
            Dim log As New System.Collections.Generic.List(Of String)
        log.Add(Attempt(Function()
                            Dim x As Object = CObj(12)
                            x += ob
                            Return x
                        End Function))
        log.Add(Attempt(Function()
                            Dim x As Object = CObj(12)
                            x -= ob
                            Return x
                        End Function))
        log.Add(Attempt(Function()
                            Dim x As Object = CObj(12)
                            x *= ob
                            Return x
                        End Function))
        log.Add(Attempt(Function()
                            Dim x As Object = CObj(12)
                            x /= ob
                            Return x
                        End Function))
        log.Add(Attempt(Function()
                            Dim x As Object = CObj(12)
                            x \= ob
                            Return x
                        End Function))
        log.Add(Attempt(Function()
                            Dim x As Object = CObj(12)
                            x ^= ob
                            Return x
                        End Function))
        log.Add(Attempt(Function()
                            Dim x As Object = CObj(12)
                            x &= ob
                            Return x
                        End Function))
        log.Add(Attempt(Function()
                            Dim x As Object = CObj(12)
                            x <<= ob
                            Return x
                        End Function))
        log.Add(Attempt(Function()
                            Dim x As Object = CObj(12)
                            x >>= ob
                            Return x
                        End Function))
        log.Add(Attempt(Function()
                            Dim x As String = "12"
                            x += ob
                            Return x
                        End Function))
        log.Add(Attempt(Function()
                            Dim x As String = "12"
                            x -= ob
                            Return x
                        End Function))
        log.Add(Attempt(Function()
                            Dim x As String = "12"
                            x *= ob
                            Return x
                        End Function))
        log.Add(Attempt(Function()
                            Dim x As String = "12"
                            x /= ob
                            Return x
                        End Function))
        log.Add(Attempt(Function()
                            Dim x As String = "12"
                            x \= ob
                            Return x
                        End Function))
        log.Add(Attempt(Function()
                            Dim x As String = "12"
                            x ^= ob
                            Return x
                        End Function))
        log.Add(Attempt(Function()
                            Dim x As String = "12"
                            x &= ob
                            Return x
                        End Function))
        log.Add(Attempt(Function()
                            Dim x As String = "12"
                            x <<= ob
                            Return x
                        End Function))
        log.Add(Attempt(Function()
                            Dim x As String = "12"
                            x >>= ob
                            Return x
                        End Function))
        log.Add(Attempt(Function()
                            Dim x As Byte = CByte(12)
                            x += ob
                            Return x
                        End Function))
        log.Add(Attempt(Function()
                            Dim x As Byte = CByte(12)
                            x -= ob
                            Return x
                        End Function))
        log.Add(Attempt(Function()
                            Dim x As Byte = CByte(12)
                            x *= ob
                            Return x
                        End Function))
        log.Add(Attempt(Function()
                            Dim x As Byte = CByte(12)
                            x /= ob
                            Return x
                        End Function))
        log.Add(Attempt(Function()
                            Dim x As Byte = CByte(12)
                            x \= ob
                            Return x
                        End Function))
        log.Add(Attempt(Function()
                            Dim x As Byte = CByte(12)
                            x ^= ob
                            Return x
                        End Function))
        log.Add(Attempt(Function()
                            Dim x As Byte = CByte(12)
                            x &= ob
                            Return x
                        End Function))
        log.Add(Attempt(Function()
                            Dim x As Byte = CByte(12)
                            x <<= ob
                            Return x
                        End Function))
        log.Add(Attempt(Function()
                            Dim x As Byte = CByte(12)
                            x >>= ob
                            Return x
                        End Function))
        log.Add(Attempt(Function()
                            Dim x As Short = 12S
                            x += ob
                            Return x
                        End Function))
        log.Add(Attempt(Function()
                            Dim x As Short = 12S
                            x -= ob
                            Return x
                        End Function))
        log.Add(Attempt(Function()
                            Dim x As Short = 12S
                            x *= ob
                            Return x
                        End Function))
        log.Add(Attempt(Function()
                            Dim x As Short = 12S
                            x /= ob
                            Return x
                        End Function))
        log.Add(Attempt(Function()
                            Dim x As Short = 12S
                            x \= ob
                            Return x
                        End Function))
        log.Add(Attempt(Function()
                            Dim x As Short = 12S
                            x ^= ob
                            Return x
                        End Function))
        log.Add(Attempt(Function()
                            Dim x As Short = 12S
                            x &= ob
                            Return x
                        End Function))
        log.Add(Attempt(Function()
                            Dim x As Short = 12S
                            x <<= ob
                            Return x
                        End Function))
        log.Add(Attempt(Function()
                            Dim x As Short = 12S
                            x >>= ob
                            Return x
                        End Function))
        log.Add(Attempt(Function()
                            Dim x As Integer? = CType(12, Integer?)
                            x += ob
                            Return x
                        End Function))
        log.Add(Attempt(Function()
                            Dim x As Integer? = CType(12, Integer?)
                            x -= ob
                            Return x
                        End Function))
        log.Add(Attempt(Function()
                            Dim x As Integer? = CType(12, Integer?)
                            x *= ob
                            Return x
                        End Function))
        log.Add(Attempt(Function()
                            Dim x As Integer? = CType(12, Integer?)
                            x /= ob
                            Return x
                        End Function))
        log.Add(Attempt(Function()
                            Dim x As Integer? = CType(12, Integer?)
                            x \= ob
                            Return x
                        End Function))
        log.Add(Attempt(Function()
                            Dim x As Integer? = CType(12, Integer?)
                            x ^= ob
                            Return x
                        End Function))
        log.Add(Attempt(Function()
                            Dim x As Integer? = CType(12, Integer?)
                            x &= ob
                            Return x
                        End Function))
        log.Add(Attempt(Function()
                            Dim x As Integer? = CType(12, Integer?)
                            x <<= ob
                            Return x
                        End Function))
        log.Add(Attempt(Function()
                            Dim x As Integer? = CType(12, Integer?)
                            x >>= ob
                            Return x
                        End Function))
        log.Add(Attempt(Function()
                            Dim x As LateBoundFlags = LateBoundFlags.Top
                            x += ob
                            Return x
                        End Function))
        log.Add(Attempt(Function()
                            Dim x As LateBoundFlags = LateBoundFlags.Top
                            x -= ob
                            Return x
                        End Function))
        log.Add(Attempt(Function()
                            Dim x As LateBoundFlags = LateBoundFlags.Top
                            x *= ob
                            Return x
                        End Function))
        log.Add(Attempt(Function()
                            Dim x As LateBoundFlags = LateBoundFlags.Top
                            x /= ob
                            Return x
                        End Function))
        log.Add(Attempt(Function()
                            Dim x As LateBoundFlags = LateBoundFlags.Top
                            x \= ob
                            Return x
                        End Function))
        log.Add(Attempt(Function()
                            Dim x As LateBoundFlags = LateBoundFlags.Top
                            x ^= ob
                            Return x
                        End Function))
        log.Add(Attempt(Function()
                            Dim x As LateBoundFlags = LateBoundFlags.Top
                            x &= ob
                            Return x
                        End Function))
        log.Add(Attempt(Function()
                            Dim x As LateBoundFlags = LateBoundFlags.Top
                            x <<= ob
                            Return x
                        End Function))
        log.Add(Attempt(Function()
                            Dim x As LateBoundFlags = LateBoundFlags.Top
                            x >>= ob
                            Return x
                        End Function))
        log.Add(Attempt(Function()
                            Dim x As Date = #1/2/2000#
                            x += ob
                            Return x
                        End Function))
        log.Add(Attempt(Function()
                            Dim x As Date = #1/2/2000#
                            x &= ob
                            Return x
                        End Function))

            Assert.Equal("Int32:15|Int32:9|Int32:36|Double:4|Int32:4|Double:1728|String:123|Int32:96|Int32:1|String:15|String:9|String:36|String:4|String:4|String:1728|String:123|String:96|String:1|Byte:15|Byte:9|Byte:36|Byte:4|Byte:4|OverflowException|Byte:123|Byte:96|Byte:1|Int16:15|Int16:9|Int16:36|Int16:4|Int16:4|Int16:1728|Int16:123|Int16:96|Int16:1|Int32:15|Int32:9|Int32:36|InvalidCastException|Int32:4|InvalidCastException|InvalidCastException|Int32:96|Int32:1|LateBoundFlags:131|LateBoundFlags:125|OverflowException|LateBoundFlags:43|LateBoundFlags:42|OverflowException|OverflowException|LateBoundFlags:0|LateBoundFlags:16|InvalidCastException|InvalidCastException", String.Join("|", log))
        Finally
            System.Globalization.CultureInfo.CurrentCulture = originalCulture
        End Try
    End Sub

    <Fact>
    Public Sub LateBoundCompoundAssignmentsToParameterizedProperty()
        Dim ob As Object = 3
        Q(1) = 10
        Q(1) += ob
        Assert.Equal(CByte(13), Q(1))
        Q(1) <<= ob
        Assert.Equal(CByte(104), Q(1))
        Q(1) \= ob
        Assert.Equal(CByte(34), Q(1))
        Q(1) = 1
        Q(1) &= ob
        Assert.Equal(CByte(13), Q(1))
    End Sub
End Class
