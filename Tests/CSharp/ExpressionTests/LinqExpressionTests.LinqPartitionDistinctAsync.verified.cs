private static IEnumerable<string> FindPicFilePath()
{
    string[] words = new[] { "an", "apple", "a", "day", "keeps", "the", "doctor", "away" };

    return words.Skip(1).SkipWhile(word => word.Length >= 1).TakeWhile(word => word.Length < 5).Take(2).Distinct();
}