Option Compare Text
Imports System.Linq.Expressions

Class TestClass
    Private Sub TestMethod(a as String, b as String)
        Dim test As Expression(Of Func(Of Boolean)) = Function() a = b
        test.Compile()()
    End Sub
End Class