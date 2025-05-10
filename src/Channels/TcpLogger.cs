using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace EventLoggingLibrary.Channels
{
    public class TcpLogger : ILoggerChannel, IDisposable
    {
        private readonly string _serverAddress;
        private readonly int _port;
        private TcpClient _tcpClient;
        private NetworkStream _stream;

        public TcpLogger(string serverAddress, int port)
        {
            _serverAddress = serverAddress;
            _port = port;
        }

        public async Task InitializeAsync()
        {
            _tcpClient = new TcpClient();
            await _tcpClient.ConnectAsync(_serverAddress, _port);
            _stream = _tcpClient.GetStream();
        }

        public async Task LogAsync(string message)
        {
            if (_tcpClient == null || !_tcpClient.Connected)
            {
                throw new SocketException((int)SocketError.NotConnected);
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentNullException(nameof(message), "Message cannot be null.");
            }

            byte[] data = Encoding.UTF8.GetBytes(message);
            await _stream.WriteAsync(data, 0, data.Length);
        }

        public void Dispose()
        {
            _stream?.Dispose();
            _tcpClient?.Close();
        }
    }
}