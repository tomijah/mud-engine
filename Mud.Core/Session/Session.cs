namespace Mud.Core.Session
{
    using System;
    using System.Threading.Channels;
    using System.Threading.Tasks;

    using Mud.Common.Communication;
    using Mud.Core.Ascii;
    using Mud.Core.Game;
    using Mud.Core.Session.State;

    /// <summary>
    /// One session per connection. All session state is owned by a single
    /// event loop (<see cref="RunAsync"/>); other threads interact with the
    /// session only by enqueueing events, so input handling, broadcasts,
    /// prompt rendering, and disconnects never race.
    /// </summary>
    public class Session : IMessageTarget
    {
        private readonly IConnection connection;

        private readonly Channel<SessionEvent> events;

        private SessionState currentState;

        private bool userAtPrompt;

        private bool closing;

        // Set by DisconnectUser (possibly from another thread) so the loop can
        // suppress the prompt while a disconnect event is still queued.
        private volatile bool disconnectPending;

        public Session(IConnection connection, GameContext context)
        {
            this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
            Context = context ?? throw new ArgumentNullException(nameof(context));
            events = Channel.CreateUnbounded<SessionEvent>(new UnboundedChannelOptions
            {
                SingleReader = true,
            });
        }

        public Guid Id => connection.Id;

        public GameContext Context { get; }

        /// <summary>
        /// Runs the session until the connection closes. This is the only
        /// place session state is touched.
        /// </summary>
        public async Task RunAsync()
        {
            SetState(new ConnectedState());

            var pump = Task.Run(PumpIncomingAsync);

            await foreach (var evt in events.Reader.ReadAllAsync())
            {
                await ProcessAsync(evt).ConfigureAwait(false);
            }

            await pump.ConfigureAwait(false);
        }

        /// <summary>
        /// Thread-safe: enqueues a message (e.g. a broadcast) for delivery by
        /// the session loop. Never blocks.
        /// </summary>
        public void SendMessage(string message)
        {
            events.Writer.TryWrite(new SessionEvent.OutputRequested(message, withPrompt: true));
        }

        /// <summary>
        /// Thread-safe: asks the session loop to disconnect the user.
        /// </summary>
        public void DisconnectUser(string message = "See ya!")
        {
            disconnectPending = true;
            events.Writer.TryWrite(new SessionEvent.DisconnectRequested(message));
        }

        /// <summary>
        /// Writes directly to the connection. Only call from the session loop
        /// (i.e. from <see cref="SessionState"/> handlers).
        /// </summary>
        public void WriteToUser(string message, bool withPrompt = true)
        {
            if (withPrompt)
            {
                Send($"\r\n{message}&W\r\n{currentState.GetPrompt()}&W");
            }
            else
            {
                Send($"\r\n{message}&W");
            }

            userAtPrompt = withPrompt;
        }

        /// <summary>
        /// Only call from the session loop.
        /// </summary>
        public void SetState(SessionState state)
        {
            currentState = state;
            currentState.Init(this);
        }

        private async Task PumpIncomingAsync()
        {
            try
            {
                await foreach (var message in connection.IncomingMessages.ReadAllAsync().ConfigureAwait(false))
                {
                    events.Writer.TryWrite(new SessionEvent.InputReceived(message));
                }
            }
            finally
            {
                events.Writer.TryWrite(new SessionEvent.ConnectionClosed());
                events.Writer.TryComplete();
            }
        }

        private async ValueTask ProcessAsync(SessionEvent evt)
        {
            switch (evt)
            {
                case SessionEvent.InputReceived input:
                    if (closing)
                    {
                        break;
                    }

                    userAtPrompt = false;
                    currentState.HandleMessage(this, input.Input);
                    if (!userAtPrompt && !closing && !disconnectPending)
                    {
                        Send($"{currentState.GetPrompt()}&W");
                        userAtPrompt = true;
                    }

                    break;

                case SessionEvent.OutputRequested output:
                    if (!closing)
                    {
                        WriteToUser(output.Message, output.WithPrompt);
                    }

                    break;

                case SessionEvent.DisconnectRequested disconnect:
                    if (!closing)
                    {
                        closing = true;
                        await connection.DisconnectAsync(AsciiOutputParser.Parse($"{disconnect.Reason}&W")).ConfigureAwait(false);
                    }

                    break;

                case SessionEvent.ConnectionClosed:
                    closing = true;
                    currentState.HandleDisconnection(this);
                    break;
            }
        }

        private void Send(string message)
        {
            _ = connection.SendAsync(AsciiOutputParser.Parse(message));
        }
    }
}
