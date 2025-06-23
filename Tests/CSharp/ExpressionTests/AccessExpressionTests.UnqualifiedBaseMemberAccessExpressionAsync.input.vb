Public Class BaseController
    Protected Request As HttpRequest
End Class

Public Class ActualController
    Inherits BaseController

    Public Sub Do()
        Request.StatusCode = 200
    End Sub
End Class