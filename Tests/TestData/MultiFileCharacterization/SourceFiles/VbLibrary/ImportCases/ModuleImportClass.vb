Imports VbNetStandardLib.ModuleWithClassAndMethod

Namespace ModuleImport
    Class ModuleImportClass
        Public Sub ModuleImportUnQualifiedMember()
            ModuleMethod()
        End Sub

        Public Sub ModuleImportUnQualifiedNestedType()
            Dim mc = New ModuleClass()
            mc.ModuleClassMethod()
        End Sub
    End Class
End Namespace