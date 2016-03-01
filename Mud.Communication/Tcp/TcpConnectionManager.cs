namespace Mud.Communication.Tcp
{
    using Common.Communication;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Sockets;

    public class TcpConnectionManager: IConnectionManager
    {
        private readonly ConcurrentDictionary<Guid, TcpConnection> connections =
            new ConcurrentDictionary<Guid, TcpConnection>();

        private bool started;

        private Socket serverSocket;

        public event Action<IConnection> UserConnected;

        public event Action<IConnection> UserDisconnected;

        public void Start(int port)
        {
            if (this.started)
            {
                return;
            }

            this.started = true;

            try
            {
                serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                var localIp = new IPEndPoint(IPAddress.Any, port);
                serverSocket.Bind(localIp);
                serverSocket.Listen(4);
                serverSocket.BeginAccept(this.OnClientConnect, null);
            }
            catch (SocketException se)
            {
                if (se.ErrorCode == 10048)
                {
                    string message = string.Format(PortInUseException.MessageFormat, se.ErrorCode, se.Message, port);
                    throw new PortInUseException(message);
                }

                throw;
            }
        }

        public void Stop()
        {
            serverSocket?.Close();

            var tempConnections = new List<TcpConnection>(this.connections.Values);
            foreach (TcpConnection conn in tempConnections)
            {
                conn.Disconnect();
            }
        }

        private void OnClientConnect(IAsyncResult asyncResult)
        {
            try
            {
                Socket socket = this.serverSocket.EndAccept(asyncResult);
                var conn = new TcpConnection(socket);
                conn.Disconnected += this.OnClientDisconnected;
                conn.StartListen();

                connections.TryAdd(conn.Id, conn);

                UserConnected?.Invoke(conn);

                serverSocket.BeginAccept(this.OnClientConnect, null);
            }
            catch (ObjectDisposedException)
            {
                // This exception was preventing the console from closing when the
                // shutdown command was issued.
            }
        }

        private void OnClientDisconnected(IConnection sender)
        {
            TcpConnection removed;
            connections.TryRemove(sender.Id, out removed);
            UserDisconnected?.Invoke(sender);
        }
    }
}
