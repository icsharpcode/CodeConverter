Imports System
Imports System.Collections.Generic
Imports Xunit

Public Class ByRefParameterizedPropertyInStructureTests

    <Fact>
    Public Sub StructureParameterizedPropertyInIfConditionWritesBackBeforeTheBody()
        Dim s As New SDb
        Assert.Equal("index 1;get 1,2;set 1,2=changed;body changed", s.ParamPropInIf())
    End Sub

    <Fact>
    Public Sub StructureDefaultPropertyOnMeInIfConditionEvaluatesItsIndexOnce()
        Dim s As New SDb
        Assert.Equal("index 1;getd 1;setd 1=changed;body changed", s.MeDefaultInIf())
    End Sub

    <Fact>
    Public Sub StructureDefaultPropertyOnMeInIfConditionWritesBack()
        Dim s As New SDb
        Assert.Equal("getd 3;setd 3=changed;body changed", s.MeDefaultPlainInIf(3))
    End Sub

    <Fact>
    Public Sub StructureParameterizedPropertyOperandWritesBackBeforeTheNextOperand()
        Dim s As New SDb
        Assert.Equal("index 1;get 1,2;set 1,2=changed;10", s.ParamPropOperand())
    End Sub

    <Fact>
    Public Sub StructureWithMeParameterizedPropertyInIfConditionWritesBack()
        Dim s As New SDb
        Assert.Equal("index 1;get 1,2;set 1,2=changed;body changed", s.WithMeInIf())
    End Sub

    <Fact>
    Public Sub WithStructureFieldParameterizedPropertyInIfConditionWritesBackInPlace()
        Dim h As New Holder
        Assert.Equal("index 1;get 1,2;set 1,2=changed;body changed", h.WithFieldInIf())
        Assert.Equal("changed", h.S.Stored)
    End Sub

    <Fact>
    Public Sub WithByRefStructureParameterParameterizedPropertyInIfConditionWritesBackInPlace()
        Dim s As New SDb
        Assert.Equal("index 1;get 1,2;set 1,2=changed;body changed", Holder.WithByRefInIf(s))
        Assert.Equal("changed", s.Stored)
    End Sub

    <Fact>
    Public Sub WithClassLocalParameterizedPropertyInIfConditionWritesBack()
        Assert.Equal("index 1;get 1,2;set 1,2=changed;body changed", Holder.WithClassInIf())
    End Sub

    <Fact>
    Public Sub StructureParameterizedPropertyInWhileConditionWritesBackEachTime()
        Dim s As New SDb
        Assert.Equal("index 1;get 1,2;set 1,2=changed;loop;index 2;get 2,2;set 2,2=changed;", s.WhileLoop())
    End Sub

    Public Class CDb
        Public Log As String = ""
        Public Stored As String = "orig"
        Public Property Qr(a As Integer, b As Integer) As String
            Get
                Log &= "get " & a & "," & b & ";"
                Return Stored
            End Get
            Set(value As String)
                Log &= "set " & a & "," & b & "=" & value & ";"
                Stored = value
            End Set
        End Property
        Public Function NextIndex() As Integer
            Log &= "index 1;"
            Return 1
        End Function
    End Class

    Public Structure SDb
        Public Log As String
        Public Stored As String
        Private _n As Integer

        Public Property Qr(a As Integer, b As Integer) As String
            Get
                Log &= "get " & a & "," & b & ";"
                Return Stored
            End Get
            Set(value As String)
                Log &= "set " & a & "," & b & "=" & value & ";"
                Stored = value
            End Set
        End Property

        Default Public Property Item(a As Integer) As String
            Get
                Log &= "getd " & a & ";"
                Return Stored
            End Get
            Set(value As String)
                Log &= "setd " & a & "=" & value & ";"
                Stored = value
            End Set
        End Property

        Public Function NextIndex() As Integer
            _n += 1
            Log &= "index " & _n & ";"
            Return _n
        End Function

        Public Function ParamPropInIf() As String
            If SetValue(Qr(NextIndex(), 2)) Then
                Log &= "body " & Stored
            End If
            Return Log
        End Function

        Public Function MeDefaultInIf() As String
            If SetValue(Me(NextIndex())) Then
                Log &= "body " & Stored
            End If
            Return Log
        End Function

        Public Function MeDefaultPlainInIf(i As Integer) As String
            If SetValue(Me(i)) Then
                Log &= "body " & Stored
            End If
            Return Log
        End Function

        Public Function ParamPropOperand() As String
            Dim r = Len(SetValueS(Qr(NextIndex(), 2))) + Len(Stored)
            Return Log & (r - 4)
        End Function

        Public Function WithMeInIf() As String
            With Me
                If SetValue(.Qr(.NextIndex(), 2)) Then
                    Log &= "body " & .Stored
                End If
            End With
            Return Log
        End Function

        Public Function WhileLoop() As String
            While SetValue(Qr(NextIndex(), 2)) AndAlso _n < 2
                Log &= "loop;"
                Stored = "orig"
            End While
            Return Log
        End Function
    End Structure

    Public Class Holder
        Public S As SDb

        Public Function WithFieldInIf() As String
            With S
                If SetValue(.Qr(.NextIndex(), 2)) Then
                    .Log &= "body " & .Stored
                End If
            End With
            Return S.Log
        End Function

        Public Shared Function WithByRefInIf(ByRef s As SDb) As String
            With s
                If SetValue(.Qr(.NextIndex(), 2)) Then
                    .Log &= "body " & .Stored
                End If
            End With
            Return s.Log
        End Function

        Public Shared Function WithClassInIf() As String
            Dim c As New CDb
            With c
                If SetValue(.Qr(.NextIndex(), 2)) Then
                    .Log &= "body " & .Stored
                End If
            End With
            Return c.Log
        End Function
    End Class

    Private Shared Function SetValue(ByRef v As String) As Boolean
        v = "changed"
        Return True
    End Function

    Private Shared Function SetValueS(ByRef v As String) As String
        v = "changed"
        Return v
    End Function
End Class
