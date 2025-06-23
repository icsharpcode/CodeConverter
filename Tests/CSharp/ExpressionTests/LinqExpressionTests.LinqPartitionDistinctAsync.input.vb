Private Shared Function FindPicFilePath() As IEnumerable(Of String)
    Dim words = {"an", "apple", "a", "day", "keeps", "the", "doctor", "away"}

    Return From word In words
            Skip 1
            Skip While word.Length >= 1
            Take While word.Length < 5
            Take 2
            Distinct
End Function