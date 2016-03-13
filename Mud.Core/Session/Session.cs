namespace Mud.Core.Session
{
    using System;

    using Mud.Common.Communication;
    using Mud.Core.Ascii;

    public class Session
    {
        private readonly IConnection connection;

        private SessionState currentState;

        private bool userAtPrompt;

        public Session(IConnection connection)
        {
            this.connection = connection;
        }

        public Guid Id => connection.Id;

        public void HandleMessage(string message)
        {
            userAtPrompt = false;
            currentState.HandleMessage(this, message);
            if (!userAtPrompt)
            {
                connection.Send(AsciiOutputParser.Parse($"{currentState.GetPrompt()}&W"));
                userAtPrompt = true;
            }
        }

        public void HandleUserDisconnected()
        {
            currentState.HandleDisconnection(this);
        }

        public void WriteToUser(string message, bool withPrompt = true)
        {
            if (withPrompt)
            {
                connection.Send(AsciiOutputParser.Parse($"\n{message}&W{Environment.NewLine}{currentState.GetPrompt()}&W"));
            }
            else
            {
                connection.Send(AsciiOutputParser.Parse($"\n{message}&W"));
            }

            userAtPrompt = withPrompt;
        }

        public void SetState(SessionState state)
        {
            currentState = state;
            currentState.Init(this);
        }

        public void DisconnectUser(string message = "See ya!")
        {
            WriteToUser(message, false);
            connection.Disconnect();
        }
    }
}