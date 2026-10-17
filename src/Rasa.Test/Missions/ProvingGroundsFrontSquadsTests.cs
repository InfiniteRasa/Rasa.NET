using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions
{
    using Rasa.Context.World;
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Missions.Scenes;
    using Rasa.Services.Preloader;
    using Rasa.Services.Preloader.Missions;
    using Rasa.Structures;
    using Rasa.Test.Database;

    /// <summary>
    /// Our squads west of the Proving Grounds' fort (ProvingGroundsFrontSquads,
    /// BootcampFrontSquadsV13, Proving_grounds_front_squads): nothing of ours out there until the
    /// fort is taken back; from Youngblood's arrival three AFS squads of three drawn soldiers and
    /// three Forean squads of two archers and a warrior spawn at the GM's spots, come back 120 s
    /// after they fall, and fight what comes near.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class ProvingGroundsFrontSquadsTests
    {
        private const string Migration = "20270111000000_Proving_grounds_front_squads";
        private const string Before = "20270110000100_Bootcamp_breach_assault";
        private const uint CaveInAreaId = 439;

        [TestMethod]
        public void TheSquadsAreWhereTheGmStoodOnOpenGround()
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            var nav = harness.BootcampMap.NavMesh;

            Assert.HasCount(6, ProvingGroundsFrontSquads.Squads);
            Assert.HasCount(3, ProvingGroundsFrontSquads.Squads.Where(squad => squad.Afs).ToList());
            foreach (var squad in ProvingGroundsFrontSquads.Squads)
            {
                var spot = new Vector3((float)squad.X, (float)squad.Y, (float)squad.Z);
                var ground = nav.Nearest(spot);
                Assert.IsTrue(ground.HasValue && Flat(ground.Value, spot) < 1f && Math.Abs(ground.Value.Y - spot.Y) < 1.5f,
                    $"{squad.Where}: ground {ground} at {spot}");
            }
        }

        [TestMethod]
        public void TheSceneWakesThePoolsWithYoungbloodAndIsOtherwiseTheBreachAssaults()
        {
            var scene = BootcampFrontSquadsV13.CaptureTheFlag();
            var arrival = scene.Sequences[scene.Names["arrival"]].World;
            var wake = arrival.OfType<WakeSpawnPoolsIntent>().Single();
            Assert.AreEqual(BootcampFortSquadV10.Youngblood, wake.Role, "held by Youngblood, as the fort's quiet");
            CollectionAssert.AreEqual(ProvingGroundsFrontSquads.Pools, wake.SpawnPools);
            Assert.IsFalse(scene.Sequences[scene.Names["delay"]].World.OfType<WakeSpawnPoolsIntent>().Any(), "not at the kill");

            // Nothing else of the scene is changed: the breach wave's routes and the cave-in's radius stay.
            var before = BootcampBreachAssaultV12.CaptureTheFlag();
            var without = arrival.Where(intent => intent is not WakeSpawnPoolsIntent).ToList();
            CollectionAssert.AreEqual(before.Sequences[before.Names["arrival"]].World, without);
            CollectionAssert.AreEquivalent(before.Routes.Keys.ToList(), scene.Routes.Keys.ToList());
            Assert.AreEqual(before.Actors[BootcampCaveInBreachV9.CaveIn], scene.Actors[BootcampCaveInBreachV9.CaveIn]);
        }

        [TestMethod]
        public void AnAfsSquadIsThreeDrawnTwoRiflesToOnePistolToOneFieldGunner()
        {
            var definitions = new Dictionary<uint, Creature>
            {
                [ProvingGroundsFrontSquads.Rifleman] = new Creature { DbId = ProvingGroundsFrontSquads.Rifleman },
                [ProvingGroundsFrontSquads.Pistoleer] = new Creature { DbId = ProvingGroundsFrontSquads.Pistoleer },
                [ProvingGroundsFrontSquads.FieldGunner] = new Creature { DbId = ProvingGroundsFrontSquads.FieldGunner },
                [ProvingGroundsFrontSquads.Archer] = new Creature { DbId = ProvingGroundsFrontSquads.Archer },
                [ProvingGroundsFrontSquads.Warrior] = new Creature { DbId = ProvingGroundsFrontSquads.Warrior },
            };
            var afs = Pool(ProvingGroundsFrontSquads.AfsSlots, ProvingGroundsFrontSquads.SquadSize);
            var forean = Pool(ProvingGroundsFrontSquads.ForeanSlots, 0);
            var random = new Random(1985);
            var counts = new Dictionary<uint, int>();

            for (var i = 0; i < 4000; i++)
            {
                var squad = SpawnPoolManager.CreateListOfCreatures(afs, definitions, random);
                Assert.HasCount(3, squad);
                foreach (var soldier in squad)
                    counts[soldier.DbId] = counts.GetValueOrDefault(soldier.DbId) + 1;
            }

            Assert.AreEqual(0.50, counts[ProvingGroundsFrontSquads.Rifleman] / 12000.0, 0.02, "an Infantryman with a rifle half the time");
            Assert.AreEqual(0.25, counts[ProvingGroundsFrontSquads.Pistoleer] / 12000.0, 0.02, "with a pistol a quarter");
            Assert.AreEqual(0.25, counts[ProvingGroundsFrontSquads.FieldGunner] / 12000.0, 0.02, "a Field Gunner a quarter");

            for (var i = 0; i < 50; i++)
                CollectionAssert.AreEquivalent(
                    new[] { ProvingGroundsFrontSquads.Archer, ProvingGroundsFrontSquads.Archer, ProvingGroundsFrontSquads.Warrior },
                    SpawnPoolManager.CreateListOfCreatures(forean, definitions, random).Select(creature => creature.DbId).ToArray(),
                    "two archers and a warrior, every time");
        }

        [TestMethod]
        public void NothingOfOursUntilTheFortIsTakenThenTheSquadsComeAndComeBack()
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            var map = harness.BootcampMap;
            harness.Client.Player.GmFlagAlwaysFriendly = true;

            // Long past their respawn: not woken, so nothing.
            for (var pass = 0; pass < 3; pass++)
                SpawnPoolManager.Instance.SpawnPoolWorker(map, 600000);
            Assert.IsEmpty(Front(map));
            Assert.IsTrue(FrontPools(map).All(pool => pool.Mode == SpawnPoolManager.ModeScripted && !pool.Woken));

            TakeTheFort(harness);
            Assert.IsTrue(FrontPools(map).All(pool => pool.Woken), "woken with Youngblood's arrival");

            SpawnPoolManager.Instance.SpawnPoolWorker(map, 600000);
            AssertSquadsStand(map);

            // A squad wiped out is back 120 s after the last of it fell, not before.
            var first = FrontPools(map).Single(pool => pool.DbId == ProvingGroundsFrontSquads.Squads[0].Pool);
            foreach (var soldier in Front(map).Where(creature => ReferenceEquals(creature.SpawnPool, first)))
                ActorManager.Instance.Damage(map, soldier, 1000000, harness.Client.Player);
            Assert.AreEqual(0, first.AliveCreatures);
            SpawnPoolManager.Instance.SpawnPoolWorker(map, 0);
            SpawnPoolManager.Instance.SpawnPoolWorker(map, 119000);
            Assert.AreEqual(0, first.AliveCreatures + first.QueuedCreatures, "not before 120 s");
            SpawnPoolManager.Instance.SpawnPoolWorker(map, 1000);
            Assert.AreEqual(3, first.AliveCreatures + first.QueuedCreatures, "a new three at 120 s");

            // A map made again has them going from the start.
            harness.ReconnectFresh();
            map = harness.BootcampMap;
            Assert.IsTrue(FrontPools(map).All(pool => pool.Woken));
            SpawnPoolManager.Instance.SpawnPoolWorker(map, 0);
            SpawnPoolManager.Instance.SpawnPoolWorker(map, 600000);
            AssertSquadsStand(map);
        }

        [TestMethod]
        public void ASquadTakesOnTheThraxThatComeNear()
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            var map = harness.BootcampMap;
            harness.Client.Player.GmFlagAlwaysFriendly = true;
            TakeTheFort(harness);
            SpawnPoolManager.Instance.SpawnPoolWorker(map, 600000);

            var forean = ProvingGroundsFrontSquads.Squads.First(squad => !squad.Afs);
            var spot = new Vector3((float)forean.X, (float)forean.Y, (float)forean.Z);
            var pool = new SpawnPool
            {
                DbId = 999002, MapContextId = map.MapInfo.MapContextId, RuntimeMapChannel = map,
                Position = map.NavMesh.Nearest(spot + new Vector3(8f, 0, 0)).Value, SpawnSlot = new List<SpawnPoolSlot> { new(510225, 1, 1) }
            };
            map.SpawnPools.Add(pool);
            var thrax = CreatureManager.Instance.CreateScenarioCreature(pool, 510225, pool.Position, 0);
            CellManager.Instance.AddToWorld(map, thrax);
            // Someone near for the map to think there; one the Thrax do not go for.
            harness.MovePlayerTo(spot + new Vector3(0, 0, 15f));

            // A creature at rest looks about every 3 s (BehaviorManager.AggroScanDelayMs).
            for (var tick = 0; tick < 20; tick++)
                BehaviorManager.Instance.MapChannelThink(map, 250);

            var squad = Front(map).Where(creature => creature.SpawnPool?.DbId == forean.Pool).ToList();
            Assert.HasCount(3, squad);
            Assert.IsTrue(squad.Any(creature => creature.Controller.CurrentAction == BehaviorManager.BehaviorActionFighting &&
                creature.Controller.ActionFighting.TargetEntityId == thrax.EntityId),
                $"the squad takes it on: thrax at {thrax.Position} {thrax.State}; {string.Join("; ", squad.Select(c => $"{c.DbId} at {c.Position} action {c.Controller.CurrentAction} target {c.Controller.ActionFighting.TargetEntityId}"))}");
        }

        [TestMethod]
        public void TheMigrationAddsTheSquadsAndDownTakesThemAway()
        {
            var directory = Path.Combine(AppContext.BaseDirectory, "TestDatabases", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);

            try
            {
                var database = Path.Combine(directory, "world");

                using (var context = PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), database))
                    MigratedDatabaseTemplates.Migrate(context, () => context.Database.Migrate());

                using var world = (WorldContext)PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), database);
                var migrator = world.GetService<IMigrator>();
                migrator.Migrate(Migration);

                var creatures = world.CreatureEntries.AsNoTracking().Where(row => row.Id >= 400301 && row.Id <= 400305).OrderBy(row => row.Id).ToList();
                Assert.HasCount(5, creatures);
                Assert.AreEqual((29423u, 8716u, 72003u), (creatures[0].ClassId, creatures[0].NameId, creatures[0].Action1), "rifle");
                Assert.AreEqual((29423u, 8716u, 510218u), (creatures[1].ClassId, creatures[1].NameId, creatures[1].Action1), "pistol");
                Assert.AreEqual((29433u, 8720u, 400301u), (creatures[2].ClassId, creatures[2].NameId, creatures[2].Action1), "Field Gunner");
                Assert.AreEqual((6415u, 7986u, 28u), (creatures[3].ClassId, creatures[3].NameId, creatures[3].Action1), "archer");
                Assert.AreEqual((6043u, 0u, 5u, 0u), (creatures[4].ClassId, creatures[4].NameId, creatures[4].Action1, creatures[4].Action2), "warrior");
                Assert.IsTrue(creatures.All(row => row.Faction == 1 && row.Level == 11 && row.MaxHitPoints == 660 && row.RunSpeed == 7));
                Assert.AreEqual(1, Count(world, "creature_action WHERE id = 400301 AND action_id = 149 AND action_arg_id = 4 AND damage_type = 6"));
                Assert.AreEqual(1, Count(world, "creature_appearance WHERE id = 400303 AND slot_id = 13 AND class_id = 20534"));
                Assert.AreEqual(5, Count(world, "creature_stat WHERE id BETWEEN 400301 AND 400305 AND health = 660 AND armor = 132"));
                Assert.AreEqual(6, Count(world, "spawnpool WHERE id BETWEEN 400301 AND 400306 AND map_context_id = 1985 AND mode = 2 AND respown_time = 1200"));
                Assert.AreEqual(3, Count(world, "spawnpool_draw WHERE id BETWEEN 400301 AND 400306 AND count = 3"));
                Assert.AreEqual(3, Count(world, "spawnpool WHERE id BETWEEN 400301 AND 400306 AND creature_1_Id = 400304 AND creature_1_min_count = 2 " +
                    "AND creature_2_Id = 400305 AND creature_2_min_count = 1"));
                StringAssert.Contains(Binding(world), $"\"{BootcampFrontSquadsV13.WakeOperation}\"");

                // Run again, as the script for a MySQL server may be: nothing is doubled.
                foreach (var sql in ProvingGroundsFrontSquads.Up())
                    world.Database.ExecuteSqlRaw(sql.Replace("{", "{{").Replace("}", "}}"));
                Assert.AreEqual(5, Count(world, "creature WHERE id BETWEEN 400301 AND 400305"));
                Assert.AreEqual(6, Count(world, "spawnpool WHERE id BETWEEN 400301 AND 400306"));
                Assert.AreEqual(3, Count(world, "spawnpool_draw"));

                migrator.Migrate(Before);

                Assert.AreEqual(0, Count(world, "creature WHERE id BETWEEN 400301 AND 400305"));
                Assert.AreEqual(0, Count(world, "creature_action WHERE id = 400301"));
                Assert.AreEqual(0, Count(world, "spawnpool WHERE id BETWEEN 400301 AND 400306"));
                Assert.AreEqual(0, Count(world, "sqlite_master WHERE type = 'table' AND name = 'spawnpool_draw'"));
                Assert.IsFalse(Binding(world).Contains(BootcampFrontSquadsV13.WakeOperation));
                StringAssert.Contains(Binding(world), "\"fort-quiet-while-held\"");
                StringAssert.Contains(Binding(world), "\"" + BootcampBreachAssaultV12.RouteKey(0) + "\"");

                migrator.Migrate(Migration);
                Assert.AreEqual(6, Count(world, "spawnpool WHERE id BETWEEN 400301 AND 400306"));
            }
            finally
            {
                try { Directory.Delete(directory, true); } catch (IOException) { }
            }
        }

        [TestMethod]
        public void TheMigrationMakesTheSameRowsOnMySql()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Before, Migration);
            var down = migrator.GenerateScript(Migration, Before);

            StringAssert.Contains(up, "CREATE TABLE `spawnpool_draw`");
            StringAssert.Contains(up, "`count` tinyint unsigned NOT NULL");
            StringAssert.Contains(up, "INSERT INTO spawnpool_draw (id, count) SELECT 400301, 3 FROM spawnpool s");
            StringAssert.Contains(up, $"\"{BootcampFrontSquadsV13.WakeOperation}\"");
            StringAssert.Contains(down, "DROP TABLE `spawnpool_draw`");
            StringAssert.Contains(down, "\"fort-quiet-while-held\"");
        }

        private static void TakeTheFort(BootcampRuntimeTestHarness.Harness harness)
        {
            var map = harness.BootcampMap;
            SpawnPoolManager.Instance.SpawnPoolWorker(map, 0);
            harness.SeedMission(harness.Client.Player.Id, 1992, (uint)MissionState.Completed, true);
            var deSimone = Creatures(map).Single(actor => actor.DbId == BootcampRuntimeTestHarness.CorporalDeSimoneCreatureId);
            harness.MovePlayerTo(deSimone);
            CellManager.Instance.UpdateVisibility(harness.Client);
            harness.Drain();
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, deSimone.EntityId, 1994));
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, deSimone.EntityId, 1994, 4, 1));
            Assert.IsTrue(harness.Manager.RecordProgress(harness.Client, MissionProgressEvent.Area(1994, CaveInAreaId)));
            harness.Drain();
            harness.MovePlayerTo(new Vector3(95f, 109.25f, 145f));
            var tizzik = BootcampRuntimeTestHarness.FindCreature(map, BootcampRuntimeTestHarness.TizzikGiCreatureId)
                ?? throw new AssertFailedException("No Tizzik Gi.");
            ActorManager.Instance.Damage(map, tizzik, 1000000, harness.Client.Player);
            Assert.IsTrue(FrontPools(map).All(pool => !pool.Woken), "not at the kill: with Youngblood");
            harness.UtcNow += TimeSpan.FromSeconds(7);
            Assert.IsTrue(harness.Manager.TickScenarios(harness.Client));
        }

        private static void AssertSquadsStand(MapChannel map)
        {
            foreach (var squad in ProvingGroundsFrontSquads.Squads)
            {
                var members = Front(map).Where(creature => creature.SpawnPool?.DbId == squad.Pool).ToList();
                Assert.HasCount(3, members, squad.Where);
                var spot = new Vector3((float)squad.X, (float)squad.Y, (float)squad.Z);
                Assert.IsTrue(members.All(member => Flat(member.Position, spot) <= 3f),
                    $"{squad.Where}: at its spot {spot}, not {string.Join(", ", members.Select(member => member.Position))}");
                if (squad.Afs)
                    Assert.IsTrue(members.All(member => member.DbId is ProvingGroundsFrontSquads.Rifleman or ProvingGroundsFrontSquads.Pistoleer or ProvingGroundsFrontSquads.FieldGunner));
                else
                    CollectionAssert.AreEquivalent(
                        new[] { ProvingGroundsFrontSquads.Archer, ProvingGroundsFrontSquads.Archer, ProvingGroundsFrontSquads.Warrior },
                        members.Select(member => member.DbId).ToArray());
                Assert.IsTrue(members.All(member => member.TargetCategory == TargetCategory.Friendly));
            }
        }

        private static SpawnPool Pool(ProvingGroundsFrontSquads.Slot[] slots, int draw) => new()
        {
            DbId = 1, DrawCount = draw,
            SpawnSlot = slots.Select(slot => new SpawnPoolSlot(slot.Creature, slot.Min, slot.Max)).ToList()
        };

        private static float Flat(Vector3 a, Vector3 b) => new Vector2(a.X - b.X, a.Z - b.Z).Length();

        private static Creature[] Creatures(MapChannel map) =>
            map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList).Distinct().ToArray();

        private static Creature[] Front(MapChannel map) =>
            Creatures(map).Where(creature => creature.SpawnPool != null && ProvingGroundsFrontSquads.Pools.Contains(creature.SpawnPool.DbId) &&
                creature.SpawnPool.ScenarioKey == null && creature.State != CharacterState.Dead).ToArray();

        private static SpawnPool[] FrontPools(MapChannel map) =>
            map.SpawnPools.Where(pool => pool.ScenarioKey == null && ProvingGroundsFrontSquads.Pools.Contains(pool.DbId)).ToArray();

        private static int Count(WorldContext world, string from) => world.Database
            .SqlQueryRaw<int>($"SELECT COUNT(*) AS Value FROM {from}").AsEnumerable().Single();

        private static string Binding(WorldContext world) => world.Database
            .SqlQueryRaw<string>("SELECT bindings AS Value FROM mission_scene_binding WHERE mission_id = 1994")
            .AsEnumerable().Single();
    }
}
