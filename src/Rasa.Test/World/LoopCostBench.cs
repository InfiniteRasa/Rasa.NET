extern alias RasaGame;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Models;
    using Rasa.Managers;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Structures;

    /// <summary>
    /// What the world loop's per-pass work costs, on a synthetic world: the numbers lane-loop's
    /// changes are measured by, before and after. Not part of the suite: each one returns at once
    /// unless RASA_BENCH is set (RASA_BENCH=1 dotnet test --filter TestCategory=Bench), and prints
    /// its numbers to the test's output.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class LoopCostBench
    {
        public TestContext TestContext { get; set; }

        private bool Off => string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RASA_BENCH"));

        private void Report(string line)
        {
            TestContext.WriteLine(line);
            Console.WriteLine(line);
        }

        /// <summary>A map that has been played on: cells over a 141 by 141 grid, about 20,000 of them.</summary>
        private static void FillCells(MapChannel map, int side = 141)
        {
            var origin = (uint)CellManager.CellBias - (uint)(side / 2);

            for (var x = 0u; x < side; x++)
                for (var z = 0u; z < side; z++)
                    CellManager.Instance.GetCell(map, origin + x, origin + z);
        }

        private static Creature Creature(WorldTestContext world, float x, float z) => new()
        {
            Name = "Bench",
            MapContextId = world.Map.MapInfo.MapContextId,
            Position = new Vector3(x, 0, z),
            EntityClass = EntityClasses.HumanBaseMale,
            State = CharacterState.Idle,
            Level = 1,
            AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
        };

        [TestMethod]
        [TestCategory("Bench")]
        public void RemovingCreatures()
        {
            if (Off)
                return;

            using var world = new WorldTestContext();
            FillCells(world.Map);

            var random = new Random(1);
            var creatures = Enumerable.Range(0, 2000)
                .Select(_ => Creature(world, random.Next(-1700, 1700), random.Next(-1700, 1700)))
                .ToList();

            foreach (var creature in creatures)
                CellManager.Instance.AddToWorld(world.Map, creature);

            var cells = world.Map.MapCellInfo.Cells.Count;
            var timer = Stopwatch.StartNew();

            foreach (var creature in creatures.Take(1000))
                Assert.IsTrue(CellManager.Instance.RemoveCreatureFromWorld(world.Map, creature));

            timer.Stop();

            Report($"RemoveCreatureFromWorld: {timer.Elapsed.TotalMilliseconds * 1000.0 / 1000:0.0} us each over {cells} cells ({timer.Elapsed.TotalMilliseconds:0.0} ms for 1000)");

            foreach (var creature in creatures.Skip(1000))
                CellManager.Instance.RemoveCreatureFromWorld(world.Map, creature);
        }

        [TestMethod]
        [TestCategory("Bench")]
        public void DetachingAPlayerFromEveryMap()
        {
            if (Off)
                return;

            using var world = new WorldTestContext();
            var maps = Enumerable.Range(0, 10).Select(_ => new MapChannel
            {
                MapInfo = world.Map.MapInfo,
                ClientList = new List<Client>()
            }).ToList();

            foreach (var map in maps)
                FillCells(map);

            var client = PlayerDeathTests.Player(world, 0, 0);

            // As RemovePlayer does: every map, the player's own among them.
            maps.Add(world.Map);

            var timer = Stopwatch.StartNew();

            for (var i = 0; i < 100; i++)
                foreach (var map in maps)
                    if (map != world.Map)
                        CellManager.Instance.DetachClient(map, client);

            timer.Stop();

            Report($"DetachClient over 10 maps the player is not on (~20,000 cells each): {timer.Elapsed.TotalMilliseconds * 1000.0 / 100:0.0} us a player removal");
        }

        [TestMethod]
        [TestCategory("Bench")]
        public void Broadcasts()
        {
            if (Off)
                return;

            using var world = new WorldTestContext();
            var players = Enumerable.Range(0, 20).Select(i => PlayerDeathTests.Player(world, i * 4, i * 3)).ToList();

            var creature = Creature(world, 10, 10);
            CellManager.Instance.AddToWorld(world.Map, creature);

            var obj = new DynamicObject { Position = new Vector3(12, 0, 12) };
            CellManager.Instance.AddToWorld(world.Map, obj);

            foreach (var client in players)
                WorldTestContext.Drain(client);

            var packet = new PerformRecoveryPacket(PerformType.TwoArgs, ActionId.WeaponPolaritygun, 1);
            var move = new Movement(new Vector3(10, 0, 10), new Vector2(1, 0));
            var self = players[0];

            Measure("CellManager.CellCallMethod(creature)", players, () => CellManager.Instance.CellCallMethod(creature, packet));
            Measure("CellManager.CellMoveObject(creature)", players, () => CellManager.Instance.CellMoveObject(creature, move));
            Measure("CellManager.CellCallMethod(map, actor)", players, () => CellManager.Instance.CellCallMethod(world.Map, self.Player, packet));
            Measure("CellManager.CellCallMethod(map, object)", players, () => CellManager.Instance.CellCallMethod(world.Map, obj, packet));
            Measure("CellManager.CellCallMethod(map, position)", players, () => CellManager.Instance.CellCallMethod(world.Map, obj.Position, obj.EntityId, packet));
            Measure("Client.CellCallMethod", players, () => self.CellCallMethod(self, self.Player.EntityId, packet));
            Measure("Client.CellIgnoreSelfCallMethod", players, () => self.CellIgnoreSelfCallMethod(self, packet));

            CellManager.Instance.RemoveCreatureFromWorld(world.Map, creature);
            CellManager.Instance.RemoveFromWorld(world.Map, obj);
        }

        /// <summary>Bytes and time a broadcast, and how many it reached; what the clients were sent emptied between rounds.</summary>
        private void Measure(string name, List<Client> clients, Action broadcast)
        {
            broadcast();
            var reached = clients.Sum(client => WorldTestContext.Drain(client).Count);

            const int rounds = 2000;
            long bytes = 0;
            long ticks = 0;

            for (var i = 0; i < rounds; i++)
            {
                var from = GC.GetAllocatedBytesForCurrentThread();
                var start = Stopwatch.GetTimestamp();
                broadcast();
                ticks += Stopwatch.GetTimestamp() - start;
                bytes += GC.GetAllocatedBytesForCurrentThread() - from;

                foreach (var client in clients)
                    while (client.DequeueOutgoingPacket() != null)
                    {
                    }
            }

            Report($"{name,-44} {bytes / rounds,6} B {ticks * 1e6 / Stopwatch.Frequency / rounds,8:0.00} us a broadcast, to {reached}");
        }

        [TestMethod]
        [TestCategory("Bench")]
        public void APassOfTheMapChannelWorker()
        {
            if (Off)
                return;

            using var world = new WorldTestContext();
            var maps = new MapChannelManager(null);

            maps.MapChannelArray.Add(world.Map.MapInfo.MapContextId, world.Map);
            for (var i = 1; i < 10; i++)
            {
                var map = new MapChannel
                {
                    MapInfo = new MapInfo((uint)(5000 + i), "bench", 1556, 0),
                    ClientList = new List<Client>()
                };
                maps.MapChannelArray.Add(map.MapInfo.MapContextId, map);
            }

            // Somebody on each map, or it does not tick.
            foreach (var map in maps.MapChannelArray.Values)
            {
                var client = world.CreateClient(0, 0);
                if (map != world.Map)
                {
                    world.Map.ClientList.Remove(client);
                    client.Player.MapChannel = map;
                    client.Player.MapContextId = map.MapInfo.MapContextId;
                    map.ClientList.Add(client);
                }
            }

            for (var i = 0; i < 50; i++)
                maps.MapChannelWorker(100);

            const int passes = 500;
            var from = GC.GetAllocatedBytesForCurrentThread();
            var timer = Stopwatch.StartNew();

            for (var i = 0; i < passes; i++)
                maps.MapChannelWorker(100);

            timer.Stop();
            var bytes = GC.GetAllocatedBytesForCurrentThread() - from;

            Report($"MapChannelWorker, 10 maps with a player each, no timers due: {bytes / passes} B and {timer.Elapsed.TotalMilliseconds * 1000.0 / passes:0.0} us a pass");

            // And where it goes, by the phases.
            LoopPhases.Reset();
            LoopPhases.Enabled = true;

            try
            {
                for (var i = 0; i < passes; i++)
                {
                    var pass = LoopPhases.Begin();
                    maps.MapChannelWorker(100);
                    LoopPhases.EndPass(pass);
                }

                foreach (var line in LoopPhases.Describe(LoopPhases.Take(), 25))
                    Report(line);
            }
            finally
            {
                LoopPhases.Enabled = false;
            }
        }

        /// <summary>
        /// A busy map: 40 players and 400 creatures spread over 600 m, every timed worker due every
        /// pass (the once-a-second ones included), so a pass here is the worst a second has. Where
        /// it goes, by the phases.
        /// </summary>
        [TestMethod]
        [TestCategory("Bench")]
        public void ABusyMapsWorstPass()
        {
            if (Off)
                return;

            using var world = new WorldTestContext();
            var random = new Random(4);
            var players = Enumerable.Range(0, 40)
                .Select(_ => PlayerDeathTests.Player(world, random.Next(-300, 300), random.Next(-300, 300)))
                .ToList();
            var creatures = Enumerable.Range(0, 400)
                .Select(_ => Creature(world, random.Next(-300, 300), random.Next(-300, 300)))
                .ToList();

            foreach (var creature in creatures)
                CellManager.Instance.AddToWorld(world.Map, creature);

            var maps = new MapChannelManager(null);
            maps.MapChannelArray.Add(world.Map.MapInfo.MapContextId, world.Map);

            foreach (var timer in new[] { "CheckForObjects", "ClientEffectUpdate", "CellUpdateVisibility", "CheckForCreatures",
                         "CheckForMapTriggers", "MissionDeadlineUpdate", "Regenerate", "MuteExpiry", "SharedInstances" })
                maps.Timer.Add(timer, 100, true, null);

            void Pass()
            {
                maps.MapChannelWorker(100);

                foreach (var client in players)
                    while (client.DequeueOutgoingPacket() != null)
                    {
                    }
            }

            for (var i = 0; i < 20; i++)
                Pass();

            LoopPhases.Reset();
            LoopPhases.Enabled = true;

            try
            {
                const int passes = 100;
                for (var i = 0; i < passes; i++)
                {
                    var pass = LoopPhases.Begin();
                    Pass();
                    LoopPhases.EndPass(pass);
                }

                foreach (var line in LoopPhases.Describe(LoopPhases.Take(), 25))
                    Report(line);
            }
            finally
            {
                LoopPhases.Enabled = false;

                foreach (var creature in creatures)
                    CellManager.Instance.RemoveCreatureFromWorld(world.Map, creature);
            }
        }

        [TestMethod]
        [TestCategory("Bench")]
        public void SpawnPoolsOfAMapWithNone()
        {
            if (Off)
                return;

            using var world = new WorldTestContext();
            var manager = SpawnPoolManager.Instance;
            var added = new List<uint>();

            for (var i = 0u; i < 5000; i++)
            {
                var id = 0x7F000000u + i;
                if (manager.LoadedSpawnPools.ContainsKey(id))
                    continue;

                manager.LoadedSpawnPools.Add(id, new SpawnPool { DbId = id, MapContextId = 4000 + i % 40 });
                added.Add(id);
            }

            try
            {
                manager.SpawnPoolWorker(world.Map, 1000);

                const int rounds = 2000;
                var from = GC.GetAllocatedBytesForCurrentThread();
                var timer = Stopwatch.StartNew();

                for (var i = 0; i < rounds; i++)
                    manager.SpawnPoolWorker(world.Map, 1000);

                timer.Stop();
                var bytes = GC.GetAllocatedBytesForCurrentThread() - from;

                Report($"SpawnPoolWorker, a map with no pools of its own, {manager.LoadedSpawnPools.Count} loaded: {bytes / rounds} B and {timer.Elapsed.TotalMilliseconds * 1000.0 / rounds:0.0} us a second");
            }
            finally
            {
                foreach (var id in added)
                    manager.LoadedSpawnPools.Remove(id);
            }
        }
    }
}
