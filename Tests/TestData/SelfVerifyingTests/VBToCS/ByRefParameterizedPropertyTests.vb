Imports System.Collections.Generic
Imports Xunit

Public Class ByRefParameterizedPropertyTests
    Private ReadOnly _log As New List(Of String)
    Private _fieldIndex As Integer = 1

    <Fact>
    Public Sub ParameterizedPropertyIsWrittenBackThroughItsSetterIssue909()
        Dim db As New Db(_log)
        db.Get_Equal(1, db.Qr(1, 21), "position")
        Assert.Equal("changed", db.Qr(1, 21))
        Assert.Equal("get 1,21;set 1,21=changed;get 1,21", LogText())
    End Sub

    <Fact>
    Public Sub IndexArgumentsAreEvaluatedOnce()
        Dim db As New Db(_log)
        SetValue(db.Qr(NextIndex(), 21))
        Assert.Equal("index 1;get 1,21;set 1,21=changed", LogText())
    End Sub

    <Fact>
    Public Sub ReceiverIsEvaluatedOnce()
        SetValue(GetDb().Qr(1, 21))
        Assert.Equal("receiver;get 1,21;set 1,21=changed", LogText())
    End Sub

    <Fact>
    Public Sub NamedIndexArgumentsAreWrittenBackThroughTheSetter()
        Dim db As New Db(_log)
        SetValue(db.Qr(fieldNumber:=21, tableBuffer:=NextIndex()))
        Assert.Equal("index 1;get 1,21;set 1,21=changed", LogText())
    End Sub

    <Fact>
    Public Sub OmittedOptionalIndexIsWrittenBackThroughTheSetter()
        Dim db As New Db(_log)
        SetValue(db.WithDefault())
        Assert.Equal("get 7;set 7=changed", LogText())
    End Sub

    <Fact>
    Public Sub ImplicitMeParameterizedPropertyIsWrittenBack()
        Dim db As New Db(_log)
        db.SetOwnValue()
        db.SetOwnValueWithMe()
        Assert.Equal("get 2,3;set 2,3=changed;get 4,5;set 4,5=changed", LogText())
    End Sub

    <Fact>
    Public Sub LocalIndexWrittenByAnotherArgumentUsesItsOriginalValue()
        Dim db As New Db(_log)
        Dim index = 1
        SetValueAndIndex(db.Qr(index, 21), index)
        Assert.Equal(5, index)
        Assert.Equal("get 1,21;set 1,21=changed", LogText())
    End Sub

    <Fact>
    Public Sub FieldIndexWrittenByAnotherArgumentUsesItsOriginalValue()
        Dim db As New Db(_log)
        SetValueAndIndex(db.Qr(_fieldIndex, 21), _fieldIndex)
        Assert.Equal(5, _fieldIndex)
        Assert.Equal("get 1,21;set 1,21=changed", LogText())
    End Sub

    <Fact>
    Public Sub ReadOnlyParameterizedPropertyIsNotWrittenBack()
        Dim db As New Db(_log)
        SetValue(db.ReadOnlyQr(NextIndex()))
        Assert.Equal("index 1;readonly get 1", LogText())
    End Sub

    <Fact>
    Public Sub DefaultPropertyIndexIsEvaluatedOnce()
        Dim db As New Db(_log)
        SetValue(db(NextIndex()))
        Assert.Equal("index 1;item get 1;item set 1=changed", LogText())
    End Sub

    <Fact>
    Public Sub PropertyReceiverIsEvaluatedOnce()
        SetValue(GetDb().Plain)
        Assert.Equal("receiver;plain get;plain set=changed", LogText())
    End Sub

    <Fact>
    Public Sub SettersAreCalledFromTheLastArgumentToTheFirst()
        Dim db As New Db(_log)
        SetTwoValues(db.Qr(1, 1), db.Qr(NextIndex(), 2))
        Assert.Equal("get 1,1;index 1;get 1,2;set 1,2=second;set 1,1=first", LogText())
    End Sub

    Private Shared Sub SetTwoValues(ByRef first As String, ByRef second As String)
        first = "first"
        second = "second"
    End Sub

    <Fact>
    Public Sub StructureReceiverInAnArrayIsWrittenInPlace()
        Dim cells(2) As Cell
        SetValue(cells(NextIndex()).Text)
        Assert.Equal("changed", cells(1).Text)
        Assert.Equal("index 1", LogText())
    End Sub

    <Fact>
    Public Sub StructureFieldReceiverIsWrittenInPlace()
        Dim holder As New CellHolder
        SetValue(holder.Cell.Text)
        SetValue(GetHolder(holder).Cell.Text)
        Assert.Equal("changedchanged", holder.Cell.Text)
        Assert.Equal("holder", LogText())
    End Sub

    Private Function GetHolder(holder As CellHolder) As CellHolder
        _log.Add("holder")
        Return holder
    End Function

    Private Class CellHolder
        Public Cell As Cell
    End Class

    Private Structure Cell
        Private _text As String
        Public Property Text As String
            Get
                Return _text
            End Get
            Set(value As String)
                _text &= value
            End Set
        End Property
    End Structure

    <Fact>
    Public Sub ReturnWritesBackThroughTheSetterBeforeReturning()
        Dim db As New Db(_log)
        Assert.Equal("changed", ReturnSetValue(db))
        Assert.Equal("index 1;get 1,21;set 1,21=changed", LogText())
    End Sub

    <Fact>
    Public Sub ConditionWritesBackThroughTheSetterBeforeTheBlock()
        Dim db As New Db(_log)
        If SetValue(db.Qr(NextIndex(), 21)) = "changed" Then
            _log.Add("then " & db.Qr(1, 21))
        End If
        Assert.Equal("index 1;get 1,21;set 1,21=changed;get 1,21;then changed", LogText())
    End Sub

    Private Function ReturnSetValue(db As Db) As String
        Return SetValue(db.Qr(NextIndex(), 21))
    End Function

    Private Function LogText() As String
        Return String.Join(";", _log)
    End Function

    Private Function NextIndex() As Integer
        _log.Add("index 1")
        Return 1
    End Function

    Private Function GetDb() As Db
        _log.Add("receiver")
        Return New Db(_log)
    End Function

    Private Shared Function SetValue(ByRef value As String) As String
        value = "changed"
        Return value
    End Function

    Private Shared Sub SetValueAndIndex(ByRef value As String, ByRef index As Integer)
        value = "changed"
        index = 5
    End Sub

    Private Class Db
        Private ReadOnly _log As List(Of String)
        Private ReadOnly _values As New Dictionary(Of String, String)

        Public Sub New(log As List(Of String))
            _log = log
        End Sub

        Public Sub Get_Equal(ByRef tableBuffer As Integer, ByRef keyValue As String, ByRef recordPosition As String)
            keyValue = "changed"
        End Sub

        Public Sub SetOwnValue()
            SetValue(Qr(2, 3))
        End Sub

        Public Sub SetOwnValueWithMe()
            SetValue(Me.Qr(4, 5))
        End Sub

        Public Property Qr(tableBuffer As Integer, fieldNumber As Integer) As String
            Get
                _log.Add("get " & tableBuffer & "," & fieldNumber)
                Return Lookup(tableBuffer & "," & fieldNumber)
            End Get
            Set(value As String)
                _log.Add("set " & tableBuffer & "," & fieldNumber & "=" & value)
                _values(tableBuffer & "," & fieldNumber) = value
            End Set
        End Property

        Public Property WithDefault(Optional index As Integer = 7) As String
            Get
                _log.Add("get " & index)
                Return Lookup(CStr(index))
            End Get
            Set(value As String)
                _log.Add("set " & index & "=" & value)
            End Set
        End Property

        Public ReadOnly Property ReadOnlyQr(index As Integer) As String
            Get
                _log.Add("readonly get " & index)
                Return Lookup(CStr(index))
            End Get
        End Property

        Default Public Property Item(index As Integer) As String
            Get
                _log.Add("item get " & index)
                Return Lookup(CStr(index))
            End Get
            Set(value As String)
                _log.Add("item set " & index & "=" & value)
            End Set
        End Property

        Public Property Plain As String
            Get
                _log.Add("plain get")
                Return Nothing
            End Get
            Set(value As String)
                _log.Add("plain set=" & value)
            End Set
        End Property

        Private Function Lookup(key As String) As String
            Dim value As String = Nothing
            _values.TryGetValue(key, value)
            Return value
        End Function
    End Class
End Class
