Imports System.Linq.Enumerable
Imports VbNetStandardLib.OuterClass

Namespace ClassImport
    Class ClassImportClass
        Public Function ClassImportUnQualifiedMember() As IEnumerable(Of String)
            Return Empty(Of string)
        End Function

        Public Sub ClassImportUnQualifiedNestedType()
            Dim ic = New InnerClass()
            ic.TestMethod()
        End Sub
    End Class
End Namespace