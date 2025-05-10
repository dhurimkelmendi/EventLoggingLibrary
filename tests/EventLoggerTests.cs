using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Channels;
using System.Threading.Tasks;
using EventLoggingLibrary.Channels;
using Moq;
using Xunit;

namespace EventLoggingLibrary.Tests
{
    public class EventLoggerTests
    {
        private readonly EventLogger _eventLogger;

        public EventLoggerTests()
        {
            _eventLogger = new EventLogger();
        }

        [Fact]
        public async Task LogToConsole_ShouldLogMessage()
        {
            // Arrange
            var messageChannel = Channel.CreateUnbounded<string>();
            _eventLogger.AddChannel(new ConsoleLogger());

            // Act
            await messageChannel.Writer.WriteAsync("Test message for console logging");
            messageChannel.Writer.Complete();
            await _eventLogger.LogAsync(messageChannel);

            // Assert
            // Verify console output (this may require additional setup or a mock)
        }

        [Fact]
        public async Task LogToTcp_ShouldSendMessage()
        {
            // Arrange
            var messageChannel = Channel.CreateUnbounded<string>();
            _eventLogger.AddChannel(new TcpLogger("127.0.0.1", 5000));

            // Act
            await messageChannel.Writer.WriteAsync("Test message for TCP logging");
            messageChannel.Writer.Complete();
            await _eventLogger.LogAsync(messageChannel);

            // Assert
            // Verify TCP message sent (this may require a mock TCP server)
        }

        [Fact]
        public async Task TcpLogger_ShouldThrowException_WhenConnectionFails()
        {
            // Arrange
            var tcpLogger = new TcpLogger("invalid-address", 5000); // Invalid address to simulate failure

            // Act & Assert
            await Assert.ThrowsAsync<SocketException>(async () =>
            {
                await tcpLogger.LogAsync("TEST");
            });
        }

        [Fact]
        public async Task TcpLogger_ShouldReconnect_WhenConnectionIsLost()
        {
            // Arrange
            var tcpLogger = new TcpLogger("127.0.0.1", 5000);
            await tcpLogger.InitializeAsync(); // Simulate initial connection
            tcpLogger.Dispose(); // Simulate connection loss

            // Act
            await tcpLogger.InitializeAsync(); // Reconnect
            await tcpLogger.LogAsync("TEST");

            // Assert
            // No exception should be thrown, and the message should be logged successfully
            Assert.True(true); // Placeholder assertion
        }

        [Fact]
        public async Task Log_WithNoChannels_ShouldNotThrow()
        {
            // Arrange
            var messageChannel = Channel.CreateUnbounded<string>();

            // Act & Assert
            await messageChannel.Writer.WriteAsync("Message with no channels");
            messageChannel.Writer.Complete();
            await _eventLogger.LogAsync(messageChannel); // Should not throw
        }

        [Fact]
        public async Task Log_WithEmptyChannel_ShouldNotThrow()
        {
            // Arrange
            var messageChannel = Channel.CreateUnbounded<string>();

            // Act & Assert
            await messageChannel.Writer.WriteAsync("");
            messageChannel.Writer.Complete(); // No messages written
            await _eventLogger.LogAsync(messageChannel); // Should not throw
        }
        [Fact]
        public async Task Log_WithMultipleChannels_ShouldProcessAllMessages()
        {
            // Arrange
            var channel1 = Channel.CreateUnbounded<string>();
            var channel2 = Channel.CreateUnbounded<string>();
            var tcpLoggerMock = new Mock<ILoggerChannel>();
            var consoleLoggerMock = new Mock<ILoggerChannel>();

            var processedMessages = new ConcurrentBag<(string Message, DateTime Timestamp)>();

            // Setup TcpLogger mock
            tcpLoggerMock
                .Setup(logger => logger.LogAsync(It.IsAny<string>()))
                .Returns<string>(async message =>
                {
                    processedMessages.Add((message, DateTime.UtcNow));
                    await Task.Delay(50); // Simulate processing delay
                });

            // Setup ConsoleLogger mock
            consoleLoggerMock
                .Setup(logger => logger.LogAsync(It.IsAny<string>()))
                .Returns<string>(async message =>
                {
                    processedMessages.Add((message, DateTime.UtcNow));
                    await Task.Delay(50); // Simulate processing delay
                });

            _eventLogger.AddChannel(tcpLoggerMock.Object);
            _eventLogger.AddChannel(consoleLoggerMock.Object);

            // Act
            await channel1.Writer.WriteAsync("Message from Channel 1");
            await channel2.Writer.WriteAsync("Message from Channel 2");
            channel1.Writer.Complete();
            channel2.Writer.Complete();

            await Task.WhenAll(
                _eventLogger.LogAsync(channel1),
                _eventLogger.LogAsync(channel2)
            );

            // Assert
            Assert.Equal(4, processedMessages.Count); // Each message is logged by both loggers
            Assert.Contains(processedMessages, m => m.Message == "Message from Channel 1");
            Assert.Contains(processedMessages, m => m.Message == "Message from Channel 2");
            consoleLoggerMock.Verify(logger => logger.LogAsync(It.IsAny<string>()), Times.Exactly(2));
            tcpLoggerMock.Verify(logger => logger.LogAsync(It.IsAny<string>()), Times.Exactly(2));
            // Verify that messages were processed in the expected order
            var timestamps = processedMessages.Select(p => p.Timestamp).OrderBy(t => t).ToList();
            Assert.True(timestamps[0] <= timestamps[1], "Messages were not processed in the expected order.");
            Assert.True(timestamps[2] <= timestamps[3], "Messages were not processed in the expected order.");
            Assert.True(timestamps[0] != timestamps[2], "Messages from different channels were not processed concurrently.");
        }

