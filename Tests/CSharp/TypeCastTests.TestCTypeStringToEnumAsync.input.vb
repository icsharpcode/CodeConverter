Public Enum TestEnum
    A
    B
End Enum

Public Class VisualBasicClass
    Public Sub Test(s as String)
        Dim x =  CType(s, TestEnum) = TestEnum.A
        Dim y = TestCast(CType(s, TestEnum))
    End Sub

    Public Function TestCast(s as System.Enum) As String
        Return s.ToString()
    End Function
End Class