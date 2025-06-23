Friend Class ContrivedFuncInferenceExample
    Private Sub TestMethod()
        For index = (Function(pList As List(Of String)) pList.All(Function(x) True)) To New Blah() Step New Blah()
            Dim buffer = index.Check(New List(Of String))
            Console.WriteLine($"{buffer}")
        Next
    End Sub

    Class Blah
        Public ReadOnly Check As Func(Of List(Of String), Boolean)

        Public Sub New(Optional check As Func(Of List(Of String), Boolean) = Nothing)
            check = check
        End Sub

        Public Shared Widening Operator CType(ByVal p1 As Func(Of List(Of String), Boolean)) As Blah
            Return New Blah(p1)
        End Operator
        Public Shared Widening Operator CType(ByVal p1 As Blah) As Func(Of List(Of String), Boolean)
            Return p1.Check
        End Operator
        Public Shared Operator -(ByVal p1 As Blah, ByVal p2 As Blah) As Blah
            Return New Blah()
        End Operator
        Public Shared Operator +(ByVal p1 As Blah, ByVal p2 As Blah) As Blah
            Return New Blah()
        End Operator
        Public Shared Operator <=(ByVal p1 As Blah, ByVal p2 As Blah) As Boolean
            Return p1.Check(New List(Of String))
        End Operator
        Public Shared Operator >=(ByVal p1 As Blah, ByVal p2 As Blah) As Boolean
            Return p2.Check(New List(Of String))
        End Operator
    End Class
End Class