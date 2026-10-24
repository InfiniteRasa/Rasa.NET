extern alias RasaGame;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using ClientState = RasaGame::Rasa.Data.ClientState;
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Models;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Structures;

    /// <summary>
    /// The broadcasts around a creature, an object, a place and a player gather their clients
    /// without allocating - no matrix made, no cells made, no list or iterator a broadcast - and
    /// must reach the same clients as many times as before. Each is checked here against the code
    /// it replaced, over cells shared by clients, clients listed twice, cells the map has not got,
    /// and clients who are loading, on another map or dropped. And the player's own broadcasts
    /// (Client.CellCallMethod, CellIgnoreSelfCallMethod) now reach a client on two of their
    /// cells' lists once, and everyone else as before.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class BroadcastRecipientsTests
    {
        private const int Rounds = 60;

        private static readonly PerformRecoveryPacket Packet = new(PerformType.TwoArgs, ActionId.WeaponPolaritygun, 1);

        /// <summary>A crowd in all the states a cell's list can hold.</summary>
        private static List<Client> Crowd(WorldTestContext world)
        {
            var other = new MapChannel { MapInfo = world.Map.MapInfo, ClientList = new List<Client>() };
            var clients = Enumerable.Range(0, 30).Select(i => world.CreateClient(i, 0)).ToList();

            clients[3].State = ClientState.Loading;
            clients[7].State = ClientState.CharacterSelection;
            clients[11].Player.MapChannel = other;
            clients[13].Player.Disconected = true;

            return clients;
        }

        /// <summary>
        /// The cells around a cell position filled at random: some not there, some shared, some
        /// listing a client twice. The seed of the centre cell comes back.
        /// </summary>
        private static void Fill(MapChannel map, uint x, uint z, List<Client> clients, Random random)
        {
            map.MapCellInfo.Cells.Clear();

            for (var dx = -3; dx <= 3; dx++)
                for (var dz = -3; dz <= 3; dz++)
                {
                    if (random.Next(0, 6) == 0)
                        continue;

                    var cell = CellManager.Instance.GetCell(map, (uint)(x + dx), (uint)(z + dz));

                    foreach (var client in clients.Where(_ => random.Next(0, 10) == 0))
                        cell.ClientList.Add(client);

                    if (cell.ClientList.Count > 0 && random.Next(0, 4) == 0)
                        cell.ClientList.Add(cell.ClientList[0]);
                }
        }

        /// <summary>A matrix of seeds around a cell, without making a cell, now and then naming one twice.</summary>
        private static uint[,] Matrix(uint x, uint z, Random random)
        {
            var cells = new uint[5, 5];

            for (var i = 0; i < 5; i++)
                for (var j = 0; j < 5; j++)
                    cells[i, j] = ((uint)(x - 2 + i) & 0xFFFF) | ((uint)(z - 2 + j) << 16);

            if (random.Next(0, 3) == 0)
                cells[4, 4] = cells[0, 0];

            return cells;
        }

        /// <summary>How many messages each client was sent, and the queues emptied.</summary>
        private static int[] Received(List<Client> clients) =>
            clients.Select(client => WorldTestContext.Drain(client).Count).ToArray();

        private static int[] Tally(List<Client> clients, IEnumerable<Client> recipients)
        {
            var counts = new int[clients.Count];

            foreach (var recipient in recipients)
                counts[clients.IndexOf(recipient)]++;

            return counts;
        }

        /// <summary>Every entry of every list of the cells named that the map has, as CellsIn walked them.</summary>
        private static IEnumerable<Client> EveryEntry(MapChannel map, uint[,] cells)
        {
            foreach (var seed in cells)
                if (map.MapCellInfo.Cells.TryGetValue(seed, out var cell))
                    foreach (var client in cell.ClientList)
                        yield return client;
        }

        [TestMethod]
        public void ACreaturesBroadcastsReachTheSameClients()
        {
            using var world = new WorldTestContext();
            var clients = Crowd(world);
            var random = new Random(3);
            var creature = new Creature { Name = "Fixture", RuntimeMapChannel = world.Map };

            for (var round = 0; round < Rounds; round++)
            {
                var x = 32768u + (uint)random.Next(-5, 5);
                Fill(world.Map, x, x, clients, random);
                creature.Cells = Matrix(x, x, random);

                var expected = Tally(clients, EveryEntry(world.Map, creature.Cells));

                CellManager.Instance.CellCallMethod(creature, Packet);
                CollectionAssert.AreEqual(expected, Received(clients), $"call, round {round}");

                CellManager.Instance.CellMoveObject(creature, new Movement(Vector3.Zero, Vector2.Zero));
                CollectionAssert.AreEqual(expected, Received(clients), $"move, round {round}");
            }
        }

        [TestMethod]
        public void APlacesBroadcastReachesTheSameClientsAndMakesNoCells()
        {
            using var world = new WorldTestContext();
            var clients = Crowd(world);
            var random = new Random(5);

            for (var round = 0; round < Rounds; round++)
            {
                var position = new Vector3(random.Next(-200, 200), 0, random.Next(-200, 200));
                var x = (uint)(position.X / CellManager.CellSize + CellManager.CellBias);
                var z = (uint)(position.Z / CellManager.CellSize + CellManager.CellBias);
                Fill(world.Map, x, z, clients, random);

                var cells = world.Map.MapCellInfo.Cells.Count;
                CellManager.Instance.CellCallMethod(world.Map, position, 77, Packet);
                var received = Received(clients);

                Assert.AreEqual(cells, world.Map.MapCellInfo.Cells.Count, "no cell made");

                // As it was: the matrix made, and every entry of its cells.
                var expected = Tally(clients, EveryEntry(world.Map, CellManager.Instance.CreateCellMatrix(world.Map, x, z)).ToList());
                CollectionAssert.AreEqual(expected, received, $"round {round}");
            }
        }

        [TestMethod]
        public void AnObjectsBroadcastReachesTheSameClientsAndMakesNoCells()
        {
            using var world = new WorldTestContext();
            var clients = Crowd(world);
            var random = new Random(7);
            var obj = new DynamicObject();

            for (var round = 0; round < Rounds; round++)
            {
                obj.Position = new Vector3(random.Next(-200, 200), 0, random.Next(-200, 200));
                Assert.IsTrue(CellManager.TryGetCellCoordinates(obj.CellPosition, out var x, out var z));
                Fill(world.Map, x, z, clients, random);

                var cells = world.Map.MapCellInfo.Cells.Count;
                var seeing = CellManager.Instance.ClientsSeeing(world.Map, obj);
                CellManager.Instance.CellCallMethod(world.Map, obj, Packet);
                var received = Received(clients);

                Assert.AreEqual(cells, world.Map.MapCellInfo.Cells.Count, "no cell made");

                // As it was: GetClientsInCells over the matrix made.
                var before = CellManager.Instance.GetClientsInCells(world.Map, CellManager.Instance.CreateCellMatrix(world.Map, x, z));
                CollectionAssert.AreEqual(before, seeing, $"seeing, round {round}");
                CollectionAssert.AreEqual(Tally(clients, before), received, $"call, round {round}");
            }
        }

        [TestMethod]
        public void AnActorsBroadcastReachesTheSameClients()
        {
            using var world = new WorldTestContext();
            var clients = Crowd(world);
            var random = new Random(9);
            var origin = clients[0].Player;

            for (var round = 0; round < Rounds; round++)
            {
                var x = 32768u + (uint)random.Next(-5, 5);
                Fill(world.Map, x, x, clients, random);
                origin.Cells = Matrix(x, x, random);

                var expected = Tally(clients, CellManager.Instance.GetClientsInCells(world.Map, origin.Cells));

                CellManager.Instance.CellCallMethod(world.Map, origin, Packet);
                CollectionAssert.AreEqual(expected, Received(clients), $"round {round}");
            }
        }

        [TestMethod]
        public void APlayersOwnBroadcastsReachEachClientOnTheirCellsOnce()
        {
            using var world = new WorldTestContext();
            var clients = Crowd(world);
            var random = new Random(11);
            var self = clients[0];
            var twice = 0;

            for (var round = 0; round < Rounds; round++)
            {
                var x = 32768u + (uint)random.Next(-5, 5);
                Fill(world.Map, x, x, clients, random);
                self.Player.Cells = Matrix(x, x, random);

                // Before: every entry, so twice for a client on two lists. Now: once each.
                var entries = EveryEntry(world.Map, self.Player.Cells).ToList();
                if (entries.Count != entries.Distinct().Count())
                    twice++;
                var once = Tally(clients, entries.Distinct());
                var withoutSelf = Tally(clients, entries.Distinct().Where(client => client != self));

                self.CellCallMethod(self, self.Player.EntityId, Packet);
                CollectionAssert.AreEqual(once, Received(clients), $"call, round {round}");

                self.CellIgnoreSelfCallMethod(self, Packet);
                CollectionAssert.AreEqual(withoutSelf, Received(clients), $"ignoring self, round {round}");

                // The same clients as before, loading, elsewhere or dropped ones among them.
                CollectionAssert.AreEquivalent(entries.Distinct().ToList(),
                    clients.Where((client, i) => once[i] > 0).ToList());
            }

            // And some rounds had a client on two lists, who was sent everything twice before.
            Assert.IsGreaterThan(10, twice);
        }

        [TestMethod]
        public void TheBroadcastsAllocateNothingOfTheirOwn()
        {
            using var world = new WorldTestContext();
            var clients = Enumerable.Range(0, 10).Select(i => PlayerDeathTests.Player(world, i, i)).ToList();
            var creature = new Creature { Name = "Fixture", RuntimeMapChannel = world.Map, Cells = (uint[,])clients[0].Player.Cells.Clone() };
            var obj = new DynamicObject { Position = clients[0].Player.Position };
            var self = clients[0];

            var broadcasts = new (string Name, Action Send)[]
            {
                ("creature", () => CellManager.Instance.CellCallMethod(creature, Packet)),
                ("actor", () => CellManager.Instance.CellCallMethod(world.Map, self.Player, Packet)),
                ("object", () => CellManager.Instance.CellCallMethod(world.Map, obj, Packet)),
                ("place", () => CellManager.Instance.CellCallMethod(world.Map, obj.Position, 77, Packet)),
                ("own", () => self.CellCallMethod(self, self.Player.EntityId, Packet)),
                ("others", () => self.CellIgnoreSelfCallMethod(self, Packet))
            };

            foreach (var (name, send) in broadcasts)
            {
                send();
                var reached = Received(clients).Sum();

                var before = GC.GetAllocatedBytesForCurrentThread();
                send();
                var bytes = GC.GetAllocatedBytesForCurrentThread() - before;
                Received(clients);

                // What is left is what each recipient is handed: its ProtocolPacket and its place in the queue.
                var perRecipient = (double)bytes / reached;
                Assert.IsLessThanOrEqualTo(48.0, perRecipient, $"{name}: {bytes} B to {reached}");
            }
        }
    }
}
