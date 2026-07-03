namespace Mud.Core.Game
{
    using System;

    public sealed class Player
    {
        public Player(Guid id, string name, IMessageTarget output)
        {
            Id = id;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Output = output ?? throw new ArgumentNullException(nameof(output));
        }

        public Guid Id { get; }

        public string Name { get; }

        public IMessageTarget Output { get; }

        /// <summary>
        /// Current location. Only mutated by <see cref="World"/> under its lock.
        /// </summary>
        public Guid RoomId { get; internal set; }
    }
}
