Class TestClass
    Public Property FirstName As String
    Public Property LastName As String

    Public Property FullName(ByVal lastNameFirst As Boolean, ByVal isFirst As Boolean) As String
        Get
            If lastNameFirst Then
                Return LastName & " " & FirstName
            Else
                Return FirstName & " " & LastName
            End If
        End Get
        ' This comment belongs to the set method
        Friend Set
            If isFirst Then FirstName = Value
        End Set
    End Property

    Public Overrides Function ToString() As String
        FullName(False, True) = "hello"
        Return FullName(False, True)
    End Function
End Class