Public Class AnonymousLambdaTypeConversionTest
    Public Sub CallThing(thingToCall As [Delegate])
    End Sub

    Public Sub SomeMethod()
    End Sub

    Public Sub Foo()
        CallThing(Sub()
                    SomeMethod()
                  End Sub)
        CallThing(Sub(a) SomeMethod())
        CallThing(Function()
                    SomeMethod()
                    Return False
                  End Function)
        CallThing(Function(a) False)
    End Sub
End Class