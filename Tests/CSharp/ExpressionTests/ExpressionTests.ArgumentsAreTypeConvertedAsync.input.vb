Imports System.Drawing

Public Class Compound
    Public Sub TypeCast(someInt As Integer)
        Dim col = Color.FromArgb(someInt * 255.0F, someInt * 255.0F, someInt * 255.0F)
        Dim arry = New Single(7/someInt) {}
    End Sub
End Class