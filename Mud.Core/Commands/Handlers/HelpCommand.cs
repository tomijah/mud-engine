namespace Mud.Core.Commands.Handlers
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public sealed class HelpCommand : ICommand
    {
        public string Name => "help";

        public IReadOnlyList<string> Aliases { get; } = Array.Empty<string>();

        public string Description => "Show this list of commands.";

        public void Execute(CommandContext context)
        {
            var sb = new StringBuilder("&YAvailable commands:&W");
            foreach (var command in context.Session.Context.Commands.Commands)
            {
                var aliases = command.Aliases.Count > 0
                    ? $" ({string.Join(", ", command.Aliases)})"
                    : string.Empty;
                sb.Append($"\r\n  &G{command.Name}{aliases}&W - {command.Description}");
            }

            context.Session.WriteToUser(sb.ToString());
        }
    }
}
