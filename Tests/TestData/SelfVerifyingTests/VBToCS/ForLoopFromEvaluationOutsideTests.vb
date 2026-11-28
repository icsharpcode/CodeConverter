Option Strict On
Imports System
Imports Xunit

Namespace ForFromOut
Public Class P
 Shared fld As Integer = 1
 Shared Function Inc(ByRef x As Integer, r As Integer) As Integer
  x = 4
  Return r
 End Function
 Shared Function SetFld(r As Integer) As Integer
  fld = 4
  Return r
 End Function
 Public Shared Function LambdaOut() As String
  Dim s = ""
  Dim x As Integer = 1
  Dim setX As Func(Of Integer) = Function()
                                   x = 4
                                   Return 5
                                 End Function
  Dim j As Integer
  For j = x To setX()
   s &= CStr(j) & ";"
  Next
  Return s
 End Function
 Public Shared Function LambdaIn() As String
  Dim s = ""
  Dim x As Integer = 1
  Dim setX As Func(Of Integer) = Function()
                                   x = 4
                                   Return 5
                                 End Function
  For j As Integer = x To setX()
   s &= CStr(j) & ";"
  Next
  Return s
 End Function
 Public Shared Function ByRefOut() As String
  Dim s = ""
  Dim x As Integer = 1
  Dim j As Integer
  For j = x To Inc(x, 5)
   s &= CStr(j) & ";"
  Next
  Return s
 End Function
 Public Shared Function ByRefIn() As String
  Dim s = ""
  Dim x As Integer = 1
  For j As Integer = x To Inc(x, 5)
   s &= CStr(j) & ";"
  Next
  Return s
 End Function
 Public Shared Function FldIn() As String
  Dim s = ""
  fld = 1
  For j As Integer = fld To SetFld(5)
   s &= CStr(j) & ";"
  Next
  Return s
 End Function
 Public Shared Function FldOut() As String
  Dim s = ""
  fld = 1
  Dim j As Integer
  For j = fld To SetFld(5)
   s &= CStr(j) & ";"
  Next
  Return s
 End Function
End Class
End Namespace

Public Class ForLoopFromEvaluationOutsideTests
    <Fact>
    Public Sub LambdaOut()
        Assert.Equal("1;2;3;4;5;", ForFromOut.P.LambdaOut())
    End Sub

    <Fact>
    Public Sub LambdaIn()
        Assert.Equal("1;2;3;4;5;", ForFromOut.P.LambdaIn())
    End Sub

    <Fact>
    Public Sub ByRefOut()
        Assert.Equal("1;2;3;4;5;", ForFromOut.P.ByRefOut())
    End Sub

    <Fact>
    Public Sub ByRefIn()
        Assert.Equal("1;2;3;4;5;", ForFromOut.P.ByRefIn())
    End Sub

    <Fact>
    Public Sub FldIn()
        Assert.Equal("1;2;3;4;5;", ForFromOut.P.FldIn())
    End Sub

    <Fact>
    Public Sub FldOut()
        Assert.Equal("1;2;3;4;5;", ForFromOut.P.FldOut())
    End Sub

    Private Shared Function One() As Integer
        Return 1
    End Function

    <Fact>
    Public Sub ToAndStepReadTheControlVariableBeforeItIsAssigned()
        Dim s = ""
        Dim i = 3
        For i = One() To i + 4 Step i
            s &= i & ","
        Next
        Assert.Equal("1,4,7,", s)
    End Sub
End Class
