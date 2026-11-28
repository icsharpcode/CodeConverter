Option Strict Off
Imports System
Imports System.Linq
Imports System.Threading.Tasks
Imports Xunit

Namespace ForOrderOff
Public Enum Col
  A = 0
  B = 1
  C = 2
  D = 3
  E = 4
End Enum
Public Class P
  Shared log As String = ""
  Shared stepField As Integer = 1
  Shared Function L(tag As String, v As Integer) As Integer
    log &= tag
    Return v
  End Function
  Shared Function LD(tag As String, v As Double) As Double
    log &= tag
    Return v
  End Function
  Shared Function LL(tag As String, v As Long) As Long
    log &= tag
    Return v
  End Function
  Shared Function LO(tag As String, v As Object) As Object
    log &= tag
    Return v
  End Function
  Shared Function LN(tag As String, v As Integer?) As Integer?
    log &= tag
    Return v
  End Function
  Shared Function SetStep(v As Integer) As Integer
    stepField = v
    Return 0
  End Function
  Public Shared Function OrderDecl() As String
    log = ""
    Dim s = ""
    For i As Integer = L("F", 1) To L("T", 5) Step L("S", 2)
      s &= i
    Next
    Return s & ":" & log
  End Function
  Public Shared Function OrderExisting() As String
    log = ""
    Dim s = ""
    Dim i As Integer
    For i = L("F", 1) To L("T", 5) Step L("S", 2)
      s &= i
    Next
    Return s & ":" & log & i
  End Function
  Public Shared Function OrderLongStep() As String
    log = ""
    Dim s = ""
    For i As Integer = L("F", 1) To L("T", 5) Step LL("S", 2)
      s &= i
    Next
    Return s & ":" & log
  End Function
  Public Shared Function OrderDoubleStepIntVar() As String
    log = ""
    Dim s = ""
    For i As Integer = L("F", 1) To L("T", 5) Step LD("S", 2.0)
      s &= i
    Next
    Return s & ":" & log
  End Function
  Public Shared Function OrderDoubleVar() As String
    log = ""
    Dim s = ""
    For d As Double = L("F", 1) To L("T", 3) Step LD("S", 0.5)
      s &= d & ";"
    Next
    Return s & ":" & log
  End Function
  Public Shared Function FieldStepSetByFrom() As String
    stepField = 1
    Dim s = ""
    For i As Integer = SetStep(3) To 9 Step stepField
      s &= i
    Next
    Return s
  End Function
  Public Shared Function FieldStepSetByFromExisting() As String
    stepField = 1
    Dim s = ""
    Dim i As Integer
    For i = SetStep(3) To 9 Step stepField
      s &= i
    Next
    Return s
  End Function
  Shared Function GetStepField() As Integer
    Return stepField
  End Function
  Public Shared Function FuncStepSetByFrom() As String
    stepField = 1
    Dim s = ""
    For i As Integer = SetStep(3) To 9 Step GetStepField()
      s &= i
    Next
    Return s
  End Function
  Public Shared Function FuncStepSetByFromExisting() As String
    stepField = 1
    Dim s = ""
    Dim i As Integer
    For i = SetStep(3) To 9 Step GetStepField()
      s &= i
    Next
    Return s
  End Function
  Public Shared Function FuncStepSetByTo() As String
    stepField = 1
    Dim s = ""
    For i As Integer = 0 To SetStep(3) + 9 Step GetStepField()
      s &= i
    Next
    Return s
  End Function
  Public Shared Function NegStep() As String
    log = ""
    Dim s = ""
    For i As Integer = 5 To 1 Step L("S", -2)
      s &= i
    Next
    Return s & ":" & log
  End Function
  Public Shared Function EnumStep() As String
    log = ""
    Dim s = ""
    For e As Col = Col.A To Col.E Step L("S", 2)
      s &= e.ToString()
    Next
    Return s & ":" & log
  End Function
  Public Shared Function EnumStepExisting() As String
    log = ""
    Dim s = ""
    Dim e As Col
    For e = Col.A To Col.E Step L("S", 2)
      s &= e.ToString()
    Next
    Return s & ":" & log
  End Function
  Public Shared Function NullableStep() As String
    log = ""
    Dim s = ""
    For i As Integer? = 1 To 6 Step LN("S", 2)
      s &= i
    Next
    Return s & ":" & log
  End Function
  Public Shared Function NullableStepIntVar() As String
    log = ""
    Dim s = ""
    For i As Integer = 1 To 6 Step LN("S", 2)
      s &= i
    Next
    Return s & ":" & log
  End Function
  Public Shared Function ObjectVar() As String
    log = ""
    Dim s = ""
    For o As Object = 1 To 6 Step LO("S", 2)
      s &= o
    Next
    Return s & ":" & log
  End Function
  Public Shared Function ObjStepIntVar() As String
    log = ""
    Dim s = ""
    For i As Integer = L("F", 1) To L("T", 6) Step LO("S", 2)
      s &= i
    Next
    Return s & ":" & log
  End Function
  Public Shared Function ExitFor() As String
    log = ""
    Dim s = ""
    For i As Integer = 1 To 10 Step L("S", 1)
      If i = 3 Then Exit For
      s &= i
    Next
    Return s & ":" & log
  End Function
  Public Shared Function Nested() As String
    log = ""
    Dim s = ""
    For i As Integer = 1 To 2 Step L("a", 1)
      For j As Integer = 1 To 2 Step L("b", 1)
        s &= i & j
      Next
      For j As Integer = 1 To 2 Step L("c", 1)
        s &= j
      Next
    Next
    Return s & ":" & log
  End Function
  Public Shared Function Clash() As String
    log = ""
    Dim s = ""
    Dim loopStep = 7
    For i As Integer = 1 To 3 Step L("a", 1)
      Dim loopStep1 = 8
      s &= i & loopStep & loopStep1
    Next
    Return s & ":" & log
  End Function
  Public Shared Function ClashInner() As String
    log = ""
    Dim s = ""
    For i As Integer = 1 To 3 Step L("a", 1)
      Dim loopStep = 8
      s &= i & loopStep
    Next
    Return s & ":" & log
  End Function
  Public Shared Function ClashAfter() As String
    log = ""
    Dim s = ""
    For i As Integer = 1 To 3 Step L("a", 1)
      s &= i
    Next
    Dim loopStep = 8
    Return s & ":" & log & loopStep
  End Function
  Public Shared Function InLambda() As String
    log = ""
    Dim f = Function()
              Dim s = ""
              For i As Integer = 1 To 3 Step L("a", 1)
                s &= i
              Next
              Return s
            End Function
    Return f() & f() & ":" & log
  End Function
  Shared Async Function AsyncLoop() As Task(Of String)
    Dim s = ""
    For i As Integer = 1 To 3 Step L("a", 1)
      Await Task.Yield()
      s &= i
    Next
    Return s
  End Function
  Public Shared Function InAsync() As String
    log = ""
    Return AsyncLoop().Result & ":" & log
  End Function
  Public Shared Function StepChangedInBody() As String
    Dim s = ""
    Dim st = 1
    For i As Integer = 1 To 10 Step st
      s &= i
      st = 3
    Next
    Return s
  End Function
  Public Shared Function StepFieldChangedInBody() As String
    Dim s = ""
    stepField = 1
    For i As Integer = 1 To 10 Step stepField
      s &= i
      stepField = 3
    Next
    Return s
  End Function
  Public Shared Function ZeroStep() As String
    log = ""
    Dim s = ""
    Dim n = 0
    For i As Integer = 1 To 3 Step L("z", 0)
      s &= i
      n += 1
      If n > 4 Then Exit For
    Next
    Return s & ":" & log
  End Function
  Public Shared Function DecimalStep() As String
    log = ""
    Dim s = ""
    For d As Decimal = 1 To 2 Step CDec(LD("S", 0.25))
      s &= d & ";"
    Next
    Return s & ":" & log
  End Function
  Public Shared Function ByteVar() As String
    log = ""
    Dim s = ""
    For b As Byte = 1 To 9 Step L("S", 3)
      s &= b
    Next
    Return s & ":" & log
  End Function
  Public Shared Function ByteVarNeg() As String
    log = ""
    Dim s = ""
    For b As Short = 9 To 1 Step L("S", -3)
      s &= b
    Next
    Return s & ":" & log
  End Function
  Public Shared Function ArrayStep() As String
    log = ""
    Dim a = {2}
    Dim s = ""
    For i As Integer = 1 To 9 Step a(0)
      s &= i
      a(0) = 4
    Next
    Return s
  End Function
  Public Shared Function PropStep() As String
    Dim s = ""
    Dim l As New System.Collections.Generic.List(Of Integer) From {1, 2}
    For i As Integer = 1 To 9 Step l.Count
      s &= i
      l.Add(1)
    Next
    Return s
  End Function
