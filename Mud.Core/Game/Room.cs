namespace Mud.Core.Game
{
    using System;
    using System.Collections.Generic;

    public sealed class Room
    {
        public Guid Id { get; init; } = Guid.NewGuid();

        public string Name { get; init; }

        public string Description { get; init; }

        /// <summary>
        /// Direction name (e.g. "north") to destination room id.
        /// </summary>
        public Dictionary<string, Guid> Exits { get; } = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

        public HashSet<Guid> PlayerIds { get; } = new HashSet<Guid>();
    }
}
