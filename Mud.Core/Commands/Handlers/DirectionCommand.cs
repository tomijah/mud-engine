namespace Mud.Core.Commands.Handlers
{
    using System.Collections.Generic;

    /// <summary>
    /// Bare direction shortcut, e.g. "north"/"n" as an alias for "go north".
    /// </summary>
    public sealed class DirectionCommand : ICommand
    {
        private readonly string direction;

        public DirectionCommand(string direction)
        {
            this.direction = direction;
        }

        public string Name => direction;

        public IReadOnlyList<string> Aliases => new[] { direction.Substring(0, 1) };

        public string Description => $"Move {direction}.";

        public void Execute(CommandContext context)
        {
            GoCommand.Move(context, direction);
        }
    }
}
