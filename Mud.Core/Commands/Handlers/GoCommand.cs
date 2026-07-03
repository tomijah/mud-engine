namespace Mud.Core.Commands.Handlers
{
    using System;
    using System.Collections.Generic;

    public sealed class GoCommand : ICommand
    {
        public static readonly IReadOnlyList<string> Directions = new[] { "north", "south", "east", "west" };

        private static readonly Dictionary<string, string> Shorthand =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["n"] = "north",
                ["s"] = "south",
                ["e"] = "east",
                ["w"] = "west",
            };

        public string Name => "go";

        public IReadOnlyList<string> Aliases { get; } = Array.Empty<string>();

        public string Description => "Move in a direction. Usage: go <direction>";

        public void Execute(CommandContext context)
        {
            if (context.Arguments.Length == 0)
            {
                context.Session.WriteToUser("Go where?");
                return;
            }

            Move(context, context.Arguments);
        }

        public static void Move(CommandContext context, string direction)
        {
            if (Shorthand.TryGetValue(direction, out var full))
            {
                direction = full;
            }

            if (context.World.TryMovePlayer(context.Player.Id, direction, out var error))
            {
                context.Session.WriteToUser(context.World.DescribeRoom(context.Player.Id));
            }
            else
            {
                context.Session.WriteToUser(error);
            }
        }
    }
}
