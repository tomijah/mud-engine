namespace Mud.Core.Session
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using Mud.Common.Communication;
    using Mud.Core.Ascii;
    using Mud.Core.Session.State;

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

        public void DisconnectUser()
        {
            this.WriteToUser("See ya!");
            connection.Disconnect();
        }
    }
}