Option Strict On
Imports System
Imports System.Collections.Generic
Imports Xunit

Namespace ForFromOn
Public Enum Col
 A = 0
 B = 1
 C = 2
 D = 3
 E = 4
 F = 5
 G = 6
End Enum
Public Class P
 Shared log As String = ""
 Shared fld As Integer = 1
 Shared arr As Integer() = {1, 2}
 Shared Function L(tag As String, v As Integer) As Integer
  log &= tag
  Return v
 End Function
 Shared Function Inc(ByRef x As Integer, r As Integer) As Integer
  x = 4
  Return r
 End Function
 Shared Function SetArr(r As Integer) As Integer
  arr(0) = 4
  Return r
 End Function
 Shared ReadOnly Property Start As Integer
  Get
   log &= "P"
   Return 1
  End Get
 End Property
 Public Shared Function ToRefsVarNoStep() As String
  log = ""
  Dim s = ""
  Dim i As Integer = 3
  For i = L("F", 1) To i + 2
   s &= CStr(i) & ","
  Next
  Return s & ":" & log
 End Function
 Public Shared Function ToRefsVarPropFrom() As String
  log = ""
  Dim s = ""
  Dim i As Integer = 3
  For i = Start To i + 2
   s &= CStr(i) & ","
  Next
  Return s & ":" & log
 End Function
 Public Shared Function ToRefsVarConstFrom() As String
  Dim s = ""
  Dim i As Integer = 3
  For i = 1 To i + 2
   s &= CStr(i) & ","
  Next
  Return s
 End Function
 Public Shared Function ByRefLocalTo() As String
  Dim s = ""
  Dim x As Integer = 1
  Dim i As Integer
  For i = x To Inc(x, 5)
   s &= CStr(i) & ","
  Next
  Return s
 End Function
 Public Shared Function ByRefLocalToIn() As String
  Dim s = ""
  Dim x As Integer = 1
  For i As Integer = x To Inc(x, 5)
   s &= CStr(i) & ","
  Next
  Return s
 End Function
 Public Shared Function ByRefLocalToInEnum() As String
  Dim s = ""
  Dim x As Integer = 1
  Dim st As Col = Col.B
  For i As Col = CType(x, Col) To CType(Inc(x, 5), Col) Step st
   s &= CStr(i) & ","
  Next
  Return s
 End Function
 Public Shared Function ArrFromToSets() As String
  Dim s = ""
  arr(0) = 1
  For i As Integer = arr(0) To SetArr(5)
   s &= CStr(i) & ","
  Next
  Return s
 End Function
 Public Shared Function FieldFromToSetsIn() As String
  Dim s = ""
  fld = 1
  For i As Integer = fld To Inc(fld, 5)
   s &= CStr(i) & ","
  Next
  Return s
 End Function
 Public Shared Function FieldFromStepSetsIn() As String
  Dim s = ""
  fld = 1
  For i As Integer = fld To 5 Step Inc(fld, 1)
   s &= CStr(i) & ","
  Next
  Return s
 End Function
 Public Shared Function LocalStepSimple() As String
  Dim s = ""
  Dim st As Integer = 2
  For i As Integer = 0 To 10 Step st
   s &= CStr(i) & ","
  Next
  Return s
 End Function
 Public Shared Function OutsideConst() As String
  Dim s = ""
  Dim i As Integer
  For i = 0 To 10
   s &= CStr(i) & ","
  Next
  Return s & i
 End Function
 Public Shared Function OutsideLocalTo() As String
  Dim s = ""
  Dim n As Integer = 4
  Dim i As Integer
  For i = 0 To n
   s &= CStr(i) & ","
  Next
  Return s & i
 End Function
 Public Shared Function OutsideLocalStep() As String
  Dim s = ""
  Dim n As Integer = 2
  Dim i As Integer
  For i = 0 To 10 Step n
   s &= CStr(i) & ","
  Next
  Return s & i
 End Function
 Public Shared Function EnumLocalStep() As String
  Dim s = ""
  Dim n As Col = Col.C
  For i As Col = Col.A To Col.G Step n
   s &= CStr(i) & ","
  Next
  Return s
 End Function
 Public Shared Function StaticLocalFrom() As String
  Static cnt As Integer = 0
  Dim s = ""
  cnt = 1
  For i As Col = CType(cnt, Col) To CType(L("T", 5), Col) Step CType(L("S", 1), Col)
   s &= CStr(i) & ","
  Next
  Return s
 End Function
 Public Shared Function LambdaCapturedFrom() As String
  Dim s = ""
  Dim x As Integer = 1
  Dim setX As Func(Of Integer) = Function()
                                   x = 4
                                   Return 5
                                 End Function
  For i As Col = CType(x, Col) To CType(setX(), Col) Step Col.B
   s &= CStr(i) & ","
  Next
  Dim j As Integer
  For j = x To setX()
   s &= CStr(j) & ";"
  Next
  Return s
 End Function
