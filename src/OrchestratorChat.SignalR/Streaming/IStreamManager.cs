namespace OrchestratorChat.SignalR.Streaming
{
    /// <summary>
    /// Interface for managing data streams in SignalR
    /// </summary>
    public interface IStreamManager
    {
        /// <summary>
        /// Create a new stream
        /// </summary>
        /// <param name="sessionId">Session ID for the stream</param>
        /// <param name="agentId">Agent ID for the stream</param>
        /// <returns>The stream ID</returns>
        Task<string> CreateStream(string sessionId, string agentId);

        /// <summary>
        /// Write data to a stream
        /// </summary>
        /// <param name="streamId">The stream ID</param>
        /// <param name="data">Data to write</param>
        Task WriteToStream(string streamId, object data);

        /// <summary>
        /// Close a stream
        /// </summary>
        /// <param name="streamId">The stream ID</param>
        Task CloseStream(string streamId);

        /// <summary>
        /// Get an async enumerable for reading from a stream
        /// </summary>
        /// <typeparam name="T">Type of data in the stream</typeparam>
        /// <param name="streamId">The stream ID</param>
        /// <returns>Async enumerable of stream data</returns>
        IAsyncEnumerable<T> GetStream<T>(string streamId);
    }
}