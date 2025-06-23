Public Class Foo
    Public Event Bar As EventHandler(Of EventArgs)

    Protected Sub OnBar(e As EventArgs)
        If BarEvent Is Nothing Then
            System.Diagnostics.Debug.WriteLine("No subscriber")
        Else
            RaiseEvent Bar(Me, e)
        End If
    End Sub
End Class