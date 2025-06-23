Public Interface IFileSystem

    Event FileChanged(FileData As String)
    Event FileCreated(FileData As String)
    Event FileDeleted(FileData As String)
    Event FileRenamed(e As RenamedEventArgs)
    Event WatcherError(e As ErrorEventArgs)

End Interface

Public Class FileSystemWin
    Implements IFileSystem

    Public Event FileChanged(FileData As String) Implements IFileSystem.FileChanged
    Public Event FileCreated(FileData As String) Implements IFileSystem.FileCreated
    Public Event FileDeleted(FileData As String) Implements IFileSystem.FileDeleted
    Public Event FileRenamed(e As RenamedEventArgs) Implements IFileSystem.FileRenamed
    Public Event WatcherError(e As ErrorEventArgs) Implements IFileSystem.WatcherError

End Class