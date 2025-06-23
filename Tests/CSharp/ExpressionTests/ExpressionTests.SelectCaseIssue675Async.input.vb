Public Class EnumTest
    Public Enum UserInterface
        Unknown
        Spectrum
        Wisdom
    End Enum

    Public Sub OnLoad(ui As UserInterface?)
        Dim activity = 0
            Select Case ui
                Case ui Is Nothing
                    activity = 1
                Case UserInterface.Spectrum
                    activity = 2
                Case UserInterface.Wisdom
                    activity = 3
                Case Else
                    activity = 4
            End Select
    End Sub
End Class