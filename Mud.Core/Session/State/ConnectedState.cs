using System;

namespace Mud.Core.Session.State
{
    public class ConnectedState : SessionStateBase
    {
        public override void HandleMessage(Session session, string input)
        {
            if (input.Contains("exit"))
            {
                session.DisconnectUser();
                return;
            }
        }

        public override void HandleDisconnection(Session session)
        {
            SessionManager.Current.Broadcast("Disconnected: " + session.Id);
        }

        public override void Init(Session session)
        {
            session.WriteToUser("&RWelcome! " + session.Id);
            SessionManager.Current.Broadcast("Connected: " + session.Id);
        }

        public override string GetPrompt()
        {
            return ">";
        }
    }
}