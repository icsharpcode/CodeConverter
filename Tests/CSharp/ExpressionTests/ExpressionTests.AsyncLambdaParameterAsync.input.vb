Class TestClass
        public Async Function mySub() As Task(Of Boolean)
            Return Await Me.ExecuteAuthenticatedAsync(Async Function() As Task(Of Boolean)
                Return Await DoSomethingAsync()
            End Function)

        End Function
        Private Async Function ExecuteAuthenticatedAsync(myFunc As Func(Of Task(Of Boolean))) As Task(Of Boolean)
            Return Await myFunc()
        End Function
        Private  Async Function DoSomethingAsync() As Task(Of Boolean)
            Return True
        End Function
End Class