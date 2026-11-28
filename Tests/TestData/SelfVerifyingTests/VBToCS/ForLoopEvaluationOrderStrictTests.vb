Option Strict On
Imports System
Imports Xunit

Namespace ForOrderOn
Public Enum Col
  A = 0
  B = 1
  C = 2
  D = 3
  E = 4
End Enum
Public Class P
  Shared log As String = ""
  Shared Function L(tag As String, v As Integer) As Integer
    log &= tag
    Return v
  End Function
  Shared Function GC(tag As String, v As Col) As Col
    log &= tag
    Return v
  End Function
  Public Shared Function EnumStepTo() As String
    log = ""
    Dim s = ""
    For e As Col = Col.A To GC("T", Col.E) Step CType(L("S", 2), Col)
      s &= e.ToString()
    Next
    Return s & ":" & log
  End Function
  Public Shared Function EnumStepEnumFn() As String
    log = ""
    Dim s = ""
    For e As Col = Col.A To Col.E Step GC("S", Col.C)
      s &= e.ToString()
    Next
    Return s & ":" & log
  End Function
  Public Shared Function StepRefsVar() As String
    Dim s = ""
    Dim i As Integer = 2
    For i = 1 To 9 Step i
      s &= i
    Next
    Return s
  End Function
  Public Shared Function StepRefsVarExpr() As String
    Dim s = ""
    Dim i As Integer = 2
    For i = 1 To 9 Step i + L("", 0)
      s &= i
    Next
    Return s
  End Function
  Public Shared Function ToRefsVar() As String
    Dim s = ""
    Dim i As Integer = 5
    For i = 1 To i + L("", 0)
      s &= i
    Next
    Return s
  End Function
  Public Shared Function LongStep() As String
    log = ""
    Dim s = ""
    For i As Long = 1 To 5 Step L("S", 2)
      s &= i
    Next
    Return s & ":" & log
  End Function
  Public Shared Function SingleStep() As String
    log = ""
    Dim s = ""
    For f As Single = 1 To 2 Step CSng(L("S", 1) / 2)
      s &= f & ";"
    Next
    Return s & ":" & log
  End Function
  Public Shared Function NegUnary() As String
    log = ""
    Dim s = ""
    For i As Integer = 5 To 1 Step -L("S", 2)
      s &= i
    Next
    Return s & ":" & log
  End Function
  Public Shared Function TernaryStep() As String
    log = ""
    Dim s = ""
    Dim up = False
    For i As Integer = 5 To 1 Step If(up, 1, L("S", -1))
      s &= i
    Next
    Return s & ":" & log
  End Function
  Public Shared Function StepNestedOuterVar() As String
    log = ""
    Dim s = ""
    For i As Integer = 1 To 3 Step L("a", 1)
      For j As Integer = i To 3 Step L("b", i)
        s &= j
      Next
      s &= ","
    Next
    Return s & ":" & log
  End Function
End Class
End Namespace

Public Class ForLoopEvaluationOrderStrictTests
    <Fact>
    Public Sub EnumStepTo()
        Assert.Equal("ACE:TS", ForOrderOn.P.EnumStepTo())
    End Sub

    <Fact>
    Public Sub EnumStepEnumFn()
        Assert.Equal("ACE:S", ForOrderOn.P.EnumStepEnumFn())
    End Sub

    <Fact>
    Public Sub StepRefsVar()
        Assert.Equal("13579", ForOrderOn.P.StepRefsVar())
    End Sub

    <Fact>
    Public Sub StepRefsVarExpr()
        Assert.Equal("13579", ForOrderOn.P.StepRefsVarExpr())
    End Sub

    <Fact>
    Public Sub ToRefsVar()
        Assert.Equal("12345", ForOrderOn.P.ToRefsVar())
    End Sub

    <Fact>
    Public Sub LongStep()
        Assert.Equal("135:S", ForOrderOn.P.LongStep())
    End Sub

    <Fact>
    Public Sub SingleStep()
        Assert.Equal("1;1.5;2;:S", ForOrderOn.P.SingleStep())
    End Sub

    <Fact>
    Public Sub NegUnary()
        Assert.Equal("531:S", ForOrderOn.P.NegUnary())
    End Sub

    <Fact>
    Public Sub TernaryStep()
        Assert.Equal("54321:S", ForOrderOn.P.TernaryStep())
    End Sub

    <Fact>
    Public Sub StepNestedOuterVar()
        Assert.Equal("123,2,3,:abbb", ForOrderOn.P.StepNestedOuterVar())
    End Sub
End Class
