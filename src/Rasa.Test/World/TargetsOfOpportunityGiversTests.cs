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
    using Rasa.Missions.Definitions;
    using Rasa.Missions.Runtime;
    using Rasa.Navigation;
    using Rasa.Services.Preloader;
    using Rasa.Structures;
    using Rasa.Test.Database;
    using Rasa.Test.Missions;
    using Rasa.Test.Missions.Wilderness;
    using Givers = Rasa.Services.Preloader.TargetsOfOpportunityGivers;

    /// <summary>
    /// Six battlefields' Targets of Opportunity given by their officers, the seven Mires officers
    /// and the Plains' Stalker Overlord stood up, and the two bosses that stood on the givers'
    /// ground moved off it (Targets_of_opportunity_givers).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class TargetsOfOpportunityGiversTests
    {
        private const string Migration = "20270111000000_Targets_of_opportunity_givers";
        private const string Previous = "20270110000100_Bootcamp_breach_assault";

        private const uint MiresAssassin = 733;
        private const uint Bushwacker = 487;

        private static readonly Dictionary<uint, string> Meshes = new()
        {
            [1148] = "adv_foreas_concordia_divide",
            [1759] = "adv_arieki_torden_mires",
            [1761] = "adv_arieki_torden_incline",
            [1764] = "adv_arieki_torden_plains",
            [2028] = "adv_arieki_torden_abyss"
        };

        #region The rows

        [TestMethod]
        public void TheRowsAreOnesOfTheirOwnAndNameWhatTheClientNames()
        {
            var ids = Givers.Givers.Select(giver => giver.Id).Concat(Givers.Bosses.Select(boss => boss.Id)).ToArray();
            Assert.HasCount(14, ids.Distinct().ToArray());
            Assert.HasCount(6, Givers.Givers.Select(giver => giver.MissionId).Distinct().ToArray());
            Assert.HasCount(13, Givers.Attacks.Select(attack => attack.Id).Distinct().ToArray());

            // Each giver's mission is one of the seed's, and the battlefields are six.
            foreach (var giver in Givers.Givers)
                Assert.IsTrue(TargetsOfOpportunitySeed.Zones.Any(zone => zone.MissionId == giver.MissionId), giver.Name);

            // The bosses: every one the Mires list asks for, and the Stalker Overlord of the Plains'.
            var mires = BossTitles.Zones.Single(zone => zone.TitleId == MiresAssassin);
            CollectionAssert.AreEquivalent(mires.Bosses.SelectMany(boss => boss.NameIds).ToArray(),
                Givers.Bosses.Where(boss => boss.Map == 1759).Select(boss => boss.NameId).ToArray());
            Assert.IsTrue(mires.Maps.Contains(1759u));

            var plains = BossTitles.Zones.Single(zone => zone.TitleId == Bushwacker);
            var overlord = Givers.Bosses.Single(boss => boss.Map == 1764);
            Assert.IsTrue(plains.Bosses.Any(boss => boss.NameIds.Contains(overlord.NameId)), "the Stalker Overlord");
            Assert.IsTrue(plains.Maps.Contains(1764u));

            // Every action a boss has is one of these or one the world already has (checked in the world below).
            foreach (var boss in Givers.Bosses)
            {
                Assert.IsTrue(boss.Actions.Length is > 0 and <= 8, boss.Comment);
                Assert.AreEqual(100 * boss.Level + 500, boss.MaxHp, boss.Comment);
                Assert.AreEqual(5 * boss.Level + 200, boss.Armor, boss.Comment);
            }

            // Nothing MySQL would read as an escape, and every quote doubled.
            foreach (var sql in Givers.Up().Concat(Givers.Down()))
            {
                Assert.IsFalse(sql.Contains('\\'), sql);
                Assert.AreEqual(0, sql.Replace("''", "").Count(c => c == '\'') % 2, sql);
            }
        }

        [TestMethod]
        public void UpPutsThemInAndDownTakesThemOut()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            var givers = string.Join(", ", Givers.Givers.Select(giver => giver.Id));
            var bosses = string.Join(", ", Givers.Bosses.Select(boss => boss.Id));
            var missions = string.Join(", ", Givers.Givers.Select(giver => giver.MissionId));
            var attacks = string.Join(", ", Givers.Attacks.Select(attack => attack.Id));

            migrator.Migrate(Previous);
            var policies = Policies(database, missions);
            Assert.HasCount(6, policies);
            Assert.AreEqual(0L, Count(database, $"creature WHERE id IN ({givers}, {bosses})"));
            Assert.AreEqual(0L, Count(database, $"spawnpool WHERE id IN ({givers}, {bosses})"));
            Assert.AreEqual(0L, Count(database, $"creature_action WHERE id IN ({attacks})"));
            Assert.AreEqual(6L, Count(database, $"mission_content_definition WHERE mission_id IN ({missions}) AND giver_id IS NULL AND radio_completeable = 1"));
            Assert.AreEqual(1L, Count(database, $"spawnpool WHERE id = {Givers.WarnetQueenPool}"));
            var queen = Position(database, Givers.WarnetQueenPool);
            foreach (var move in Givers.Horsemen)
                Assert.AreEqual((move.FromX, move.FromZ), Position(database, move.Pool), move.Who);

            migrator.Migrate(Migration);

            Assert.AreEqual(14L, Count(database, $"creature WHERE id IN ({givers}, {bosses})"));
            Assert.AreEqual(14L, Count(database, $"spawnpool WHERE id IN ({givers}, {bosses})"));
            Assert.AreEqual(8L, Count(database, $"creature_stat WHERE id IN ({bosses})"));
            Assert.AreEqual(6L * Givers.Uniform.Length, Count(database, $"creature_appearance WHERE id IN ({givers})"));
            Assert.AreEqual(13L, Count(database, $"creature_action WHERE id IN ({attacks})"));
            Assert.AreEqual(0L, Count(database, $"mission_channel_policy WHERE mission_id IN ({missions})"));
            foreach (var giver in Givers.Givers)
            {
                Assert.AreEqual(1L, Count(database, $"mission_content_definition WHERE mission_id = {giver.MissionId} AND content_revision = '{Givers.Revision}' "
                    + $"AND giver_id = {giver.Id} AND receiver_id = {giver.Id} AND radio_completeable = 0"), giver.Name);
                Assert.AreEqual(1L, Count(database, $"spawnpool WHERE id = {giver.Id} AND map_context_id = {giver.Map} AND respown_time = 20 AND creature_1_Id = {giver.Id}"), giver.Name);
                Assert.AreEqual(1L, Count(database, $"creature WHERE id = {giver.Id} AND faction = 1 AND class_id = {Givers.SwapsetMale} AND name_id = {giver.NameId}"), giver.Name);
            }
            foreach (var boss in Givers.Bosses)
            {
                Assert.AreEqual(1L, Count(database, $"creature WHERE id = {boss.Id} AND faction = 0 AND class_id = {boss.ClassId} AND name_id = {boss.NameId} "
                    + $"AND level = {boss.Level} AND max_hp = {boss.MaxHp} AND action1 = {boss.Actions[0]}"), boss.Comment);
                Assert.AreEqual(1L, Count(database, $"spawnpool WHERE id = {boss.Id} AND map_context_id = {boss.Map} AND respown_time = {Givers.BossRespawn}"), boss.Comment);
                Assert.AreEqual((Math.Round(boss.X, 2), Math.Round(boss.Z, 2)), Position(database, boss.Id), boss.Comment);
            }
            foreach (var move in Givers.Horsemen)
                Assert.AreEqual((move.ToX, move.ToZ), Position(database, move.Pool), move.Who);
            Assert.AreEqual(0L, Count(database, $"spawnpool WHERE id = {Givers.WarnetQueenPool}"));
            // Wilderness's own giver is untouched.
            Assert.AreEqual(1L, Count(database, "mission_content_definition WHERE mission_id = 1449 AND giver_id = 118"));

            migrator.Migrate(Previous);

            CollectionAssert.AreEqual(policies, Policies(database, missions));
            Assert.AreEqual(0L, Count(database, $"creature WHERE id IN ({givers}, {bosses})"));
            Assert.AreEqual(0L, Count(database, $"spawnpool WHERE id IN ({givers}, {bosses})"));
            Assert.AreEqual(0L, Count(database, $"creature_stat WHERE id IN ({bosses})"));
            Assert.AreEqual(0L, Count(database, $"creature_appearance WHERE id IN ({givers})"));
            Assert.AreEqual(0L, Count(database, $"creature_action WHERE id IN ({attacks})"));
            Assert.AreEqual(6L, Count(database, $"mission_content_definition WHERE mission_id IN ({missions}) AND giver_id IS NULL AND receiver_id IS NULL AND radio_completeable = 1"));
            Assert.AreEqual(queen, Position(database, Givers.WarnetQueenPool));
            Assert.AreEqual(1L, Count(database, $"spawnpool WHERE id = {Givers.WarnetQueenPool} AND map_context_id = 1148 AND creature_1_Id = {Givers.WarnetQueenPool}"));
            foreach (var move in Givers.Horsemen)
                Assert.AreEqual((move.FromX, move.FromZ), Position(database, move.Pool), move.Who);
        }

        [TestMethod]
        public void AMissionSomeoneElseHasGivenAnOfficerIsLeftAlone()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            database.Context.Database.ExecuteSqlRaw("UPDATE mission_content_definition SET giver_id = 118, receiver_id = 118 WHERE mission_id = 1582");
            database.Context.Database.ExecuteSqlRaw("UPDATE spawnpool SET pos_x = -170 WHERE id = 520025");
            migrator.Migrate(Migration);

            Assert.AreEqual(1L, Count(database, "mission_content_definition WHERE mission_id = 1582 AND giver_id = 118 AND receiver_id = 118"));
            Assert.AreEqual((-170.0, 309.5), Position(database, 520025), "a pool a GM has moved stays where it is");
            Assert.AreEqual((-76.0, 215.0), Position(database, 520024));
        }

        [TestMethod]
        public void OnMySqlTheRowsGoInAndComeOut()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Previous, Migration);
            var down = migrator.GenerateScript(Migration, Previous);

            foreach (var sql in Givers.Up())
                StringAssert.Contains(up, sql);
            StringAssert.Contains(up, $"'{Migration}'");
            foreach (var sql in Givers.Down())
                StringAssert.Contains(down, sql);
        }

        #endregion

        #region In the world

        [TestMethod]
        public void TheGiversHaveTheirMissionsAndTheBossesTheirAttacks()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var creatures = CreatureManager.Instance.LoadedCreatures;

            foreach (var giver in Givers.Givers)
            {
                var npc = creatures[giver.Id];
                Assert.IsNotNull(npc.Npc, giver.Name);
                CollectionAssert.AreEqual(new[] { giver.MissionId }, npc.Npc.NpcMissionIds, giver.Name);
                Assert.AreEqual(TargetCategory.Friendly, npc.TargetCategory, giver.Name);
                Assert.HasCount(Givers.Uniform.Length, npc.AppearanceData, giver.Name);

                var mission = harness.Manager.LoadedMissions[giver.MissionId];
                Assert.IsTrue(mission.IsOperational, $"{giver.Name}: {mission.OperationalDiagnostic}");
                Assert.AreEqual(MissionChannel.Npc, mission.AcceptanceChannel, giver.Name);
                Assert.AreEqual(MissionChannel.Npc, mission.CompletionChannel, giver.Name);
            }

            foreach (var boss in Givers.Bosses)
            {
                var creature = creatures[boss.Id];
                Assert.IsNull(creature.Npc, boss.Comment);
                Assert.AreEqual(TargetCategory.Hostile, creature.TargetCategory, boss.Comment);
                Assert.HasCount(boss.Actions.Length, creature.Actions, boss.Comment);
            }

            // The pools are loaded on their maps, the Warnet Queen's no longer.
            var pools = SpawnPoolManager.Instance.LoadedSpawnPools;
            foreach (var giver in Givers.Givers)
                Assert.AreEqual(giver.Map, pools[giver.Id].MapContextId, giver.Name);
            foreach (var boss in Givers.Bosses)
                Assert.AreEqual(boss.Map, pools[boss.Id].MapContextId, boss.Comment);
            Assert.IsFalse(pools.ContainsKey(Givers.WarnetQueenPool));
        }

        [TestMethod]
        public void EveryoneStandsOnTheGround()
        {
            var root = RepositoryRoot();
            var standing = Givers.Givers.Select(giver => (giver.Map, giver.X, giver.Y, giver.Z, giver.Name))
                .Concat(Givers.Bosses.Select(boss => (boss.Map, boss.X, boss.Y, boss.Z, Name: boss.Comment)))
                .Concat(Givers.Horsemen.Select(move => (Map: 1761u, X: move.ToX, Y: move.ToY, Z: move.ToZ, Name: move.Who)));

            foreach (var group in standing.GroupBy(spot => spot.Map))
            {
                var nav = TestNavMeshes.Query(NavMeshFile.PathFor(Path.Combine(root, "navmesh"), Meshes[group.Key]));

                foreach (var spot in group)
                {
                    var position = new Vector3((float)spot.X, (float)spot.Y, (float)spot.Z);
                    var floor = nav.Nearest(position);
                    Assert.IsTrue(floor.HasValue, $"{spot.Name}: no ground at {position}");
                    Assert.IsTrue(Vector3.Distance(floor.Value, position) <= 0.5f, $"{spot.Name}: {position} is {Vector3.Distance(floor.Value, position):F2} m off {floor.Value}");
                }
            }
        }

        [TestMethod]
        public void NoBossIsOnSafeGroundOrInATurretsScanAndNoGiverIsInACamp()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var places = new List<(uint Map, Vector3 Position, string What)>();
            foreach (var teleporter in DynamicObjectManager.Instance.Teleporters.Values)
                if (teleporter.ObjectData is WaypointInfo info && (info.WaypointType == WaypointType.Hospital || info.WaypointType == WaypointType.Waypoint))
                    places.Add((teleporter.MapContextId, teleporter.Position, $"{info.WaypointType} {info.WaypointId}"));

            var found = SpawnPoolManager.Instance.PoolsTooClose(places).Select(entry => entry.Message).ToList();

            foreach (var id in Givers.Bosses.Select(boss => boss.Id).Concat(Givers.Horsemen.Select(move => move.Pool)))
                Assert.IsFalse(found.Any(message => message.StartsWith($"spawnpool {id}:")), string.Join("\n", found.Where(message => message.StartsWith($"spawnpool {id}:"))));

            foreach (var giver in Givers.Givers)
                Assert.IsFalse(found.Any(message => message.EndsWith($"from the NPCs of pool {giver.Id}, which should be safe ground.")),
                    string.Join("\n", found.Where(message => message.Contains($"pool {giver.Id},"))));
        }

        [TestMethod]
        public void AnOfficersMissionIsTakenFromHimAndNotFromTheRadio()
        {
            using var harness = BootcampRuntimeTestHarness.Create();

            foreach (var giver in Givers.Givers)
            {
                Assert.IsFalse(harness.Manager.TryAcceptRadioMission(harness.Client, giver.MissionId), giver.Name);
                Assert.IsTrue(harness.TakeFromGiver(giver.MissionId), giver.Name);
                Assert.AreEqual(MissionState.Active, harness.Client.Player.Missions[giver.MissionId].State, giver.Name);
            }
        }

        [TestMethod]
        public void TheSeventhMiresOfficerMakesAMiresAssassin()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            harness.Drain();
            var mires = new MapChannel { MapInfo = new MapInfo(1759, "mires_fixture", 628, 0), ClientList = new List<Client>() };
            var officers = Givers.Bosses.Where(boss => boss.Map == 1759).ToArray();

            foreach (var officer in officers.Take(6))
                BossTitles.Killed(harness.Client, mires, new Creature { DbId = officer.Id, NameId = officer.NameId, EntityClass = (EntityClasses)officer.ClassId });
            Assert.IsFalse(harness.Client.Player.Titles.Contains(MiresAssassin));

            BossTitles.Killed(harness.Client, mires, new Creature { DbId = officers[6].Id, NameId = officers[6].NameId, EntityClass = (EntityClasses)officers[6].ClassId });
            Assert.IsTrue(harness.Client.Player.Titles.Contains(MiresAssassin));
        }

        #endregion

        private static long Count(OrdinaryLootTestDatabase database, string from) =>
            database.Context.Database.SqlQueryRaw<long>($"SELECT COUNT(*) AS Value FROM {from}").AsEnumerable().Single();

        private static (double, double) Position(OrdinaryLootTestDatabase database, uint pool)
        {
            var x = database.Context.Database.SqlQueryRaw<double>($"SELECT pos_x AS Value FROM spawnpool WHERE id = {pool}").AsEnumerable().Single();
            var z = database.Context.Database.SqlQueryRaw<double>($"SELECT pos_z AS Value FROM spawnpool WHERE id = {pool}").AsEnumerable().Single();
            return (Math.Round(x, 2), Math.Round(z, 2));
        }

        private static string[] Policies(OrdinaryLootTestDatabase database, string missions) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT CAST(mission_id AS TEXT) || '|' || content_revision || '|' || CAST(acceptance_channel AS TEXT) || '|' || "
                    + $"CAST(completion_channel AS TEXT) || '|' || radio_sources AS Value FROM mission_channel_policy WHERE mission_id IN ({missions}) ORDER BY mission_id")
                .AsEnumerable().ToArray();

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "navmesh")))
                directory = directory.Parent;

            return directory?.FullName ?? AppContext.BaseDirectory;
        }
    }
}
