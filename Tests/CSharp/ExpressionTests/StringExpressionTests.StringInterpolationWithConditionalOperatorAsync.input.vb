Public Function GetString(yourBoolean as Boolean) As String
    Return $"You {if (yourBoolean, "do", "do not")} have a true value"
End Function