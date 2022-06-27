Imports System
Imports System.Linq
Imports Xunit

Public Class TestCase1
    Public Sub SubA(type As Type, Optional code As String = "code", Optional argInt as Integer = 1)
    End Sub
    Public Sub SubA(type As Object , Optional code As String = "code", Optional argInt as Integer = 1)
        Throw New InvalidOperationException("This overload shouldn't be called")
    End Sub
    Public Sub Run()
        SubA(Nothing, , argInt:= 1)
    End Sub
End Class

Public Class TestCase2
    Public Sub SubA(type As Type, Optional code As String = "code", Optional argInt as Integer = 1)
        Throw New InvalidOperationException("This overload shouldn't be called")
    End Sub
    Public Sub SubA(type As Object , Optional code As String = "code", Optional argInt2 as Integer = 1)
    End Sub
    Public Sub Run()
        SubA(Nothing, , argInt2:= 1)
    End Sub
End Class

Public Class TestCase3
    Public Function SubA(Optional arg1 As String = "arg1", Optional arg2 As String = "arg2") As String
        Return arg1 & " " & arg2
    End Function
    Public Function SubA(arg1 As String) As String
        Throw New InvalidOperationException("This overload shouldn't be called")
    End Function
    Public Sub Run()
        Assert.Equal("test arg2", SubA("test",))
        Assert.Equal("arg1 test", SubA(,"test"))
    End Sub
End Class

Public Class TestCase4
    Public Function SubA(Optional arg1 As String = "arg1", Optional arg2 As String = "arg2") As String
        Return arg1 & " " & arg2
    End Function
    Public Function SubA(arg1 As String) As String
        Return arg1
    End Function
    Public Function SubA(arg2 As String, Optional arg1 As Integer = 3) As String
        Return arg1.ToString() & " " & arg2
    End Function
    Public Sub Run()
        Assert.Equal("arg1 test", SubA(,"test"))
        Assert.Equal("arg1 test", SubA(, arg2:="test"))

        Assert.Equal("test", SubA(arg1:= "test"))
        Assert.Equal("test 5", SubA(arg1:= "test", "5"))
        Assert.Equal("test arg2", SubA(arg1:= "test", ))
        Assert.Equal("5 test", SubA(arg1:="5", arg2:= "test"))
        Assert.Equal("5 test", SubA(arg1:=5, arg2:= "test"))
        Assert.Equal("6 test", SubA(arg2:= "test", "6"))
        Assert.Equal("test 6", SubA(arg1:= "test", "6"))
        Assert.Equal("3 test", SubA(arg2:= "test", ))
        Assert.Equal("5 arg2", SubA(arg1:= 5, ))
        Assert.Equal("3 5", SubA(arg2:= 5, ))
        Assert.Equal("arg1 5", SubA(,arg2:= 5))
    End Sub
End Class

Public Class TestCase5
    Public Function SubA(Optional ByRef arg1 As String = "arg1", Optional arg2 As String = "arg2") As String
        arg1 &= "changed"
        Return arg1 & " " & arg2
    End Function
    Public Function SubA(arg1 As String) As String
        Return arg1
    End Function
    Public Sub Run()
        Assert.Equal("arg1changed test", SubA(, "test"))
        Assert.Equal("testchanged arg2", SubA("test", ))
        Assert.Equal("test", SubA(arg1:="test"))
        Assert.Equal("test1changed test2", SubA(arg2:="test2", arg1:="test1"))
        Assert.Equal("test2changed test1", SubA(arg1:="test2", arg2:="test1"))
        Assert.Equal("testchanged arg2", SubA(arg1:="test",))
        Assert.Equal("arg1changed test", SubA(arg2:="test"))
        Assert.Equal("arg1changed test", SubA(,arg2:="test"))

        Dim arg = "test"
        Assert.Equal("test", SubA(arg))
        Assert.Equal("testchanged arg2", SubA(arg,))
        Assert.Equal("arg1changed testchanged", SubA(,arg))
    End Sub
End Class

Public Class TestCase6
    Public Function M() As String
        Return ""
    End Function
    Public Function M(a As String, b as string) As String
        Return $"{a} {b}"
    End Function
    Public Function M(Optional a As String = "1", Optional b as string = "2", optional c as string = "3") As String
        Return $"{a} {b} {c}"
    End Function
    Public Sub Run()
        Assert.Equal("11 2 3", M(a:="11", ))
        Assert.Equal("11 22", M(a:="11", "22"))
        Assert.Equal("11 22 3", M(a:="11", "22", ))
        Assert.Equal("1 2 3", M(,))
        Assert.Equal("1 22 3", M(,b:="22"))
        Assert.Equal("1 22 3", M(,b:="22",))
        Assert.Equal("1 2 33", M(,c:="33"))
        Assert.Equal("1 2 33", M(,,c:="33"))
        Assert.Equal("1 2 33", M(,,c:="33"))
        Assert.Equal("11 22", M(a:="11",b:="22"))
        Assert.Equal("11 22 3", M(a:="11",b:="22",))
    End Sub
End Class

Public Class TestCase7
    Public Function M() As String
        Return ""
    End Function
    Public Function M(Optional a As String = "3", optional b as string = "4") As String
        Return $"{a} {b}"
    End Function
    Public Function M(Optional a As String = "1", Optional b as string = "2", optional c as string = "3") As String
        Return $"{a} {b} {c}"
    End Function
    Public Sub Run()
        Assert.Equal("11 2 3", M(a:="11",,))
        Assert.Equal("11 2 3", M("11",,))
    End Sub
End Class

Public Class OmittedArgumentsTests

    <Fact>
    Public Sub Test1()
        Dim test1 As New TestCase1()
        test1.Run()
    End Sub

    <Fact>
    Public Sub Test2()
        Dim test2 As New TestCase2()
        test2.Run()
    End Sub

    <Fact>
    Public Sub Test3()
        Dim test3 As New TestCase3()
        test3.Run()
    End Sub

    <Fact>
    Public Sub Test4()
        Dim test4 As New TestCase4()
        test4.Run()
    End Sub

    <Fact>
    Public Sub Test5()
        Dim test5 As New TestCase5()
        test5.Run()
    End Sub

    <Fact>
    Public Sub Test6()
        Dim test6 As New TestCase6()
        test6.Run()
    End Sub

End Class
