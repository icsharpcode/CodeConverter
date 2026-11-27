Option Strict Off
Imports System
Imports Xunit

Public Class NothingConstantConversionTests
    Private Const StrC As String = Nothing
    Private Const ObjC As Object = Nothing

    Private Shared Function Outcome(f As Func(Of Object)) As String
        Try
            Return f().ToString()
        Catch ex As Exception
            Return ex.GetType().Name
        End Try
    End Function

    <Fact>
    Public Sub StringNothingConvertsAsAString()
        Assert.Equal("InvalidCastException,InvalidCastException,InvalidCastException,0", String.Join(",",
            Outcome(Function()
                        Dim b As Boolean = StrC
                        Return b
                    End Function),
            Outcome(Function()
                        Dim d As Date = CStr(Nothing)
                        Return d
                    End Function),
            Outcome(Function()
                        Dim b As Boolean = DirectCast(Nothing, String)
                        Return b
                    End Function),
            Outcome(Function()
                        Dim i As Integer = StrC
                        Return i
                    End Function)))
    End Sub

    <Fact>
    Public Sub ObjectNothingConvertsToTheDefault()
        Dim i As Integer = ObjC
        Dim b As Boolean = ObjC
        Dim d As Date = DirectCast(Nothing, Object)
        Assert.Equal("0,False,True", String.Join(",", i, b, d = Nothing))
    End Sub
End Class
