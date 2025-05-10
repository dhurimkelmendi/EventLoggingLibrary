using System;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace EventLoggingLibrary.Channels
{
    public class ConsoleLogger : ILoggerChannel
    {
        public Task InitializeAsync()
        {
            // Initialization logic if needed
            return Task.CompletedTask;
        }

        public async Task LogAsync(string message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message), "Message channel cannot be null.");
            }

            Console.WriteLine(message);
            return;
        }
    }
}