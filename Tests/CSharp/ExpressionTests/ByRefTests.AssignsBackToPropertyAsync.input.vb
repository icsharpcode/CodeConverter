Imports System

Public Class MyTestClass

    Private Property Prop As Integer
    Private Property Prop2 As Integer
        
    Private Function TakesRef(ByRef vrbTst As Integer) As Boolean
        vrbTst = Prop + 1
            Return vrbTst > 3
    End Function
        
    Private Sub TakesRefVoid(ByRef vrbTst As Integer)
        vrbTst = vrbTst + 1
    End Sub

    Public Sub UsesRef(someBool As Boolean, someInt As Integer)

        TakesRefVoid(someInt) ' Convert directly
        TakesRefVoid(1) 'Requires variable before
        TakesRefVoid(Prop2) ' Requires variable before, and to assign back after
                
        Dim a = TakesRef(someInt) ' Convert directly
        Dim b = TakesRef(2) 'Requires variable before
        Dim c = TakesRef(Prop) ' Requires variable before, and to assign back after

        If 16 > someInt OrElse TakesRef(someInt) ' Convert directly
            Console.WriteLine(1)    
        Else If someBool AndAlso TakesRef(3 * a) 'Requires variable before (in local function)
            someInt += 1
        Else If TakesRef(Prop) ' Requires variable before, and to assign back after (in local function)
            someInt -=2
        End If
        Console.WriteLine(someInt)
    End Sub
End Class