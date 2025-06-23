Class TestClass
    Private Sub TestMethod()
        Dim i=1
        Do
            Dim b As Integer
            b  +=1
            Console.WriteLine("b={0}", b)
            For j = 1 To 3
                Dim c As Integer
                c  +=1
                Console.WriteLine("c={0}", c)
            Next
            For j = 1 To 3
                Dim c As Integer
                c +=1
                Console.WriteLine("c1={0}", c)
            Next
            Dim k=1
            Do while k <= 3
                Dim c As Integer
                c +=1
                Console.WriteLine("c2={0}", c)
                k+=1
            Loop
        i += 1
        Loop Until i > 3
    End Sub
End Class