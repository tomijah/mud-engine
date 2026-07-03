namespace Mud.Core.Commands.Handlers
{
    using System;
    using System.Collections.Generic;

    public sealed class QuitCommand : ICommand
    {
        public string Name => "quit";

        public IReadOnlyList<string> Aliases { get; } = Array.Empty<string>();

        public string Description => "Leave the game.";

        public void Execute(CommandContext context)
        {
            context.Session.DisconnectUser("See ya!");
        }
    }
}
