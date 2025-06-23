
Public Class VisualBasicClass
    Public Function Test(applicationRoles)
        For Each appRole In applicationRoles
            Dim objectUnit = appRole
            While objectUnit IsNot Nothing
                If appRole < 10 Then
                    If appRole < 3 Then
                        Return True
                    Else If appRole < 4 Then
                        Continue While ' Continue While
                    Else If appRole < 5 Then
                        Exit For ' Exit For
                    Else If appRole < 6 Then
                        Continue For ' Continue For
                    Else If appRole < 7 Then
                        Exit For ' Exit For
                    Else If appRole < 8 Then
                        Exit While ' Exit While
                    Else If appRole < 9 Then
                        Continue While ' Continue While
                    Else
                        Continue For ' Continue For
                    End If
                End IF
                objectUnit = objectUnit.ToString
            End While
        Next
    End Function
End Class