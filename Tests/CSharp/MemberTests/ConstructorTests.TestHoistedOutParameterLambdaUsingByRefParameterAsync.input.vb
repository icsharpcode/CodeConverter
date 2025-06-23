Public Class SomeClass
    Sub S(Optional ByRef x As Integer = -1)
        Dim i As Integer = 0
        If F1(x, i) Then
        ElseIf F2(x, i) Then
        ElseIf F3(x, i) Then
        End If
    End Sub

    Function F1(x As Integer, ByRef o As Object) As Boolean : End Function
    Function F2(ByRef x As Integer, ByRef o As Object) As Boolean : End Function
    Function F3(ByRef x As Object, ByRef o As Object) As Boolean : End Function
End Class