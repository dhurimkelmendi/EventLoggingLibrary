using System.Threading.Channels;
using System.Threading.Tasks;

namespace EventLoggingLibrary.Channels
{
    /// <summary>
    /// Interface for logging channels.
    /// </summary>
    /// <remarks>
    /// This interface defines the contract for logging channels, which are responsible for
    /// sending log messages to various destinations (e.g., console, TCP server).
    /// </remarks>
    public interface ILoggerChannel
    {
        Task InitializeAsync();
        Task LogAsync(string messageChannel);
    }
}