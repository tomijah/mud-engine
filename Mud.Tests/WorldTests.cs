namespace Mud.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Mud.Core.Game;

    [TestClass]
    public class WorldTests
    {
        private World world;

        private Room start;

        private Room north;

        [TestInitialize]
        public void Setup()
        {
            world = new World();
            start = new Room { Name = "Start", Description = "Start room." };
            north = new Room { Name = "North", Description = "North room." };
            start.Exits["north"] = north.Id;
            north.Exits["south"] = start.Id;
            world.AddRoom(start, isStartingRoom: true);
            world.AddRoom(north);
        }

        [TestMethod]
        public void TryAddPlayer_PlacesPlayerInStartingRoom()
        {
            var player = NewPlayer("Alice");

            Assert.IsTrue(world.TryAddPlayer(player, out _));
            Assert.AreEqual(start.Id, player.RoomId);
            CollectionAssert.Contains(world.GetPlayerNames().ToArray(), "Alice");
        }

        [TestMethod]
        public void TryAddPlayer_RejectsDuplicateNameIgnoringCase()
        {
            Assert.IsTrue(world.TryAddPlayer(NewPlayer("Alice"), out _));
            Assert.IsFalse(world.TryAddPlayer(NewPlayer("alice"), out var error));
            Assert.IsNotNull(error);
        }

        [TestMethod]
        public void TryMovePlayer_MovesThroughExit()
        {
            var player = NewPlayer("Alice");
            world.TryAddPlayer(player, out _);

            Assert.IsTrue(world.TryMovePlayer(player.Id, "north", out _));
            Assert.AreEqual(north.Id, player.RoomId);
        }

        [TestMethod]
        public void TryMovePlayer_FailsWithoutExit()
        {
            var player = NewPlayer("Alice");
            world.TryAddPlayer(player, out _);

            Assert.IsFalse(world.TryMovePlayer(player.Id, "west", out var error));
            Assert.AreEqual(start.Id, player.RoomId);
            Assert.IsNotNull(error);
        }

        [TestMethod]
        public void BroadcastToRoom_ReachesOnlyPlayersInThatRoom()
        {
            var alice = NewPlayer("Alice", out var aliceOutput);
            var bob = NewPlayer("Bob", out var bobOutput);
            world.TryAddPlayer(alice, out _);
            world.TryAddPlayer(bob, out _);
            world.TryMovePlayer(bob.Id, "north", out _);
            aliceOutput.Clear();
            bobOutput.Clear();

            world.BroadcastToRoom(start.Id, "hello");

            Assert.AreEqual(1, aliceOutput.Count);
            Assert.AreEqual(0, bobOutput.Count);
        }

        [TestMethod]
        public void BroadcastToRoom_ExcludesSender()
        {
            var alice = NewPlayer("Alice", out var aliceOutput);
            var bob = NewPlayer("Bob", out var bobOutput);
            world.TryAddPlayer(alice, out _);
            world.TryAddPlayer(bob, out _);
            aliceOutput.Clear();
            bobOutput.Clear();

            world.BroadcastToRoom(start.Id, "hello", exceptPlayerId: alice.Id);

            Assert.AreEqual(0, aliceOutput.Count);
            Assert.AreEqual(1, bobOutput.Count);
        }

        [TestMethod]
        public void TryMovePlayer_NotifiesBothRooms()
        {
            var alice = NewPlayer("Alice");
            var bob = NewPlayer("Bob", out var bobOutput);
            var carol = NewPlayer("Carol", out var carolOutput);
            world.TryAddPlayer(alice, out _);
            world.TryAddPlayer(bob, out _);
            world.TryAddPlayer(carol, out _);
            world.TryMovePlayer(carol.Id, "north", out _);
            bobOutput.Clear();
            carolOutput.Clear();

            world.TryMovePlayer(alice.Id, "north", out _);

            Assert.IsTrue(bobOutput.Single().Contains("leaves north"));
            Assert.IsTrue(carolOutput.Single().Contains("arrives"));
        }

        [TestMethod]
        public void RemovePlayer_TakesPlayerOutOfWorld()
        {
            var player = NewPlayer("Alice");
            world.TryAddPlayer(player, out _);

            world.RemovePlayer(player.Id);

            Assert.AreEqual(0, world.GetPlayerNames().Count);
        }

        [TestMethod]
        public void TrySendToPlayer_FindsPlayerIgnoringCase()
        {
            var player = NewPlayer("Alice", out var output);
            world.TryAddPlayer(player, out _);
            output.Clear();

            Assert.IsTrue(world.TrySendToPlayer("ALICE", "psst", out var actualName));
            Assert.AreEqual("Alice", actualName);
            Assert.AreEqual("psst", output.Single());
        }

        private static Player NewPlayer(string name)
        {
            return NewPlayer(name, out _);
        }

        private static Player NewPlayer(string name, out List<string> output)
        {
            var sink = new RecordingTarget();
            output = sink.Messages;
            return new Player(Guid.NewGuid(), name, sink);
        }

        private sealed class RecordingTarget : IMessageTarget
        {
            public List<string> Messages { get; } = new List<string>();

            public void SendMessage(string message)
            {
                Messages.Add(message);
            }
        }
    }
}
