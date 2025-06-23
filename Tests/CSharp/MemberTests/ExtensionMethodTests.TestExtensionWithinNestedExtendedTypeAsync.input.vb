Module Extensions
    <Extension()>
    Sub TestExtension(extendedClass As NestingClass.ExtendedClass)
    End Sub
End Module

Class NestingClass
    Class ExtendedClass
      Sub TestExtensionConsumer()
        TestExtension()
      End Sub        
    End Class
End Class