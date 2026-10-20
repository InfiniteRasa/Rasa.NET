using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Memory;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Services.Preloader;
    using Rasa.Structures;
    using Rasa.Test.Missions.Wilderness;
    using Rasa.Structures.World;
    using Rasa.Test.Database;
    using Rasa.Test.Missions;
    using Rasa.Test.Missions.Wilderness;

    /// <summary>
    /// NPC barks (NpcBarks): that an NPC with barks says one within 10 minutes half the time and
    /// within 30 always, to everyone around it, and again after; that one with none never barks;
    /// and the barks seeded (npc_bark).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class NpcBarkTests
    {
        private const string Migration = "20261231000000_Add_npc_barks";
        private const string Before = "20261230000000_Weapon_template_rows";
        private const uint Rogers = NpcBarkPreloader.OutpostCommanderRogers;
        private const uint RiverFront = NpcBarkPreloader.RiverFrontReinforcements;

        private Func<int, int> _roll;
        private Func<long> _now;
        private long _clock;

        [TestInitialize]
        public void Initialize()
        {
            _roll = NpcBarks.Roll;
            _now = NpcBarks.Now;
            _clock = 1_000_000;
            NpcBarks.Now = () => _clock;
        }

        [TestCleanup]
        public void Cleanup()
        {
            NpcBarks.Roll = _roll;
            NpcBarks.Now = _now;
            NpcBarks.Load(null);
        }

        [TestMethod]
        public void TheBarkIsItsIdAlone()
        {
            using var stream = new MemoryStream(MissionTestContext.Encode(new BarkPackage(RiverFront)));
            using var reader = new PythonReader(new BinaryReader(stream));

            Assert.AreEqual(1, reader.ReadTuple());
            Assert.AreEqual((int)RiverFront, reader.ReadInt());
        }

        [TestMethod]
        public void HalfTheDrawsComeWithinTenMinutesAndEveryOneWithinThirty()
        {
            NpcBarks.Roll = _ => 0;
            Assert.AreEqual(0L, NpcBarks.NextDelay(), "the soonest");

            var rolls = new Queue<int>(new[] { 0, NpcBarks.HalfMs - 1 });
            NpcBarks.Roll = _ => rolls.Dequeue();
            Assert.AreEqual(NpcBarks.HalfMs - 1L, NpcBarks.NextDelay(), "the latest of the first half");

            NpcBarks.Roll = max => max - 1;
            Assert.AreEqual(NpcBarks.WithinMs - 1L, NpcBarks.NextDelay(), "the latest of all");

            rolls = new Queue<int>(new[] { 1, 0 });
            NpcBarks.Roll = _ => rolls.Dequeue();
            Assert.AreEqual((long)NpcBarks.HalfMs, NpcBarks.NextDelay(), "the soonest of the second half");

            NpcBarks.Roll = _roll;
            var random = new Random(1234);
            NpcBarks.Roll = max => random.Next(max);
            var draws = Enumerable.Range(0, 20000).Select(_ => NpcBarks.NextDelay()).ToList();

            Assert.IsTrue(draws.All(delay => delay >= 0 && delay < NpcBarks.WithinMs), "always within 30 minutes");
            var withinTen = draws.Count(delay => delay < NpcBarks.HalfMs) / (double)draws.Count;
            Assert.AreEqual(0.5, withinTen, 0.02, "within 10 minutes half the time");
        }

        [TestMethod]
        public void AnNpcWithABarkSaysItWhenItIsDueToThoseAroundAndThenDrawsTheNext()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 10);
            var rogers = Spawn(world, Rogers, x: 0);
            NpcBarks.Load(new[] { Row(Rogers, RiverFront) });
            NpcBarks.Roll = max => max == 2 ? 0 : 1000;

            NpcBarks.Observe(world.Map, rogers);
            Assert.AreEqual(_clock + 1000, rogers.NextBarkAt, "drawn when first seen");
            Assert.IsEmpty(Barks(watcher), "not said as it is first seen");

            _clock += 999;
            NpcBarks.Observe(world.Map, rogers);
            Assert.IsEmpty(Barks(watcher), "not before it is due");

            _clock++;
            NpcBarks.Observe(world.Map, rogers);
            var barks = Barks(watcher);
            Assert.HasCount(1, barks);
            Assert.AreEqual(rogers.EntityId, barks[0].EntityId);
            Assert.AreEqual(RiverFront, barks[0].Packet.BarkId);
            Assert.AreEqual(_clock + 1000, rogers.NextBarkAt, "the next drawn");

            NpcBarks.Observe(world.Map, rogers);
            Assert.IsEmpty(Barks(watcher), "once");
        }

        [TestMethod]
        public void AnNpcWithoutBarksNeverBarks()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 10);
            var quiet = Spawn(world, 4242, x: 0);
            NpcBarks.Load(new[] { Row(Rogers, RiverFront) });
            NpcBarks.Roll = _ => 0;

            for (var minute = 0; minute <= 60; minute++)
            {
                NpcBarks.Observe(world.Map, quiet);
                _clock += 60_000;
            }

            Assert.IsNull(quiet.NextBarkAt);
            Assert.IsEmpty(Barks(watcher));
        }

        [TestMethod]
        public void AnNpcWithSeveralBarksSaysOneOfThemEachTime()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 10);
            var talker = Spawn(world, 4243, x: 0);
            NpcBarks.Load(new[] { Row(4243, 96), Row(4243, 92), Row(4243, 92) });
            CollectionAssert.AreEqual(new uint[] { 92, 96 }, NpcBarks.BarksOf(talker).ToArray(), "each once");

            // First seen: due at once.
            NpcBarks.Roll = _ => 0;
            NpcBarks.Observe(world.Map, talker);
            Assert.AreEqual(_clock, talker.NextBarkAt);

            // Due: the next drawn (the second half, its soonest) and the second of the two said.
            NpcBarks.Roll = max => max == 2 ? 1 : 0;
            NpcBarks.Observe(world.Map, talker);
            Assert.AreEqual(_clock + NpcBarks.HalfMs, talker.NextBarkAt);
            Assert.AreEqual(96U, Barks(watcher).Single().Packet.BarkId, "the second of the two");
        }

        [TestMethod]
        public void ItBarksFromItsOwnThinks()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 10);
            var rogers = Spawn(world, Rogers, x: 0);
            NpcBarks.Load(new[] { Row(Rogers, RiverFront) });
            NpcBarks.Roll = _ => 0;

            BehaviorManager.Instance.MapChannelThink(world.Map, 250);
            Assert.AreEqual(_clock, rogers.NextBarkAt);

            BehaviorManager.Instance.MapChannelThink(world.Map, 250);
            Assert.AreEqual(RiverFront, Barks(watcher).Single(bark => bark.EntityId == rogers.EntityId).Packet.BarkId);
        }

        [TestMethod]
        public void RogersIsSeededWithHisBarkAndDownDropsTheTable()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Migration);
            var rows = database.Context.NpcBarkEntries.AsNoTracking().ToList();
            Assert.HasCount(1, rows);
            Assert.AreEqual((Rogers, RiverFront), (rows[0].CreatureId, rows[0].BarkId));

            migrator.Migrate(Before);
            Assert.AreEqual(0, database.Context.Database
                .SqlQueryRaw<int>($"select count(*) as Value from sqlite_master where type = 'table' and name = '{NpcBarkEntry.TableName}'").AsEnumerable().Single());
        }

        [TestMethod]
        public void TheServerLoadsRogersBark()
        {
            using var harness = WildernessRuntimeTestHarness.Create();

            CollectionAssert.AreEqual(new[] { RiverFront }, NpcBarks.BarksOf(harness.Creatures.LoadedCreatures[Rogers]).ToArray());
        }

        [TestMethod]
        public void OnMySqlTheTableGoesInAndComesOut()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Before, Migration);
            var down = migrator.GenerateScript(Migration, Before);

            StringAssert.Contains(up, "CREATE TABLE `npc_bark`");
            StringAssert.Contains(up, "PRIMARY KEY (`creature_id`, `bark_id`)");
            StringAssert.Contains(up, $"'{Migration}'");
            StringAssert.Contains(down, "DROP TABLE `npc_bark`");
        }

        private static NpcBarkEntry Row(uint creatureId, uint barkId) => new NpcBarkEntry { CreatureId = creatureId, BarkId = barkId };

        /// <summary>The barks the client has been sent since it was last asked, with whose they are.</summary>
        private static List<(ulong EntityId, BarkPackage Packet)> Barks(Client watcher) =>
            WorldTestContext.Drain(watcher).Select(packet => packet.Message).OfType<CallMethodMessage>()
                .Where(message => message.Packet is BarkPackage)
                .Select(message => (message.EntityId, (BarkPackage)message.Packet)).ToList();

        /// <summary>A friendly NPC of creature row dbId standing idle in the map's cells.</summary>
        private static Creature Spawn(WorldTestContext world, uint dbId, float x)
        {
            var creature = new Creature
            {
                Name = "Barker",
                DbId = dbId,
                TargetCategory = TargetCategory.Friendly,
                MapContextId = world.Map.MapInfo.MapContextId,
                RuntimeMapChannel = world.Map,
                Position = new Vector3(x, 0, 0),
                EntityClass = EntityClasses.HumanBaseMale,
                State = CharacterState.Idle,
                Level = 1,
                AggroRange = 0,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };

            creature.HomePos.Position = creature.Position;
            creature.HomePos.MapContextid = creature.MapContextId;
            creature.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0);
            BehaviorManager.StartWandering(creature, false);
            EntityManager.Instance.RegisterEntity(creature.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(creature);
            EntityManager.Instance.RegisterActor(creature.EntityId, creature);
            var seed = CellManager.Instance.GetCellSeed(creature.Position);
            creature.Cells = CellsAt(world, creature.Position);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).CreatureList.Add(creature);
            return creature;
        }

        /// <summary>A client standing in the map's cells, so what is sent about what is near it reaches it.</summary>
        private static Client Watch(WorldTestContext world, float x)
        {
            var client = world.CreateClient(x: x);
            var seed = CellManager.Instance.GetCellSeed(client.Player.Position);
            client.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 1000, 1000, 1000, 0, 0);
            client.Player.Cells = CellsAt(world, client.Player.Position);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).ClientList.Add(client);
            return client;
        }

        private static uint[,] CellsAt(WorldTestContext world, Vector3 position)
        {
            var seed = CellManager.Instance.GetCellSeed(position);
            return CellManager.Instance.CreateCellMatrix(world.Map, seed & 0xFFFF, seed >> 16);
        }
    }
}
