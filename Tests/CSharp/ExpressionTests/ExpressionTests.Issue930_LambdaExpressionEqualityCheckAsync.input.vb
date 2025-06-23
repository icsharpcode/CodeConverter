Option Compare Text
Imports System.Linq.Expressions

Class TestClass
    Private Sub TestMethod(a as Object)
        Dim test As Expression(Of Func(Of Boolean)) = Function() a = Nothing
        test.Compile()()
    End Sub
End Class