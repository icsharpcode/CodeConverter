MustInherit Class TestClass
        Public MustOverride ReadOnly Property ReadOnlyProp As String
        Public MustOverride WriteOnly Property WriteOnlyProp As String
End Class

Class ChildClass
    Inherits TestClass

    Public Overrides ReadOnly Property ReadOnlyProp As String
    Public Overrides WriteOnly Property WriteOnlyProp As String
        Set
        End Set
    End Property
End Class
