namespace Mud.Core.Session.State
{
    using System.Linq;

    using Mud.Core.Game;

    /// <summary>
    /// Initial state: greets the user and asks for a character name before
    /// entering the world.
    /// </summary>
    public class ConnectedState : SessionState
    {
        private const int MinNameLength = 2;

        private const int MaxNameLength = 16;

        public override void Init(Session session)
        {
            session.WriteToUser("&RWelcome to the MUD!&W\r\nWhat is your name?");
        }

        public override void HandleMessage(Session session, string input)
        {
            var name = input.Trim();

            if (name.Length < MinNameLength || name.Length > MaxNameLength || !name.All(char.IsLetter))
            {
                session.WriteToUser($"Names must be {MinNameLength}-{MaxNameLength} letters. What is your name?");
                return;
            }

            name = char.ToUpperInvariant(name[0]) + name.Substring(1).ToLowerInvariant();

            var player = new Player(session.Id, name, session);
            if (!session.Context.World.TryAddPlayer(player, out var error))
            {
                session.WriteToUser($"{error} What is your name?");
                return;
            }

            session.SetState(new PlayingState(player));
        }

        public override void HandleDisconnection(Session session)
        {
            // Not in the world yet; nothing to clean up.
        }

        public override string GetPrompt()
        {
            return "Name: ";
        }
    }
}
