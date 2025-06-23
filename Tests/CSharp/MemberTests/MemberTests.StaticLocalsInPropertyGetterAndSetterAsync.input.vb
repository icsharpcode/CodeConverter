
Public Property Prop As String
    Get
        Static b As Boolean
        b = True
    End Get

    Set(ByVal s As String)
        Static b As Boolean
        b = False
    End Set
End Property
