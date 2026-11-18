Imports System
Imports System.Collections.Generic
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
Imports Xunit

Public Class ByRefWriteBackPlacementTests
    <Fact>
    Public Sub ReturnWritesBackBeforeReturningIssue896()
        Dim testClass As New OutParamTestClass
        Assert.Equal(2, testClass.Number)
        Dim result = CallSubWithOutParam(testClass)
        Assert.True(result)
        Assert.Equal(1, testClass.Number)
    End Sub

    Private Shared Function CallSubWithOutParam(testClass As OutParamTestClass) As Boolean
        Return SubWithOutParam(testClass.Number)
    End Function

    Private Shared Function SubWithOutParam(<Out> ByRef out_a As Integer) As Boolean
        out_a = 1
        Return True
    End Function

    Private Class OutParamTestClass
        Friend Property Number As Integer = 2
    End Class

    <Fact>
    Public Sub ReturnWithConversionWritesBackOnce()
        Dim probe As New Probe
        Assert.Equal(1L, ReturnAsLong(probe))
        Assert.Equal("get,set", probe.LogText)
    End Sub

    Private Shared Function ReturnAsLong(probe As Probe) As Long
        Return Increment(probe.Value)
    End Function

    <Fact>
    Public Sub ReturnFromPropertyGetterWritesBack()
        Dim probe As New Probe
        Assert.Equal(1, probe.IncrementedValue)
        Assert.Equal(1, probe.Stored)
    End Sub

    <Fact>
    Public Sub ReturnInMultiLineLambdaWritesBack()
        Dim probe As New Probe
        Dim f As Func(Of Integer) = Function()
                                        Return Increment(probe.Value)
                                    End Function
        Assert.Equal(1, f())
        Assert.Equal(1, probe.Stored)
    End Sub

    <Fact>
    Public Sub ThrowWritesBackBeforeThrowing()
        Dim probe As New Probe
        Dim ex = Assert.Throws(Of InvalidOperationException)(Sub() ThrowCreated(probe))
        Assert.Equal("1", ex.Message)
        Assert.Equal(1, probe.Stored)
        Assert.Equal("get,set", probe.LogText)
    End Sub

    Private Shared Sub ThrowCreated(probe As Probe)
        Throw CreateException(probe.Value)
    End Sub

    Private Shared Function CreateException(ByRef value As Integer) As Exception
        value += 1
        Return New InvalidOperationException(value.ToString())
    End Function

    <Fact>
    Public Sub YieldWritesBackBeforeTheConsumerResumes()
        Dim probe As New Probe
        For Each value In YieldIncremented(probe)
            Assert.Equal(value, probe.Stored)
        Next
        Assert.Equal(2, probe.Stored)
    End Sub

    Private Shared Iterator Function YieldIncremented(probe As Probe) As IEnumerable(Of Integer)
        Yield Increment(probe.Value)
        Yield Increment(probe.Value)
    End Function

    <Fact>
    Public Sub ReturnWritesBackWhenResultNameIsTaken()
        Dim probe As New Probe
        Assert.Equal(42, ReturnWithLocalNamedRet(probe))
        Assert.Equal(42, probe.Stored)
    End Sub

    Private Shared Function ReturnWithLocalNamedRet(probe As Probe) As Integer
        Dim ret = 41
        probe.Stored = ret
        Return Increment(probe.Value)
    End Function

    <Fact>
    Public Sub ReturnToWiderTypeWritesBack()
        Dim probe As New Probe
        Assert.Equal(CObj(1), ReturnAsObject(probe))
        Assert.Equal(1, probe.Stored)
    End Sub

    Private Shared Function ReturnAsObject(probe As Probe) As Object
        Return Increment(probe.Value)
    End Function

    <Fact>
    Public Sub SelectCaseBodySeesWrittenBackValue()
        Dim probe As New Probe
        Select Case Increment(probe.Value)
            Case 1
                Assert.Equal(1, probe.Stored)
            Case Else
                Assert.Fail("Unexpected case")
        End Select
        Assert.Equal("get,set", probe.LogText)
    End Sub

    <Fact>
    Public Sub CaseClauseWritesBackBeforeItsBody()
        Dim probe As New Probe
        Dim x = 1
        Select Case x
            Case Increment(probe.Value)
                Assert.Equal(1, probe.Stored)
            Case Else
                Assert.Fail("Unexpected case")
        End Select
        Assert.Equal("get,set", probe.LogText)
    End Sub

    <Fact>
    Public Sub ForBoundWritesBackBeforeTheBody()
        Dim probe As New Probe
        Dim iterations = 0
        For i = 0 To Increment(probe.Value)
            iterations += 1
            Assert.Equal(1, probe.Stored)
        Next
        Assert.Equal(2, iterations)
        Assert.Equal("get,set", probe.LogText)
    End Sub

    <Fact>
    Public Sub ForEachCollectionWritesBackBeforeTheBody()
        Dim probe As New Probe
        For Each item In New Integer() {Increment(probe.Value)}
            Assert.Equal(1, probe.Stored)
        Next
        For Each item In Repeat(probe.Value)
            Assert.Equal(2, probe.Stored)
        Next
        Assert.Equal(2, probe.Stored)
    End Sub

    Private Shared Function Repeat(ByRef value As Integer) As Integer()
        value += 1
        Return New Integer() {value}
    End Function

    <Fact>
    Public Sub UsingResourceWritesBackBeforeTheBody()
        Dim probe As New Probe
        Using d = CreateDisposable(probe.Value)
            Assert.Equal(1, probe.Stored)
        End Using
        Assert.Equal("get,set", probe.LogText)
    End Sub

    Private Shared Function CreateDisposable(ByRef value As Integer) As IDisposable
        value += 1
        Return New IO.MemoryStream()
    End Function

    <Fact>
    Public Sub SyncLockWritesBackBeforeTheBody()
        Dim probe As New Probe
        SyncLock CreateLock(probe.Value)
            Assert.Equal(1, probe.Stored)
        End SyncLock
    End Sub

    Private Shared Function CreateLock(ByRef value As Integer) As Object
        value += 1
        Return New Object()
    End Function

    <Fact>
    Public Sub WithWritesBackBeforeTheBody()
        Dim probe As New Probe
        With CreateLock(probe.Value)
            Assert.Equal(1, probe.Stored)
        End With
    End Sub

    <Fact>
    Public Sub ForStartWritesBackBeforeTheBody()
        Dim probe As New Probe
        For i = Increment(probe.Value) To 1
            Assert.Equal(1, probe.Stored)
        Next
        Assert.Equal("get,set", probe.LogText)
    End Sub

    <Fact>
    Public Sub CatchFilterWritesBackBeforeTheHandler()
        Dim probe As New Probe
        Try
            Throw New InvalidOperationException()
        Catch ex As InvalidOperationException When IncrementIsOne(probe.Value)
            Assert.Equal(1, probe.Stored)
        End Try
        Assert.Equal("get,set", probe.LogText)
    End Sub

    Private Shared Function IncrementIsOne(ByRef value As Integer) As Boolean
        Return Increment(value) = 1
    End Function

    <Fact>
    Public Sub LaterCaseClauseIsOnlyEvaluatedWhenReached()
        Dim probe As New Probe
        Select Case 1
            Case 1
                Assert.Equal(0, probe.Stored)
            Case Increment(probe.Value)
                Assert.Fail("Unexpected case")
        End Select
        Assert.Equal("", probe.LogText)
    End Sub

    <Fact>
    Public Sub NestedBlockHeadersWriteBackBeforeTheirBodies()
        Dim probe As New Probe
        For Each item In Repeat(probe.Value)
            Select Case Increment(probe.Value)
                Case 2
                    Assert.Equal(2, probe.Stored)
            End Select
        Next
        Assert.Equal(2, probe.Stored)
    End Sub

    <Fact>
    Public Sub UnchangedStatementShapesStillWriteBack()
        Dim probe As New Probe
        Increment(probe.Value)
        Dim local = Increment(probe.Value)
        local = Increment(probe.Value)
        Assert.Equal(3, local)
        Assert.Equal(3, probe.Stored)
    End Sub

    <Fact>
    Public Sub InstanceFieldInitializerWithOmittedOptionalByRefIssue1081()
        Dim initialized As New FieldInitializers
        Assert.Same(initialized.First, initialized.Second.ReceivedInner)
        Assert.Equal("", initialized.Second.ReceivedX)
    End Sub

    <Fact>
    Public Sub InstanceFieldInitializersWriteBackInDeclarationOrder()
        Dim initialized As New FieldInitializers
        Assert.Equal(1, initialized.A)
        Assert.Equal(22, initialized.C)
        Assert.Equal(2, initialized.Counter)
    End Sub

    <Fact>
    Public Sub EachVariableOfAFieldDeclarationGetsItsOwnTemporary()
        Dim initialized As New FieldInitializers
        Assert.NotSame(initialized.D, initialized.E)
        Assert.Same(initialized.First, initialized.D.ReceivedInner)
        Assert.Same(initialized.First, initialized.E.ReceivedInner)
    End Sub

    <Fact>
    Public Sub SharedFieldInitializerWritesBack()
        Assert.Equal(1, SharedFieldInitializer.B)
        Assert.Equal(1, SharedFieldInitializer.SharedCounter)
    End Sub

    Private Class FieldInitializers
        Public Property Counter As Integer
        Public ReadOnly First As New Inner()
        Public ReadOnly Second As New Outer(First)
        Public ReadOnly A As Integer = Increment(Counter)
        Public ReadOnly C As Integer = Increment(Counter) + Counter * 10
        Public ReadOnly D, E As New Outer(First)
    End Class

    Private Class SharedFieldInitializer
        Public Shared Property SharedCounter As Integer
        Public Shared ReadOnly B As Integer = Increment(SharedCounter)
    End Class

    Private Class Inner
    End Class

    Private Class Outer
        Public ReadOnly ReceivedInner As Inner
        Public ReadOnly ReceivedX As String

        Sub New(inner As Inner, Optional ByRef x As String = "")
            ReceivedInner = inner
            ReceivedX = x
            x = "changed"
        End Sub
    End Class

    <Fact>
    Public Sub PartialClassFieldInitializerWritesBack()
        Dim initialized As New PartialFieldInitializers()
        Assert.Equal(1, initialized.A)
        Assert.Equal(1, initialized.Counter)
    End Sub

    Partial Private Class PartialFieldInitializers
        Public Property Counter As Integer
        Public ReadOnly A As Integer = Increment(Counter)
    End Class

    Partial Private Class PartialFieldInitializers
        Public Sub New()
        End Sub
    End Class

    <Fact>
    Public Sub SeveralDeclaratorsInOneFieldDeclarationEachWriteBack()
        Dim initialized As New SeveralDeclarators
        Assert.Equal(1, initialized.A)
        Assert.Equal(2, initialized.B)
        Assert.Equal(4, initialized.Arr.Length)
        Assert.Equal(3, initialized.Counter)
    End Sub

    Private Class SeveralDeclarators
        Public Property Counter As Integer
        Public ReadOnly A As Integer = Increment(Counter), B As Integer = Increment(Counter)
        Public Arr(Increment(Counter)), Other As Integer
    End Class

    <Fact>
    Public Sub WithEventsFieldInitializerWritesBack()
        Dim initialized As New WithEventsFieldInitializer
        Assert.Equal(1, initialized.Counter)
        Assert.Equal(1, initialized.Source.Received)
    End Sub

    Private Class WithEventsFieldInitializer
        Public Property Counter As Integer
        Public WithEvents Source As New EventSource(Counter)
    End Class

    Private Class EventSource
        Public Event Changed()
        Public ReadOnly Received As Integer

        Sub New(ByRef value As Integer)
            value += 1
            Received = value
        End Sub
    End Class

    <Fact>
    Public Sub CallWritesBackItsArgumentsInReverseOrder()
        Dim probe As New Probe
        Two(probe.Value, probe.Value)
        Assert.Equal(1, probe.Stored)
        Assert.Equal("get,get,set,set", probe.LogText)
    End Sub

    <Fact>
    Public Sub NestedCallWritesBackBeforeTheOuterCallsReversedWriteBacks()
        Dim probe As New Probe
        Three(SetToNine(probe.Value), probe.Value, probe.Value)
        Assert.Equal(1, probe.Stored)
    End Sub

    <Fact>
    Public Sub ReturnWritesBackInReverseOrderBeforeReturning()
        Dim probe As New Probe
        Assert.Equal(3, ReturnTwo(probe))
        Assert.Equal(1, probe.Stored)
    End Sub

    Private Shared Function ReturnTwo(probe As Probe) As Integer
        Return TwoAndSum(probe.Value, probe.Value)
    End Function

    <Fact>
    Public Sub BlockHeaderWritesBackInReverseOrderBeforeTheBody()
        Dim probe As New Probe
        Select Case TwoAndSum(probe.Value, probe.Value)
            Case 3
                Assert.Equal(1, probe.Stored)
            Case Else
                Assert.Fail("Unexpected case")
        End Select
    End Sub

    Private Shared Sub Two(ByRef a As Integer, ByRef b As Integer)
        a = 1
        b = 2
    End Sub

    Private Shared Function TwoAndSum(ByRef a As Integer, ByRef b As Integer) As Integer
        Two(a, b)
        Return a + b
    End Function

    Private Shared Sub Three(x As Integer, ByRef a As Integer, ByRef b As Integer)
        Two(a, b)
    End Sub

    Private Shared Function SetToNine(ByRef value As Integer) As Integer
        value = 9
        Return value
    End Function

    <Fact>
    Public Sub EachDeclaratorOfALocalDeclarationWritesBackBeforeTheNext()
        Dim probe As New Probe
        Dim a = Increment(probe.Value), b = Increment(probe.Value), c = probe.Stored
        Assert.Equal(1, a)
        Assert.Equal(2, b)
        Assert.Equal(2, c)
        Assert.Equal(2, probe.Stored)
        Assert.Equal("get,set,get,set", probe.LogText)
    End Sub

    <Fact>
    Public Sub AssignmentToTheWrittenBackPropertyKeepsTheCallResult()
        Dim h As New Holder
        h.Plain = SetValueReturnOther(h.Plain)
        Assert.Equal("result", h.Plain)
    End Sub

    <Fact>
    Public Sub AssignmentToAnIndexOfTheWriteBackWritesBackAtTheOldIndex()
        Dim h As New Holder
        Dim x = 0
        x = SetValueReturnOne(h.Items(x))
        Assert.Equal(1, x)
        Assert.Equal("changed", h.Items(0))
        Assert.Equal("b", h.Items(1))
        x += SetValueReturnOne(h.Items(x))
        Assert.Equal(2, x)
        Assert.Equal("changed", h.Items(1))
    End Sub

    <Fact>
    Public Sub AssignmentToTheWriteBackReceiverWritesBackToTheOldReceiver()
        Dim h As New Holder
        Dim original = h
        h = SetValueReturnHolder(h.Plain)
        Assert.NotSame(original, h)
        Assert.Equal("changed", original.Plain)
        Assert.Equal("", h.Plain)
        Dim second = h
        h = SetValueReturnHolder(h.Items(1))
        Assert.Equal("changed", second.Items(1))
        Assert.Equal("b", h.Items(1))
    End Sub

    <Fact>
    Public Sub CompoundAssignmentReadsItsTargetBeforeTheCall()
        Dim probe As New Probe
        probe.Value += Increment(probe.Value)
        Assert.Equal(1, probe.Stored)
        Assert.Equal("get,get,set,set", probe.LogText)
    End Sub

    <Fact>
    Public Sub AssignmentToTheWrittenBackExtensionReceiverKeepsTheCallResult()
        Dim h As New Holder
        h.Plain = h.Plain.SetToChangedReturnResult()
        Assert.Equal("result", h.Plain)
        h.Plain = ""
        h.AssignOwnPlain()
        Assert.Equal("result", h.Plain)
    End Sub

    Public Class Holder
        Public Property Plain As String = ""
        Public Property Items As New List(Of String) From {"a", "b"}

        Public Sub AssignOwnPlain()
            Plain = Plain.SetToChangedReturnResult()
        End Sub
    End Class

    Private Shared Function SetValueReturnOther(ByRef value As String) As String
        value = "changed"
        Return "result"
    End Function

    Private Shared Function SetValueReturnOne(ByRef value As String) As Integer
        value = "changed"
        Return 1
    End Function

    Private Shared Function SetValueReturnHolder(ByRef value As String) As Holder
        value = "changed"
        Return New Holder
    End Function

    <Fact>
    Public Sub LocalFunctionResultAvoidsANameInScope()
        Dim probe As New Probe
        Assert.Equal(2, AddOne(probe))
        Assert.Equal(1, probe.Stored)
    End Sub

    Private Shared Function AddOne(ret As Probe) As Integer
        Return Increment(ret.Value) + 1
    End Function

    <Fact>
    Public Sub StructureAssignmentWritesBackBeforeAssigning()
        Dim s As New Counter
        s.Go()
        Assert.Equal(5, s.P)
    End Sub

    Private Structure Counter
        Public Property P As Integer

        Public Sub Go()
            P = SetToTwoReturnFive(P)
        End Sub
    End Structure

    Private Shared Function SetToTwoReturnFive(ByRef value As Integer) As Integer
        value = 2
        Return 5
    End Function

    <Fact>
    Public Sub ParenthesizedCallAssignedToTheWrittenBackPropertyKeepsTheCallResult()
        Dim h As New Holder
        h.Plain = (SetValueReturnOther(h.Plain))
        Assert.Equal("result", h.Plain)
    End Sub

    <Fact>
    Public Sub LastDeclaratorWritesBackAfterTheDeclaration()
        Dim probe As New Probe
        Dim a = Increment(probe.Value), b = Increment(probe.Value)
        Assert.Equal(1, a)
        Assert.Equal(2, b)
        Assert.Equal(2, probe.Stored)
    End Sub

    <Fact>
    Public Sub MidAssignmentToTheWrittenBackProperty()
        Dim h As New Holder With {.Plain = "abcdef"}
        Mid(h.Plain, 1) = SetValueReturnZZ(h.Plain)
        Assert.Equal("ZZcdef", h.Plain)
    End Sub

    Private Shared Function SetValueReturnZZ(ByRef value As String) As String
        value = "changed"
        Return "ZZ"
    End Function

    <Fact>
    Public Sub ExtensionReceiverInLoopConditionWritesBackEachTime()
        Dim h As New Holder
        Dim count = 0
        Do While h.Plain.GrowIsShort()
            count += 1
        Loop
        Assert.Equal(2, count)
        Assert.Equal("xxx", h.Plain)
    End Sub

    <Fact>
    Public Sub StructureIfConditionWritesBackBeforeTheBody()
        Dim s As New LoggedValue(New List(Of String))
        s.IfCondition()
        Assert.Equal("get 0,set 1,then 1", s.LogText)
    End Sub

    <Fact>
    Public Sub StructureElseIfConditionWritesBackBeforeTheBody()
        Dim s As New LoggedValue(New List(Of String))
        s.ElseIfCondition()
        Assert.Equal("mark if,get 0,set 1,elseif 1", s.LogText)
    End Sub

    <Fact>
    Public Sub StructureShortCircuitedOperandOnlyWritesBackWhenCalled()
        Dim s As New LoggedValue(New List(Of String))
        s.ShortCircuitedOperand(False)
        s.ShortCircuitedOperand(True)
        Assert.Equal("mark left,mark left,get 0,set 1,then 1", s.LogText)
    End Sub

    <Fact>
    Public Sub StructureWhileConditionWritesBackEachTime()
        Dim s As New LoggedValue(New List(Of String))
        s.WhileCondition()
        Assert.Equal("get 0,set 1,body 1,get 1,set 2,body 2,get 2,set 3", s.LogText)
    End Sub

    <Fact>
    Public Sub StructureDoLoopUntilConditionWritesBackEachTime()
        Dim s As New LoggedValue(New List(Of String))
        s.DoLoopUntilCondition()
        Assert.Equal("body 0,get 0,set 1,body 1,get 1,set 2", s.LogText)
    End Sub

    <Fact>
    Public Sub StructureOperandWritesBackBeforeTheNextOperand()
        Dim s As New LoggedValue(New List(Of String))
        Assert.Equal(2, s.OperandFollowedByPeek())
        Assert.Equal("get 0,set 1,peek 1", s.LogText)
    End Sub

    <Fact>
    Public Sub StructureDeclaratorWritesBackBeforeTheNext()
        Dim s As New LoggedValue(New List(Of String))
        Assert.Equal(2, s.DeclaratorFollowedByPeek())
        Assert.Equal("get 0,set 1,peek 1", s.LogText)
    End Sub

    <Fact>
    Public Sub StructureNestedCallsWriteBackInnerFirst()
        Dim s As New LoggedValue(New List(Of String))
        s.NestedCalls()
        Assert.Equal("get 0,get 0,set 1,set 0,then 0", s.LogText)
    End Sub

    <Fact>
    Public Sub StructureInstanceMethodSeesItsOwnState()
        Dim s As New LoggedValue(New List(Of String))
        s.InstanceMethodInCondition()
        Assert.Equal("get 0,instance 0,set 1,then 1", s.LogText)
    End Sub

    <Fact>
    Public Sub StructureConstructorConditionWritesBackBeforeTheBody()
        Dim s As New LoggedValue(New List(Of String), True)
        Assert.Equal("get 0,set 1,then 1", s.LogText)
    End Sub

    <Fact>
    Public Sub StructurePropertyGetterConditionWritesBackBeforeTheBody()
        Dim s As New LoggedValue(New List(Of String))
        Assert.Equal(1, s.IncrementedIfPositive)
        Assert.Equal("get 0,set 1,get 1", s.LogText)
    End Sub

    <Fact>
    Public Sub StructureSharedMemberConditionWritesBackBeforeTheBody()
        LoggedValue.SharedValue = 0
        Assert.True(LoggedValue.SharedCondition())
        Assert.Equal(1, LoggedValue.SharedValue)
    End Sub

    <Fact>
    Public Sub WithStructureFieldConditionWritesBackBeforeTheBody()
        Dim h As New CounterHolder
        Assert.Equal(1, h.IncrementInWith())
        Assert.Equal(1, h.Inner.P)
    End Sub

    <Fact>
    Public Sub WithInsideALambdaArgumentStaysInTheLambda()
        Dim h As New CounterHolder
        Assert.Equal(13, h.WithInLambdaArgument())
    End Sub
    <Fact>
    Public Sub WithByRefParameterConditionWritesBackBeforeTheBody()
        Dim h As New CounterHolder
        Dim c As New Counter
        Assert.Equal(11, CounterHolder.IncrementInWithByRefParameters(h, c))
        Assert.Equal(1, c.P)
    End Sub
    <Fact>
    Public Sub WithOverAReassignedClassVariableKeepsTheOriginalObject()
        Dim h As New CounterHolder
        Dim original = h
        With h
            h = New CounterHolder
            .Total = 1
        End With
        Assert.Equal("1,0", original.Total & "," & h.Total)
    End Sub
    Private Class CounterHolder
        Public Inner As Counter
        Public Property Total As Integer

        Public Shared Function IncrementInWithByRefParameters(ByRef holder As CounterHolder, ByRef counter As Counter) As Integer
            Dim result = 0
            With holder
                If Increment(.Total) > 0 Then result = .Total * 10
            End With
            With counter
                If Increment(.P) > 0 Then result += .P
            End With
            Return result
        End Function
        Public Function WithInLambdaArgument() As Integer
            If AddResult(Total, Function()
                                    With Inner
                                        .P = 3
                                    End With
                                    Return 1
                                End Function) > 0 Then Return Total * 10 + Inner.P
            Return -1
        End Function

        Private Shared Function AddResult(ByRef target As Integer, f As Func(Of Integer)) As Integer
            target += f()
            Return target
        End Function

        Public Function IncrementInWith() As Integer
            With Inner
                If Increment(.P) > 0 Then Return .P
            End With
            Return -1
        End Function
    End Class

    <Fact>
    Public Sub StructureWithStructureFieldConditionWritesBackBeforeTheBody()
        Dim s As New LoggedValue(New List(Of String))
        Assert.Equal(1, s.IncrementInnerInWith())
        Assert.Equal(1, s.Inner.P)
    End Sub

    <Fact>
    Public Sub StructureWithMeConditionWritesBackBeforeTheBody()
        Dim s As New LoggedValue(New List(Of String))
        s.IfConditionInWithMe()
        Assert.Equal("get 0,set 1,then 1", s.LogText)
    End Sub
    <Fact>
    Public Sub StructureMePassedByRefIsCopiedAfterAnEarlierWriteBack()
        Dim s As New LoggedValue(New List(Of String))
        Assert.Equal(2, s.OperandThenMeByRef())
    End Sub

    <Fact>
    Public Sub StructureMePassedByRefAfterAndAlsoIsCopiedAfterAnEarlierWriteBack()
        Dim s As New LoggedValue(New List(Of String))
        Assert.True(s.MeByRefAfterAndAlso())
    End Sub

    Private Structure LoggedValue
        Public Shared SharedValue As Integer
        Private ReadOnly _log As List(Of String)
        Private _value As Integer
        Public Inner As Counter

        Public Sub New(log As List(Of String))
            _log = log
        End Sub

        Public Sub New(log As List(Of String), incrementInCondition As Boolean)
            _log = log
            If incrementInCondition AndAlso Increment(Value) > 0 Then
                _log.Add("then " & _value)
            End If
        End Sub

        Public ReadOnly Property LogText As String
            Get
                Return String.Join(",", _log)
            End Get
        End Property

        Public Property Value As Integer
            Get
                _log.Add("get " & _value)
                Return _value
            End Get
            Set(v As Integer)
                _log.Add("set " & v)
                _value = v
            End Set
        End Property

        Public ReadOnly Property IncrementedIfPositive As Integer
            Get
                If Increment(Value) > 0 Then
                    Return Value
                End If
                Return -1
            End Get
        End Property

        Private Function Mark(name As String, result As Boolean) As Boolean
            _log.Add("mark " & name)
            Return result
        End Function

        Private Function Peek() As Integer
            _log.Add("peek " & _value)
            Return _value
        End Function

        Private Function IncrementLogged(ByRef v As Integer) As Integer
            _log.Add("instance " & _value)
            v += 1
            Return v
        End Function

        Public Sub IfCondition()
            If Increment(Value) > 0 Then
                _log.Add("then " & _value)
            End If
        End Sub

        Public Sub ElseIfCondition()
            If Mark("if", False) Then
                _log.Add("if")
            ElseIf Increment(Me.Value) > 0 Then
                _log.Add("elseif " & _value)
            End If
        End Sub

        Public Sub ShortCircuitedOperand(callIt As Boolean)
            If Mark("left", callIt) AndAlso Increment(Value) > 0 Then
                _log.Add("then " & _value)
            End If
        End Sub

        Public Sub WhileCondition()
            While Increment(Value) < 3
                _log.Add("body " & _value)
            End While
        End Sub

        Public Sub DoLoopUntilCondition()
            Do
                _log.Add("body " & _value)
            Loop Until Increment(Value) >= 2
        End Sub

        Public Function OperandFollowedByPeek() As Integer
            Return Increment(Value) + Peek()
        End Function

        Public Function DeclaratorFollowedByPeek() As Integer
            Dim a = Increment(Value), b = Peek()
            Return a + b
        End Function

        Public Sub NestedCalls()
            If AddTo(Value, Increment(Value)) > 0 Then
                _log.Add("then " & _value)
            End If
        End Sub

        Public Sub InstanceMethodInCondition()
            If IncrementLogged(Value) > 0 Then
                _log.Add("then " & _value)
            End If
        End Sub

        Public Function IncrementInnerInWith() As Integer
            With Inner
                If Increment(.P) > 0 Then Return .P
            End With
            Return -1
        End Function

        Public Sub IfConditionInWithMe()
            With Me
                If Increment(.Value) > 0 Then
                    _log.Add("then " & _value)
                End If
            End With
        End Sub
        Public Function OperandThenMeByRef() As Integer
            Return Increment(Value) + ValueOf(Me)
        End Function

        Public Function MeByRefAfterAndAlso() As Boolean
            Return Increment(Value) > 0 AndAlso ValueOf(Me) = 1
        End Function

        Private Shared Function ValueOf(ByRef s As LoggedValue) As Integer
            Return s._value
        End Function

        Public Shared Function SharedCondition() As Boolean
            If Increment(SharedValue) > 0 Then
                Return SharedValue = 1
            End If
            Return False
        End Function
    End Structure

    Private Shared Function AddTo(ByRef target As Integer, amount As Integer) As Integer
        target = 0
        Return amount
    End Function

    Private Shared Function Increment(ByRef value As Integer) As Integer
        value += 1
        Return value
    End Function

    Private Class Probe
        Public Stored As Integer
        Public ReadOnly Log As New List(Of String)

        Public ReadOnly Property LogText As String
            Get
                Return String.Join(",", Log)
            End Get
        End Property

        Public Property Value As Integer
            Get
                Log.Add("get")
                Return Stored
            End Get
            Set(value As Integer)
                Log.Add("set")
                Stored = value
            End Set
        End Property

        Public ReadOnly Property IncrementedValue As Integer
            Get
                Return Increment(Value)
            End Get
        End Property
    End Class
End Class

Public Module ByRefWriteBackPlacementExtensions
    <Extension>
    Public Function SetToChangedReturnResult(ByRef value As String) As String
        value = "changed"
        Return "result"
    End Function

    <Extension>
    Public Function GrowIsShort(ByRef value As String) As Boolean
        value &= "x"
        Return value.Length < 3
    End Function
End Module
