Namespace TestNamespace
    Public Module TestModule
        Public Sub ModuleFunction()
        End Sub
    End Module
End Namespace

Class TestClass
    Public Sub TestMethod(dir As String)
        TestNamespace.ModuleFunction()
    End Sub
End Class