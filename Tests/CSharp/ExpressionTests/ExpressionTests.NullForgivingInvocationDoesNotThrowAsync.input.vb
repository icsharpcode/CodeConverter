Imports System

Public Class AClass
        Public Shared Sub Identify(ByVal talker As ITraceMessageTalker)
            talker?.IdentifyTalker(IdentityTraceMessage())
        End Sub

    Private Shared Function IdentityTraceMessage() As Object
        Throw New NotImplementedException()
    End Function
End Class

Public Interface ITraceMessageTalker
    Function IdentifyTalker(v As Object) As Object
End Interface