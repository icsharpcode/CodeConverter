Public ReadOnly Property Prop() As Object
    Get
        Try
            Prop = New Object
            Exit Property
        Catch ex As Exception
        End Try
    End Get
End Property

Public Function Func() As Object
    Try
        Func = New Object
        Exit Function
    Catch ex As Exception
    End Try
End Function