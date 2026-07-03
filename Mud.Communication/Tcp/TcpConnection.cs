namespace Mud.Communication.Tcp
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Channels;
    using System.Threading.Tasks;

    using Mud.Common.Communication;

    /// <summary>
    /// TCP connection with buffered async reads, a serialized outbound send
    /// queue (no overlapping sends), slow-client backpressure, and idempotent
    /// disconnect handling.
    /// </summary>
    public sealed class TcpConnection : IConnection
    {
        private const int ReceiveBufferSize = 4096;

        private const int MaxCommandLength = MessageBuffer.DefaultMaxMessageLength;

        private const int IncomingQueueCapacity = 64;

        private const int OutgoingQueueCapacity = 256;

        private static readonly TimeSpan DisconnectFlushTimeout = TimeSpan.FromSeconds(2);

        private readonly Socket socket;

        private readonly Channel<string> incoming;

        private readonly Channel<string> outgoing;

        private readonly CancellationTokenSource cts;

        private int disconnectRequested;

        private Task sendLoop;

        public TcpConnection(Socket socket)
        {
            var remoteEndPoint = (IPEndPoint)socket.RemoteEndPoint;
            this.Id = Guid.NewGuid();
            this.Ip = remoteEndPoint.Address;
            this.socket = socket;
            this.cts = new CancellationTokenSource();

            this.incoming = Channel.CreateBounded<string>(new BoundedChannelOptions(IncomingQueueCapacity)
            {
                SingleReader = true,
                SingleWriter = true,
            });

            this.outgoing = Channel.CreateBounded<string>(new BoundedChannelOptions(OutgoingQueueCapacity)
            {
                SingleReader = true,
                SingleWriter = false,
            });
        }

        /// <summary>
        /// Raised exactly once, after the socket is fully closed.
        /// </summary>
        public event Action<TcpConnection> Closed;

        public Guid Id { get; }

        public IPAddress Ip { get; }

        public ChannelReader<string> IncomingMessages => this.incoming.Reader;

        public void Start()
        {
            this.sendLoop = Task.Run(this.SendLoopAsync);
            _ = Task.Run(this.ReceiveLoopAsync);
        }

        public ValueTask SendAsync(string message, CancellationToken cancellationToken = default)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (!this.outgoing.Writer.TryWrite(message))
            {
                // Backpressure policy: the outbound queue is full, which means the
                // client is not reading fast enough. Disconnect instead of buffering
                // without bound. (TryWrite also fails once the channel is completed,
                // in which case DisconnectAsync is a no-op.)
                return this.DisconnectAsync("You are receiving data too slowly.", cancellationToken);
            }

            return ValueTask.CompletedTask;
        }

        public ValueTask DisconnectAsync(string reason = null, CancellationToken cancellationToken = default)
        {
            if (Interlocked.Exchange(ref this.disconnectRequested, 1) != 0)
            {
                return ValueTask.CompletedTask;
            }

            if (!string.IsNullOrEmpty(reason))
            {
                this.outgoing.Writer.TryWrite($"\r\n{reason}\r\n");
            }

            // Completing the writers ends both loops: the send loop drains what is
            // already queued, the session sees the incoming channel complete.
            this.outgoing.Writer.TryComplete();
            this.incoming.Writer.TryComplete();

            _ = Task.Run(this.CloseAsync);
            return ValueTask.CompletedTask;
        }

        private async Task CloseAsync()
        {
            if (this.sendLoop != null)
            {
                // Give queued output a moment to flush, then cut off hung sends.
                await Task.WhenAny(this.sendLoop, Task.Delay(DisconnectFlushTimeout)).ConfigureAwait(false);
            }

            this.cts.Cancel();

            try
            {
                this.socket.Shutdown(SocketShutdown.Both);
            }
            catch (SocketException)
            {
            }
            catch (ObjectDisposedException)
            {
            }

            this.socket.Close();
            this.cts.Dispose();
            this.Closed?.Invoke(this);
        }

        private async Task ReceiveLoopAsync()
        {
            var buffer = new byte[ReceiveBufferSize];
            var messageBuffer = new MessageBuffer(MaxCommandLength);

            try
            {
                while (!this.cts.Token.IsCancellationRequested)
                {
                    int received = await this.socket.ReceiveAsync(buffer, SocketFlags.None, this.cts.Token).ConfigureAwait(false);
                    if (received == 0)
                    {
                        break;
                    }

                    IReadOnlyList<string> messages;
                    try
                    {
                        messages = messageBuffer.Append(buffer, received);
                    }
                    catch (MessageTooLongException)
                    {
                        await this.DisconnectAsync("Command too long.").ConfigureAwait(false);
                        return;
                    }

                    foreach (var message in messages)
                    {
                        // Bounded write: pauses reading from the socket when the
                        // session falls behind instead of queueing without limit.
                        await this.incoming.Writer.WriteAsync(message, this.cts.Token).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (ChannelClosedException)
            {
            }
            catch (SocketException)
            {
            }
            catch (ObjectDisposedException)
            {
            }

            await this.DisconnectAsync().ConfigureAwait(false);
        }

        private async Task SendLoopAsync()
        {
            try
            {
                await foreach (var message in this.outgoing.Reader.ReadAllAsync(this.cts.Token).ConfigureAwait(false))
                {
                    var data = Encoding.Latin1.GetBytes(message);
                    int sent = 0;
                    while (sent < data.Length)
                    {
                        sent += await this.socket.SendAsync(data.AsMemory(sent), SocketFlags.None, this.cts.Token).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (SocketException)
            {
            }
            catch (ObjectDisposedException)
            {
            }

            await this.DisconnectAsync().ConfigureAwait(false);
        }
    }
}
