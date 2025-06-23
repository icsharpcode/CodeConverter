Class TestClass
    Public Property FirstName As String
    Public Property LastName As String

    Public Property FullName(Optional ByVal isFirst As Boolean = False) As String
        Get
            Return FirstName & " " & LastName
        End Get
'This comment belongs to the set method
        Friend Set
            If isFirst Then FirstName = Value
        End Set
    End Property

    Public Overrides Function ToString() As String
        FullName(True) = "hello2"
        FullName() = "hello3"
        FullName = "hello4"
        Return FullName
    End Function
End Class