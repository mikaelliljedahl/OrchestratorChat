using System.Diagnostics;
using System.Text;

namespace OrchestratorChat.Agents.Tests.TestHelpers;

/// <summary>
/// Mock process helper for testing process management without actual process execution.
/// Designed specifically for ClaudeAgent process testing scenarios.
/// </summary>
public class MockProcessHelper : IDisposable
{
    private readonly Dictionary<string, ProcessBehavior> _processBehaviors = new();
    private readonly List<ProcessExecution> _executionHistory = new();
    private bool _disposed;

    /// <summary>
    /// Gets the history of all process executions that were attempted.
    /// </summary>
    public IReadOnlyList<ProcessExecution> ExecutionHistory => _executionHistory.AsReadOnly();

    /// <summary>
    /// Gets the last process execution that was attempted, or null if none.
    /// </summary>
    public ProcessExecution? LastExecution => _executionHistory.LastOrDefault();

    /// <summary>
    /// Configures the behavior for a specific executable.
    /// </summary>
    /// <param name="executable">The executable name or path</param>
    /// <param name="exitCode">Exit code to return (default: 0)</param>
    /// <param name="standardOutput">Standard output to simulate</param>
    /// <param name="standardError">Standard error to simulate</param>
    /// <param name="startDelayMs">Delay before process "starts" (default: 0)</param>
    /// <param name="executionTimeMs">Time before process "completes" (default: 100)</param>
    /// <param name="shouldCrash">Whether the process should simulate a crash</param>
    /// <param name="crashDelayMs">Delay before crash occurs (default: 1000)</param>
    public void SetupProcess(
        string executable, 
        int exitCode = 0, 
        string standardOutput = "", 
        string standardError = "", 
        int startDelayMs = 0,
        int executionTimeMs = 100,
        bool shouldCrash = false,
        int crashDelayMs = 1000)
    {
        _processBehaviors[executable.ToLowerInvariant()] = new ProcessBehavior
        {
            ExitCode = exitCode,
            StandardOutput = standardOutput,
            StandardError = standardError,
            StartDelayMs = startDelayMs,
            ExecutionTimeMs = executionTimeMs,
            ShouldCrash = shouldCrash,
            CrashDelayMs = crashDelayMs,
            OutputChunks = new Queue<string>(SplitIntoChunks(standardOutput))
        };
    }

    /// <summary>
    /// Sets up a process to simulate streaming output in chunks.
    /// </summary>
    /// <param name="executable">The executable name or path</param>
    /// <param name="outputChunks">Sequence of output chunks to stream</param>
    /// <param name="chunkDelayMs">Delay between chunks</param>
    /// <param name="exitCode">Final exit code</param>
    public void SetupStreamingProcess(
        string executable, 
        IEnumerable<string> outputChunks, 
        int chunkDelayMs = 50,
        int exitCode = 0)
    {
        _processBehaviors[executable.ToLowerInvariant()] = new ProcessBehavior
        {
            ExitCode = exitCode,
            StandardOutput = string.Join("", outputChunks),
            StandardError = "",
            StartDelayMs = 0,
            ExecutionTimeMs = outputChunks.Count() * chunkDelayMs,
            ShouldCrash = false,
            CrashDelayMs = 0,
            OutputChunks = new Queue<string>(outputChunks),
            ChunkDelayMs = chunkDelayMs
        };
    }

    /// <summary>
    /// Simulates starting a process with the configured behavior.
    /// </summary>
    /// <param name="executable">The executable to start</param>
    /// <param name="arguments">Command line arguments</param>
    /// <param name="workingDirectory">Working directory</param>
    /// <returns>A mock process that simulates the configured behavior</returns>
    public MockProcess StartProcess(string executable, string arguments = "", string workingDirectory = "")
    {
        ThrowIfDisposed();

        var execution = new ProcessExecution
        {
            Executable = executable,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            StartTime = DateTime.UtcNow
        };

        _executionHistory.Add(execution);

        var behaviorKey = executable.ToLowerInvariant();
        var behavior = _processBehaviors.ContainsKey(behaviorKey)
            ? _processBehaviors[behaviorKey]
            : new ProcessBehavior(); // Default behavior

        return new MockProcess(behavior, execution);
    }

    /// <summary>
    /// Simulates a process that fails to start.
    /// </summary>
    /// <param name="executable">The executable that should fail</param>
    /// <param name="errorMessage">Error message to throw</param>
    public void SimulateStartFailure(string executable, string errorMessage = "Failed to start process")
    {
        _processBehaviors[executable.ToLowerInvariant()] = new ProcessBehavior
        {
            ShouldFailToStart = true,
            StartErrorMessage = errorMessage
        };
    }

    /// <summary>
    /// Verifies that a process was started with the specified parameters.
    /// </summary>
    /// <param name="executable">Expected executable</param>
    /// <param name="arguments">Expected arguments (can be partial)</param>
    /// <returns>True if a matching process execution was found</returns>
    public bool VerifyProcessStarted(string executable, string arguments = "")
    {
        return _executionHistory.Any(e =>
            string.Equals(e.Executable, executable, StringComparison.OrdinalIgnoreCase) &&
            (string.IsNullOrEmpty(arguments) || e.Arguments.Contains(arguments)));
    }

