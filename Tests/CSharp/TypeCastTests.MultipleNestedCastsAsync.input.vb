Public Class MultipleCasts
    Public Shared Function ToGenericParameter(Of T)(Value As Object) As T
        If Value Is Nothing Then
            Return Nothing
        End If
        Dim reflectedType As Global.System.Type = GetType(T)
        If Global.System.Type.Equals(reflectedType, GetType(Global.System.Int16)) Then
            Return DirectCast(CObj(CShort(Value)), T)
        ElseIf Global.System.Type.Equals(reflectedType, GetType(Global.System.UInt64)) Then
            Return DirectCast(CObj(CULng(Value)), T)
        Else
            Return DirectCast(Value, T)
        End If
    End Function
End Class