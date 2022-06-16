Imports System
Imports System.Linq
Imports Xunit

Public Class LinqExpressionTests

    <Fact>
    Sub TestWhereAfterGroup()
        Dim numbers = New List(Of Integer) From {1, 2, 3, 4, 4}
        Dim duplicates = From x In numbers
                         Group By x Into Group
                         Where Group.Count > 1
        Assert.Equal(1, duplicates.Count)
    End Sub

End Class
