Public Class ByRefArgument
    Sub UseArr()
        Dim arrObj() As Object
        Modify(arrObj(0))

        Dim arrInt() As Integer
        Modify(arrInt(0))
    End Sub

    Sub UseRefReturn()
        Dim lstObj As CSharpRefReturn.RefReturnList(Of Object)
        Modify(lstObj(0))
        Modify(lstObj.RefProperty)

        Dim lstInt As CSharpRefReturn.RefReturnList(Of Integer)
        Modify(lstInt(0))
        Modify(lstInt.RefProperty)
    End Sub

    Sub Modify(ByRef o As Object)
    End Sub
End Class
