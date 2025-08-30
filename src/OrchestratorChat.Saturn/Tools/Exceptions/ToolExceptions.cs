using System;
using System.IO;

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
    /// Wrapper for DirectoryInfo to provide abstraction
    /// </summary>
    public class DirectoryInfoWrapper
    {
        private readonly DirectoryInfo _directoryInfo;

        public DirectoryInfoWrapper(string path)
        {
            _directoryInfo = new DirectoryInfo(path);
        }

        public bool Exists => _directoryInfo.Exists;
        public string FullName => _directoryInfo.FullName;
        public string Name => _directoryInfo.Name;
        public DateTime CreationTime => _directoryInfo.CreationTime;
        public DateTime LastWriteTime => _directoryInfo.LastWriteTime;
        public DirectoryInfo Info => _directoryInfo;
    }
}