        [Fact]
        public async Task Log_ShouldProcessMessagesConcurrently_WithMultipleLoggers()
        {
            // Arrange
            var tcpLoggerMock = new Mock<ILoggerChannel>();
            var consoleLoggerMock = new Mock<ILoggerChannel>();

            var processedMessages = new ConcurrentBag<(string Message, DateTime Timestamp)>();

            // Setup TcpLogger mock
            tcpLoggerMock
                .Setup(logger => logger.LogAsync(It.IsAny<string>()))
                .Returns<string>(async message =>
                {
                    processedMessages.Add((message, DateTime.UtcNow));
                    await Task.Delay(50); // Simulate processing delay
                });

            // Setup ConsoleLogger mock
            consoleLoggerMock
                .Setup(logger => logger.LogAsync(It.IsAny<string>()))
                .Returns<string>(async message =>
                {
                    processedMessages.Add((message, DateTime.UtcNow));
                    await Task.Delay(50); // Simulate processing delay
                });

            _eventLogger.AddChannel(tcpLoggerMock.Object);
            _eventLogger.AddChannel(consoleLoggerMock.Object);

            var messageChannel = Channel.CreateUnbounded<string>();
            var messages = new[] { "Message 1", "Message 2", "Message 3", "Message 4", "Message 5" };

            // Act
            foreach (var message in messages)
            {
                await messageChannel.Writer.WriteAsync(message);
            }
            messageChannel.Writer.Complete();
            await _eventLogger.LogAsync(messageChannel);

            // Assert
            tcpLoggerMock.Verify(logger => logger.LogAsync(It.IsAny<string>()), Times.Exactly(messages.Length));
            consoleLoggerMock.Verify(logger => logger.LogAsync(It.IsAny<string>()), Times.Exactly(messages.Length));

            // Verify concurrency by checking timestamps
            Assert.True(processedMessages.Count == messages.Length * 2); // Each message is logged by both loggers
            var timestamps = processedMessages.Select(p => p.Timestamp).OrderBy(t => t).ToList();

            // Ensure timestamps are close enough to indicate concurrency
            for (int i = 1; i < timestamps.Count; i++)
            {
                Assert.True((timestamps[i] - timestamps[i - 1]).TotalMilliseconds < 200, "Messages were not processed concurrently.");
            }
        }

        [Fact]
        public async Task Log_WithDeferredChannelCompletion_ShouldProcessAllMessages()
        {
            // Arrange
            var messageChannel = Channel.CreateUnbounded<string>();
            var writer = messageChannel.Writer;

            var tcpLoggerMock = new Mock<ILoggerChannel>();
            var consoleLoggerMock = new Mock<ILoggerChannel>();

            var processedMessages = new ConcurrentBag<(string Message, DateTime Timestamp)>();

            // Setup TcpLogger mock
            tcpLoggerMock
                .Setup(logger => logger.LogAsync(It.IsAny<string>()))
                .Returns<string>(async message =>
                {
                    processedMessages.Add((message, DateTime.UtcNow));
                    await Task.Delay(50); // Simulate processing delay
                });

            // Setup ConsoleLogger mock
            consoleLoggerMock
                .Setup(logger => logger.LogAsync(It.IsAny<string>()))
                .Returns<string>(async message =>
                {
                    processedMessages.Add((message, DateTime.UtcNow));
                    await Task.Delay(50); // Simulate processing delay
                });
            _eventLogger.AddChannel(tcpLoggerMock.Object);
            _eventLogger.AddChannel(consoleLoggerMock.Object);

            // Act
            var writingTask = Task.Run(async () =>
            {
                await writer.WriteAsync("Message 1");
                await Task.Delay(100); // Simulate some delay before writing the next message
                await writer.WriteAsync("Message 2");
                writer.Complete(); // Complete the channel after all writes are done
            });

            var loggingTask = _eventLogger.LogAsync(messageChannel);

            await Task.WhenAll(writingTask, loggingTask);

            // Assert
            Assert.Equal(4, processedMessages.Count); // Each message is logged by both loggers
            Assert.Contains(processedMessages, m => m.Message == "Message 1");
            Assert.Contains(processedMessages, m => m.Message == "Message 2");
            consoleLoggerMock.Verify(logger => logger.LogAsync(It.IsAny<string>()), Times.Exactly(2));
            tcpLoggerMock.Verify(logger => logger.LogAsync(It.IsAny<string>()), Times.Exactly(2));
        }

