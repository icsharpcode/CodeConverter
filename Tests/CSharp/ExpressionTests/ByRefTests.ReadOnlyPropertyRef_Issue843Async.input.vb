Module Module1

    Public Class TestClass
        Public ReadOnly Property Foo As String

        Public Sub New()
            Foo = "abc"
        End Sub
    End Class

    Sub Main()
        Test02()
    End Sub

    Private Sub Test02()
        Dim t As New TestClass
        Test02Sub(t.Foo)
    End Sub

    Private Sub Test02Sub(ByRef value As String)
        Console.WriteLine(value)
    End Sub

End Module