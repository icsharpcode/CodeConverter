Class Class1
    Private Function Test1(a as Boolean?) As Boolean
        Return a
    End Function
    Private Function Test2(a as Boolean?) As Boolean?
        Return a
    End Function
    Private Function Test3(a as Boolean) As Boolean?
        Return a
    End Function

    Private Function Test4(a as Integer?) As Boolean
        Return a
    End Function
    Private Function Test5(a as Integer?) As Boolean?
        Return a
    End Function
    Private Function Test6(a as Integer) As Boolean?
        Return a
    End Function

    Private Function Test4(a as Boolean?) As Integer
        Return a
    End Function
    Private Function Test5(a as Boolean?) As Integer?
        Return a
    End Function
    Private Function Test6(a as Boolean) As Integer?
        Return a
    End Function

    Private Function Test7(a as Boolean?) As String
        Return a
    End Function
    Private Function Test8(a as Boolean?) As String
        Return a
    End Function

    Private Function Test9(a as String) As Boolean?
        Return a
    End Function
    Private Function Test10(a as String) As Boolean
        Return a
    End Function
End Class