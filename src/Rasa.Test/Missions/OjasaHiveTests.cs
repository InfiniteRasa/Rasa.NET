using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.MapChannel.Server;
using Rasa.Services.Preloader;
using Rasa.Structures;
using Rasa.Structures.Char;
using Rasa.Structures.World;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions
{
    using H = OjasaHiveContent;

    /// <summary>
    /// The Ojasa Atta Hive as the guide tells it (OjasaHiveContent, OjasaHive): its rows, John
    /// and his five missions, the flamethrower test, the corpse, the bins and the device, the
    /// Queen's brawl and its two ways, and the sulfur lakes.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class OjasaHiveTests
    {
        private long _now = 1_000_000;

        [TestInitialize]
        public void Clock()
        {
            OjasaHive.Reset();
            OjasaHive.Now = () => _now;
        }

        [TestCleanup]
        public void Restore()
        {
            OjasaHive.Reset();
            OjasaHive.Now = () => Environment.TickCount64;
        }

        [TestMethod]
        public void TheHiveHasItsPeopleItsAttaAtTheGuidesLevelsAndItsFiveMissions()
        {
            using var harness = Create();
            var world = harness.World;

            foreach (var missionId in H.Missions)
            {
                Assert.IsTrue(harness.Manager.TryGetOperationalMission(missionId, out var mission), $"mission {missionId}");
                Assert.AreEqual(H.Category, mission.CategoryId);
            }

            Assert.IsTrue(harness.Manager.TryGetOperationalMission(H.LongLiveTheQueen, out var queen));
            CollectionAssert.AreEqual(new uint[] { 1, 2, 3, 4 }, queen.Objectives.Keys.OrderBy(id => id).ToArray());

            // The hive's 31 pools are its own Atta now, at 24 to 25; none of the Incline's is left in it.
            var hive = world.Set<SpawnPoolEntry>().Where(pool => pool.MapContextId == H.Hive).ToList();
            var incline = new[] { H.InclineAttaSoldier, H.InclineAttaHarvester, H.InclineAttaGrub };
            Assert.IsFalse(hive.Any(pool => incline.Contains(pool.Creature1Id) || incline.Contains(pool.Creature2Id) || incline.Contains(pool.Creature3Id)));
            Assert.AreEqual(31, hive.Count(pool => pool.Id is >= 531248 and <= 531278 &&
                new[] { H.AttaSoldier, H.AttaHarvester }.Contains(pool.Creature1Id)));
            Assert.IsTrue(world.Set<SpawnPoolEntry>().Any(pool => pool.MapContextId == H.Incline && pool.Creature1Id == H.InclineAttaSoldier),
                "the Incline keeps its own Atta");

            var levels = world.Set<CreatureEntry>().Where(c => c.Id >= 565010 && c.Id <= 565018).ToDictionary(c => c.Id, c => c.Level);
            Assert.IsTrue(levels.Values.All(level => level is >= 24 and <= 28));
            Assert.AreEqual(27U, levels[H.RoyalGuard]);
            Assert.AreEqual(26U, levels[H.Taskmaster]);
            Assert.AreEqual(25U, levels[H.Defender]);

            var sal = world.Set<SpawnPoolEntry>().Single(pool => pool.Id == H.SalPool);
            Assert.AreEqual(1761U, sal.MapContextId);
            Assert.AreEqual(H.Sal, sal.Creature1Id);
            Assert.AreEqual(9659U, world.Set<CreatureEntry>().Single(c => c.Id == H.Sal).NameId);
            Assert.IsTrue(world.Set<CreatureAppearanceEntry>().Any(a => a.Id == H.Sal));
            Assert.AreEqual(H.JohnPackage, world.Set<NpcPackageEntry>().Single(p => p.Id == H.John).PackageId);
            Assert.AreEqual(4095U, world.Set<CreatureAppearanceEntry>().Single(a => a.Id == H.John && a.SlotId == 13).ClassId);

            // Each marker is the client's own name for it (Brann Corpse, Brann Camp, Ojasa Hive Atta
            // Queen, Ojasa Atta Hive), or none, and the client names it by its objective.
            var markers = world.Set<MissionIndicatorEntry>().Where(row => row.ContentRevision == H.Revision).ToList();
            Assert.AreEqual(9, markers.Count);
            Assert.IsTrue(markers.All(row => new[] { 234U, 235U, 236U, 278U }.Contains(row.IndicatorId) ||
                row.IndicatorId > Rasa.Structures.MissionIndicator.UnnamedFrom), string.Join(", ", markers.Select(row => row.IndicatorId)));

            // Every reward of Brann Bacon and Long Live the Queen carries its brand's module.
            foreach (var missionId in new[] { H.BrannBacon, H.LongLiveTheQueen })
                Assert.IsTrue(world.Set<MissionRewardItemEntry>().Where(item => item.MissionId == missionId)
                    .ToList().All(item => item.Module1 != 0), $"mission {missionId}");
        }

        [TestMethod]
        public void SalsCanisterGoesToJohnAndTheFlamethrowerTestIsProtectJohn()
        {
            using var harness = Create();
            var john = SpawnJohn(harness);
            var sal = Put(harness, H.Sal, H.JohnAtCamp + new Vector3(4, 0, 0));

            WildernessTwinPillarsTests.Accept(harness, sal, H.FriedAtta);
            Assert.AreEqual(1, WildernessTwinPillarsTests.Held(harness, H.CanisterTemplate));

            WildernessTwinPillarsTests.Talk(harness, john, H.FriedAtta, 1);
            Assert.AreEqual(0, WildernessTwinPillarsTests.Held(harness, H.CanisterTemplate));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, H.FriedAtta, 2));

            // Far from John nothing comes; at his side, six Atta out of the tunnel, at him.
            harness.MoveTo(H.Entrance);
            Work(harness);
            Assert.AreEqual(0, Living(harness, H.WaveAtta).Count);

            harness.MoveTo(H.JohnAtCamp + new Vector3(3, 0, 0));
            Work(harness);
            var wave = Living(harness, H.WaveAtta);
            Assert.AreEqual(6, wave.Count);
            Assert.IsTrue(wave.All(atta => Hates(atta, john.EntityId)));
            Assert.IsTrue(wave.All(atta => Vector3.Distance(atta.Position, H.WaveSpawns[0]) < 120));

            foreach (var atta in wave)
                Kill(harness, atta);
            Work(harness);

            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, H.FriedAtta, 2));
            WildernessTwinPillarsTests.Reward(harness, john, H.FriedAtta, 2, 47597, 12500, 2500);
        }

        [TestMethod]
        public void TheTestIsCalledOffWhenJohnIsBadlyHurtAndCanBeRunAgain()
        {
            using var harness = Create();
            var john = SpawnJohn(harness);
            StartOn(harness, H.FriedAtta, 1);

            harness.MoveTo(H.JohnAtCamp + new Vector3(3, 0, 0));
            Work(harness);
            Assert.AreEqual(6, Living(harness, H.WaveAtta).Count);

            var health = john.Attributes[Attributes.Health];
            health.Current = (int)(health.CurrentMax * 0.1);
            Work(harness);

            Assert.AreEqual(0, Living(harness, H.WaveAtta).Count, "the wave is taken away");
            Assert.AreEqual(health.CurrentMax, health.Current, "John is healed");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, H.FriedAtta, 2));

            Work(harness);
            Assert.AreEqual(0, Living(harness, H.WaveAtta).Count, "not at once");

            _now += OjasaHive.WaveRearmMs;
            Work(harness);
            Assert.AreEqual(6, Living(harness, H.WaveAtta).Count);
        }

        [TestMethod]
        public void TheBrannCorpseGivesTheMedallionAndTheBinsTheCamp()
        {
            using var harness = Create();
            var john = SpawnJohn(harness);
            Completed(harness, H.FriedAtta);
            Work(harness);

            var corpse = Object(harness, H.BrannCorpseClass);
            Assert.AreEqual(H.BrannCorpse, corpse.Position);
            Assert.AreEqual(2, harness.Map.DynamicObjects.Count(obj => (uint)obj.EntityClassId == H.BrannBinClass));

            WildernessTwinPillarsTests.Accept(harness, john, H.SomethingSmells);
            Use(harness, corpse);
            Assert.AreEqual(1, WildernessTwinPillarsTests.Held(harness, H.MedallionTemplate));
            Assert.IsTrue(harness.Client.Player.Missions[H.SomethingSmells].Completeable);

            WildernessTwinPillarsTests.Reward(harness, john, H.SomethingSmells, 0, 111230, 12500, 2500);
            Assert.AreEqual(0, WildernessTwinPillarsTests.Held(harness, H.MedallionTemplate), "the medallion goes with the turn-in");

            WildernessTwinPillarsTests.Accept(harness, john, H.BrannBacon);
            Use(harness, harness.Map.DynamicObjects.First(obj => (uint)obj.EntityClassId == H.BrannBinClass));
            Assert.IsTrue(harness.Client.Player.Missions[H.BrannBacon].Completeable);
        }

        [TestMethod]
        public void JohnGoesWhereTheStoryIs()
        {
            using var harness = Create();
            var john = SpawnJohn(harness);

            Work(harness);
            Assert.IsTrue(Vector3.Distance(john.Position, H.JohnAtCamp) < 1);

            StartOn(harness, H.BrannBacon, 1);
            Work(harness);
            Assert.IsTrue(Vector3.Distance(john.Position, H.JohnAtCellThree) < 1, $"John at {john.Position}");
            Assert.AreEqual(BehaviorManager.BehaviorActionIdle, john.Controller.CurrentAction, "his own again, not a scene's");

            harness.Client.Player.MissionHistory[H.AttaOughtToDoIt] = MissionState.Completed;
            Work(harness);
            Assert.IsTrue(Vector3.Distance(john.Position, H.JohnAtChamber) < 1);

            // Not while he is fighting.
            var atta = Put(harness, H.AttaSoldier, H.JohnAtChamber + new Vector3(2, 0, 0));
            john.Hate.Ensure(atta.EntityId, Threat.NoticedThreat);
            harness.Client.Player.MissionHistory.Remove(H.AttaOughtToDoIt);
            Work(harness);
            Assert.IsTrue(Vector3.Distance(john.Position, H.JohnAtChamber) < 1);
        }

        [TestMethod]
        public void TheShockingDeviceHatchesTwoYoungAttaAndWaitsForThem()
        {
            using var harness = Create();
            SpawnJohn(harness);
            StartOn(harness, H.AttaOughtToDoIt, 1);
            Work(harness);

            var device = Object(harness, H.ShockingDeviceClass);
            Assert.AreEqual(H.ShockingDeviceName, device.NameOverrideId);
            Assert.AreEqual(UseObjectState.ThreeStateSwithState0, device.StateId);

            Use(harness, device);
            Assert.AreEqual(UseObjectState.ThreeStateSwithState1, device.StateId);
            Assert.AreEqual(0, Living(harness, H.YoungAtta).Count, "not before the shock is over");

            _now += OjasaHive.DeviceShockMs;
            Work(harness);
            var young = Living(harness, H.YoungAtta);
            Assert.AreEqual(2, young.Count);
            Assert.IsTrue(young.All(atta => Vector3.Distance(atta.Position, H.Eggs) < 4));
            Assert.IsTrue(young.All(atta => Hates(atta, harness.Client.Player.EntityId)));

            Use(harness, device);
            _now += OjasaHive.DeviceRearmMs;
            Work(harness);
            Assert.AreEqual(2, Living(harness, H.YoungAtta).Count, "spent while its two live");

            Kill(harness, young[0]);
            Work(harness);
            Assert.IsFalse(harness.Client.Player.Missions[H.AttaOughtToDoIt].Completeable, "both");

            Kill(harness, young[1]);
            Work(harness);
            Assert.IsTrue(harness.Client.Player.Missions[H.AttaOughtToDoIt].Completeable);
            Assert.AreEqual(UseObjectState.ThreeStateSwithState0, device.StateId);
        }

        [TestMethod]
        public void HelpingTheAttaTurnsTheHiveAndJohnCleansUp()
        {
            using var harness = Create();
            var john = SpawnJohn(harness);
            Completed(harness, H.AttaOughtToDoIt);
            Work(harness);
            WildernessTwinPillarsTests.Accept(harness, john, H.LongLiveTheQueen);

            CheckOnTheQueen(harness);
            var atta = Brawlers(harness, H.BrawlAtta);
            var brann = Brawlers(harness, H.BrannRaider);
            Assert.AreEqual(3, atta.Count);
            Assert.AreEqual(3, brann.Count);
            Assert.IsTrue(atta.Concat(brann).All(c => c.TargetCategory == TargetCategory.Neutral), "nobody's side yet");

            // At it, and nobody the worse until a side is taken.
            atta[0].Attributes[Attributes.Health].Current = 10;
            Work(harness);
            Assert.AreEqual(atta[0].Attributes[Attributes.Health].CurrentMax, atta[0].Attributes[Attributes.Health].Current);
            Assert.IsTrue(Hates(atta[0], brann[0].EntityId));

            Hit(harness, brann[0]);
            Assert.AreEqual(OjasaHive.Side.Atta, OjasaHive.ChosenSide(harness.Map));
            Assert.IsTrue(atta.All(c => c.TargetCategory == TargetCategory.Friendly));
            Assert.IsTrue(brann.All(c => c.TargetCategory == TargetCategory.Hostile));
            Assert.IsTrue(brann.All(c => Hates(c, harness.Client.Player.EntityId)));

            Work(harness);
            Assert.AreEqual(MissionObjectiveState.Failed, Objective(harness, H.LongLiveTheQueen, 3));

            // The hive's Atta are on the player's side now.
            var hiveAtta = Put(harness, H.AttaSoldier, H.JohnAtChamber + new Vector3(5, 0, 5));
            Work(harness);
            Assert.AreEqual(TargetCategory.Friendly, hiveAtta.TargetCategory);

            foreach (var raider in brann)
                Kill(harness, raider);
            Work(harness);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, H.LongLiveTheQueen, 2));
            Assert.IsTrue(harness.Client.Player.Missions[H.LongLiveTheQueen].Completeable);

            WildernessTwinPillarsTests.Reward(harness, john, H.LongLiveTheQueen, 1, 47728, 20000, 4000);
            Assert.AreEqual(TargetCategory.Neutral, hiveAtta.TargetCategory, "John turns on the Atta about him");
            Assert.IsTrue(Hates(john, hiveAtta.EntityId));

            Work(harness);
            Assert.AreEqual(TargetCategory.Neutral, hiveAtta.TargetCategory, "and they stay his to fight");
        }

        [TestMethod]
        public void HelpingTheBrannLeavesTheHiveHostile()
        {
            using var harness = Create();
            var john = SpawnJohn(harness);
            Completed(harness, H.AttaOughtToDoIt);
            Work(harness);
            WildernessTwinPillarsTests.Accept(harness, john, H.LongLiveTheQueen);
            CheckOnTheQueen(harness);

            var atta = Brawlers(harness, H.BrawlAtta);
            Hit(harness, atta[1]);
            Assert.AreEqual(OjasaHive.Side.Brann, OjasaHive.ChosenSide(harness.Map));
            Work(harness);
            Assert.AreEqual(MissionObjectiveState.Failed, Objective(harness, H.LongLiveTheQueen, 2));

            foreach (var a in atta)
                Kill(harness, a);
            Work(harness);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, H.LongLiveTheQueen, 3));
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, H.LongLiveTheQueen, 4));

            var hiveAtta = Put(harness, H.AttaSoldier, H.JohnAtChamber + new Vector3(5, 0, 5));
            Work(harness);
            Assert.AreEqual(TargetCategory.Hostile, hiveAtta.TargetCategory);
        }

        [TestMethod]
        public void TheChamberEnteredEarlyDoesNotLoseTheBrawl()
        {
            using var harness = Create();
            var john = SpawnJohn(harness);
            Completed(harness, H.AttaOughtToDoIt);

            harness.MoveTo(H.Throne + new Vector3(-20, -14, 40));
            Work(harness);
            Assert.AreEqual(0, Brawlers(harness, H.BrawlAtta).Count, "no brawl without the mission");

            WildernessTwinPillarsTests.Accept(harness, john, H.LongLiveTheQueen);
            CheckOnTheQueen(harness);
            Assert.AreEqual(3, Brawlers(harness, H.BrawlAtta).Count);
        }

        [TestMethod]
        public void ALateComerIsGivenTheCopysChoiceAndOutcome()
        {
            using var harness = Create();
            var john = SpawnJohn(harness);
            Completed(harness, H.AttaOughtToDoIt);
            Work(harness);

            var state = OjasaHive.StateOf(harness.Map);
            state.Chosen = OjasaHive.Side.Brann;
            state.Won = true;
            state.BrawlBrann.Add(Put(harness, H.BrannRaider, H.Brawl));

            WildernessTwinPillarsTests.Accept(harness, john, H.LongLiveTheQueen);
            harness.MoveTo(H.Throne + new Vector3(-20, -14, 40));
            Work(harness);

            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, H.LongLiveTheQueen, 1));
            Assert.AreEqual(MissionObjectiveState.Failed, Objective(harness, H.LongLiveTheQueen, 2));
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, H.LongLiveTheQueen, 3));
            Assert.IsTrue(harness.Client.Player.Missions[H.LongLiveTheQueen].Completeable);
        }

        [TestMethod]
        public void APlayerStuckInASulfurLakeIsPutBackOnDryGround()
        {
            using var harness = Create();
            var dry = H.ShockingDevice;
            harness.MoveTo(dry);
            Work(harness);

            var wet = new Vector3(-180f, 99.1f, -330f);
            Assert.IsTrue(OjasaHive.InLake(wet));
            Assert.IsFalse(OjasaHive.InLake(dry));
            harness.MoveTo(wet);
            Work(harness);
            _now += OjasaHive.LakeRescueMs - 200;
            Work(harness);
            Assert.AreEqual(wet, harness.Client.Player.Position, "a swim is not being stuck");

            Work(harness);
            Assert.AreEqual(dry, harness.Client.Player.Position);
        }

        [TestMethod]
        public void NothingStandsOnTheLakesOrInAFightWhereThePeopleAre()
        {
            foreach (var spot in new[] { H.JohnAtCamp, H.JohnAtCellThree, H.JohnAtChamber, H.BrannCorpse, H.ShockingDevice, H.Eggs, H.Brawl }
                         .Concat(H.BrannBins).Concat(H.WaveSpawns).Concat(H.BrawlBrann).Concat(H.BrawlAttaSpots))
                Assert.IsFalse(OjasaHive.InLake(spot), $"{spot}");
        }

        [TestMethod]
        public void DownGivesTheHiveBackTheInclinesAttaAndUpAgainChangesNothing()
        {
            const string migration = "20270216000000_Ojasa_hive", previous = "20270215000000_Ojasa_atta_colony_regions";
            using var database = new Rasa.Test.Database.OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(previous);
            var before = Count(database, $"SELECT COUNT(*) AS Value FROM spawnpool WHERE map_context_id = {H.Hive} AND creature_1_Id = {H.InclineAttaSoldier}");
            Assert.IsTrue(before > 0);

            migrator.Migrate(migration);
            Assert.AreEqual(0, Count(database, $"SELECT COUNT(*) AS Value FROM spawnpool WHERE map_context_id = {H.Hive} AND creature_1_Id = {H.InclineAttaSoldier}"));
            Assert.AreEqual(5, Count(database, $"SELECT COUNT(*) AS Value FROM mission_content_definition WHERE content_revision = '{H.Revision}' AND enabled = 1"));
            var rows = Count(database, "SELECT COUNT(*) AS Value FROM creature WHERE id BETWEEN 565000 AND 565999");

            foreach (var sql in H.Up().Where(sql => !sql.StartsWith("INSERT INTO mission") && !sql.StartsWith("UPDATE mission")))
                database.Context.Database.ExecuteSqlRaw(sql);
            Assert.AreEqual(rows, Count(database, "SELECT COUNT(*) AS Value FROM creature WHERE id BETWEEN 565000 AND 565999"), "guarded");

            migrator.Migrate(previous);
            Assert.AreEqual(before, Count(database, $"SELECT COUNT(*) AS Value FROM spawnpool WHERE map_context_id = {H.Hive} AND creature_1_Id = {H.InclineAttaSoldier}"));
            foreach (var table in new[] { "creature", "creature_action", "creature_stat", "creature_appearance", "spawnpool", "npc_package" })
                Assert.AreEqual(0, Count(database, $"SELECT COUNT(*) AS Value FROM {table} WHERE id BETWEEN 565000 AND 565999"), table);
            Assert.AreEqual(0, Count(database, $"SELECT COUNT(*) AS Value FROM mission_content_definition WHERE content_revision = '{H.Revision}'"));

            migrator.Migrate(migration);
            Assert.AreEqual(5, Count(database, $"SELECT COUNT(*) AS Value FROM mission_content_definition WHERE content_revision = '{H.Revision}' AND enabled = 1"));
        }

        #region Helpers

        private static long Count(Rasa.Test.Database.OrdinaryLootTestDatabase database, string sql) =>
            database.Context.Database.SqlQueryRaw<long>(sql).ToArray().Single();

        private static WildernessRuntimeTestHarness Create()
        {
            var harness = WildernessRuntimeTestHarness.Create(mapContextId: H.Hive);
            try
            {
                using var unit = harness.CreateChar();
                unit.ExecuteTransaction(() => unit.CharacterStartingExperience.Add(
                    new CharacterStartingExperienceEntry(harness.Client.Player.Id, H.Revision,
                        CharacterStartingExperienceState.Skipped)));
                harness.Client.Player.StartingExperienceCompleted = true;
                harness.Drain();
                return harness;
            }
            catch
            {
                harness.Dispose();
                throw;
            }
        }

        private static bool Hates(Creature creature, ulong entityId) => creature.Hate.Ranked().Any(entry => entry.Key == entityId);

        private static Creature SpawnJohn(WildernessRuntimeTestHarness harness)
        {
            harness.SpawnWorld(H.JohnPool);
            return WildernessTwinPillarsTests.RequireNpc(harness, H.JohnPool, H.John);
        }

        private static Creature Put(WildernessRuntimeTestHarness harness, uint dbId, Vector3 position)
        {
            var creature = CreatureManager.Instance.CreateCreature(dbId, null);
            CreatureManager.Instance.SetLocation(creature, NavMeshManager.SnapToGround(harness.Map, position), 0, H.Hive);
            CellManager.Instance.AddToWorld(harness.Map, creature);
            return creature;
        }

        private void Work(WildernessRuntimeTestHarness harness)
        {
            _now += 100;
            OjasaHive.Worker(harness.Map);
            harness.Drain();
        }

        private static List<Creature> Living(WildernessRuntimeTestHarness harness, uint dbId) =>
            harness.Map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList).Distinct()
                .Where(c => c.DbId == dbId && c.State != CharacterState.Dead && EntityManager.Instance.Creatures.ContainsKey(c.EntityId)).ToList();

        private static List<Creature> Brawlers(WildernessRuntimeTestHarness harness, uint dbId) =>
            Living(harness, dbId).OrderBy(c => c.EntityId).ToList();

        private static DynamicObject Object(WildernessRuntimeTestHarness harness, uint classId) =>
            harness.Map.DynamicObjects.Single(obj => (uint)obj.EntityClassId == classId);

        private static MissionObjectiveState Objective(WildernessRuntimeTestHarness harness, uint missionId, uint objectiveId) =>
            harness.Client.Player.Missions[missionId].Objectives[objectiveId].State;

        private static void Kill(WildernessRuntimeTestHarness harness, Creature creature)
        {
            harness.MoveTo(creature.Position + new Vector3(2, 0, 0));
            var lethal = creature.Attributes[Attributes.Health].Current + Math.Max(0, creature.Attributes[Attributes.Armor].Current);
            ActorManager.Instance.Damage(harness.Map, creature, lethal, harness.Client.Player);
            harness.Drain();
        }

        private static void Hit(WildernessRuntimeTestHarness harness, Creature creature)
        {
            harness.MoveTo(creature.Position + new Vector3(4, 0, 0));
            ActorManager.Instance.Damage(harness.Map, creature, 10, harness.Client.Player);
            harness.Drain();
        }

        private static void Use(WildernessRuntimeTestHarness harness, DynamicObject obj)
        {
            harness.MoveTo(obj.Position + new Vector3(1, 0, 0));
            harness.Objects.RequestUseObjectPacket(harness.Client, new RequestUseObjectPacket
            {
                EntityId = obj.EntityId,
                ActionId = ActionId.UseObject,
                ActionArgId = DynamicObjectManager.FootlockerUseArgId
            });
            ActorActionManager.Instance.DoWork(harness.Map, 10000);
            harness.Drain();
        }

        private void CheckOnTheQueen(WildernessRuntimeTestHarness harness)
        {
            harness.MoveTo(H.Throne + new Vector3(-20, -14, 40));
            Work(harness);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, H.LongLiveTheQueen, 1));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, H.LongLiveTheQueen, 4));
            Assert.AreEqual(1u, harness.Client.Player.CameraScriptId, "the fly-over plays");
            Work(harness);
        }

        /// <summary>The mission done before, as a history row the server keeps.</summary>
        private static void Completed(WildernessRuntimeTestHarness harness, uint missionId)
        {
            foreach (var earlier in H.Missions.Where(id => id <= missionId))
            {
                using var unit = harness.CreateChar();
                unit.ExecuteTransaction(() => unit.CharacterMissions.Runtime.Archive(
                    new CharacterMissionEntry(harness.Client.Player.Id, earlier, (uint)MissionState.Completed)
                    {
                        AssignmentId = Guid.NewGuid().ToString("N"),
                        Generation = 1,
                        ContentRevision = H.Revision
                    }, harness.UtcNow));
                harness.Client.Player.MissionHistory[earlier] = MissionState.Completed;
                harness.Client.Player.MissionSuccessHistory.Add(earlier);
            }
        }

        /// <summary>The missions before it done and this one taken from John.</summary>
        private static void StartOn(WildernessRuntimeTestHarness harness, uint missionId, uint objectiveId)
        {
            if (missionId > H.FriedAtta)
                Completed(harness, missionId - 1);

            if (missionId == H.FriedAtta)
            {
                var sal = Put(harness, H.Sal, H.JohnAtCamp + new Vector3(4, 0, 0));
                WildernessTwinPillarsTests.Accept(harness, sal, H.FriedAtta);
                WildernessTwinPillarsTests.Talk(harness, OjasaHive.John(harness.Map), H.FriedAtta, 1);
                return;
            }

            WildernessTwinPillarsTests.Accept(harness, OjasaHive.John(harness.Map), missionId);
        }

        #endregion
    }
}
