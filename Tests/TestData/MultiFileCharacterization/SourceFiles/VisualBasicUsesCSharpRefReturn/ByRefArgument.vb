Public Class ByRefArgument
    Sub UseArr()
        Dim arr() As Object
        Modify(arr(0))
    End Sub

    Sub UseRefReturn()
        Dim lst As CSharpRefReturn.RefReturnList(Of Object)
        Modify(lst(0))
    End Sub

    Sub Modify(ByRef o As Object)
    End Sub
End Class
