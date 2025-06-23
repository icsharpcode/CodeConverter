Interface IClass
    ReadOnly Property ReadOnlyPropToRename(i as Integer) As String
    WriteOnly Property WriteOnlyPropToRename(i as Integer) As String
    Property PropToRename(i as Integer) As String

    ReadOnly Property ReadOnlyPropNonPublic(i as Integer) As String
    WriteOnly Property WriteOnlyPropNonPublic(i as Integer) As String
    Property PropNonPublic(i as Integer) As String

    ReadOnly Property ReadOnlyPropToRenameNonPublic(i as Integer) As String
    WriteOnly Property WriteOnlyPropToRenameNonPublic(i as Integer) As String
    Property PropToRenameNonPublic(i as Integer) As String

End Interface

Class ChildClass
    Implements IClass

    Public ReadOnly Property ReadOnlyPropRenamed(i As Integer) As String Implements IClass.ReadOnlyPropToRename
        Get
            Throw New NotImplementedException
        End Get
    End Property

    Public Overridable WriteOnly Property WriteOnlyPropRenamed(i As Integer) As String Implements IClass.WriteOnlyPropToRename
        Set
            Throw New NotImplementedException
        End Set
    End Property

    Public Overridable Property PropRenamed(i As Integer) As String Implements IClass.PropToRename
        Get
            Throw New NotImplementedException
        End Get
        Set
            Throw New NotImplementedException
        End Set
    End Property

    Private ReadOnly Property ReadOnlyPropNonPublic(i As Integer) As String Implements IClass.ReadOnlyPropNonPublic
        Get
            Throw New NotImplementedException
        End Get
    End Property

    Protected Friend Overridable WriteOnly Property WriteOnlyPropNonPublic(i As Integer) As String Implements IClass.WriteOnlyPropNonPublic
        Set
            Throw New NotImplementedException
        End Set
    End Property

    Friend Overridable Property PropNonPublic(i As Integer) As String Implements IClass.PropNonPublic
        Get
            Throw New NotImplementedException
        End Get
        Set
            Throw New NotImplementedException
        End Set
    End Property

    Protected Friend Overridable ReadOnly Property ReadOnlyPropRenamedNonPublic(i As Integer) As String Implements IClass.ReadOnlyPropToRenameNonPublic
        Get
            Throw New NotImplementedException
        End Get
    End Property

    Private WriteOnly Property WriteOnlyPropRenamedNonPublic(i As Integer) As String Implements IClass.WriteOnlyPropToRenameNonPublic
        Set
            Throw New NotImplementedException
        End Set
    End Property

    Friend Overridable Property PropToRenameNonPublic(i As Integer) As String Implements IClass.PropToRenameNonPublic
        Get
            Throw New NotImplementedException
        End Get
        Set
            Throw New NotImplementedException
        End Set
    End Property
End Class
