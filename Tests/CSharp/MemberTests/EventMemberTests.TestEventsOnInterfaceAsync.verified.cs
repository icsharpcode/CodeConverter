using System.IO;

public partial interface IFileSystem
{

    event FileChangedEventHandler FileChanged;

    delegate void FileChangedEventHandler(string FileData);
    event FileCreatedEventHandler FileCreated;

    delegate void FileCreatedEventHandler(string FileData);
    event FileDeletedEventHandler FileDeleted;

    delegate void FileDeletedEventHandler(string FileData);
    event FileRenamedEventHandler FileRenamed;

    delegate void FileRenamedEventHandler(RenamedEventArgs e);
    event WatcherErrorEventHandler WatcherError;

    delegate void WatcherErrorEventHandler(ErrorEventArgs e);

}

public partial class FileSystemWin : IFileSystem
{

    public event IFileSystem.FileChangedEventHandler FileChanged;
    public event IFileSystem.FileCreatedEventHandler FileCreated;
    public event IFileSystem.FileDeletedEventHandler FileDeleted;
    public event IFileSystem.FileRenamedEventHandler FileRenamed;
    public event IFileSystem.WatcherErrorEventHandler WatcherError;

}