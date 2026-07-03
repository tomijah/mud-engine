namespace Mud.Core.Commands.Handlers
{
    using System.Collections.Generic;

    public sealed class SayCommand : ICommand
    {
        public string Name => "say";

        public IReadOnlyList<string> Aliases { get; } = new[] { "'" };

        public string Description => "Say something to everyone in the room. Usage: say <message>";

        public void Execute(CommandContext context)
        {
            if (context.Arguments.Length == 0)
            {
                context.Session.WriteToUser("Say what?");
                return;
            }

            context.World.BroadcastToRoom(
                context.Player.RoomId,
                $"&G{context.Player.Name} says: '{context.Arguments}'",
                exceptPlayerId: context.Player.Id);

            context.Session.WriteToUser($"&GYou say: '{context.Arguments}'");
        }
    }
}
