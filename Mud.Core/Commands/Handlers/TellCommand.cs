namespace Mud.Core.Commands.Handlers
{
    using System;
    using System.Collections.Generic;

    public sealed class TellCommand : ICommand
    {
        private static readonly char[] Whitespace = { ' ', '\t' };

        public string Name => "tell";

        public IReadOnlyList<string> Aliases { get; } = new[] { "t" };

        public string Description => "Send a private message. Usage: tell <player> <message>";

        public void Execute(CommandContext context)
        {
            var parts = context.Arguments.Split(Whitespace, 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                context.Session.WriteToUser("Tell whom what? Usage: tell <player> <message>");
                return;
            }

            var targetName = parts[0];
            var message = parts[1].Trim();

            if (string.Equals(targetName, context.Player.Name, StringComparison.OrdinalIgnoreCase))
            {
                context.Session.WriteToUser("You mutter to yourself.");
                return;
            }

            if (context.World.TrySendToPlayer(targetName, $"&B{context.Player.Name} tells you: '{message}'", out var actualName))
            {
                context.Session.WriteToUser($"&BYou tell {actualName}: '{message}'");
            }
            else
            {
                context.Session.WriteToUser($"There is no player named '{targetName}'.");
            }
        }
    }
}