End Class
End Namespace

Public Class ForLoopEvaluationOrderTests
    <Fact>
    Public Sub OrderDecl()
        Assert.Equal("135:FTS", ForOrderOff.P.OrderDecl())
    End Sub

    <Fact>
    Public Sub OrderExisting()
        Assert.Equal("135:FTS7", ForOrderOff.P.OrderExisting())
    End Sub

    <Fact>
    Public Sub OrderLongStep()
        Assert.Equal("135:FTS", ForOrderOff.P.OrderLongStep())
    End Sub

    <Fact>
    Public Sub OrderDoubleStepIntVar()
        Assert.Equal("135:FTS", ForOrderOff.P.OrderDoubleStepIntVar())
    End Sub

    <Fact>
    Public Sub OrderDoubleVar()
        Assert.Equal("1;1.5;2;2.5;3;:FTS", ForOrderOff.P.OrderDoubleVar())
    End Sub

    <Fact>
    Public Sub FieldStepSetByFrom()
        Assert.Equal("0369", ForOrderOff.P.FieldStepSetByFrom())
    End Sub

    <Fact>
    Public Sub FieldStepSetByFromExisting()
        Assert.Equal("0369", ForOrderOff.P.FieldStepSetByFromExisting())
    End Sub

    <Fact>
    Public Sub FuncStepSetByFrom()
        Assert.Equal("0369", ForOrderOff.P.FuncStepSetByFrom())
    End Sub

    <Fact>
    Public Sub FuncStepSetByFromExisting()
        Assert.Equal("0369", ForOrderOff.P.FuncStepSetByFromExisting())
    End Sub

    <Fact>
    Public Sub FuncStepSetByTo()
        Assert.Equal("0369", ForOrderOff.P.FuncStepSetByTo())
    End Sub

    <Fact>
    Public Sub NegStep()
        Assert.Equal("531:S", ForOrderOff.P.NegStep())
    End Sub

    <Fact>
    Public Sub EnumStep()
        Assert.Equal("ACE:S", ForOrderOff.P.EnumStep())
    End Sub

    <Fact>
    Public Sub EnumStepExisting()
        Assert.Equal("ACE:S", ForOrderOff.P.EnumStepExisting())
    End Sub

    <Fact>
    Public Sub NullableStep()
        Assert.Equal("135:S", ForOrderOff.P.NullableStep())
    End Sub

    <Fact>
    Public Sub NullableStepIntVar()
        Assert.Equal("135:S", ForOrderOff.P.NullableStepIntVar())
    End Sub

    <Fact>
    Public Sub ObjectVar()
        Assert.Equal("135:S", ForOrderOff.P.ObjectVar())
    End Sub

    <Fact>
    Public Sub ObjStepIntVar()
        Assert.Equal("135:FTS", ForOrderOff.P.ObjStepIntVar())
    End Sub

    <Fact>
    Public Sub ExitFor()
        Assert.Equal("12:S", ForOrderOff.P.ExitFor())
    End Sub

    <Fact>
    Public Sub Nested()
        Assert.Equal("111212212212:abcbc", ForOrderOff.P.Nested())
    End Sub

    <Fact>
    Public Sub Clash()
        Assert.Equal("178278378:a", ForOrderOff.P.Clash())
    End Sub

    <Fact>
    Public Sub ClashInner()
        Assert.Equal("182838:a", ForOrderOff.P.ClashInner())
    End Sub

    <Fact>
    Public Sub ClashAfter()
        Assert.Equal("123:a8", ForOrderOff.P.ClashAfter())
    End Sub

    <Fact>
    Public Sub InLambda()
        Assert.Equal("123123:aa", ForOrderOff.P.InLambda())
    End Sub

    <Fact>
    Public Sub InAsync()
        Assert.Equal("123:a", ForOrderOff.P.InAsync())
    End Sub

    <Fact>
    Public Sub StepChangedInBody()
        Assert.Equal("12345678910", ForOrderOff.P.StepChangedInBody())
    End Sub

    <Fact>
    Public Sub StepFieldChangedInBody()
        Assert.Equal("12345678910", ForOrderOff.P.StepFieldChangedInBody())
    End Sub

    <Fact>
    Public Sub ZeroStep()
        Assert.Equal("11111:z", ForOrderOff.P.ZeroStep())
    End Sub

    <Fact>
    Public Sub DecimalStep()
        Assert.Equal("1;1.25;1.50;1.75;2.00;:S", ForOrderOff.P.DecimalStep())
    End Sub

    <Fact>
    Public Sub ByteVar()
        Assert.Equal("147:S", ForOrderOff.P.ByteVar())
    End Sub

    <Fact>
    Public Sub ByteVarNeg()
        Assert.Equal("963:S", ForOrderOff.P.ByteVarNeg())
    End Sub

    <Fact>
    Public Sub ArrayStep()
        Assert.Equal("13579", ForOrderOff.P.ArrayStep())
    End Sub

    <Fact>
    Public Sub PropStep()
        Assert.Equal("13579", ForOrderOff.P.PropStep())
    End Sub
End Class