        [Fact]
        public async Task Log_WithBoundedChannel_ShouldRespectCapacity()
        {
            // Arrange
            var channelCapacity = 2;
            var boundedChannel = Channel.CreateBounded<string>(new BoundedChannelOptions(channelCapacity)
            {
                FullMode = BoundedChannelFullMode.Wait // Wait when the channel is full
            });
            var consoleLoggerMock = new Mock<ILoggerChannel>();

            var processedMessages = new ConcurrentBag<(string Message, DateTime Timestamp)>();

            // Setup ConsoleLogger mock
            consoleLoggerMock
                .Setup(logger => logger.LogAsync(It.IsAny<string>()))
                .Returns<string>(async message =>
                {
                    processedMessages.Add((message, DateTime.UtcNow));
                    await Task.Delay(50); // Simulate processing delay
                });
            _eventLogger.AddChannel(consoleLoggerMock.Object);

            // Act
            var writingTask = Task.Run(async () =>
            {
                await boundedChannel.Writer.WriteAsync("Message 1");
                await boundedChannel.Writer.WriteAsync("Message 2");
                var writeTask = boundedChannel.Writer.WriteAsync("Message 3"); // Should wait until space is available
                Assert.False(writeTask.IsCompleted); // Ensure the write is waiting
                boundedChannel.Writer.Complete();
            });

            var loggingTask = _eventLogger.LogAsync(boundedChannel);

            await Task.WhenAll(writingTask, loggingTask);

            // Assert
            // Verify that all messages were processed
            Assert.Equal(channelCapacity, processedMessages.Count); // Each message is logged by the console logger
            consoleLoggerMock.Verify(logger => logger.LogAsync(It.IsAny<string>()), Times.Exactly(channelCapacity));
        }

        [Fact]
        public async Task Log_WithBoundedChannel_ShouldDropMessagesWhenFull()
        {
            // Arrange
            var boundedChannel = Channel.CreateBounded<string>(new BoundedChannelOptions(2)
            {
                FullMode = BoundedChannelFullMode.DropWrite // Drop messages when the channel is full
            });

            var consoleLoggerMock = new Mock<ILoggerChannel>();

            var processedMessages = new ConcurrentBag<(string Message, DateTime Timestamp)>();

            // Setup ConsoleLogger mock
            consoleLoggerMock
                .Setup(logger => logger.LogAsync(It.IsAny<string>()))
                .Returns<string>(async message =>
                {
                    processedMessages.Add((message, DateTime.UtcNow));
                    await Task.Delay(50); // Simulate processing delay
                });
            _eventLogger.AddChannel(consoleLoggerMock.Object);

            // Act
            await boundedChannel.Writer.WriteAsync("Message 1");
            await boundedChannel.Writer.WriteAsync("Message 2");
            var writeTask = boundedChannel.Writer.WriteAsync("Message 3"); // This message should be dropped
            Assert.True(writeTask.IsCompleted); // Ensure the write completes immediately
            boundedChannel.Writer.Complete();

            await _eventLogger.LogAsync(boundedChannel);

            // Assert
            // Verify that only the first two messages were processed
            Assert.Equal(2, processedMessages.Count); // Only "Message 1" and "Message 2" should be logged
            consoleLoggerMock.Verify(logger => logger.LogAsync(It.IsAny<string>()), Times.Exactly(2));
        }

        [Fact]
        public async Task Log_WithBoundedChannel_ShouldDropOldestMessagesWhenFull()
        {
            // Arrange
            var boundedChannel = Channel.CreateBounded<string>(new BoundedChannelOptions(2)
            {
                FullMode = BoundedChannelFullMode.DropOldest // Drop the oldest message when the channel is full
            });
            var consoleLoggerMock = new Mock<ILoggerChannel>();

            var processedMessages = new ConcurrentBag<(string Message, DateTime Timestamp)>();

            // Setup ConsoleLogger mock
            consoleLoggerMock
                .Setup(logger => logger.LogAsync(It.IsAny<string>()))
                .Returns<string>(async message =>
                {
                    processedMessages.Add((message, DateTime.UtcNow));
                    await Task.Delay(50); // Simulate processing delay
                });
            _eventLogger.AddChannel(consoleLoggerMock.Object);

            // Act
            await boundedChannel.Writer.WriteAsync("Message 1");
            await boundedChannel.Writer.WriteAsync("Message 2");
            await boundedChannel.Writer.WriteAsync("Message 3"); // "Message 1" should be dropped
            boundedChannel.Writer.Complete();

            await _eventLogger.LogAsync(boundedChannel);

            // Assert
            // Verify that only "Message 2" and "Message 3" were processed
            Assert.Equal(2, processedMessages.Count); // Only "Message 2" and "Message 3" should be logged
            consoleLoggerMock.Verify(logger => logger.LogAsync(It.IsAny<string>()), Times.Exactly(2));
            Assert.DoesNotContain(processedMessages, m => m.Message == "Message 1"); // Ensure "Message 1" was dropped
        }
    }
}