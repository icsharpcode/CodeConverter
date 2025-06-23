
Interface IClass
    ReadOnly Property ReadOnlyPropParam(i as Integer) As Integer
    ReadOnly Property ReadOnlyProp As Integer

    WriteOnly Property WriteOnlyPropParam(i as Integer) As Integer
    WriteOnly Property WriteOnlyProp As Integer
End Interface

Class ChildClass
    Implements IClass

    Public Overridable Property RenamedPropertyParam(i As Integer) As Integer Implements IClass.ReadOnlyPropParam
        Get
            Return 1
        End Get
        Set
        End Set
    End Property

    Public Overridable Property RenamedReadOnlyProperty As Integer Implements IClass.ReadOnlyProp ' Comment moves because this line gets split
        Get
            Return 2
        End Get
        Set
        End Set
    End Property

    Public Overridable Property RenamedWriteOnlyPropParam(i As Integer) As Integer Implements IClass.WriteOnlyPropParam
        Get
            Return 1
        End Get
        Set
        End Set
    End Property

    Public Overridable Property RenamedWriteOnlyProperty As Integer Implements IClass.WriteOnlyProp ' Comment moves because this line gets split
        Get
            Return 2
        End Get
        Set
        End Set
    End Property
End Class
