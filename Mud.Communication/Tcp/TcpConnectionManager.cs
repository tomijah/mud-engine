namespace Mud.Communication.Tcp
{
    using System;
    using System.Collections.Concurrent;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;

    using Mud.Common.Communication;

    public sealed class TcpConnectionManager : IConnectionManager
    {
        private const int AcceptBacklog = 512;

        private readonly int port;

        private readonly ConcurrentDictionary<Guid, TcpConnection> connections =
            new ConcurrentDictionary<Guid, TcpConnection>();

        private Socket serverSocket;

        private CancellationTokenSource cts;

        private bool started;

        public TcpConnectionManager(int port)
        {
            this.port = port;
        }

        public event Action<IConnection> ConnectionAccepted;

        public void Start()
        {
            if (this.started)
            {
                return;
            }

            this.started = true;

            try
            {
                this.serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                this.serverSocket.Bind(new IPEndPoint(IPAddress.Any, this.port));
                this.serverSocket.Listen(AcceptBacklog);
            }
            catch (SocketException se)
            {
                if (se.SocketErrorCode == SocketError.AddressAlreadyInUse)
                {
                    string message = string.Format(PortInUseException.MessageFormat, se.ErrorCode, se.Message, this.port);
                    throw new PortInUseException(message);
                }

                throw;
            }

            this.cts = new CancellationTokenSource();
            _ = Task.Run(this.AcceptLoopAsync);
        }

        public void Stop()
        {
            if (!this.started)
            {
                return;
            }

            this.cts?.Cancel();
            this.serverSocket?.Close();

            foreach (var connection in this.connections.Values)
            {
                _ = connection.DisconnectAsync("Server is shutting down.");
            }
        }

        private async Task AcceptLoopAsync()
        {
            while (!this.cts.Token.IsCancellationRequested)
            {
                Socket clientSocket;
                try
                {
                    clientSocket = await this.serverSocket.AcceptAsync(this.cts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (SocketException)
                {
                    // A single failed accept (e.g. client reset during handshake)
                    // should not take down the accept loop.
                    continue;
                }

                clientSocket.NoDelay = true;

                var connection = new TcpConnection(clientSocket);
                connection.Closed += this.OnConnectionClosed;
                this.connections.TryAdd(connection.Id, connection);
                connection.Start();

                this.ConnectionAccepted?.Invoke(connection);
            }
        }

        private void OnConnectionClosed(TcpConnection connection)
        {
            this.connections.TryRemove(connection.Id, out _);
        }
    }
}
