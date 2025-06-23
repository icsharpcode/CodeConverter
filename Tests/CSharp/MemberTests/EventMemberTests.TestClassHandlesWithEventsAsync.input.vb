Class MyEventClass
    Public Event TestEvent()

    Sub RaiseEvents()
        RaiseEvent TestEvent()
    End Sub
End Class

Class Class1
    Shared WithEvents SharedEventClassInstance As New MyEventClass
    WithEvents NonSharedEventClassInstance As New MyEventClass 'Comment moves to initialization in c# constructor

    Public Sub New(num As Integer)
    End Sub

    Public Sub New(obj As Object)
        MyClass.New(7)
    End Sub

    Shared Sub PrintTestMessage2() Handles SharedEventClassInstance.TestEvent, NonSharedEventClassInstance.TestEvent
    End Sub

    Sub PrintTestMessage3() Handles NonSharedEventClassInstance.TestEvent
    End Sub

    Public Class NestedShouldNotGainConstructor
    End Class
End Class

Public Class ShouldNotGainConstructor
End Class