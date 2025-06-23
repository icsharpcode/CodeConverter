
Module Module1
    Public Class BaseImpl
        Protected Overridable Function GetImplName() As String
            Return NameOf(BaseImpl)
        End Function
    End Class

    ''' <summary>
    ''' The fact that this class doesn't contain a definition for GetImplName is crucial to the repro
    ''' </summary>
    Public Class ErrorSite
        Inherits BaseImpl
        Public Function PublicGetImplName()
            ' This must not be qualified with MyBase since the method is overridable
            Return GetImplName()
        End Function
    End Class

    Public Class OverrideImpl
        Inherits ErrorSite
        Protected Overrides Function GetImplName() As String
            Return NameOf(OverrideImpl)
        End Function
    End Class

    Sub Main()
        Dim c As New OverrideImpl
        Console.WriteLine(c.PublicGetImplName())
    End Sub
End Module