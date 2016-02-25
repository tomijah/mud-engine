namespace Mud.Communication.Tcp
{
    using Common.Communication;
    using Common.Extensions;
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;

    public class TcpConnection : IConnection
    {
        private readonly Socket socket;

        private readonly byte[] buffer;

        private readonly MessageBuffer messageBuffer;

        private bool connected;

        public TcpConnection(Socket socket)
        {
            var remoteEndPoint = (IPEndPoint)socket.RemoteEndPoint;
            this.Id = Guid.NewGuid();
            this.socket = socket;
            this.buffer = new byte[1];
            this.connected = true;
            this.Ip = remoteEndPoint.Address;
            this.messageBuffer = new MessageBuffer();
            this.messageBuffer.Message += this.OnMessage;
        }

        public event Action<IConnection, string> MessageReceived;

        public event Action<IConnection> Disconnected;

        public IPAddress Ip { get; private set; }

        public Guid Id { get; private set; }

        public void StartListen()
        {
            this.Listen();
        }

        public void Disconnect()
        {
            this.OnConnectionDisconnect();
        }

        public void Send(string message)
        {
            this.Send(Encoding.GetEncoding(437).GetBytes(message));
        }

        private void Send(byte[] data)
        {
            try
            {
                this.socket.BeginSend(data, 0, data.Length, 0, this.OnSendComplete, null);
            }
            catch
            {
                this.OnConnectionDisconnect();
            }
        }

        private void Listen()
        {
            try
            {
                this.socket.BeginReceive(this.buffer, 0, this.buffer.Length, SocketFlags.None, this.OnDataReceived, null);
            }
            catch
            {
                this.OnConnectionDisconnect();
            }
        }

        private void OnMessage(string message)
        {
            if (this.MessageReceived != null)
            {
                this.MessageReceived(this, message);
            }
        }

        private void OnDataReceived(IAsyncResult asyncResult)
        {
            try
            {
                if (this.socket.Connected)
                {
                    int bytesCount = this.socket.EndReceive(asyncResult);

                    if (bytesCount == 0)
                    {
                        this.OnConnectionDisconnect();
                    }
                    else
                    {
                        if (this.buffer.Length > 0)
                        {
                            this.messageBuffer.Push(this.buffer);
                        }

                        this.Listen();
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                this.OnConnectionDisconnect();
            }
            catch (ThreadAbortException)
            {
                this.OnConnectionDisconnect();
            }
            catch (Exception ex)
            {
                var socketEx = ex.CastTo<SocketException>();
                var connectionReset = socketEx != null && socketEx.SocketErrorCode == SocketError.ConnectionReset;
                if (!connectionReset)
                {
                    // TODO: log connection error
                }

                this.OnConnectionDisconnect();
            }
        }

        private void OnSendComplete(IAsyncResult asyncResult)
        {
            try
            {
                this.socket.EndSend(asyncResult);
            }
            catch
            {
                this.OnConnectionDisconnect();
            }
        }

        private void OnConnectionDisconnect()
        {
            if (this.socket.Connected)
            {
                this.socket.Shutdown(SocketShutdown.Both);
                this.socket.Close();
            }
            if (this.connected)
            {
                this.connected = false;
                if (this.Disconnected != null)
                {
                    this.Disconnected(this);
                }
            }
        }
    }
}
