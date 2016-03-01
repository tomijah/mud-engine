namespace Mud.Core.Session
{
    using System;

    using Mud.Common.Communication;
    using Mud.Core.Ascii;

    public class Session
    {
        private readonly IConnection connection;

        private SessionStateBase currentState;

        public Session(IConnection connection)
        {
            this.connection = connection;
        }

        public Guid Id => connection.Id;

        public void HandleMessage(string message)
        {
            currentState.HandleMessage(this, message);
        }

        public void HandleUserDisconnected()
        {
            currentState.HandleDisconnection(this);
        }

        public void WriteToUser(string message)
        {
            connection.Send(AsciiOutputParser.Parse($"{message}&W\n"));
        }

        public void SetState(SessionStateBase state)
        {
            currentState = state;
            currentState.Init(this);
        }

        public void DisconnectUser(string message = "See ya!")
        {
            WriteToUser(message);
            connection.Disconnect();
        }
    }
}