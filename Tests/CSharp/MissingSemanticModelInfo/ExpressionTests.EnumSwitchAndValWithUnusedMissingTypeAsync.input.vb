Public Class EnumAndValTest
    Public Enum PositionEnum As Integer
        None = 0
        LeftTop = 1
    End Enum

    Public TitlePosition As PositionEnum = PositionEnum.LeftTop
    Public TitleAlign As PositionEnum = 2
    Public Ratio As Single = 0

    Function PositionEnumFromString(ByVal pS As String, missing As MissingType) As PositionEnum
        Dim tPos As PositionEnum
        Select Case pS.ToUpper
            Case "NONE", "0"
                tPos = 0
            Case "LEFTTOP", "1"
                tPos = 1
            Case Else
                Ratio = Val(pS)
        End Select
        Return tPos
    End Function
    Function PositionEnumStringFromConstant(ByVal pS As PositionEnum) As String
        Dim tS As String
        Select Case pS
            Case 0
                tS = "NONE"
            Case 1
                tS = "LEFTTOP"
            Case Else
                tS = pS
        End Select
        Return tS
    End Function
End Class