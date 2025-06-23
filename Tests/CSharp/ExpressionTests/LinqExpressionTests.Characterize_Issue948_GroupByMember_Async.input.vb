Imports System.Collections.Generic
Imports System.Linq

Class C
    Public Property MyString As String
End Class

Public Module Module1
    Public Sub Main()
        Dim list As New List(Of C)()
        Dim result = From f In list
                     Group f By f.MyString Into Group
                     Order By MyString
	End Sub
End Module