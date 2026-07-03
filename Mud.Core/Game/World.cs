namespace Mud.Core.Game
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Authoritative game state: rooms, players, and who is where.
    /// All state is guarded by a single lock; message delivery through
    /// <see cref="IMessageTarget"/> is non-blocking, so holding the lock while
    /// broadcasting is safe.
    /// </summary>
    public sealed class World
    {
        private readonly object sync = new object();

        private readonly Dictionary<Guid, Room> rooms = new Dictionary<Guid, Room>();

        private readonly Dictionary<Guid, Player> players = new Dictionary<Guid, Player>();

        public Guid StartingRoomId { get; private set; }

        public void AddRoom(Room room, bool isStartingRoom = false)
        {
            lock (sync)
            {
                rooms.Add(room.Id, room);
                if (isStartingRoom || rooms.Count == 1)
                {
                    StartingRoomId = room.Id;
                }
            }
        }

        public bool TryAddPlayer(Player player, out string error)
        {
            lock (sync)
            {
                if (players.Values.Any(p => string.Equals(p.Name, player.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    error = $"The name '{player.Name}' is already taken.";
                    return false;
                }

                var room = rooms[StartingRoomId];
                BroadcastToRoomLocked(room, $"&Y{player.Name} has entered the world.", player.Id);

                players.Add(player.Id, player);
                player.RoomId = room.Id;
                room.PlayerIds.Add(player.Id);

                error = null;
                return true;
            }
        }

        public void RemovePlayer(Guid playerId)
        {
            lock (sync)
            {
                if (!players.Remove(playerId, out var player))
                {
                    return;
                }

                if (rooms.TryGetValue(player.RoomId, out var room))
                {
                    room.PlayerIds.Remove(playerId);
                    BroadcastToRoomLocked(room, $"&Y{player.Name} has left the world.");
                }
            }
        }

        public bool TryMovePlayer(Guid playerId, string direction, out string error)
        {
            lock (sync)
            {
                var player = players[playerId];
                var fromRoom = rooms[player.RoomId];

                if (!fromRoom.Exits.TryGetValue(direction, out var toRoomId) ||
                    !rooms.TryGetValue(toRoomId, out var toRoom))
                {
                    error = "You can't go that way.";
                    return false;
                }

                fromRoom.PlayerIds.Remove(playerId);
                BroadcastToRoomLocked(fromRoom, $"&y{player.Name} leaves {direction.ToLowerInvariant()}.");
                BroadcastToRoomLocked(toRoom, $"&y{player.Name} arrives.");
                toRoom.PlayerIds.Add(playerId);
                player.RoomId = toRoom.Id;

                error = null;
                return true;
            }
        }

        public string DescribeRoom(Guid playerId)
        {
            lock (sync)
            {
                var player = players[playerId];
                var room = rooms[player.RoomId];

                var sb = new StringBuilder();
                sb.Append($"&Y{room.Name}&W\r\n");
                sb.Append($"{room.Description}\r\n");

                var exits = room.Exits.Count > 0
                    ? string.Join(", ", room.Exits.Keys.OrderBy(e => e, StringComparer.OrdinalIgnoreCase))
                    : "none";
                sb.Append($"&gExits: {exits}&W");

                var others = room.PlayerIds
                    .Where(id => id != playerId)
                    .Select(id => players[id].Name)
                    .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                if (others.Length > 0)
                {
                    sb.Append($"\r\nAlso here: {string.Join(", ", others)}");
                }

                return sb.ToString();
            }
        }

        public void BroadcastToRoom(Guid roomId, string message, Guid exceptPlayerId = default)
        {
            lock (sync)
            {
                if (rooms.TryGetValue(roomId, out var room))
                {
                    BroadcastToRoomLocked(room, message, exceptPlayerId);
                }
            }
        }

        public void BroadcastGlobal(string message, Guid exceptPlayerId = default)
        {
            lock (sync)
            {
                foreach (var player in players.Values)
                {
                    if (player.Id != exceptPlayerId)
                    {
                        player.Output.SendMessage(message);
                    }
                }
            }
        }

        public bool TrySendToPlayer(string playerName, string message, out string actualName)
        {
            lock (sync)
            {
                var player = players.Values.FirstOrDefault(
                    p => string.Equals(p.Name, playerName, StringComparison.OrdinalIgnoreCase));

                if (player == null)
                {
                    actualName = null;
                    return false;
                }

                player.Output.SendMessage(message);
                actualName = player.Name;
                return true;
            }
        }

        public IReadOnlyList<string> GetPlayerNames()
        {
            lock (sync)
            {
                return players.Values
                    .Select(p => p.Name)
                    .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
        }

        private void BroadcastToRoomLocked(Room room, string message, Guid exceptPlayerId = default)
        {
            foreach (var playerId in room.PlayerIds)
            {
                if (playerId != exceptPlayerId)
                {
                    players[playerId].Output.SendMessage(message);
                }
            }
        }
    }
}
