Imports System.Drawing

Public Class AShape
    Private PaneArea As RectangleF
    Private _OuterGap As Integer
    Public Sub SetSize(ByVal clientRectangle As Rectangle)
        Dim area = RectangleF.op_Implicit(clientRectangle)
        area.Inflate(-Me._OuterGap, -Me._OuterGap)
        Me.PaneArea = area
    End Sub
End Class