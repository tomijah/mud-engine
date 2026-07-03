namespace Mud.Common.Communication
{
    using System;
    using System.Net;
    using System.Threading;
    using System.Threading.Channels;
    using System.Threading.Tasks;

    public interface IConnection
    {
        Guid Id { get; }

        IPAddress Ip { get; }

        /// <summary>
        /// Complete commands (one per line) read from the client.
        /// The channel is completed when the connection closes.
        /// </summary>
        ChannelReader<string> IncomingMessages { get; }

        /// <summary>
        /// Enqueues a message on the connection's outbound send queue.
        /// Never blocks the caller; slow clients that fill the queue are disconnected.
        /// </summary>
        ValueTask SendAsync(string message, CancellationToken cancellationToken = default);

        /// <summary>
        /// Closes the connection, flushing queued output first. Safe to call multiple times.
        /// </summary>
        ValueTask DisconnectAsync(string reason = null, CancellationToken cancellationToken = default);
    }
}
