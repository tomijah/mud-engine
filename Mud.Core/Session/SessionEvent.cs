namespace Mud.Core.Session
{
    /// <summary>
    /// Events processed one at a time by the session loop. Everything that
    /// touches session state (input, broadcasts, disconnects) goes through
    /// these, so no locking is needed inside the session.
    /// </summary>
    public abstract class SessionEvent
    {
        public sealed class InputReceived : SessionEvent
        {
            public InputReceived(string input)
            {
                Input = input;
            }

            public string Input { get; }
        }

        public sealed class OutputRequested : SessionEvent
        {
            public OutputRequested(string message, bool withPrompt)
            {
                Message = message;
                WithPrompt = withPrompt;
            }

            public string Message { get; }

            public bool WithPrompt { get; }
        }

        public sealed class DisconnectRequested : SessionEvent
        {
            public DisconnectRequested(string reason)
            {
                Reason = reason;
            }

            public string Reason { get; }
        }

        public sealed class ConnectionClosed : SessionEvent
        {
        }
    }
}
