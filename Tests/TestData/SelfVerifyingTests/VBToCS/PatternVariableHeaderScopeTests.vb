Option Strict Off
Imports System
Imports Xunit

Namespace HeaderScope
Public Module P
  Private cnt As Integer
  Function NI(v As Integer?) As Integer?
    cnt += 1
    Return v
  End Function
  Function NB(v As Boolean?) As Boolean?
    Return v
  End Function
  Function S(o As Object) As String
    If o Is Nothing Then Return "N"
    Return o.ToString()
  End Function
  Public Function IfCond() As String
    Dim s2 = ""
    If NI(2) ^ NI(3) > 4 Then
      Dim arg1 = 5
      s2 &= arg1
    End If
    Return s2
  End Function
  Public Function DoCond() As String
    Dim i = 0
    Do While NI(i) ^ NI(1) < 3
      Dim arg1 = i
      i = arg1 + 1
    Loop
    Return i.ToString()
  End Function
  Public Function SelExpr() As String
    Dim s2 = ""
    For k = 1 To 3
      Select Case NI(k) ^ NI(2)
        Case 1
          Dim arg1 = 9
          s2 &= arg1
        Case Else
          Dim arg2 = 8
          s2 &= arg2
      End Select
    Next
    Return s2
  End Function
  Public Function ElseIfCond() As String
    Dim s2 = ""
    Dim z = 0
    If z = 1 Then
      s2 = "a"
    ElseIf NI(2) ^ NI(3) > 4 Then
      Dim arg1 = 5
      s2 &= arg1
    End If
    Return s2
  End Function
  Public Function CaseIs() As String
    Dim s2 = ""
    For k = 1 To 3
      Select Case k
        Case Is < NI(2) ^ NI(1)
          Dim arg1 = 9
          s2 &= arg1
        Case Else
          Dim arg2 = 8
          s2 &= arg2
      End Select
    Next
    Return s2
  End Function
  Public Function ForEachColl() As String
    Dim s2 = ""
    For Each x In {NI(2) ^ NI(3)}
      Dim arg1 = x
      s2 &= S(arg1)
    Next
    Return s2
  End Function
  Public Function NotEqIfBody() As String
    Dim s2 = ""
    If NI(1) <> NI(2) Then
      Dim arg1 = 1
      s2 = arg1.ToString()
    End If
    Return s2
  End Function
  Public Function AndAlsoIfBody() As String
    Dim s2 = ""
    If NB(True) AndAlso NB(True) Then
      Dim arg1 = 1
      s2 = arg1.ToString()
    End If
    Return s2
  End Function
  Public Function LambdaInIfCond() As String
    Dim f As Func(Of Integer?, Boolean)
    f = Function(x)
          If NI(x) ^ NI(2) > 1 Then
            Dim arg1 = 1
            Return arg1 = 1
          End If
          Return False
        End Function
    Return f(3).ToString()
  End Function
End Module
End Namespace

Public Class PatternVariableHeaderScopeTests
    <Fact>
    Public Sub IfCond()
        Assert.Equal("5", HeaderScope.P.IfCond())
    End Sub

    <Fact>
    Public Sub DoCond()
        Assert.Equal("3", HeaderScope.P.DoCond())
    End Sub

    <Fact>
    Public Sub SelExpr()
        Assert.Equal("988", HeaderScope.P.SelExpr())
    End Sub

    <Fact>
    Public Sub ElseIfCond()
        Assert.Equal("5", HeaderScope.P.ElseIfCond())
    End Sub

    <Fact>
    Public Sub CaseIs()
        Assert.Equal("988", HeaderScope.P.CaseIs())
    End Sub

    <Fact>
    Public Sub ForEachColl()
        Assert.Equal("8", HeaderScope.P.ForEachColl())
    End Sub

    <Fact>
    Public Sub NotEqIfBody()
        Assert.Equal("1", HeaderScope.P.NotEqIfBody())
    End Sub

    <Fact>
    Public Sub AndAlsoIfBody()
        Assert.Equal("1", HeaderScope.P.AndAlsoIfBody())
    End Sub

    <Fact>
    Public Sub LambdaInIfCond()
        Assert.Equal("True", HeaderScope.P.LambdaInIfCond())
    End Sub
End Class
