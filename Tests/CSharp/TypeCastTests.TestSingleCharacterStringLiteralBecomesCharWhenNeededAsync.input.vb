Class CharTestClass
    Private Function QuoteSplit(ByVal text As String) As String()
        Return text.Split("""")
    End Function
End Class