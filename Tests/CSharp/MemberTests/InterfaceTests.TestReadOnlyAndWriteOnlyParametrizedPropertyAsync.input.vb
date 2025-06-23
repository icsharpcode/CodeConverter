Interface IClass
    ReadOnly Property ReadOnlyProp(i as Integer) As String
    WriteOnly Property WriteOnlyProp(i as Integer) As String
End Interface

Class ChildClass
    Implements IClass

    Public Overridable ReadOnly Property ReadOnlyProp(i As Integer) As String Implements IClass.ReadOnlyProp
        Get
            Throw New NotImplementedException
        End Get
    End Property

    Public Overridable WriteOnly Property WriteOnlyProp(i As Integer) As String Implements IClass.WriteOnlyProp
        Set
            Throw New NotImplementedException
        End Set
    End Property
End Class
