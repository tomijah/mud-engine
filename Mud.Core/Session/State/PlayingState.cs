namespace Mud.Core.Session.State
{
    using Mud.Core.Game;

    /// <summary>
    /// The user has a character in the world; input is parsed and dispatched
    /// as game commands.
    /// </summary>
    public class PlayingState : SessionState
    {
        private readonly Player player;

        public PlayingState(Player player)
        {
            this.player = player;
        }

        public override void Init(Session session)
        {
            session.WriteToUser($"&GWelcome, {player.Name}! Type 'help' for a list of commands.", withPrompt: false);
            session.WriteToUser(session.Context.World.DescribeRoom(player.Id));
        }

        public override void HandleMessage(Session session, string input)
        {
            session.Context.Commands.Dispatch(session, player, session.Context.World, input);
        }

        public override void HandleDisconnection(Session session)
        {
            session.Context.World.RemovePlayer(player.Id);
        }

        public override string GetPrompt()
        {
            return "> ";
        }
    }
}
