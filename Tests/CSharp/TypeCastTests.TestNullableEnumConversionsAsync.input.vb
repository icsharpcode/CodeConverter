
Enum TestEnum
    None = 1
End Enum
Class Class1
    Private Function Test1(a as Integer) As TestEnum?
        Return a
    End Function
    Private Function Test2(a as Integer?) As TestEnum?
        Return a
    End Function
    Private Function Test3(a as Integer?) As TestEnum
        Return a
    End Function

    Private Function Test4(a as TestEnum) As Integer?
        Return a
    End Function
    Private Function Test5(a as TestEnum?) As Integer?
        Return a
    End Function
    Private Function Test6(a as TestEnum?) As TestEnum?
        Return a
    End Function
    Private Function Test7(a as TestEnum?) As Integer
        Return a
    End Function

    Private Function Test8(a as TestEnum?) As String
        Return a
    End Function
    Private Function Test9(a as TestEnum?) As String
        Return a
    End Function
    
    Private Function Test10(a as String) As TestEnum?
        Return a
    End Function
    Private Function Test11(a as String) As TestEnum
        Return a
    End Function
End Class