    /// <summary>
    /// Gets the number of times a specific executable was started.
    /// </summary>
    /// <param name="executable">The executable to count</param>
    /// <returns>Number of times the executable was started</returns>
    public int GetStartCount(string executable)
    {
        return _executionHistory.Count(e => 
            string.Equals(e.Executable, executable, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Clears all configured behaviors and execution history.
    /// </summary>
    public void Reset()
    {
        _processBehaviors.Clear();
        _executionHistory.Clear();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        Reset();
        _disposed = true;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(MockProcessHelper));
    }

    private static string[] SplitIntoChunks(string text, int chunkSize = 50)
    {
        if (string.IsNullOrEmpty(text))
            return Array.Empty<string>();

        var chunks = new List<string>();
        for (int i = 0; i < text.Length; i += chunkSize)
        {
            chunks.Add(text.Substring(i, Math.Min(chunkSize, text.Length - i)));
        }
        return chunks.ToArray();
    }

    /// <summary>
    /// Represents the configured behavior for a mock process.
    /// </summary>
    public class ProcessBehavior
    {
        public int ExitCode { get; set; } = 0;
        public string StandardOutput { get; set; } = "";
        public string StandardError { get; set; } = "";
        public int StartDelayMs { get; set; } = 0;
        public int ExecutionTimeMs { get; set; } = 100;
        public bool ShouldCrash { get; set; } = false;
        public int CrashDelayMs { get; set; } = 1000;
        public bool ShouldFailToStart { get; set; } = false;
        public string StartErrorMessage { get; set; } = "Process failed to start";
        public Queue<string> OutputChunks { get; set; } = new();
        public int ChunkDelayMs { get; set; } = 50;
    }

    /// <summary>
    /// Represents a recorded process execution.
    /// </summary>
    public class ProcessExecution
    {
        public string Executable { get; set; } = "";
        public string Arguments { get; set; } = "";
        public string WorkingDirectory { get; set; } = "";
        public DateTime StartTime { get; set; }
    }

    /// <summary>
    /// Mock process that simulates process behavior.
    /// </summary>
    public class MockProcess : IDisposable
    {
        private readonly ProcessBehavior _behavior;
        private readonly ProcessExecution _execution;
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private bool _disposed;
        private bool _hasStarted;
        private bool _hasExited;

        public MockProcess(ProcessBehavior behavior, ProcessExecution execution)
        {
            _behavior = behavior;
            _execution = execution;
        }

        public int Id { get; } = Random.Shared.Next(1000, 9999);
        public bool HasExited => _hasExited;
        public int ExitCode => _hasExited ? _behavior.ExitCode : throw new InvalidOperationException("Process has not exited");

        /// <summary>
        /// Starts the mock process and returns immediately.
        /// </summary>
        public async Task StartAsync()
        {
            if (_behavior.ShouldFailToStart)
            {
                throw new InvalidOperationException(_behavior.StartErrorMessage);
            }

            if (_behavior.StartDelayMs > 0)
            {
                await Task.Delay(_behavior.StartDelayMs, _cancellationTokenSource.Token);
            }

            _hasStarted = true;

            // Simulate crash if configured
            if (_behavior.ShouldCrash)
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(_behavior.CrashDelayMs, _cancellationTokenSource.Token);
                    if (!_hasExited && !_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        _hasExited = true;
                    }
                });
            }
            else
            {
                // Normal execution completion
                _ = Task.Run(async () =>
                {
                    await Task.Delay(_behavior.ExecutionTimeMs, _cancellationTokenSource.Token);
                    if (!_hasExited && !_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        _hasExited = true;
                    }
                });
            }
        }

        /// <summary>
        /// Simulates reading from standard output stream.
        /// </summary>
        /// <returns>Async enumerable of output chunks</returns>
        public async IAsyncEnumerable<string> ReadOutputStreamAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (!_hasStarted)
                throw new InvalidOperationException("Process has not been started");

            using var combinedToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token);

            while (_behavior.OutputChunks.Count > 0 && !combinedToken.Token.IsCancellationRequested)
            {
                if (_behavior.ChunkDelayMs > 0)
                {
                    await Task.Delay(_behavior.ChunkDelayMs, combinedToken.Token);
                }

                if (_behavior.OutputChunks.TryDequeue(out var chunk))
                {
                    yield return chunk;
                }
            }
        }

        /// <summary>
        /// Waits for the process to exit.
        /// </summary>
        /// <param name="timeoutMs">Timeout in milliseconds</param>
        /// <returns>True if process exited within timeout</returns>
        public async Task<bool> WaitForExitAsync(int timeoutMs = 30000)
        {
            var timeout = TimeSpan.FromMilliseconds(timeoutMs);
            var start = DateTime.UtcNow;

            while (!_hasExited && DateTime.UtcNow - start < timeout)
            {
                await Task.Delay(10);
            }

            return _hasExited;
        }

        /// <summary>
        /// Kills the mock process.
        /// </summary>
        public void Kill()
        {
            _cancellationTokenSource.Cancel();
            _hasExited = true;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _disposed = true;
        }
    }
}