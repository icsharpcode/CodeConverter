
Public Property Prop(ByVal i As Integer) As String
    Get
        Static bGet As Boolean
        bGet = False
    End Get

    Set(ByVal s As String)
        Static bSet As Boolean
        bSet = False
    End Set
End Property
