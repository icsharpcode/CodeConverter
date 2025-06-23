Imports System.Collections.Generic
Imports System.Linq

Public Class VisualBasicClass
    Sub Main
	    Dim list1 As New List(Of Integer)() From {1,2,3}
	    Dim list2 As New List(Of Integer) From {2, 4,5}
	
	    Dim qs = From n In list1, x In list2
			     Where x = n 
			     Select New With {x, n}
    End Sub
End Class
