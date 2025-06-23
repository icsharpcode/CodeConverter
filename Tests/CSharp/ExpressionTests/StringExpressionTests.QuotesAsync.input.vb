Class TestClass
    Shared Function GetTextFeedInput(pStream As String, pTitle As String, pText As String) As String
        Return "{" & AccessKey() & ",""streamName"": """ & pStream & """,""point"": [" & GetTitleTextPair(pTitle, pText) & "]}"
    End Function

    Shared Function AccessKey() As String
        Return """accessKey"": ""8iaiHNZpNbBkYHHGbMNiHhAp4uPPyQke"""
    End Function

    Shared Function GetNameValuePair(pName As String, pValue As Integer) As String
        Return ("{""name"": """ & pName & """, ""value"": """ & pValue & """}")
    End Function

    Shared Function GetNameValuePair(pName As String, pValue As String) As String
        Return ("{""name"": """ & pName & """, ""value"": """ & pValue & """}")
    End Function

    Shared Function GetTitleTextPair(pName As String, pValue As String) As String
        Return ("{""title"": """ & pName & """, ""msg"": """ & pValue & """}")
    End Function
    Shared Function GetDeltaPoint(pDelta As Integer) As String
        Return ("{""delta"": """ & pDelta & """}")
    End Function
End Class