End Class
End Namespace

Public Class ForLoopFromEvaluationTests
    <Fact>
    Public Sub ToRefsVarNoStep()
        Assert.Equal("1,2,3,4,5,:F", ForFromOn.P.ToRefsVarNoStep())
    End Sub

    <Fact>
    Public Sub ToRefsVarPropFrom()
        Assert.Equal("1,2,3,4,5,:P", ForFromOn.P.ToRefsVarPropFrom())
    End Sub

    <Fact>
    Public Sub ToRefsVarConstFrom()
        Assert.Equal("1,2,3,4,5,", ForFromOn.P.ToRefsVarConstFrom())
    End Sub

    <Fact>
    Public Sub ByRefLocalTo()
        Assert.Equal("1,2,3,4,5,", ForFromOn.P.ByRefLocalTo())
    End Sub

    <Fact>
    Public Sub ByRefLocalToIn()
        Assert.Equal("1,2,3,4,5,", ForFromOn.P.ByRefLocalToIn())
    End Sub

    <Fact>
    Public Sub ByRefLocalToInEnum()
        Assert.Equal("1,2,3,4,5,", ForFromOn.P.ByRefLocalToInEnum())
    End Sub

    <Fact>
    Public Sub ArrFromToSets()
        Assert.Equal("1,2,3,4,5,", ForFromOn.P.ArrFromToSets())
    End Sub

    <Fact>
    Public Sub FieldFromToSetsIn()
        Assert.Equal("1,2,3,4,5,", ForFromOn.P.FieldFromToSetsIn())
    End Sub

    <Fact>
    Public Sub FieldFromStepSetsIn()
        Assert.Equal("1,2,3,4,5,", ForFromOn.P.FieldFromStepSetsIn())
    End Sub

    <Fact>
    Public Sub LocalStepSimple()
        Assert.Equal("0,2,4,6,8,10,", ForFromOn.P.LocalStepSimple())
    End Sub

    <Fact>
    Public Sub OutsideConst()
        Assert.Equal("0,1,2,3,4,5,6,7,8,9,10,11", ForFromOn.P.OutsideConst())
    End Sub

    <Fact>
    Public Sub OutsideLocalTo()
        Assert.Equal("0,1,2,3,4,5", ForFromOn.P.OutsideLocalTo())
    End Sub

    <Fact>
    Public Sub OutsideLocalStep()
        Assert.Equal("0,2,4,6,8,10,12", ForFromOn.P.OutsideLocalStep())
    End Sub

    <Fact>
    Public Sub EnumLocalStep()
        Assert.Equal("0,2,4,6,", ForFromOn.P.EnumLocalStep())
    End Sub

    <Fact>
    Public Sub StaticLocalFrom()
        Assert.Equal("1,2,3,4,5,", ForFromOn.P.StaticLocalFrom())
    End Sub

    <Fact>
    Public Sub LambdaCapturedFrom()
        Assert.Equal("1,2,3,4,5,4;5;", ForFromOn.P.LambdaCapturedFrom())
    End Sub
End Class
