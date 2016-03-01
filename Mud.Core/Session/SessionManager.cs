namespace Mud.Core.Session
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq;

    using Mud.Common.Communication;
    using Mud.Core.Session.State;

    public class SessionManager: IDisposable
    {
        private static SessionManager current;

        private readonly IConnectionManager connectionManager;

        private readonly ConcurrentDictionary<Guid, Session> sessions = new ConcurrentDictionary<Guid, Session>();

        public SessionManager(IConnectionManager connectionManager)
        {
            current = this;
            this.connectionManager = connectionManager;
            this.connectionManager.UserConnected += OnUserConnected;
            this.connectionManager.UserDisconnected += OnUserDisconnected;
            this.connectionManager.Start();
        }

        public static SessionManager Current => current;

        public Session GetSession(Guid id)
        {
            Session result;
            return sessions.TryGetValue(id, out result) ? result : null;
        }

        public void Broadcast(string message)
        {
            var all = sessions.Values.ToArray();
            foreach (var session in all)
            {
                session.WriteToUser(message);
            }
        }

        private void OnUserDisconnected(IConnection connection)
        {
            Session removed;
            if (sessions.TryRemove(connection.Id, out removed))
            {
                connection.MessageReceived -= OnMessageReceived;
                removed.HandleUserDisconnected();
            }
        }

        private void OnUserConnected(IConnection connection)
        {
            var session = new Session(connection);
            if (sessions.TryAdd(connection.Id, session))
            {
                connection.MessageReceived += OnMessageReceived;
                session.SetState(new ConnectedState());
            }
        }

        private void OnMessageReceived(IConnection connection, string message)
        {
            GetSession(connection.Id)?.HandleMessage(message);
        }

        public void Dispose()
        {
            var allSessions = sessions.Values.ToArray();
            foreach (var session in allSessions)
            {
                session.DisconnectUser("Server stopped");
            }

            connectionManager.Stop();
            connectionManager.UserConnected -= OnUserConnected;
            connectionManager.UserDisconnected -= OnUserDisconnected;
        }
    }
}