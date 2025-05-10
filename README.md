# Event Logging Library

## Overview
The Event Logging Library is a reusable .NET library for application event logging. It supports multiple logging channels and ensures non-blocking operations, making it suitable for high-performance applications.

## Features
- **Multiple Logging Channels**: Includes TCP and Console logging channels.
- **Non-blocking Design**: Uses asynchronous operations to avoid blocking the main application thread.
- **Extensible**: Easily add new logging channels by implementing the `ILoggerChannel` interface.

---

## Technical Requirements
- **.NET SDK**: Version 6.0 or later
- **Operating System**: Windows, macOS, or Linux
- **Dependencies**: No third-party logging libraries are used.

---

## How to Use the Library in an Application

### 1. Add the Library to Your Project
- Clone the repository:
  ```bash
  git clone <repository-url>
  ```
- Build the library:
  ```bash
  dotnet build
  ```
- Add a reference to the library in your project:
  ```xml
  <ProjectReference Include="..\EventLoggingLibrary\EventLoggingLibrary.csproj" />
  ```

### 2. Initialize the Logger
Create an instance of the `EventLogger` and add the desired channels.

```csharp
using EventLoggingLibrary;
using EventLoggingLibrary.Channels;

var logger = new EventLogger();
logger.AddChannel(new ConsoleLogger());
logger.AddChannel(new TcpLogger("127.0.0.1", 5000));
```

### 3. Log Messages
Use the `LogAsync` method to log messages asynchronously.

```csharp
var messageChannel = Channel.CreateUnbounded<string>();
await messageChannel.Writer.WriteAsync("This is a test log message.");
messageChannel.Writer.Complete();

await logger.LogAsync(messageChannel);
```

---

## Explanation of Channels

### 0. **Extensibility**
The library is designed to be extensible, allowing developers to add new logging channels without modifying the core library. To add a new channel:
1. Implement the `ILoggerChannel` interface.
2. Define the behavior for the `LogAsync` method.
3. Add the new channel to the `EventLogger` using the `AddChannel` method.

#### Example: Adding a File Logger
Here’s an example of how to implement a file-based logger:

```csharp
using System.IO;
using System.Threading.Channels;
using System.Threading.Tasks;

public class FileLogger : ILoggerChannel
{
    private readonly string _filePath;

    public FileLogger(string filePath)
    {
        _filePath = filePath;
    }

    public async Task LogAsync(Channel<string> messageChannel)
    {
        await using var writer = new StreamWriter(_filePath, append: true);
        await foreach (var message in messageChannel.Reader.ReadAllAsync())
        {
            await writer.WriteLineAsync(message);
        }
    }
}
```

#### Usage:
```csharp
var logger = new EventLogger();
logger.AddChannel(new FileLogger("logs.txt"));
```

---

### 1. **ConsoleLogger**
Logs messages to the console. This is useful for debugging or simple logging needs.

- **Usage**:
  ```csharp
  logger.AddChannel(new ConsoleLogger());
  ```

### 2. **TcpLogger**
Logs messages to a remote server over TCP. This is useful for centralized logging in distributed systems.

- **Usage**:
  ```csharp
  logger.AddChannel(new TcpLogger("127.0.0.1", 5000));
  ```

- **Error Handling**:
  - Throws a `SocketException` if the connection fails.
  - Ensures messages are not sent if the connection is not established.

---

## Code Coverage
The library includes unit tests to ensure reliability. All core functionalities are tested thoroughly to ensure edge cases are covered.

---

## Design Choices

### 1. **Asynchronous Design**
The library uses `Channel<T>` and asynchronous methods to ensure non-blocking operations. This allows applications to continue running without being hindered by logging operations.

### 2. **Extensibility**
The `ILoggerChannel` interface allows developers to add new logging channels (e.g., file, database) without modifying the core library.

### 3. **Error Handling**
The `TcpLogger` includes error handling for connection failures and invalid messages. Additional channels can implement their own error handling as needed.

### 4. **Concurrency**
The library is designed to handle concurrent logging operations efficiently:
- Multiple loggers can process messages concurrently without blocking each other.
- The use of `Channel<T>` ensures thread-safe message passing between producers and consumers.
- Unit tests verify that messages are processed concurrently by multiple loggers, ensuring high performance in multi-threaded environments.

---

## Testing
To run the unit tests:

1. Navigate to the tests directory:
   ```bash
   cd tests
   ```
2. Run the tests:
   ```bash
   dotnet test
   ```

---

## Contributing
Contributions are welcome! Please submit a pull request or open an issue for any enhancements or bug fixes.

---

## License
This project is licensed under the MIT License. See the LICENSE file for details.