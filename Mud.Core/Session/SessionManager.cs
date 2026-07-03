namespace Mud.Core.Session
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading.Tasks;

    using Mud.Common.Communication;

    public class SessionManager : IDisposable
    {
        private readonly IConnectionManager connectionManager;

        private readonly GameContext context;

        private readonly ConcurrentDictionary<Guid, Session> sessions = new ConcurrentDictionary<Guid, Session>();

        public SessionManager(IConnectionManager connectionManager, GameContext context)
        {
            this.connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
            this.context = context ?? throw new ArgumentNullException(nameof(context));
            this.connectionManager.ConnectionAccepted += OnConnectionAccepted;
        }

        public void Start()
        {
            connectionManager.Start();
        }

        /// <summary>
        /// Server-wide announcement to every session, including those not yet
        /// in the world (e.g. still at the name prompt).
        /// </summary>
        public void Broadcast(string message)
        {
            foreach (var session in sessions.Values)
            {
                session.SendMessage(message);
            }
        }

        public void Dispose()
        {
            connectionManager.ConnectionAccepted -= OnConnectionAccepted;

            foreach (var session in sessions.Values)
            {
                session.DisconnectUser("Server stopped");
            }

            connectionManager.Stop();
        }

        private void OnConnectionAccepted(IConnection connection)
        {
            var session = new Session(connection, context);
            if (sessions.TryAdd(session.Id, session))
            {
                _ = RunSessionAsync(session);
            }
        }

        private async Task RunSessionAsync(Session session)
        {
            try
            {
                await session.RunAsync().ConfigureAwait(false);
            }
            catch (Exception)
            {
                // TODO: log; a crashed session must not take the server down.
            }
            finally
            {
                sessions.TryRemove(session.Id, out _);
            }
        }
    }
}
