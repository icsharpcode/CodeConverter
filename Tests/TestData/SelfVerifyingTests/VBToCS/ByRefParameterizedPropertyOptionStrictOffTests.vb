Option Strict Off

Imports System.Collections.Generic
Imports Xunit

Public Class ByRefParameterizedPropertyOptionStrictOffTests
    Private ReadOnly _log As New List(Of String)

    <Fact>
    Public Sub ConvertedValueIsWrittenBackThroughTheSetter()
        Dim table As New Table(_log)
        Dim index As Object = "2"
        Increment(table.Cell(index))
        Assert.Equal(11, table.Cell(2))
        Assert.Equal("get 2;set 2=11;get 2", String.Join(";", _log))
    End Sub

    Private Shared Sub Increment(ByRef value As String)
        value = CStr(CInt(value) + 1)
    End Sub

    Private Class Table
        Private ReadOnly _log As List(Of String)
        Private ReadOnly _values As New Dictionary(Of Integer, Integer)

        Public Sub New(log As List(Of String))
            _log = log
        End Sub

        Public Property Cell(index As Integer) As Integer
            Get
                _log.Add("get " & index)
                Dim value As Integer
                Return If(_values.TryGetValue(index, value), value, 10)
            End Get
            Set(value As Integer)
                _log.Add("set " & index & "=" & value)
                _values(index) = value
            End Set
        End Property
    End Class
End Class
