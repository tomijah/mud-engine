namespace Mud.Core.Session
{
    public abstract class SessionStateBase
    {
        public abstract void HandleMessage(Session session, string input);

        public abstract void HandleDisconnection(Session session);

        public abstract void Init(Session session);

        public abstract string GetPrompt();
    }
}