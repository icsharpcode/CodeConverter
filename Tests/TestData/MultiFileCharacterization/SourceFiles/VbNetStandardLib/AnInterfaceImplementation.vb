Public Class AnInterfaceImplementation
    Implements AnInterface

    Public ReadOnly Property APropertyWithDifferentNameThanPropertyFromInterface As String Implements AnInterface.AnInterfaceProperty
        Get
            Return "Const"
        End Get
    End Property
End Class