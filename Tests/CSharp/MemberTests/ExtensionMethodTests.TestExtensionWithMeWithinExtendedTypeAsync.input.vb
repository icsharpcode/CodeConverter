Module Extensions
    <Extension()>
    Sub TestExtension(extendedClass As ExtendedClass)
    End Sub
End Module

Class ExtendedClass
  Sub TestExtensionConsumer()
    Me.TestExtension()
  End Sub
End Class