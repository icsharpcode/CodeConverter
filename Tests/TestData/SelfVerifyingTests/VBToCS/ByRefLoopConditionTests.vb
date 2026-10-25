Imports System.Collections.Generic
Imports System.Runtime.CompilerServices
Imports Xunit

Public Class ByRefLoopConditionTests
    <Fact>
    Public Sub WhileCopiesBackBeforeTheBodyAndAfterTheFinalCondition()
        Dim probe As New Probe
        Dim value As Integer = 0
        Dim seen As New List(Of Integer)
        While probe.BelowFour(value)
            seen.Add(value)
        End While
        Assert.Equal("1,2,3", String.Join(",", seen))
        Assert.Equal(4, value)
        Assert.Equal(4, probe.Calls)
    End Sub

    <Fact>
    Public Sub TopTestedDoWhileCopiesBackOnEveryCall()
        Dim probe As New Probe
        Dim value As Integer = 0
        Dim seen As New List(Of Integer)
        Do While probe.BelowFour(value)
            seen.Add(value)
        Loop
        Assert.Equal("1,2,3", String.Join(",", seen))
        Assert.Equal(4, value)
        Assert.Equal(4, probe.Calls)
    End Sub

    <Fact>
    Public Sub TopTestedDoUntilCopiesBackOnEveryCall()
        Dim probe As New Probe
        Dim value As Integer = 0
        Dim seen As New List(Of Integer)
        Do Until probe.AtLeastFour(value)
            seen.Add(value)
        Loop
        Assert.Equal("1,2,3", String.Join(",", seen))
        Assert.Equal(4, value)
        Assert.Equal(4, probe.Calls)
    End Sub

    <Fact>
    Public Sub BottomTestedDoWhileRunsTheBodyBeforeTheFirstCall()
        Dim probe As New Probe
        Dim value As Integer = 0
        Dim seen As New List(Of Integer)
        Do
            seen.Add(value)
        Loop While probe.BelowFour(value)
        Assert.Equal("0,1,2,3", String.Join(",", seen))
        Assert.Equal(4, value)
        Assert.Equal(4, probe.Calls)
    End Sub

    <Fact>
    Public Sub BottomTestedDoUntilRunsTheBodyBeforeTheFirstCall()
        Dim probe As New Probe
        Dim value As Integer = 0
        Dim seen As New List(Of Integer)
        Do
            seen.Add(value)
        Loop Until probe.AtLeastFour(value)
        Assert.Equal("0,1,2,3", String.Join(",", seen))
        Assert.Equal(4, value)
        Assert.Equal(4, probe.Calls)
    End Sub

    <Fact>
    Public Sub ZeroIterationsStillCopiesBackTheConditionResult()
        Dim probe As New Probe
        Dim value As Integer = 3
        While probe.BelowFour(value)
            Assert.Fail("The body must not run")
        End While
        Assert.Equal(4, value)
        value = 3
        Do While probe.BelowFour(value)
            Assert.Fail("The body must not run")
        Loop
        Assert.Equal(4, value)
        value = 3
        Do Until probe.AtLeastFour(value)
            Assert.Fail("The body must not run")
        Loop
        Assert.Equal(4, value)
        Assert.Equal(3, probe.Calls)
    End Sub

    <Fact>
    Public Sub BodyChangesAreCopiedInAtTheNextCondition()
        Dim probe As New Probe
        Dim value As Integer = 0
        Dim seen As New List(Of Integer)
        While probe.BelowFour(value)
            seen.Add(value)
            value += 1
        End While
        Assert.Equal("1,3", String.Join(",", seen))
        Assert.Equal(5, value)
        Assert.Equal(3, probe.Calls)
    End Sub

    <Fact>
    Public Sub ContinueDoEvaluatesTheBottomConditionAfterBodyChanges()
        Dim probe As New Probe
        Dim value As Integer = 0
        Dim seen As New List(Of Integer)
        Do
            value += 1
            seen.Add(value)
            Continue Do
        Loop Until probe.AtLeastFour(value)
        Assert.Equal("1,3", String.Join(",", seen))
        Assert.Equal(4, value)
        Assert.Equal(2, probe.Calls)
    End Sub

    <Fact>
    Public Sub ExitAndReturnDoNotDelayOrInventConditionCalls()
        Dim probe As New Probe
        Dim value As Integer = 0
        While probe.BelowFour(value)
            Exit While
        End While
        Assert.Equal(1, value)
        Assert.Equal(1, probe.Calls)
        Do
            value += 1
            Exit Do
        Loop While probe.BelowFour(value)
        Assert.Equal(2, value)
        Assert.Equal(1, probe.Calls)
        Assert.Equal(1, ReturnFromLoop())
    End Sub

    Private Shared Function ReturnFromLoop() As Integer
        Dim value As Integer = 0
        While IncrementBelowFour(value)
            Return value
        End While
        Return -1
    End Function

    <Fact>
    Public Sub ParentByRefParameterCanBePassedThroughTheLocalFunction()
        Dim value As Integer = 0
        RunWithByRefParameter(value)
        Assert.Equal(4, value)
    End Sub

    Private Shared Sub RunWithByRefParameter(ByRef value As Integer)
        Dim probe As New Probe
        While probe.BelowFour(value)
            Assert.InRange(value, 1, 3)
        End While
    End Sub

    <Fact>
    Public Sub NestedLoopsKeepTheirOwnConditionLocals()
        Dim outerProbe As New Probe
        Dim outer As Integer = 1
        Dim seen As New List(Of String)
        While outerProbe.BelowFour(outer)
            Dim innerProbe As New Probe
            Dim inner As Integer = 1
            While innerProbe.BelowFour(inner)
                seen.Add(outer & ":" & inner)
            End While
            Assert.Equal(4, inner)
        End While
        Assert.Equal("2:2,2:3,3:2,3:3", String.Join(",", seen))
        Assert.Equal(4, outer)
    End Sub

    <Fact>
    Public Sub ShortCircuitUsesTheCopiedBackValueAndSkipsUnusedCalls()
        Dim probe As New Probe
        Dim value As Integer = 0
        Dim seen As New List(Of Integer)
        While False AndAlso probe.BelowFour(value)
            Assert.Fail("The body must not run")
        End While
        Assert.Equal(0, probe.Calls)
        While probe.BelowFour(value) AndAlso value < 3
            seen.Add(value)
        End While
        Assert.Equal("1,2", String.Join(",", seen))
        Assert.Equal(3, value)
        Assert.Equal(3, probe.Calls)
    End Sub

    <Fact>
    Public Sub PropertyIsReadAndWrittenOncePerConditionCall()
        Dim probe As New Probe
        Dim seen As New List(Of Integer)
        While probe.BelowFour(probe.Value)
            seen.Add(probe.Stored)
        End While
        Assert.Equal("1,2,3", String.Join(",", seen))
        Assert.Equal(4, probe.Stored)
        Assert.Equal(4, probe.Reads)
        Assert.Equal(4, probe.Writes)
        Assert.Equal(4, probe.Calls)
    End Sub

    <Fact>
    Public Sub ReadOnlyPropertyIsReevaluatedWithoutCopyBack()
        Dim probe As New Probe
        Dim seen As New List(Of Integer)
        While probe.BelowFour(probe.NextInput)
            seen.Add(probe.Reads)
        End While
        Assert.Equal("1,2,3", String.Join(",", seen))
        Assert.Equal(4, probe.Reads)
        Assert.Equal(0, probe.Writes)
        Assert.Equal(4, probe.Calls)
    End Sub

    <Fact>
    Public Sub ParenthesizedArgumentIsCopiedInAgainWithoutCopyBack()
        Dim probe As New Probe
        Dim value As Integer = 0
        Dim seen As New List(Of Integer)
        While probe.BelowFour((value))
            seen.Add(value)
            value += 2
        End While
        Assert.Equal("0,2", String.Join(",", seen))
        Assert.Equal(4, value)
        Assert.Equal(3, probe.Calls)
    End Sub

    <Fact>
    Public Sub ConstantArgumentGetsAFreshTemporaryOnEveryCall()
        Dim probe As New Probe
        Dim bodies As Integer = 0
        While probe.IsFresh(0)
            bodies += 1
        End While
        Assert.Equal(2, bodies)
        Assert.Equal(3, probe.Calls)
    End Sub

    <Fact>
    Public Sub OmittedOptionalArgumentGetsAFreshTemporaryOnEveryCall()
        Dim probe As New Probe
        Dim bodies As Integer = 0
        Do While probe.IsFresh()
            bodies += 1
        Loop
        Assert.Equal(2, bodies)
        Assert.Equal(3, probe.Calls)
    End Sub

    <Fact>
    Public Sub ExistingIfCopiesBackBeforeItsBody()
        Dim value As Integer = 0
        If IncrementBelowFour(value) Then
            Assert.Equal(1, value)
        Else
            Assert.Fail("The body must run")
        End If
        value = 3
        If IncrementBelowFour(value) Then
            Assert.Fail("The body must not run")
        End If
        Assert.Equal(4, value)
    End Sub

    <Fact>
    Public Sub BareOptionalCallGetsAFreshTemporaryOnEveryCall()
        Dim probe As New Probe
        Dim bodies As Integer = 0
        Do While probe.IsFresh
            bodies += 1
        Loop
        Assert.Equal(2, bodies)
        Assert.Equal(3, probe.Calls)
    End Sub

    <Fact>
    Public Sub ExtensionPropertyReceiverCopiesBackOnEveryCall()
        Dim probe As New Probe
        Dim seen As New List(Of Integer)
        While probe.Value.IncrementBelowThree()
            seen.Add(probe.Stored)
            Assert.InRange(seen.Count, 1, 3)
        End While
        Assert.Equal("1,2", String.Join(",", seen))
        Assert.Equal(3, probe.Stored)
        Assert.Equal(3, probe.Reads)
        Assert.Equal(3, probe.Writes)
    End Sub

    <Fact>
    Public Sub RepeatedParentByRefArgumentIsPassedOnlyOnceToTheLocalFunction()
        Dim value As Integer = 0
        RunRepeatedParameter(value)
        Assert.Equal(4, value)
    End Sub

    Private Shared Sub RunRepeatedParameter(ByRef value As Integer)
        Dim bodies As Integer = 0
        While ChangeTwo(first:=value, second:=value)
            bodies += 1
            Assert.InRange(bodies, 1, 3)
            Assert.Equal(bodies, value)
        End While
    End Sub

    Private Shared Function ChangeTwo(ByRef first As Object, ByRef second As Object) As Boolean
        first = CInt(first) + 1
        second = CInt(second) + 1
        Return CInt(first) < 4
    End Function

    <Fact>
    Public Sub ParentByRefReceiverCanBeUsedInTheCondition()
        Dim probe As New Probe
        RunWithByRefReceiver(probe)
        Assert.Equal(4, probe.Stored)
        Assert.Equal(4, probe.Calls)
    End Sub

    Private Shared Sub RunWithByRefReceiver(ByRef probe As Probe)
        While probe.BelowFour(probe.Value)
            Assert.InRange(probe.Stored, 1, 3)
        End While
    End Sub

    <Fact>
    Public Sub LambdaInTheConditionKeepsItsOwnInvocationScope()
        Dim probe As New Probe
        Dim value As Integer = 0
        Dim seen As New List(Of Integer)
        While InvokePredicate(Function() probe.BelowFour(value))
            seen.Add(value)
        End While
        Assert.Equal("1,2,3", String.Join(",", seen))
        Assert.Equal(4, value)
        Assert.Equal(4, probe.Calls)
    End Sub

    Private Shared Function InvokePredicate(predicate As System.Func(Of Boolean)) As Boolean
        Return predicate()
    End Function

    Private Class Probe
        Public Calls As Integer
        Public Reads As Integer
        Public Writes As Integer
        Public Stored As Integer

        Public Property Value As Integer
            Get
                Reads += 1
                Return Stored
            End Get
            Set(value As Integer)
                Writes += 1
                Stored = value
            End Set
        End Property

        Public ReadOnly Property NextInput As Integer
            Get
                Reads += 1
                Return Reads - 1
            End Get
        End Property

        Public Function BelowFour(ByRef value As Object) As Boolean
            Calls += 1
            Assert.InRange(Calls, 1, 20)
            Return IncrementBelowFour(value)
        End Function

        Public Function AtLeastFour(ByRef value As Object) As Boolean
            Return Not BelowFour(value)
        End Function

        Public Function IsFresh(Optional ByRef value As Integer = 0) As Boolean
            Calls += 1
            Assert.InRange(Calls, 1, 20)
            value += 1
            Return value = 1 AndAlso Calls < 3
        End Function
    End Class

    Private Shared Function IncrementBelowFour(ByRef value As Object) As Boolean
        value = CInt(value) + 1
        Return CInt(value) < 4
    End Function
End Class

Public Module ByRefLoopConditionExtensions
    <Extension>
    Public Function IncrementBelowThree(ByRef value As Integer) As Boolean
        value += 1
        Return value < 3
    End Function
End Module
