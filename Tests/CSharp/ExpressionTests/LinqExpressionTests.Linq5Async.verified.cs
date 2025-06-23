private static string FindPicFilePath(List<FileInfo> AList, string picId)
{
    foreach (FileInfo FileInfo in from FileInfo1 in AList
                                  where FileInfo1.Name.Substring(0, 6) == picId
                                  select FileInfo1)
        return FileInfo.FullName;
    return string.Empty;
}