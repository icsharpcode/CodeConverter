Class OmittedArguments
    Sub M(Optional a As String = "a", ByRef Optional b As String = "b")
        Dim s As String = ""

        M() 'omitted implicitly
        M(,) 'omitted explicitly

        M(s) 'omitted implicitly
        M(s,) 'omitted explicitly

        M(a:=s) 'omitted implicitly
        M(a:=s, ) 'omitted explicitly
    End Sub
End Class