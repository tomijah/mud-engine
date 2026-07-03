namespace Mud.Core
{
    using System;

    using Mud.Core.Commands;
    using Mud.Core.Game;

    /// <summary>
    /// Shared game services handed to every session, replacing the old
    /// SessionManager.Current global singleton.
    /// </summary>
    public sealed class GameContext
    {
        public GameContext(World world, CommandDispatcher commands)
        {
            World = world ?? throw new ArgumentNullException(nameof(world));
            Commands = commands ?? throw new ArgumentNullException(nameof(commands));
        }

        public World World { get; }

        public CommandDispatcher Commands { get; }
    }
}
