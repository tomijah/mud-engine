namespace Mud.Core.Commands.Handlers
{
    using System.Collections.Generic;

    public sealed class LookCommand : ICommand
    {
        public string Name => "look";

        public IReadOnlyList<string> Aliases { get; } = new[] { "l" };

        public string Description => "Look around the room you are in.";

        public void Execute(CommandContext context)
        {
            context.Session.WriteToUser(context.World.DescribeRoom(context.Player.Id));
        }
    }
}
