
MustInherit Class TestClass
    Implements IReadOnlyDictionary(Of Integer, Integer)
    Public Function TryGetValue(key as Integer, ByRef value As Integer) As Boolean Implements IReadOnlyDictionary(Of Integer, Integer).TryGetValue
        value = key
    End Function

    Private Sub TestMethod()
        Dim value As Integer
        Me.TryGetValue(5, value)
    End Sub

    Public MustOverride Function ContainsKey(key As Integer) As Boolean Implements IReadOnlyDictionary(Of Integer, Integer).ContainsKey
    Public MustOverride Function GetEnumerator() As IEnumerator(Of KeyValuePair(Of Integer, Integer)) Implements IEnumerable(Of KeyValuePair(Of Integer, Integer)).GetEnumerator
    Public MustOverride Function IEnumerable_GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
    Default Public MustOverride ReadOnly Property Item(key As Integer) As Integer Implements IReadOnlyDictionary(Of Integer, Integer).Item
    Public MustOverride ReadOnly Property Keys As IEnumerable(Of Integer) Implements IReadOnlyDictionary(Of Integer, Integer).Keys
    Public MustOverride ReadOnly Property Values As IEnumerable(Of Integer) Implements IReadOnlyDictionary(Of Integer, Integer).Values
    Public MustOverride ReadOnly Property Count As Integer Implements IReadOnlyCollection(Of KeyValuePair(Of Integer, Integer)).Count
End Class