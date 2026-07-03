namespace Mud.Core.Commands.Handlers
{
    using System;
    using System.Collections.Generic;

    public sealed class WhoCommand : ICommand
    {
        public string Name => "who";

        public IReadOnlyList<string> Aliases { get; } = Array.Empty<string>();

        public string Description => "List everyone in the world.";

        public void Execute(CommandContext context)
        {
            var names = context.World.GetPlayerNames();
            var list = string.Join("\r\n", names);
            context.Session.WriteToUser($"&YPlayers online ({names.Count}):&W\r\n{list}");
        }
    }
}
