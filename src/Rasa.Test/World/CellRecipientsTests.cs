extern alias RasaGame;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using ClientState = RasaGame::Rasa.Data.ClientState;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Structures;

    /// <summary>
    /// Who a broadcast around something goes to: the clients in the world in its cells, once each
    /// (CellManager.GetClientsInCells). It was a chain of LINQ; it is a loop now, and must find
    /// the same clients in the same order - checked here against the LINQ it replaced, over cells
    /// shared by clients, a cell named twice, cells the map does not have, and clients who are
    /// not to be sent anything: not in the world, on another map, dropped, or the one excluded.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class CellRecipientsTests
    {
        private const uint FirstSeed = 0x7F000000;

        [TestMethod]
        public void TheSameClientsInTheSameOrderAsBefore()
        {
            using var world = new WorldTestContext();
            var other = new MapChannel();
            var clients = Enumerable.Range(0, 40).Select(i => world.CreateClient(i, 0)).ToList();

            // Some who are not to be sent anything.
            clients[3].State = ClientState.Loading;
            clients[7].State = ClientState.CharacterSelection;
            clients[11].Player.MapChannel = other;
            clients[13].Player.Disconected = true;

            var random = new Random(5);

            for (var round = 0; round < 200; round++)
            {
                var cells = new uint[5, 5];

                for (var x = 0; x < 5; x++)
                    for (var z = 0; z < 5; z++)
                        cells[x, z] = FirstSeed + (uint)random.Next(0, 40);

                // Now and then a cell named twice, and one the map does not have.
                cells[4, 4] = cells[0, 0];
                if (round % 3 == 0)
                    cells[1, 1] = 0x7EFFFFFF;

                foreach (var seed in Enumerable.Range(0, 40).Select(i => FirstSeed + (uint)i))
                {
                    var cell = new MapCell { CellSeed = seed };

                    // Shared between cells, some listed twice, and a gap.
                    foreach (var client in clients.Where(_ => random.Next(0, 8) == 0))
                        cell.ClientList.Add(client);
                    if (random.Next(0, 10) == 0)
                        cell.ClientList.Add(null);
                    if (cell.ClientList.Count > 0 && random.Next(0, 5) == 0)
                        cell.ClientList.Add(cell.ClientList[0]);

                    world.Map.MapCellInfo.Cells[seed] = cell;
                }

                var excluded = round % 2 == 0 ? clients[random.Next(0, 40)] : null;

                var found = CellManager.Instance.GetClientsInCells(world.Map, cells, excluded);
                var expected = AsBefore(world.Map, cells, excluded);

                CollectionAssert.AreEqual(expected, found, $"round {round}");
            }
        }

        [TestMethod]
        public void ItTakesLessThanHalfWhatItDidToFindThem()
        {
            using var world = new WorldTestContext();
            var clients = Enumerable.Range(0, 100).Select(i => world.CreateClient(i, 0)).ToList();
            var cells = new uint[5, 5];

            for (var x = 0; x < 5; x++)
                for (var z = 0; z < 5; z++)
                {
                    var seed = FirstSeed + (uint)(x * 5 + z);
                    cells[x, z] = seed;
                    world.Map.MapCellInfo.Cells[seed] = new MapCell { CellSeed = seed, ClientList = clients.Skip(x * 20 + z * 4).Take(4).ToList() };
                }

            CellManager.Instance.GetClientsInCells(world.Map, cells);
            AsBefore(world.Map, cells, null);

            var before = GC.GetAllocatedBytesForCurrentThread();
            var found = CellManager.Instance.GetClientsInCells(world.Map, cells);
            var now = GC.GetAllocatedBytesForCurrentThread() - before;

            before = GC.GetAllocatedBytesForCurrentThread();
            AsBefore(world.Map, cells, null);
            var then = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.HasCount(100, found);
            Assert.IsLessThan(then / 2, now, $"{now} bytes, against {then}");
        }

        /// <summary>The lookup as it was.</summary>
        private static List<Client> AsBefore(MapChannel map, uint[,] cells, Client excluded) =>
            Cells(map, cells.Cast<uint>()).SelectMany(cell => cell.ClientList)
                .Where(client => client != null && client != excluded &&
                    client.State == ClientState.Ingame && client.Player?.MapChannel == map &&
                    !client.Player.Disconected)
                .Distinct().ToList();

        private static IEnumerable<MapCell> Cells(MapChannel map, IEnumerable<uint> seeds)
        {
            foreach (var seed in seeds.Distinct())
                if (map.MapCellInfo.Cells.TryGetValue(seed, out var cell))
                    yield return cell;
        }
    }
}
