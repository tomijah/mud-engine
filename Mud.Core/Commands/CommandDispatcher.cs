namespace Mud.Core.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Mud.Core.Commands.Handlers;
    using Mud.Core.Game;
    using Mud.Core.Session;

    /// <summary>
    /// Parses raw input into a verb plus arguments and dispatches to the
    /// matching command handler (exact match on name or alias, case-insensitive).
    /// </summary>
    public sealed class CommandDispatcher
    {
        private static readonly char[] Whitespace = { ' ', '\t' };

        private readonly Dictionary<string, ICommand> commands =
            new Dictionary<string, ICommand>(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyList<ICommand> Commands => commands.Values.Distinct().OrderBy(c => c.Name).ToArray();

        public static CommandDispatcher CreateDefault()
        {
            var dispatcher = new CommandDispatcher();

            dispatcher.Register(new LookCommand());
            dispatcher.Register(new SayCommand());
            dispatcher.Register(new GoCommand());
            dispatcher.Register(new WhoCommand());
            dispatcher.Register(new TellCommand());
            dispatcher.Register(new QuitCommand());
            dispatcher.Register(new HelpCommand());

            foreach (var direction in GoCommand.Directions)
            {
                dispatcher.Register(new DirectionCommand(direction));
            }

            return dispatcher;
        }

        public void Register(ICommand command)
        {
            commands.Add(command.Name, command);
            foreach (var alias in command.Aliases)
            {
                commands.Add(alias, command);
            }
        }

        public void Dispatch(Session session, Player player, World world, string input)
        {
            var trimmed = input?.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                return;
            }

            var parts = trimmed.Split(Whitespace, 2, StringSplitOptions.RemoveEmptyEntries);
            var verb = parts[0];
            var arguments = parts.Length > 1 ? parts[1].Trim() : string.Empty;

            if (!commands.TryGetValue(verb, out var command))
            {
                session.WriteToUser("Huh? Type 'help' for a list of commands.");
                return;
            }

            command.Execute(new CommandContext(session, player, world, arguments));
        }
    }
}
