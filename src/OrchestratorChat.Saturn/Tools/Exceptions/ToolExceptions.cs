using System;
using System.IO;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace OrchestratorChat.Saturn.Tools
{
    /// <summary>
    /// Exception thrown when attempting to delete a non-empty directory
    /// </summary>
    public class DirectoryNotEmptyException : IOException
    {
        public DirectoryNotEmptyException(string message) : base(message) { }
        public DirectoryNotEmptyException(string message, Exception innerException) 
            : base(message, innerException) { }
    }

    /// <summary>
    /// Wrapper for DirectoryInfo to provide abstraction for file globbing
    /// </summary>
    public class DirectoryInfoWrapper : DirectoryInfoBase
    {
        private readonly DirectoryInfo _directoryInfo;

        public DirectoryInfoWrapper(DirectoryInfo directoryInfo)
        {
            _directoryInfo = directoryInfo ?? throw new ArgumentNullException(nameof(directoryInfo));
        }

        public DirectoryInfoWrapper(string path)
        {
            _directoryInfo = new DirectoryInfo(path);
        }

        public override string Name => _directoryInfo.Name;
        public override string FullName => _directoryInfo.FullName;
        public override DirectoryInfoBase ParentDirectory => 
            _directoryInfo.Parent != null ? new DirectoryInfoWrapper(_directoryInfo.Parent) : null!;

        public override IEnumerable<FileSystemInfoBase> EnumerateFileSystemInfos()
        {
            return _directoryInfo.EnumerateFileSystemInfos()
                .Select(CreateWrapper);
        }

        public override DirectoryInfoBase GetDirectory(string path)
        {
            var subDirectory = new DirectoryInfo(Path.Combine(_directoryInfo.FullName, path));
            return new DirectoryInfoWrapper(subDirectory);
        }

        public override FileInfoBase GetFile(string path)
        {
            var file = new FileInfo(Path.Combine(_directoryInfo.FullName, path));
            return new FileInfoWrapper(file);
        }

        private static FileSystemInfoBase CreateWrapper(FileSystemInfo info)
        {
            return info switch
            {
                DirectoryInfo dir => new DirectoryInfoWrapper(dir),
                FileInfo file => new FileInfoWrapper(file),
                _ => throw new NotSupportedException($"Unsupported file system info type: {info.GetType()}")
            };
        }

        public bool Exists => _directoryInfo.Exists;
        public DateTime CreationTime => _directoryInfo.CreationTime;
        public DateTime LastWriteTime => _directoryInfo.LastWriteTime;
        public DirectoryInfo Info => _directoryInfo;
    }

    /// <summary>
    /// Wrapper for FileInfo to provide abstraction for file globbing
    /// </summary>
    public class FileInfoWrapper : FileInfoBase
    {
        private readonly FileInfo _fileInfo;

        public FileInfoWrapper(FileInfo fileInfo)
        {
            _fileInfo = fileInfo ?? throw new ArgumentNullException(nameof(fileInfo));
        }

        public override string Name => _fileInfo.Name;
        public override string FullName => _fileInfo.FullName;
        public override DirectoryInfoBase ParentDirectory => 
            _fileInfo.Directory != null ? new DirectoryInfoWrapper(_fileInfo.Directory) : null!;
    }
}