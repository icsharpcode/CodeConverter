
Public Interface InterfaceWithOptionalParameters
    Sub S(Optional i As Integer = 0)
End Interface

Public Class ImplInterfaceWithOptionalParameters : Implements InterfaceWithOptionalParameters
    Public Sub InterfaceWithOptionalParameters_S(Optional i As Integer = 0) Implements InterfaceWithOptionalParameters.S
    End Sub
End Class