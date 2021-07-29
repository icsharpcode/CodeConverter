Public Class AnInterfaceImplementation
    Implements AnInterface

    Public ReadOnly Property APropertyWithDifferentName As String Implements AnInterface.AnInterfaceProperty
        Get
            Return "Const"
        End Get
    End Property

    Public Sub AMethodWithDifferentName() Implements AnInterface.AnInterfaceMethod
    End Sub
End Class