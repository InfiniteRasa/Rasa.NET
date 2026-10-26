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
using Rasa.Structures;
using Rasa.Structures.Char;
using Rasa.Structures.World;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions
{
    using D = Rasa.Services.Preloader.DevilsDenContent;

    /// <summary>
    /// The Devil's Den as the walkthrough tells it (DevilsDenContent, DevilsDen): its rows, the
    /// Hightower rangers and their missions, the corpses and artifact stones, the egg clusters
    /// and the machinery that count, the bombs, the laser door, the teleporters, the howitzer
    /// panels, Fransisco's company and the Comm Tower's region.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class DevilsDenTests
    {
        private const string Migration = "20270305000000_Devils_den", Previous = "20270224000000_Logos_shrine_heights";

        private long _now = 1_000_000;

        [TestInitialize]
        public void Clock()
        {
            DevilsDen.Reset();
            DevilsDen.Now = () => _now;
            PlacedDestructibles.Now = () => _now;
            PlacedUsables.Now = () => _now;
        }

        [TestCleanup]
        public void Restore()
        {
            DevilsDen.Reset();
            DevilsDen.Now = () => Environment.TickCount64;
            PlacedDestructibles.Load(null);
            PlacedDestructibles.Reset();
            PlacedUsables.Load(null);
            PlacedUsables.Reset();
        }

        [TestMethod]
        public void TheDenHasItsPeopleItsCreaturesAtTheWalkthroughsLevelsItsObjectsAndEightMissions()
        {
            using var harness = Create();
            var world = harness.World;

            foreach (var missionId in D.Missions)
            {
                Assert.IsTrue(harness.Manager.TryGetOperationalMission(missionId, out var mission), $"mission {missionId}");
                Assert.AreEqual(D.Category, mission.CategoryId);
            }

            // The Den's pools are its own rows now, at 19 to 21; the Palisades keep theirs.
            var den = world.Set<SpawnPoolEntry>().Where(pool => pool.MapContextId == D.Den).ToList();
            var palisadesRows = D.Repointed.Select(pair => pair.From).ToArray();
            Assert.IsFalse(den.Any(pool => palisadesRows.Contains(pool.Creature1Id) || palisadesRows.Contains(pool.Creature2Id) || palisadesRows.Contains(pool.Creature3Id)));
            Assert.AreEqual(11, den.Count(pool => pool.Creature1Id == D.JasperFithik && pool.Id is >= 531515 and <= 531525));
            Assert.AreEqual(D.HiveMaster, den.Single(pool => pool.Id == 520038).Creature1Id);
            Assert.IsTrue(world.Set<SpawnPoolEntry>().Any(pool => pool.MapContextId == D.Palisades && pool.Creature1Id == 531068),
                "the Palisades keep their own Fithik");

            var creatures = world.Set<CreatureEntry>().Where(c => c.Id >= 567010 && c.Id <= 567040).ToDictionary(c => c.Id);
            Assert.IsTrue(creatures.Values.All(c => c.Level is >= 19 and <= 23), string.Join(", ", creatures.Values.Select(c => $"{c.Id}:{c.Level}")));
            Assert.AreEqual(21U, creatures[D.HiveMaster].Level);
            Assert.AreEqual(3300U, creatures[D.HiveMaster].MaxHitPoints);
            Assert.AreEqual(23U, creatures[D.Qraal].Level);
            Assert.AreEqual(20U, creatures[D.Yzub].Level);
            Assert.AreEqual(21U, creatures[D.Kryell].Level);

            // The Hightower rangers beside Cyrida; Fransisco with his launcher; the packages the objectives talk to.
            foreach (var ranger in new[] { D.Kogari, D.Siri, D.Lurori })
            {
                var pool = world.Set<SpawnPoolEntry>().Single(p => p.Creature1Id == ranger);
                Assert.AreEqual(D.Palisades, pool.MapContextId);
                Assert.IsTrue(Vector3.Distance(new Vector3((float)pool.PosX, (float)pool.PosY, (float)pool.PosZ), D.CyridaAt) < 6, $"ranger {ranger}");
            }

            Assert.AreEqual(D.RocketLauncher, world.Set<CreatureAppearanceEntry>().Single(a => a.Id == D.Fransisco && a.SlotId == 13).ClassId);
            foreach (var (creature, package, _) in D.Packages)
                Assert.AreEqual(package, world.Set<NpcPackageEntry>().Single(p => p.Id == creature).PackageId);

            // 50 egg clusters, the machinery, the teleporters; the corpses, stones, panels and bombs; the crate of five detonators.
            var breakables = world.Set<WorldDestructibleEntry>().Where(row => row.MapContextId == D.Den).ToList();
            Assert.AreEqual(50, breakables.Count(row => row.ClassId == D.EggClusterClass && row.CreatureId == D.RoseFithikYoung));
            Assert.AreEqual(6, breakables.Count(row => row.ClassId == D.MachineryClass));
            Assert.AreEqual(4, breakables.Count(row => row.ClassId == D.BaneTeleporterClass));
            var usables = world.Set<WorldUsableEntry>().Where(row => row.MapContextId == D.Den).ToList();
            Assert.AreEqual(5, usables.Count(row => row.ItemTemplateId == D.ArtifactTemplate));
            Assert.AreEqual(3, usables.Count(row => row.ClassId == D.BombClass && row.ItemClassId == D.DetonatorClass));
            Assert.AreEqual(5U, world.Set<WorldContainerItemEntry>().Single(item => item.ContainerId == D.DetonatorCrate).Quantity);

            // The counted objectives name their counters; each armour and weapon reward carries a module.
            Assert.IsTrue(harness.Manager.TryGetOperationalMission(D.DevilsStrike, out var strike));
            CollectionAssert.AreEqual(new uint[] { 1, 2 }, strike.Objectives.Keys.OrderBy(id => id).ToArray());
            foreach (var missionId in new[] { D.DevilsStrike, D.MissingStrikeTeam, D.FinishTheTask, D.SeverTheHead })
                Assert.IsTrue(world.Set<MissionRewardItemEntry>().Where(item => item.MissionId == missionId).ToList().All(item => item.Module1 != 0), $"mission {missionId}");
        }

        [TestMethod]
        public void HakarisCorpseGivesHisBandAndSiriTakesIt()
        {
            using var harness = Create();
            Place(harness);
            var siri = Put(harness, D.Siri, D.HakariAt + new Vector3(3, 0, 0));

            WildernessTwinPillarsTests.Accept(harness, siri, D.HakarisBand);
            Use(harness, Usable(harness, D.HakariCorpse));

            Assert.AreEqual(1, WildernessTwinPillarsTests.Held(harness, D.HakariBandTemplate));
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, D.HakarisBand, 1));
            WildernessTwinPillarsTests.Reward(harness, siri, D.HakarisBand, 1, 18633, 19000, 2850);
            Assert.AreEqual(0, WildernessTwinPillarsTests.Held(harness, D.HakariBandTemplate), "the band goes with the turn-in");
        }

        [TestMethod]
        public void FiveArtifactStonesAndKogariHearsOfIt()
        {
            using var harness = Create();
            Place(harness);
            var kogari = Put(harness, D.Kogari, D.Art1At + new Vector3(3, 0, 0));

            WildernessTwinPillarsTests.Accept(harness, kogari, D.ElohArtifacts);

            for (var i = 0; i < 5; i++)
            {
                Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, D.ElohArtifacts, 1), $"stone {i}");
                Use(harness, Usable(harness, D.FirstArtifact + (uint)i));
            }

            Assert.AreEqual(5, WildernessTwinPillarsTests.Held(harness, D.ArtifactTemplate));
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, D.ElohArtifacts, 1));
            WildernessTwinPillarsTests.Talk(harness, kogari, D.ElohArtifacts, 2);
            WildernessTwinPillarsTests.Reward(harness, kogari, D.ElohArtifacts, 0, 17514, 38000, 3800);
        }

        [TestMethod]
        public void EachEggClusterDestroyedCountsAndTheHiveMasterDeadIsDevilsStrike()
        {
            using var harness = Create();
            Place(harness);
            var brody = Put(harness, D.Brody, D.HatcheryAt + new Vector3(3, 0, 0));
            WildernessTwinPillarsTests.Accept(harness, brody, D.DevilsStrike);
            harness.MoveTo(D.HatcheryAt);

            var clusters = Enumerable.Range(0, 50).Select(i => PlacedDestructibles.LiveOf(harness.Map, D.FirstEggCluster + (uint)i)).ToList();
            Assert.IsTrue(clusters.All(live => live != null && !live.IsDown));

            foreach (var live in clusters.Take(49))
                PlacedDestructibles.Destroy(live, harness.Client.Player);
            Work(harness);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, D.DevilsStrike, 2), "49 of 50");

            // A cluster back and down again is the same one: still 49.
            PlacedDestructibles.Respawn(clusters[0]);
            Work(harness);
            PlacedDestructibles.Destroy(clusters[0], harness.Client.Player);
            Work(harness);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, D.DevilsStrike, 2));

            PlacedDestructibles.Destroy(clusters[49], harness.Client.Player);
            Work(harness);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, D.DevilsStrike, 2));

            Kill(harness, Put(harness, D.HiveMaster, D.HiveMasterAt));
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, D.DevilsStrike, 1));
            WildernessTwinPillarsTests.Reward(harness, brody, D.DevilsStrike, 1, 20717, 34000, 3400);
        }

        [TestMethod]
        public void TheMachineryCountsAndTheDepotTakesABomb()
        {
            using var harness = Create();
            Place(harness);
            var brody = Put(harness, D.Brody, D.QraalAt + new Vector3(3, 0, 0));
            WildernessTwinPillarsTests.Accept(harness, brody, D.SeverTheHead);
            harness.MoveTo(D.PadAAt);

            for (var row = D.FirstNode; row < D.FirstPad + 3; row++)
                PlacedDestructibles.Destroy(PlacedDestructibles.LiveOf(harness.Map, row), harness.Client.Player);
            Work(harness);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, D.SeverTheHead, 3));
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, D.SeverTheHead, 4));
            Assert.IsTrue(PlacedDestructibles.LiveOf(harness.Map, D.FirstNode).RespawnAt >= _now + DevilsDen.MachineryDownMs, "down a while");

            // The fuel depot is no target; the bomb beside it, gone off, takes it down.
            var depot = PlacedDestructibles.LiveOf(harness.Map, D.FuelDepot);
            Assert.IsFalse(depot.IsDown);
            PlacedUsables.SetState(PlacedUsables.LiveOf(harness.Map, D.DepotBomb), UseObjectState.BombStateDetonated, 0);
            Work(harness);
            Assert.IsTrue(depot.IsDown);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, D.SeverTheHead, 5));

            Kill(harness, Put(harness, D.Qraal, D.QraalAt));
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, D.SeverTheHead, 1));
            WildernessTwinPillarsTests.Reward(harness, brody, D.SeverTheHead, 2, 29023, 38000, 3800);
        }

        [TestMethod]
        public void TheLaserDoorFallsToItsConsoleOrTheGeneratorsBomb()
        {
            using var harness = Create();
            Place(harness);
            harness.MoveTo(D.ConsoleAt);
            Work(harness);
            Assert.IsTrue(DevilsDen.DoorStands(harness.Map));

            // The console on: down, and kept down.
            var console = Usable(harness, D.DoorConsole);
            Use(harness, console);
            Assert.AreEqual(UseObjectState.TsState1, console.StateId);
            Work(harness);
            Assert.IsFalse(DevilsDen.DoorStands(harness.Map));
            _now += PlacedDestructibles.RespawnMaxMs + 1000;
            PlacedDestructibles.Worker(harness.Map);
            Work(harness);
            Assert.IsFalse(DevilsDen.DoorStands(harness.Map), "the console still on");

            // Off: back when it is due.
            Use(harness, console);
            Assert.AreEqual(UseObjectState.TsState0, console.StateId);
            _now += PlacedDestructibles.RespawnMaxMs + 1000;
            PlacedDestructibles.Worker(harness.Map);
            Work(harness);
            Assert.IsTrue(DevilsDen.DoorStands(harness.Map));

            // The generator's bomb gone off: the generator down, and the door with it.
            PlacedUsables.SetState(PlacedUsables.LiveOf(harness.Map, D.GeneratorBomb), UseObjectState.BombStateDetonated, 0);
            Work(harness);
            Assert.IsTrue(PlacedDestructibles.LiveOf(harness.Map, D.GeneratorRow).IsDown);
            Work(harness);
            Assert.IsFalse(DevilsDen.DoorStands(harness.Map));
        }

        [TestMethod]
        public void TheTeleportersSendBaneUntilTheyAreDestroyed()
        {
            using var harness = Create();
            Place(harness);
            var teleporter = PlacedDestructibles.LiveOf(harness.Map, D.FirstTeleporter);

            harness.MoveTo(D.Entrance);
            Work(harness);
            Assert.AreEqual(0, DevilsDen.SentBy(harness.Map, D.FirstTeleporter).Count, "nobody near");

            harness.MoveTo(D.Tele1At + new Vector3(10, 0, 0));
            Work(harness);
            Assert.AreEqual(1, DevilsDen.SentBy(harness.Map, D.FirstTeleporter).Count);
            Assert.IsTrue(D.TeleportedBane.Contains(DevilsDen.SentBy(harness.Map, D.FirstTeleporter)[0].DbId));
            Work(harness);
            Assert.AreEqual(1, DevilsDen.SentBy(harness.Map, D.FirstTeleporter).Count, "one at a time");

            _now += DevilsDen.TeleporterIntervalMs;
            Work(harness);
            _now += DevilsDen.TeleporterIntervalMs;
            Work(harness);
            Assert.AreEqual(DevilsDen.TeleporterLimit, DevilsDen.SentBy(harness.Map, D.FirstTeleporter).Count, "no more than its limit about");

            foreach (var bane in DevilsDen.SentBy(harness.Map, D.FirstTeleporter).ToList())
                Kill(harness, bane);
            PlacedDestructibles.Destroy(teleporter, harness.Client.Player);
            harness.MoveTo(D.Tele1At + new Vector3(10, 0, 0));
            _now += DevilsDen.TeleporterIntervalMs;
            Work(harness);
            Assert.AreEqual(0, DevilsDen.SentBy(harness.Map, D.FirstTeleporter).Count, "destroyed, it sends none");
            Assert.IsTrue(teleporter.RespawnAt >= _now + DevilsDen.TeleporterDownMs - 1000);
        }

        [TestMethod]
        public void AHowitzerPanelTurnsTheNearerHowitzerOnTheBane()
        {
            using var harness = Create();
            Place(harness);
            var west = Put(harness, D.Howitzer, D.MortarWest);
            var east = Put(harness, D.Howitzer, D.MortarEast);
            var thrax = Put(harness, D.ThraxSoldier, D.MortarWest + new Vector3(-20, 0, -20));

            harness.MoveTo(D.PanelWAt);
            Use(harness, Usable(harness, D.WestPanel));
            Work(harness);

            Assert.AreEqual(TargetCategory.Friendly, west.TargetCategory);
            Assert.AreEqual(TargetCategory.Hostile, east.TargetCategory);
            Assert.IsTrue(west.Hate.Contains(thrax.EntityId), "after the Bane within its reach");

            Use(harness, Usable(harness, D.WestPanel));
            Work(harness);
            Assert.AreEqual(TargetCategory.Hostile, west.TargetCategory, "off, it is the Bane's again");
        }

        [TestMethod]
        public void FransiscoFollowsWhoFoundHimIsPutBesideThemAndKilledGoesBackToHisAlcove()
        {
            using var harness = Create();
            harness.SpawnWorld(D.FirstPool + 3);
            var fransisco = WildernessTwinPillarsTests.RequireNpc(harness, D.FirstPool + 3, D.Fransisco);
            var brody = Put(harness, D.Brody, D.FransiscoAt + new Vector3(3, 0, 0));

            WildernessTwinPillarsTests.Accept(harness, brody, D.MissingStrikeTeam);
            Work(harness);
            Assert.AreEqual(0U, DevilsDen.FransiscoFollows(harness.Map), "not found yet");

            WildernessTwinPillarsTests.Talk(harness, fransisco, D.MissingStrikeTeam, 1);
            harness.MoveTo(D.FransiscoAt + new Vector3(2, 0, 0));
            Work(harness);
            Assert.AreEqual(harness.Client.Player.Id, DevilsDen.FransiscoFollows(harness.Map));
            Assert.AreEqual(harness.Client.Player.Id, fransisco.SpawnPool.FollowOwnerCharacterId);
            Assert.AreEqual(D.MissingStrikeTeam, fransisco.SpawnPool.ScenarioMissionId);

            // Gone on where he cannot walk: put beside them.
            harness.MoveTo(D.CaritasAt + new Vector3(-6, 0, 10));
            Work(harness);
            Assert.IsTrue(Vector3.Distance(fransisco.Position, harness.Client.Player.Position) < 6f, $"{fransisco.Position}");

            // Killed: no one's, and his pool's again.
            Kill(harness, fransisco);
            Work(harness);
            Assert.AreEqual(0U, DevilsDen.FransiscoFollows(harness.Map));
            Assert.AreEqual(0U, fransisco.SpawnPool.FollowOwnerCharacterId);
        }

        [TestMethod]
        public void TheCommTowersRoomsAreItsRegion()
        {
            using var database = new Rasa.Test.Database.OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            var command = new Vector3(178f, 142f, 700f);
            var wing = new Vector3(225f, 151f, 640f);

            migrator.Migrate(Previous);
            var before = Volumes(database);
            Assert.AreEqual(0u, Shown(before, command), "nothing over the command center");
            Assert.AreEqual(2u, Shown(before, wing), "the Shard Impact Site's circle over the service wing");

            migrator.Migrate(Migration);
            var after = Volumes(database);
            Assert.AreEqual(3u, Shown(after, command));
            Assert.AreEqual(3u, Shown(after, new Vector3(225f, 143f, 690f)), "the teleporter hall");
            Assert.AreEqual(3u, Shown(after, wing));
            Assert.AreEqual(2u, Shown(after, new Vector3(221f, 136f, 530f)), "the impact site as it was");
        }

        [TestMethod]
        public void DownGivesTheDenBackThePalisadesRowsAndUpAgainChangesNothing()
        {
            using var database = new Rasa.Test.Database.OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            var fithik = $"SELECT COUNT(*) AS Value FROM spawnpool WHERE map_context_id = {D.Den} AND creature_1_Id = 531068";

            migrator.Migrate(Previous);
            var before = Count(database, fithik);
            Assert.AreEqual(11, before);

            migrator.Migrate(Migration);
            Assert.AreEqual(0, Count(database, fithik));
            Assert.AreEqual(8, Count(database, $"SELECT COUNT(*) AS Value FROM mission_content_definition WHERE content_revision = '{D.Revision}' AND enabled = 1"));
            var rows = Count(database, "SELECT COUNT(*) AS Value FROM creature WHERE id BETWEEN 567000 AND 567999");

            foreach (var sql in D.Up().Where(sql => !sql.StartsWith("INSERT INTO mission") && !sql.StartsWith("UPDATE mission")))
                database.Context.Database.ExecuteSqlRaw(sql);
            Assert.AreEqual(rows, Count(database, "SELECT COUNT(*) AS Value FROM creature WHERE id BETWEEN 567000 AND 567999"), "guarded");
            Assert.AreEqual(3, Count(database, $"SELECT COUNT(*) AS Value FROM map_region WHERE map_context_id = {D.Den} AND region_id = {D.CommTowerRegion}"), "guarded");

            migrator.Migrate(Previous);
            Assert.AreEqual(before, Count(database, fithik));
            foreach (var table in new[] { "creature", "creature_action", "creature_stat", "creature_appearance", "spawnpool", "npc_package",
                         "world_destructible", "world_usable", "world_container" })
                Assert.AreEqual(0, Count(database, $"SELECT COUNT(*) AS Value FROM {table} WHERE id BETWEEN 567000 AND 567999"), table);
            Assert.AreEqual(0, Count(database, $"SELECT COUNT(*) AS Value FROM mission_content_definition WHERE content_revision = '{D.Revision}'"));
            Assert.AreEqual(0, Count(database, $"SELECT COUNT(*) AS Value FROM map_region WHERE map_context_id = {D.Den} AND region_id = {D.CommTowerRegion}"));

            migrator.Migrate(Migration);
            Assert.AreEqual(8, Count(database, $"SELECT COUNT(*) AS Value FROM mission_content_definition WHERE content_revision = '{D.Revision}' AND enabled = 1"));
        }

        #region Helpers

        /// <summary>The Den's regions as the .map has them: priority and the settings they turn on (name 16).</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Regions = new()
        {
            [0] = (0, 31), [1] = (1, 13), [9] = (2, 23), [8] = (3, 0), [7] = (4, 23), [2] = (5, 23), [3] = (6, 23), [5] = (7, 23),
            [4] = (8, 23), [6] = (9, 23)
        };

        private static uint Shown(List<MapRegion> volumes, Vector3 at) =>
            volumes.Where(v => v.Enabled && Regions.TryGetValue(v.RegionId, out var r) && (r.Flags & 16) != 0 && v.Contains(at, false))
                .OrderByDescending(v => Regions[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(Rasa.Test.Database.OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == D.Den).AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static long Count(Rasa.Test.Database.OrdinaryLootTestDatabase database, string sql) =>
            database.Context.Database.SqlQueryRaw<long>(sql).ToArray().Single();

        private static WildernessRuntimeTestHarness Create()
        {
            var harness = WildernessRuntimeTestHarness.Create(mapContextId: D.Den);
            try
            {
                using var unit = harness.CreateChar();
                unit.ExecuteTransaction(() => unit.CharacterStartingExperience.Add(
                    new CharacterStartingExperienceEntry(harness.Client.Player.Id, D.Revision,
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

        /// <summary>The Den's destructibles and usables, from its rows, put down on the harness's map.</summary>
        private static void Place(WildernessRuntimeTestHarness harness)
        {
            PlacedDestructibles.Load(harness.World.Set<WorldDestructibleEntry>().AsNoTracking().Where(row => row.MapContextId == D.Den).ToList());
            PlacedUsables.Load(harness.World.Set<WorldUsableEntry>().AsNoTracking().Where(row => row.MapContextId == D.Den).ToList());
            Assert.AreEqual(D.Breakables.Length, PlacedDestructibles.Place(harness.Map));
            Assert.AreEqual(D.Usables.Length, PlacedUsables.Place(harness.Map));
        }

        private static DynamicObject Usable(WildernessRuntimeTestHarness harness, uint row) => PlacedUsables.LiveOf(harness.Map, row).Object;

        private static Creature Put(WildernessRuntimeTestHarness harness, uint dbId, Vector3 position)
        {
            var creature = CreatureManager.Instance.CreateCreature(dbId, null);
            CreatureManager.Instance.SetLocation(creature, NavMeshManager.SnapToGround(harness.Map, position), 0, D.Den);
            CellManager.Instance.AddToWorld(harness.Map, creature);
            return creature;
        }

        private void Work(WildernessRuntimeTestHarness harness)
        {
            _now += 100;
            DevilsDen.Worker(harness.Map);
            harness.Drain();
        }

        private static MissionObjectiveState Objective(WildernessRuntimeTestHarness harness, uint missionId, uint objectiveId) =>
            harness.Client.Player.Missions[missionId].Objectives[objectiveId].State;

        private static void Kill(WildernessRuntimeTestHarness harness, Creature creature)
        {
            harness.MoveTo(creature.Position + new Vector3(2, 0, 0));
            var lethal = creature.Attributes[Attributes.Health].Current + Math.Max(0, creature.Attributes[Attributes.Armor].Current);
            ActorManager.Instance.Damage(harness.Map, creature, lethal, harness.Client.Player);
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

        #endregion
    }
}
