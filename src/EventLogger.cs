using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace EventLoggingLibrary.Channels
{
    /// <summary>
    /// EventLogger class for managing multiple logging channels.
    /// </summary>
    /// <remarks>
    /// This class is responsible for initializing and logging messages to various channels.
    /// </remarks>
    public class EventLogger
    {
        private readonly List<ILoggerChannel> _logChannels;

        public EventLogger()
        {
            _logChannels = new List<ILoggerChannel>();
        }

        public void AddChannel(ILoggerChannel channel)
        {
            channel.InitializeAsync();
            _logChannels.Add(channel);
        }

        public async Task LogAsync(Channel<string> messageChannel)
        {
            await foreach (var message in messageChannel.Reader.ReadAllAsync())
            {
                var tasks = _logChannels.Select(channel => channel.LogAsync(message));
                await Task.WhenAll(tasks);
            }
        }
    }
}