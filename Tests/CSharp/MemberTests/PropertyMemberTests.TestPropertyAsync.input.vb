Class TestClass
    Public Property Test As Integer

    Public Property Test2 As Integer
        Get
            Return 0
        End Get
    End Property

    Private m_test3 As Integer

    Public Property Test3 As Integer
        Get
            If 7 = Integer.Parse("7") Then Exit Property
            Return Me.m_test3
        End Get
        Set(ByVal value As Integer)
            If 7 = Integer.Parse("7") Then Exit Property
            Me.m_test3 = value
        End Set
    End Property
End Class