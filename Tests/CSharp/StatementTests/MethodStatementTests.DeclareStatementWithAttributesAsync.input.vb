Public Class AcmeClass
    Friend Declare Ansi Function GetNumDevices Lib "CP210xManufacturing.dll" Alias "CP210x_GetNumDevices" (ByRef NumDevices As String) As Integer
End Class