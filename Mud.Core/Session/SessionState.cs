namespace Mud.Core.Session
{
    public abstract class SessionState
    {
        public abstract void HandleMessage(Session session, string input);

        public abstract void HandleDisconnection(Session session);

        public abstract void Init(Session session);

        public virtual string GetPrompt()
        {
            return ">";
        }
    }
}