namespace Mud.Core.Commands
{
    using Mud.Core.Game;
    using Mud.Core.Session;

    public sealed class CommandContext
    {
        public CommandContext(Session session, Player player, World world, string arguments)
        {
            Session = session;
            Player = player;
            World = world;
            Arguments = arguments;
        }

        public Session Session { get; }

        public Player Player { get; }

        public World World { get; }

        /// <summary>
        /// Everything after the command verb, trimmed. Empty when the command
        /// was given without arguments.
        /// </summary>
        public string Arguments { get; }
    }
}
