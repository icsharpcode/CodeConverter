Public Class TestFunc
    Public pubIdent = Function(row As Integer) row
    Public pubWrite = Function(row As Integer) Console.WriteLine(row)
    Dim isFalse = Function(row As Integer) False
    Dim write0 = Sub()
        Console.WriteLine(0)
    End Sub

    Private Sub TestMethod()
        Dim index = (Function(pList As List(Of String)) pList.All(Function(x) True)),
            index2 = (Function(pList As List(Of String)) pList.All(Function(x) False)),
            index3 = (Function(pList As List(Of Integer)) pList.All(Function(x) True))
        Dim isTrue = Function(pList As List(Of String))
                            Return pList.All(Function(x) True)
                     End Function
        Dim isTrueWithNoStatement = (Function(pList As List(Of String)) pList.All(Function(x) True))
        Dim write = Sub() Console.WriteLine(1)
    End Sub
End Class