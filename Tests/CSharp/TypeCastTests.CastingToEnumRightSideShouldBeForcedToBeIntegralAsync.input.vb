
Public Class C
    Public Enum OrderStatus
        Pending = 0
        Fullfilled = 1
    End Enum
 
    Sub Test1()
        Dim val As Object = "1"
        Dim os1 = CType(val, OrderStatus)
        Dim os2 As OrderStatus = val

        Dim null1 = CType(val, OrderStatus?)
        Dim null2 As OrderStatus? = val
    End Sub
    Sub Test2()
        Dim val As String = "1"
        Dim os1 = CType(val, OrderStatus)
        Dim os2 As OrderStatus = val

        Dim null1 = CType(val, OrderStatus?)
        Dim null2 As OrderStatus? = val
    End Sub
    Sub Test3()
        Dim val As Object = 1
        Dim os1 = CType(val, OrderStatus)
        Dim os2 As OrderStatus = val

        Dim null1 = CType(val, OrderStatus?)
        Dim null2 As OrderStatus? = val
    End Sub
    Sub Test4()
        Dim val = CType(1.5D, Object)
        Dim os1 = CType(val, OrderStatus)
        Dim os2 As OrderStatus = val

        Dim null1 = CType(val, OrderStatus?)
        Dim null2 As OrderStatus? = val
    End Sub
End Class