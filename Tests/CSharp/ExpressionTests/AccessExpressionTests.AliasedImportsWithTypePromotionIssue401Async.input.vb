Imports System.IO
Imports SIO = System.IO
Imports Microsoft.VisualBasic
Imports VB = Microsoft.VisualBasic

Public Class Test
    Private aliased As String = VB.Left("SomeText", 1)
    Private aliasedAgain As String = VB.Left("SomeText", 1)
    Private aliased2 As System.Delegate = New SIO.ErrorEventHandler(AddressOf OnError)

    ' Make use of the non-aliased imports, but ensure there's a name clash that requires the aliases in the above case
    Private Tr As String = NameOf(TextReader)
    Private Strings As String = NameOf(AppWinStyle)

    Class ErrorEventHandler
    End Class

    Shared Sub OnError(s As Object, e As ErrorEventArgs)
    End Sub
End Class