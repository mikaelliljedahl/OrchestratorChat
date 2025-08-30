using System.Text;

namespace OrchestratorChat.Saturn.Tests.TestHelpers;

/// <summary>
/// Helper class for managing temporary files and directories in tests.
/// Ported from SaturnFork project with namespace updates.
/// </summary>
public class FileTestHelper : IDisposable
{
    private readonly string _testDirectory;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of FileTestHelper with a unique test directory.
    /// </summary>
    /// <param name="testName">Optional test name for directory naming. If null, uses GUID.</param>
    public FileTestHelper(string? testName = null)
    {
        var dirName = testName ?? $"OrchestratorTest_{Guid.NewGuid():N}";
        _testDirectory = Path.Combine(Path.GetTempPath(), dirName);
        Directory.CreateDirectory(_testDirectory);
    }

    /// <summary>
    /// Gets the root test directory path.
    /// </summary>
    public string TestDirectory => _testDirectory;

    /// <summary>
    /// Creates a file with the specified content at the given relative path.
    /// </summary>
    /// <param name="relativePath">Path relative to the test directory</param>
    /// <param name="content">Content to write to the file</param>
    /// <returns>The full path to the created file</returns>
    public string CreateFile(string relativePath, string content)
    {
        ThrowIfDisposed();
        
        var fullPath = Path.Combine(_testDirectory, relativePath);
        var directory = Path.GetDirectoryName(fullPath);
        
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
        File.WriteAllText(fullPath, content, Encoding.UTF8);
        return fullPath;
    }

    /// <summary>
    /// Creates a directory at the given relative path.
    /// </summary>
    /// <param name="relativePath">Path relative to the test directory</param>
    /// <returns>The full path to the created directory</returns>
    public string CreateDirectory(string relativePath)
    {
        ThrowIfDisposed();
        
        var fullPath = Path.Combine(_testDirectory, relativePath);
        Directory.CreateDirectory(fullPath);
        return fullPath;
    }

    /// <summary>
    /// Reads the content of a file at the given relative path.
    /// </summary>
    /// <param name="relativePath">Path relative to the test directory</param>
    /// <returns>The file content as string</returns>
    public string ReadFile(string relativePath)
    {
        ThrowIfDisposed();
        
        var fullPath = Path.Combine(_testDirectory, relativePath);
        return File.ReadAllText(fullPath, Encoding.UTF8);
    }

    /// <summary>
    /// Checks if a file exists at the given relative path.
    /// </summary>
    /// <param name="relativePath">Path relative to the test directory</param>
    /// <returns>True if the file exists, false otherwise</returns>
    public bool FileExists(string relativePath)
    {
        ThrowIfDisposed();
        
        var fullPath = Path.Combine(_testDirectory, relativePath);
        return File.Exists(fullPath);
    }

    /// <summary>
    /// Checks if a directory exists at the given relative path.
    /// </summary>
    /// <param name="relativePath">Path relative to the test directory</param>
    /// <returns>True if the directory exists, false otherwise</returns>
    public bool DirectoryExists(string relativePath)
    {
        ThrowIfDisposed();
        
        var fullPath = Path.Combine(_testDirectory, relativePath);
        return Directory.Exists(fullPath);
    }

    /// <summary>
    /// Gets the full path for a relative path within the test directory.
    /// </summary>
    /// <param name="relativePath">Path relative to the test directory</param>
    /// <returns>The full path</returns>
    public string GetFullPath(string relativePath)
    {
        ThrowIfDisposed();
        return Path.Combine(_testDirectory, relativePath);
    }

    /// <summary>
    /// Lists all files in the test directory.
    /// </summary>
    /// <param name="searchPattern">Optional search pattern</param>
    /// <param name="searchOption">Search option for subdirectories</param>
    /// <returns>Array of file paths relative to the test directory</returns>
    public string[] ListFiles(string searchPattern = "*", SearchOption searchOption = SearchOption.AllDirectories)
    {
        ThrowIfDisposed();
        
        var files = Directory.GetFiles(_testDirectory, searchPattern, searchOption);
        return files.Select(f => Path.GetRelativePath(_testDirectory, f)).ToArray();
    }

    /// <summary>
    /// Deletes a file at the given relative path.
    /// </summary>
    /// <param name="relativePath">Path relative to the test directory</param>
    public void DeleteFile(string relativePath)
    {
        ThrowIfDisposed();
        
        var fullPath = Path.Combine(_testDirectory, relativePath);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }

    /// <summary>
    /// Cleans up the test directory and all its contents.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        try
        {
            if (Directory.Exists(_testDirectory))
            {
                // Handle readonly files that might prevent deletion
                SetDirectoryReadWriteRecursive(_testDirectory);
                Directory.Delete(_testDirectory, true);
            }
        }
        catch (Exception ex)
        {
            // In test scenarios, we don't want cleanup failures to fail tests
            System.Diagnostics.Debug.WriteLine($"Failed to cleanup test directory {_testDirectory}: {ex.Message}");
        }

        _disposed = true;
    }

    private void SetDirectoryReadWriteRecursive(string directory)
    {
        try
        {
            var dirInfo = new DirectoryInfo(directory);
            dirInfo.Attributes = FileAttributes.Normal;

            foreach (var file in dirInfo.GetFiles())
            {
                file.Attributes = FileAttributes.Normal;
            }

            foreach (var subDir in dirInfo.GetDirectories())
            {
                SetDirectoryReadWriteRecursive(subDir.FullName);
            }
        }
        catch
        {
            // Ignore errors in cleanup
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(FileTestHelper));
        }
    }
}