Class TestClass
    Private Async Sub TestMethod()
        Dim test0 As Func(Of Task(Of Integer)) = Async Function()  2
        Dim test1 As Func(Of Integer, Task(Of Integer)) = Async Function(a) a * 2
        Dim test2 As Func(Of Integer, Integer, Task(Of Double)) = Async Function(a, b)
            If b > 0 Then Return a / b
            Return 0
        End Function

        Dim test3 As Func(Of Integer, Integer, Task(Of Integer)) = Async Function(a, b) a Mod b
        Dim test4 As Func(Of Task(Of Integer)) = Async Function()  
            dim i as Integer = 2
            dim x as Integer = 3
            return 3
        End Function
        
Await test1(3)
    End Sub
End Class