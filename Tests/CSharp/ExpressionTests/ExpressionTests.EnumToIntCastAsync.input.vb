Public Class MyTest
    Public Enum TestEnum As Integer
        Test1 = 0
        Test2 = 1
    End Enum

    Sub Main()
        Dim EnumVariable = TestEnum.Test1
        Dim t1 As Integer = EnumVariable
    End Sub
End Class