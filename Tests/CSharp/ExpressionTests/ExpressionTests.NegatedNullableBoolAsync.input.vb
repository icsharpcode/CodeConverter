Public Enum CrashEnum
    None = 0
    One = 1
    Two = 2
End Enum
Public Class CrashClass
    Public Property CrashEnum As CrashEnum?
    Public Property IsSet As Boolean
End Class
Public Class CrashTest
    Public Function Edit(Optional flag2 As Boolean = False, Optional crashEnum As CrashEnum? = Nothing) As Object
        Dim CrashClass As CrashClass = Nothing
        Dim Flag0 As Boolean = True
        Dim Flag1 As Boolean = True
        If Flag0 Then
            If Flag1 AndAlso flag2 Then
                If crashEnum.GetValueOrDefault() > 0 AndAlso (Not CrashClass.CrashEnum.HasValue OrElse crashEnum <> CrashClass.CrashEnum) Then
                    CrashClass.CrashEnum = crashEnum
                    CrashClass.IsSet = True
                End If
            End If
        End If
        Return Nothing
    End Function
End Class