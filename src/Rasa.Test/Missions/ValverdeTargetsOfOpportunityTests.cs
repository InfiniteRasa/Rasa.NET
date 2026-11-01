using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions
{
    using Rasa.Context.Char;
    using Rasa.Context.World;
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Missions.Content;
    using Rasa.Missions.Definitions;
    using Rasa.Missions.Runtime;
    using Rasa.Navigation;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Mission.Server;
    using Rasa.Services.Preloader;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Structures.World;
    using Rasa.Test.Database;
    using Rasa.Test.Missions.Wilderness;

    using Seed = Rasa.Services.Preloader.TargetsOfOpportunitySeed;
    using Valverde = Rasa.Services.Preloader.TargetsOfOpportunitySeed.Valverde;

    /// <summary>
    /// The rest of the Targets of Opportunity of Plateau (1586) and Valverde Pools (1594)
    /// (TargetsOfOpportunitySeed.Valverde): every objective the client gives them, "Complete All"
    /// as their sum, Doss as Plateau's giver, the turn-in reward, and Pools' three new bosses.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class ValverdeTargetsOfOpportunityTests
    {
        private const uint Plateau = Valverde.PlateauMissionId;
        private const uint Pools = Valverde.PoolsMissionId;
        private const uint PlateauMap = 1497;
        private const uint PoolsMap = Valverde.PoolsMap;

        private const string Migration = "20271103000100_Valverde_targets_of_opportunity";
        private const string WorldBefore = "20271006000000_Pools_turpis_refinery";
        private const string CharBefore = "20270130000000_Item_default_color";

        // The client's titles that are given elsewhere, for the same things the objectives count.
        private const uint PlateauPathfinder = 402, PlateauAssassin = 407, PlateauMentalist = 410;
        private const uint PoolsWanderer = 469, HuntersHunter = 473, PoolsMentalist = 477;

        private static readonly uint[] Missions = { Plateau, Pools };

        private static readonly string[] MissionTables =
        {
            "mission_channel_policy", "mission_repeat_policy", "mission_scene_binding", "mission_evidence",
            "mission_action", "mission_trigger", "mission_scenario_step", "mission_scenario",
            "mission_spawn", "mission_spawn_group", "mission_indicator", "mission_area",
            "mission_reward_item", "mission_reward_definition", "mission_objective_transition",
            "mission_objective_definition", "mission_prerequisite", "mission_content_definition"
        };

        private static readonly string[] CreatureTables = { "creature", "creature_stat", "spawnpool" };

        #region The seed

        [TestMethod]
        public void WhatIsAddedIsTheClientsObjectivesAndNoneTheMissionHad()
        {
            foreach (var mission in Missions)
            {
                var zone = Seed.Zones.Single(candidate => candidate.MissionId == mission);
                var had = zone.Kills.Select(kill => kill.ObjectiveId).Append(zone.AllObjectiveId).ToArray();
                var added = Valverde.AllObjectives.Where(objective => objective.MissionId == mission).Select(objective => objective.ObjectiveId).ToArray();

                Assert.IsFalse(added.Intersect(had).Any(), zone.Name);
                Assert.HasCount(added.Length, added.Distinct().ToArray(), zone.Name);

                // In the log after the mission's own, one ordinal each.
                var ordinals = Valverde.ObjectiveRows.Where(row => (uint)row[0] == mission).Select(row => (uint)row[9]).ToArray();
                CollectionAssert.AreEqual(Enumerable.Range(zone.Kills.Count + 2, added.Length).Select(value => (uint)value).ToArray(), ordinals, zone.Name);
            }

            Assert.AreEqual(Seed.Zones.Single(zone => zone.MissionId == Plateau).AllObjectiveId, Valverde.PlateauAll);
            Assert.AreEqual(Seed.Zones.Single(zone => zone.MissionId == Pools).AllObjectiveId, Valverde.PoolsAll);

            // "Complete All 10" and "Complete All 8": the shown objectives but the sum itself.
            var plateau = Valverde.Scene(Plateau).ObjectiveAggregations[Valverde.PlateauAll];
            var pools = Valverde.Scene(Pools).ObjectiveAggregations[Valverde.PoolsAll];
            Assert.AreEqual(10u, plateau.TargetCount);
            Assert.HasCount(10, plateau.ChildObjectiveIds);
            Assert.AreEqual(8u, pools.TargetCount);
            Assert.HasCount(8, pools.ChildObjectiveIds);

            // No title is one another objective or system gives.
            var given = Seed.Zones.SelectMany(zone => zone.Kills).Select(kill => kill.TitleId)
                .Concat(WaypointTitles.Zones.Select(zone => zone.TitleId))
                .Concat(LogosTitles.Zones.Select(zone => zone.TitleId))
                .Concat(BossTitles.Zones.Select(zone => zone.TitleId)).ToHashSet();
            var titles = Missions.SelectMany(mission => Valverde.Scene(mission).Titles)
                .Where(title => Valverde.AllObjectives.Any(added => added.ObjectiveId == title.Key) || title.Key is Valverde.PlateauAll or Valverde.PoolsAll)
                .Select(title => title.Value).ToArray();

            CollectionAssert.AreEquivalent(new uint[] { 403, 404, 408, 411, 412, 413, 474, 476, 478 }, titles);
            foreach (var title in titles)
                Assert.IsFalse(given.Contains(title), $"{title}");

            // The comments fit their columns, and the SQL carries nothing MySQL would read as an escape.
            foreach (var row in Valverde.ObjectiveRows.Concat(Valverde.TransitionRows).Concat(Valverde.TriggerRows).Concat(Valverde.RewardRows))
                Assert.IsTrue(((string)row[^1]).Length <= 64, (string)row[^1]);
            foreach (var row in Valverde.Evidence)
                Assert.IsTrue(((string)row[^1]).Length <= 256 && ((string)row[6]).Length <= 256 && ((string)row[7]).Length <= 256, (string)row[^1]);
            foreach (var attack in Valverde.Attacks)
                Assert.IsTrue(attack.Description.Length <= 64, attack.Description);
            foreach (var statement in Valverde.WorldUp.Concat(Valverde.WorldDown).Concat(Valverde.CharUp).Concat(Valverde.CharDown))
                Assert.IsFalse(statement.Contains('\\'), statement);
        }

        [TestMethod]
        public void TheStoryMissionsAreOnesOnceEachAndNoneThatCannotBeFinished()
        {
            foreach (var (name, story) in new[] { ("Plateau", Valverde.PlateauStory), ("Pools", Valverde.PoolsStory) })
            {
                var all = story.SelectMany(group => group).ToArray();
                Assert.HasCount(42, story, name);
                Assert.HasCount(all.Length, all.Distinct().ToArray(), name);

                // The Targets of Opportunity, the zone's Logos fetch (714), the Velon Hollow one (1327),
                // the five Trinity Bridge Logos and the chain after them, and Mislaid Plans.
                foreach (var excluded in new uint[] { 1586, 1594, 714, 1327, 1047, 1048, 1049, 1051, 1057, 935, 1171, 1167, 1178, 1181, 965 })
                    Assert.IsFalse(all.Contains(excluded), $"{name}: {excluded}");
            }
        }

        #endregion

        #region The world database

        [TestMethod]
        public void TheObjectivesCountWhatTheTitlesForTheSameThingsCount()
        {
            WithDatabase<SqliteWorldContext, int>(world =>
            {
                world.Database.Migrate();

                try
                {
                    WaypointTitles.Load(world.Set<TeleporterEntry>().AsNoTracking().ToArray());
                    LogosTitles.Load(world.Set<LogosEntry>().AsNoTracking().ToArray());

                    CollectionAssert.AreEquivalent(WaypointTitles.WaypointsOf(PlateauPathfinder).ToArray(), Valverde.PlateauWaypoints);
                    CollectionAssert.AreEquivalent(WaypointTitles.WaypointsOf(PoolsWanderer).ToArray(), Valverde.PoolsWaypoints);
                    CollectionAssert.AreEquivalent(LogosTitles.LogosOf(PlateauMentalist).ToArray(), Valverde.PlateauLogos);
                    CollectionAssert.AreEquivalent(LogosTitles.LogosOf(PoolsMentalist).ToArray(), Valverde.PoolsLogos);
                }
                finally
                {
                    WaypointTitles.Load(null);
                    LogosTitles.Load(null);
                }

                // A boss objective a boss of the title, by the name its creature row has.
                var creatures = world.Set<CreatureEntry>().AsNoTracking().ToDictionary(row => row.Id);
                foreach (var (title, bosses) in new[] { (PlateauAssassin, Valverde.PlateauBosses), (HuntersHunter, Valverde.PoolsBosses) })
                {
                    var zone = BossTitles.Zones.Single(candidate => candidate.TitleId == title);

                    CollectionAssert.AreEquivalent(zone.Bosses.Select(boss => boss.Name).ToArray(), bosses.Select(boss => boss.Name).ToArray(), zone.Name);
                    foreach (var boss in bosses)
                        CollectionAssert.Contains(zone.Bosses.Single(candidate => candidate.Name == boss.Name).NameIds.ToArray(), creatures[boss.CreatureId].NameId, boss.Name);
                }

                Assert.IsTrue(creatures.ContainsKey(Valverde.EliteGuard));

                // The missions the history objectives ask for are the world's.
                var missions = world.MissionContentDefinitionEntries.AsNoTracking().Select(row => row.MissionId).ToHashSet();
                foreach (var mission in Missions)
                    foreach (var aggregation in Valverde.Scene(mission).ObjectiveHistoryAggregations)
                        foreach (var id in aggregation.Value.Groups.SelectMany(group => group))
                            Assert.IsTrue(missions.Contains(id), $"{mission} objective {aggregation.Key}: {id}");

                return 0;
            });
        }

        [TestMethod]
        public void TheMigrationAddsItsRowsAndDownLeavesTheMissionsAsTheyWere()
        {
            WithDatabase<SqliteWorldContext, int>(world =>
            {
                world.GetService<IMigrator>().Migrate(WorldBefore);
                var before = Dump(world);

                Assert.AreEqual(Migration, world.Database.GetPendingMigrations().First());
                Assert.AreEqual(1, world.Set<MissionChannelPolicyEntry>().AsNoTracking().Count(row => row.MissionId == Plateau), "Plateau's radio before");

                world.Database.Migrate();

                foreach (var mission in Missions)
                {
                    var zone = Seed.Zones.Single(candidate => candidate.MissionId == mission);

                    CollectionAssert.AreEquivalent(
                        zone.Kills.Select(kill => kill.ObjectiveId).Append(zone.AllObjectiveId)
                            .Concat(Valverde.AllObjectives.Where(added => added.MissionId == mission).Select(added => added.ObjectiveId)).ToArray(),
                        world.Set<MissionObjectiveDefinitionEntry>().AsNoTracking().Where(row => row.MissionId == mission).Select(row => row.ObjectiveId).ToArray(),
                        zone.Name);
                    Assert.AreEqual(
                        JsonSerializer.Serialize(Valverde.Scene(mission), MissionContentCodec.Options),
                        world.Set<MissionSceneBindingEntry>().AsNoTracking().Single(row => row.MissionId == mission).Bindings, zone.Name);
                    Assert.AreEqual(Valverde.Experience, world.Set<MissionRewardDefinitionEntry>().AsNoTracking().Single(row => row.MissionId == mission).Experience, zone.Name);
                    Assert.AreEqual(1, world.Set<MissionEvidenceEntry>().AsNoTracking().Count(row => row.MissionId == mission && row.EvidenceId == Valverde.EvidenceId), zone.Name);
                }

                // Plateau's is Doss's; Pools stays the radio's.
                var plateau = world.MissionContentDefinitionEntries.AsNoTracking().Single(row => row.MissionId == Plateau);
                Assert.AreEqual(Valverde.Doss, plateau.GiverId);
                Assert.AreEqual(Valverde.Doss, plateau.ReceiverId);
                Assert.IsFalse(world.Set<MissionChannelPolicyEntry>().AsNoTracking().Any(row => row.MissionId == Plateau));
                Assert.IsNull(world.MissionContentDefinitionEntries.AsNoTracking().Single(row => row.MissionId == Pools).GiverId);
                Assert.AreEqual(1, world.Set<MissionChannelPolicyEntry>().AsNoTracking().Count(row => row.MissionId == Pools));

                // The three new bosses, on Pools, each its own pool.
                foreach (var boss in Valverde.NewBosses)
                {
                    var creature = world.Set<CreatureEntry>().AsNoTracking().Single(row => row.Id == boss.Id);
                    Assert.AreEqual(boss.ClassId, creature.ClassId, boss.Comment);
                    Assert.AreEqual(boss.NameId, creature.NameId, boss.Comment);
                    Assert.AreEqual(Valverde.NewBoss.Level, creature.Level, boss.Comment);
                    Assert.AreEqual(PoolsMap, world.Set<SpawnPoolEntry>().AsNoTracking().Single(row => row.Id == boss.Id).MapContextId, boss.Comment);
                }

                // The Striders: Thrax Cavalrymen as the Foreas Base past's, in a pool of their own on Pools.
                var cavalryman = world.Set<CreatureEntry>().AsNoTracking().Single(row => row.Id == Rasa.Services.Preloader.Missions.Plateau.PlateauForeasBasePastNpcsV1.Cavalryman);
                var strider = world.Set<CreatureEntry>().AsNoTracking().Single(row => row.Id == Valverde.Strider);
                Assert.AreEqual(cavalryman.ClassId, strider.ClassId);
                Assert.AreEqual(cavalryman.Level, strider.Level);
                Assert.AreEqual(cavalryman.Action1, strider.Action1);
                Assert.AreEqual(cavalryman.Action2, strider.Action2);
                var striders = world.Set<SpawnPoolEntry>().AsNoTracking().Single(row => row.Id == Valverde.StriderPool);
                Assert.AreEqual(PoolsMap, striders.MapContextId);
                Assert.AreEqual(Valverde.Strider, striders.Creature1Id);
                Assert.AreEqual(Valverde.StriderMax, striders.Creature1MaxCount);
                Assert.AreEqual((byte)Valverde.StriderMin, striders.Creature1MinCount);
                Assert.AreEqual(Valverde.StriderRespawn, striders.RespawnTime);

                world.GetService<IMigrator>().Migrate(WorldBefore);

                CollectionAssert.AreEqual(before, Dump(world));
                return 0;
            });
        }

        [TestMethod]
        public void OnMySqlTheRowsGoInAndComeOut()
        {
            using (var world = PersistenceIntegrationTests.CreateContext(typeof(MySqlWorldContext), "unused"))
            {
                var migrator = world.GetService<IMigrator>();
                var up = migrator.GenerateScript(WorldBefore, Migration);
                var down = migrator.GenerateScript(Migration, WorldBefore);

                foreach (var sql in Valverde.WorldUp)
                    StringAssert.Contains(up, sql);
                StringAssert.Contains(up, $"'{Migration}'");
                foreach (var sql in Valverde.WorldDown)
                    StringAssert.Contains(down, sql);
            }

            using (var characters = PersistenceIntegrationTests.CreateContext(typeof(MySqlCharContext), "unused"))
            {
                var migrator = characters.GetService<IMigrator>();
                var up = migrator.GenerateScript(CharBefore, Migration);
                var down = migrator.GenerateScript(Migration, CharBefore);

                foreach (var sql in Valverde.CharUp)
                    StringAssert.Contains(up, sql);
                foreach (var sql in Valverde.CharDown)
                    StringAssert.Contains(down, sql);
            }
        }

        #endregion

        #region The character database

        [TestMethod]
        public void ACharacterHoldingAMissionIsGivenItsNewObjectivesAndKeepsItsCounts()
        {
            WithDatabase<SqliteCharContext, int>(characters =>
            {
                characters.GetService<IMigrator>().Migrate(CharBefore);

                // The account and character rows by hand, as OneOffTitlesTests has them.
                characters.Database.ExecuteSqlRaw(
                    "INSERT INTO account (id, email, name, family_name, selected_slot, last_ip) " +
                    "VALUES (1, 'valverde@example.invalid', 'Valverde', 'Fixture', 1, '127.0.0.1')");
                foreach (var id in new uint[] { 1, 2 })
                    characters.Database.ExecuteSqlRaw(
                        "INSERT INTO character (id, account_id, slot, name, race, class, gender, scale, body, mind, spirit, level, map_context_id, " +
                        "coord_x, coord_y, coord_z, rotation, run_state, last_login, created_at, last_pvp_clan) " +
                        "VALUES ({0}, 1, {0}, {1}, 1, 1, 0, 1, 0, 0, 0, 30, 1497, 0, 0, 0, 0, 1, {2}, {2}, {2})",
                        id, $"Holder{id}", DateTime.UtcNow);

                // Character 1 holds Plateau's as it was; character 2 holds both, Pools' as it was and
                // the Divide's, which gains nothing.
                characters.CharacterMissionEntries.Add(Held(1, Plateau, 56, 57, 69));
                characters.CharacterMissionEntries.Add(Held(2, Pools, 5, 56, 57, 74));
                characters.CharacterMissionEntries.Add(Held(2, 1582, 3, 5));
                characters.SaveChanges();
                characters.ChangeTracker.Clear();

                characters.Database.Migrate();
                Check();

                // Run again, it changes nothing.
                foreach (var statement in Valverde.CharUp)
                    characters.Database.ExecuteSqlRaw(statement);
                Check();

                void Check()
                {
                    foreach (var (character, mission, had) in new[] { (1u, Plateau, new uint[] { 56, 57, 69 }), (2u, Pools, new uint[] { 5, 56, 57, 74 }) })
                    {
                        var added = Valverde.AllObjectives.Where(objective => objective.MissionId == mission).ToArray();
                        var objectives = characters.CharacterMissionObjectiveEntries.AsNoTracking()
                            .Where(row => row.CharacterId == character && row.MissionId == mission).ToArray();
                        var counters = characters.CharacterMissionObjectiveCounterEntries.AsNoTracking()
                            .Where(row => row.CharacterId == character && row.MissionId == mission).ToArray()
                            .ToDictionary(row => row.ObjectiveId, row => row.CounterValue);

                        CollectionAssert.AreEquivalent(had.Concat(added.Select(objective => objective.ObjectiveId)).ToArray(),
                            objectives.Select(row => row.ObjectiveId).ToArray(), $"{mission}");
                        Assert.IsTrue(objectives.All(row => row.ObjectiveState == (byte)MissionObjectiveState.Incomplete), $"{mission}");

                        foreach (var objective in had)
                            Assert.AreEqual(7u, counters[objective], $"{mission} {objective}: the count kept");
                        foreach (var objective in added)
                            if (objective.Counted)
                                Assert.AreEqual(0u, counters[objective.ObjectiveId], $"{mission} {objective.ObjectiveId}");
                            else
                                Assert.IsFalse(counters.ContainsKey(objective.ObjectiveId), $"{mission} {objective.ObjectiveId}: no count of its own");
                    }

                    // A character without the mission is given nothing of it, nor a mission that gains nothing.
                    Assert.IsFalse(characters.CharacterMissionObjectiveEntries.AsNoTracking().Any(row => row.CharacterId == 2 && row.MissionId == Plateau));
                    Assert.AreEqual(2, characters.CharacterMissionObjectiveEntries.AsNoTracking().Count(row => row.MissionId == 1582));
                }

                // Back: the rows go, the counts stay.
                characters.GetService<IMigrator>().Migrate(CharBefore);

                Assert.AreEqual(3 + 4 + 2, characters.CharacterMissionObjectiveEntries.AsNoTracking().Count());
                Assert.AreEqual(3 + 4 + 2, characters.CharacterMissionObjectiveCounterEntries.AsNoTracking().Count(row => row.CounterValue == 7));
                Assert.AreEqual(3 + 4 + 2, characters.CharacterMissionObjectiveCounterEntries.AsNoTracking().Count());
                return 0;
            });

            static CharacterMissionEntry Held(uint characterId, uint missionId, params uint[] objectives)
            {
                var held = new CharacterMissionEntry(characterId, missionId, (uint)MissionState.Active) { ContentRevision = Seed.Revision };

                foreach (var objectiveId in objectives)
                {
                    var objective = new CharacterMissionObjectiveEntry
                    {
                        CharacterId = characterId, MissionId = missionId, ObjectiveId = objectiveId, ObjectiveState = (byte)MissionObjectiveState.Incomplete
                    };
                    objective.Counters.Add(new CharacterMissionObjectiveCounterEntry
                    {
                        CharacterId = characterId, MissionId = missionId, ObjectiveId = objectiveId, CounterId = 0, CounterValue = 7
                    });
                    held.Objectives.Add(objective);
                }

                return held;
            }
        }

        [TestMethod]
        public void AMissionTakenBeforeIsKeptOnceItHasTheNewObjectives()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            TakePlateau(harness);

            for (var kills = 0; kills < 3; kills++)
                Assert.IsTrue(harness.Manager.RecordProgress(harness.Client, MissionProgressEvent.CreatureFlag(Seed.Flag.Kael)));

            // As a character who took the mission before it had its new objectives holds it; then
            // the character database's half of the migration.
            using (var characters = harness.Context.Open())
            {
                foreach (var statement in Valverde.CharDown)
                    characters.Database.ExecuteSqlRaw(statement);

                Assert.IsFalse(characters.CharacterMissionObjectiveEntries.AsNoTracking().Any(row => row.MissionId == Plateau && row.ObjectiveId == Valverde.Waypoints));

                foreach (var statement in Valverde.CharUp)
                    characters.Database.ExecuteSqlRaw(statement);
            }

            harness.ReconnectFresh();

            var log = harness.Client.Player.Missions[Plateau];
            Assert.AreEqual(MissionState.Active, log.State);
            Assert.AreEqual(3U, log.Objectives[56].Counters[0], "the Kael killed before");
            Assert.AreEqual(MissionObjectiveState.Incomplete, log.Objectives[Valverde.EliteGuards].State);

            Arrive(harness, PlateauMap);
            harness.Drain();
            Assert.IsTrue(harness.Manager.RecordProgress(harness.Client, MissionProgressEvent.Creature(Valverde.EliteGuard)));
            Assert.AreEqual(1U, harness.Client.Player.Missions[Plateau].Objectives[Valverde.EliteGuards].Counters[0]);
        }

        #endregion

        #region The missions in the world

        [TestMethod]
        public void TheMissionsStandUpWithWhatIsAdded()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var missions = harness.Manager.LoadedMissions;

            foreach (var mission in Missions)
                Assert.IsTrue(missions[mission].IsOperational, $"{mission}: {missions[mission].OperationalDiagnostic}");

            var plateau = missions[Plateau];
            Assert.AreEqual(Valverde.Doss, plateau.MissionGiver);
            Assert.AreEqual(Valverde.Doss, plateau.MissionReciver);
            Assert.AreEqual(MissionChannel.Npc, plateau.AcceptanceChannel);
            Assert.AreEqual(MissionChannel.Npc, plateau.CompletionChannel);
            Assert.AreEqual(MissionChannel.Radio, missions[Pools].AcceptanceChannel);

            foreach (var (mission, all, title, children) in new[] { (Plateau, Valverde.PlateauAll, Valverde.MasterOfPlateau, 10u), (Pools, Valverde.PoolsAll, Valverde.PoolsMaster, 8u) })
            {
                var sum = missions[mission].Objectives[all];
                Assert.IsTrue(sum.IsAggregate, $"{mission}");
                Assert.IsTrue(sum.IsVisible, $"{mission}");
                Assert.AreEqual(title, sum.TitleId, $"{mission}");
                Assert.AreEqual(children, sum.Aggregation.TargetCount, $"{mission}");
                Assert.IsEmpty(sum.Counters, $"{mission}: the client has no counter for it");

                // The bosses: one hidden objective each, the shown one their sum.
                var bosses = mission == Plateau ? Valverde.PlateauBosses : Valverde.PoolsBosses;
                var shown = missions[mission].Objectives[Valverde.Bosses];
                Assert.IsTrue(shown.IsAggregate, $"{mission}");
                Assert.AreEqual((uint)bosses.Length, shown.Aggregation.TargetCount, $"{mission}");
                foreach (var boss in bosses)
                {
                    var hidden = missions[mission].Objectives[boss.ObjectiveId];
                    Assert.IsFalse(hidden.IsVisible, boss.Name);
                    Assert.IsNull(hidden.TitleId, boss.Name);
                    Assert.AreEqual(MissionCreditMode.NearbyParty, hidden.CreditPolicy.Mode, boss.Name);
                }
            }

            Assert.AreEqual(11u, plateau.Objectives[Valverde.Waypoints].Counters[0].TargetValue);
            Assert.AreEqual(10u, plateau.Objectives[Valverde.Logos].Counters[0].TargetValue);
            Assert.AreEqual(Valverde.EliteGuardCount, plateau.Objectives[Valverde.EliteGuards].Counters[0].TargetValue);
            Assert.AreEqual(Valverde.PlateauSentinel, plateau.Objectives[Valverde.EliteGuards].TitleId);
            Assert.AreEqual((uint)Valverde.PlateauStory.Length, plateau.Objectives[Valverde.Story].HistoryAggregation.TargetCount);
            Assert.AreEqual(Valverde.PlateauVeteran, plateau.Objectives[Valverde.Story].TitleId);
            Assert.AreEqual((uint)Valverde.PoolsStory.Length, missions[Pools].Objectives[Valverde.Story].HistoryAggregation.TargetCount);
            Assert.AreEqual(Valverde.PoolsOperative, missions[Pools].Objectives[Valverde.PoolsOperations].TitleId);
            foreach (var hidden in new[] { Valverde.RetreadCaves, Valverde.LamnaArmory, Valverde.TurpisRefinery })
                Assert.IsFalse(missions[Pools].Objectives[hidden].IsVisible, $"{hidden}");

            // A history objective needs every group done: a mission of one that cannot be taken here
            // would leave it, and "Complete All", never done.
            foreach (var mission in Missions)
                foreach (var (objective, aggregation) in Valverde.Scene(mission).ObjectiveHistoryAggregations)
                    foreach (var group in aggregation.Groups)
                        Assert.IsTrue(group.Any(id => missions.TryGetValue(id, out var done) && done.IsOperational),
                            $"{mission} objective {objective}: {string.Join("/", group)} is not operational");
        }

        [TestMethod]
        public void ThePoolsBossesStandOnTheGroundAndNotOnSafeGround()
        {
            var nav = TestNavMeshes.Query(NavMeshFile.PathFor(Path.Combine(RepositoryRoot(), "navmesh"), "adv_foreas_valverde_pools"));
            foreach (var boss in Valverde.NewBosses)
            {
                var position = new Vector3((float)boss.X, (float)boss.Y, (float)boss.Z);
                var floor = nav.Nearest(position);
                Assert.IsTrue(floor.HasValue, $"{boss.Comment}: no ground at {position}");
                Assert.IsTrue(Vector3.Distance(floor.Value, position) <= 0.5f, $"{boss.Comment}: {position} is {Vector3.Distance(floor.Value, position):F2} m off {floor.Value}");
            }

            using var harness = WildernessRuntimeTestHarness.Create();
            var places = new List<(uint Map, Vector3 Position, string What)>();
            foreach (var teleporter in DynamicObjectManager.Instance.Teleporters.Values)
                if (teleporter.ObjectData is WaypointInfo info && (info.WaypointType == WaypointType.Hospital || info.WaypointType == WaypointType.Waypoint))
                    places.Add((teleporter.MapContextId, teleporter.Position, $"{info.WaypointType} {info.WaypointId}"));

            var found = SpawnPoolManager.Instance.PoolsTooClose(places).Select(entry => entry.Message).ToList();
            foreach (var boss in Valverde.NewBosses)
            {
                Assert.IsFalse(found.Any(message => message.StartsWith($"spawnpool {boss.Id}:")), string.Join("\n", found.Where(message => message.StartsWith($"spawnpool {boss.Id}:"))));

                var creature = CreatureManager.Instance.LoadedCreatures[boss.Id];
                Assert.AreEqual(TargetCategory.Hostile, creature.TargetCategory, boss.Comment);
                Assert.HasCount(boss.Actions.Length, creature.Actions, boss.Comment);
                Assert.AreEqual(PoolsMap, SpawnPoolManager.Instance.LoadedSpawnPools[boss.Id].MapContextId, boss.Comment);
            }
        }

        [TestMethod]
        public void ThePoolsHaveStridersToKillBesidesKrammitron()
        {
            var nav = TestNavMeshes.Query(NavMeshFile.PathFor(Path.Combine(RepositoryRoot(), "navmesh"), "adv_foreas_valverde_pools"));
            var position = new Vector3((float)Valverde.StriderX, (float)Valverde.StriderY, (float)Valverde.StriderZ);
            var floor = nav.Nearest(position);
            Assert.IsTrue(floor.HasValue, $"no ground at {position}");
            Assert.IsTrue(Vector3.Distance(floor.Value, position) <= 0.5f, $"{position} is {Vector3.Distance(floor.Value, position):F2} m off {floor.Value}");

            using var harness = WildernessRuntimeTestHarness.Create();
            var places = new List<(uint Map, Vector3 Position, string What)>();
            foreach (var teleporter in DynamicObjectManager.Instance.Teleporters.Values)
                if (teleporter.ObjectData is WaypointInfo info && (info.WaypointType == WaypointType.Hospital || info.WaypointType == WaypointType.Waypoint))
                    places.Add((teleporter.MapContextId, teleporter.Position, $"{info.WaypointType} {info.WaypointId}"));
            var found = SpawnPoolManager.Instance.PoolsTooClose(places).Select(entry => entry.Message).Where(message => message.StartsWith($"spawnpool {Valverde.StriderPool}:")).ToList();
            Assert.IsEmpty(found, string.Join("\n", found));

            // Hostile, armed, and a Strider: its kill counts on objective 57.
            var creature = CreatureManager.Instance.LoadedCreatures[Valverde.Strider];
            Assert.AreEqual(TargetCategory.Hostile, creature.TargetCategory);
            Assert.HasCount(2, creature.Actions);
            Assert.Contains((int)Seed.Flag.Strider, CreatureManager.CreatureFlagsOf(creature));
            var pool = SpawnPoolManager.Instance.LoadedSpawnPools[Valverde.StriderPool];
            Assert.AreEqual(PoolsMap, pool.MapContextId);
        }

        [TestMethod]
        public void EveryTargetOfPlateauMakesAMasterOfPlateauAndDossTakesItBack()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var start = harness.Context.ReadRewardTotals();

            // Most of the story done before the mission is taken; it counts when taken. The CID
            // murder, the Penumbra agent and Ustor Yard's operation after, and the first of the story.
            var later = new uint[] { 928, 719, 1190, Valverde.PlateauStory[0][0] };
            Archive(harness, Valverde.PlateauStory.Select(group => group[0]).Except(later));
            var doss = TakePlateau(harness);
            var log = harness.Client.Player.Missions[Plateau];
            Assert.AreEqual(MissionObjectiveState.Incomplete, log.Objectives[Valverde.Story].State);
            Assert.AreEqual(MissionObjectiveState.Incomplete, log.Objectives[Valverde.PlateauOperations].State);
            Assert.AreEqual(1U, log.Objectives[Valverde.PlateauOperations].Counters[0], "One Way Trip, done before");

            var packets = new List<Rasa.Packets.PythonPacket>();

            foreach (var waypoint in Valverde.PlateauWaypoints)
                Waypoint(harness, waypoint);
            foreach (var logos in Valverde.PlateauLogos)
                Logos(harness, logos);
            foreach (var boss in Valverde.PlateauBosses)
                Assert.IsTrue(harness.Manager.RecordProgress(harness.Client, MissionProgressEvent.Creature(boss.CreatureId)), boss.Name);
            for (var kill = 0; kill < Valverde.EliteGuardCount; kill++)
                Assert.IsTrue(harness.Manager.RecordProgress(harness.Client, MissionProgressEvent.Creature(Valverde.EliteGuard)));
            for (var kill = 0; kill < 30; kill++)
                harness.Manager.RecordProgress(harness.Client, MissionProgressEvent.CreatureFlag(Seed.Flag.Kael));
            for (var kill = 0; kill < 40; kill++)
                harness.Manager.RecordProgress(harness.Client, MissionProgressEvent.CreatureFlag(Seed.Flag.Mox));
            packets.AddRange(harness.Drain());

            foreach (var mission in later)
            {
                Archive(harness, new[] { mission });
                harness.Manager.RecordProgress(harness.Client, MissionProgressEvent.Mission(mission));
                packets.AddRange(harness.Drain());

                if (mission != Valverde.PlateauStory[0][0])
                    Assert.IsFalse(harness.Client.Player.Missions[Plateau].Completeable, $"not all before {mission}");
            }

            log = harness.Client.Player.Missions[Plateau];
            foreach (var objective in new[] { Valverde.Waypoints, Valverde.Bosses, Valverde.Logos, Valverde.Story, 56u, 57u,
                Valverde.EliteGuards, Valverde.Murder, Valverde.Penumbra, Valverde.PlateauOperations, Valverde.PlateauAll })
                Assert.AreEqual(MissionObjectiveState.Completed, log.Objectives[objective].State, $"{objective}");
            Assert.IsTrue(log.Completeable);

            var titles = packets.OfType<TitleAddedPacket>().Select(packet => packet.TitleId).ToArray();
            foreach (var title in new[] { Valverde.PlateauSentinel, Valverde.PlateauDetective, Valverde.PlateauSecretAgent, Valverde.PlateauOperative,
                Valverde.PlateauVeteran, Valverde.MasterOfPlateau })
                CollectionAssert.Contains(titles, title);

            // Not by radio: back to Doss.
            Assert.IsFalse(harness.Manager.TryCompleteRadioMission(harness.Client, Plateau, null, null));
            Assert.IsTrue(harness.Manager.CompleteOfferedMission(harness.Client, doss.EntityId, Plateau, null));
            Assert.AreEqual(MissionState.Completed, harness.Client.Player.Missions[Plateau].State);
            Assert.AreEqual(start.Experience + Valverde.Experience, harness.Context.ReadRewardTotals().Experience);
        }

        [TestMethod]
        public void EveryTargetOfThePoolsMakesAPoolsMasterAndTheRadioTakesItBack()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var start = harness.Context.ReadRewardTotals();

            // The story done before the mission is taken, but the operations' missions.
            var operations = Valverde.RetreadCavesMissions.Concat(Valverde.LamnaArmoryMissions).Concat(Valverde.TurpisRefineryMissions)
                .Select(group => group[0]).ToArray();
            Archive(harness, Valverde.PoolsStory.Select(group => group[0]).Except(operations));
            Arrive(harness, PoolsMap);
            harness.Manager.OfferArrivalMissions(harness.Client);
            Assert.IsTrue(harness.Manager.TryAcceptRadioMission(harness.Client, Pools));

            var gained = harness.Drain().OfType<MissionGainedPacket>().Single(packet => packet.MissionId == Pools);
            var shown = gained.MissionInfo.ObjectivesList.Select(objective => objective.ObjectiveId).ToArray();
            foreach (var hidden in Valverde.PoolsBosses.Select(boss => boss.ObjectiveId).Concat(new[] { Valverde.RetreadCaves, Valverde.LamnaArmory, Valverde.TurpisRefinery }))
                CollectionAssert.DoesNotContain(shown, hidden);
            Assert.AreEqual((uint)(Valverde.PoolsStory.Length - operations.Length), harness.Client.Player.Missions[Pools].Objectives[Valverde.Story].Counters[0], "the story done before");

            var packets = new List<Rasa.Packets.PythonPacket>();

            foreach (var waypoint in Valverde.PoolsWaypoints)
            {
                Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[Pools].Objectives[Valverde.Waypoints].State, $"before waypoint {waypoint}");
                Waypoint(harness, waypoint, counted: false);
            }
            Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[Pools].Objectives[Valverde.Waypoints].State);
            foreach (var logos in Valverde.PoolsLogos)
            {
                Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[Pools].Objectives[Valverde.Logos].State, $"before Logos {logos}");
                Logos(harness, logos, counted: false);
            }
            Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[Pools].Objectives[Valverde.Logos].State);
            foreach (var boss in Valverde.PoolsBosses)
                Assert.IsTrue(harness.Manager.RecordProgress(harness.Client, MissionProgressEvent.Creature(boss.CreatureId)), boss.Name);
            foreach (var (flag, count) in new[] { (Seed.Flag.Thrax, 200), (Seed.Flag.Howler, 40), (Seed.Flag.Strider, 10) })
                for (var kill = 0; kill < count; kill++)
                    harness.Manager.RecordProgress(harness.Client, MissionProgressEvent.CreatureFlag(flag));
            packets.AddRange(harness.Drain());

            foreach (var mission in operations)
            {
                Assert.IsFalse(harness.Client.Player.Missions[Pools].Completeable, $"not all before {mission}");
                Archive(harness, new[] { mission });
                harness.Manager.RecordProgress(harness.Client, MissionProgressEvent.Mission(mission));
                packets.AddRange(harness.Drain());
            }

            var log = harness.Client.Player.Missions[Pools];
            foreach (var objective in new[] { Valverde.Waypoints, 5u, Valverde.Bosses, Valverde.Logos, Valverde.PoolsOperations, Valverde.Story, 56u, 57u, Valverde.PoolsAll })
                Assert.AreEqual(MissionObjectiveState.Completed, log.Objectives[objective].State, $"{objective}");
            Assert.AreEqual(10U, log.Objectives[Valverde.Bosses].Counters[0]);
            Assert.AreEqual(3U, log.Objectives[Valverde.PoolsOperations].Counters[0]);
            Assert.IsTrue(log.Completeable);

            var titles = packets.OfType<TitleAddedPacket>().Select(packet => packet.TitleId).ToArray();
            foreach (var title in new[] { Valverde.PoolsOperative, Valverde.PoolsMaster })
                CollectionAssert.Contains(titles, title);

            Assert.IsTrue(harness.Manager.TryCompleteRadioMission(harness.Client, Pools, null, null));
            Assert.AreEqual(MissionState.Completed, harness.Client.Player.Missions[Pools].State);
            Assert.AreEqual(start.Experience + Valverde.Experience, harness.Context.ReadRewardTotals().Experience);
        }

        #endregion

        #region Helpers

        /// <summary>Onto Plateau, and 1586 taken from Doss stood beside the player.</summary>
        private static Creature TakePlateau(BootcampRuntimeTestHarness.Harness harness)
        {
            Arrive(harness, PlateauMap);
            var player = harness.Client.Player;
            var doss = harness.Context.AddNpc(Valverde.Doss, player.MapChannel, position: player.Position);
            doss.Npc.NpcMissionIds = harness.Manager.LoadedMissions.Values
                .Where(mission => mission.MissionGiver == doss.DbId || mission.MissionReciver == doss.DbId)
                .Select(mission => mission.MissionId).ToList();
            CollectionAssert.Contains(doss.Npc.NpcMissionIds, Plateau);

            harness.Manager.OfferArrivalMissions(harness.Client);
            Assert.IsFalse(harness.Manager.TryAcceptRadioMission(harness.Client, Plateau), "Doss's, not the radio's");
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, doss.EntityId, Plateau));
            harness.Drain();
            return doss;
        }

        /// <summary>A waypoint gained. Counted, each one shows; not counted (Pools), only the last completes the objective.</summary>
        private static void Waypoint(BootcampRuntimeTestHarness.Harness harness, uint waypoint, bool counted = true)
        {
            using var unit = harness.Context.CreateChar();
            var row = new CharacterTeleporterEntry(harness.Client.Player.Id, waypoint, (byte)WaypointType.Waypoint);
            unit.CharacterTeleporters.Add(row);
            harness.Client.Player.GainedWaypoints.Add(row);
            var progressed = harness.Manager.RecordProgress(harness.Client, MissionProgressEvent.Waypoint(waypoint));
            if (counted)
                Assert.IsTrue(progressed, $"waypoint {waypoint}");
        }

        private static void Logos(BootcampRuntimeTestHarness.Harness harness, uint logos, bool counted = true)
        {
            using var unit = harness.Context.CreateChar();
            unit.CharacterLogoses.SetLogos(harness.Client.Player.Id, logos);
            harness.Client.Player.Logos.Add(logos);
            var progressed = harness.Manager.RecordProgress(harness.Client, MissionProgressEvent.Logos(logos));
            if (counted)
                Assert.IsTrue(progressed, $"Logos {logos}");
        }

        /// <summary>Missions done, in the character's history as a turn-in leaves them.</summary>
        private static void Archive(BootcampRuntimeTestHarness.Harness harness, IEnumerable<uint> missions)
        {
            foreach (var mission in missions)
            {
                using var unit = harness.Context.CreateChar();
                unit.ExecuteTransaction(() => unit.CharacterMissions.Runtime.Archive(
                    new CharacterMissionEntry(harness.Client.Player.Id, mission, (uint)MissionState.Completed)
                    {
                        AssignmentId = Guid.NewGuid().ToString("N"), Generation = 1, ContentRevision = "valverde-targets-test"
                    }, harness.UtcNow));
                harness.Client.Player.MissionHistory[mission] = MissionState.Completed;
                harness.Client.Player.MissionSuccessHistory.Add(mission);
            }
        }

        /// <summary>Another map for the harness, if it has not got it.</summary>
        private static void AddMap(BootcampRuntimeTestHarness.Harness harness, uint mapContextId)
        {
            if (harness.Maps.FindByContextId(mapContextId) == null)
                harness.Maps.MapChannelArray.Add(mapContextId, new MapChannel
                {
                    MapInfo = new MapInfo(mapContextId, "battlefield_fixture", 1556, 0),
                    ClientList = new List<Client>(),
                    PlayerLimit = 128
                });
        }

        /// <summary>The player onto another of the harness's maps, as a map change leaves them.</summary>
        private static void Arrive(BootcampRuntimeTestHarness.Harness harness, uint mapContextId)
        {
            var client = harness.Client;

            AddMap(harness, mapContextId);
            var destination = harness.Maps.FindByContextId(mapContextId);

            CellManager.Instance.RemoveFromWorld(client);
            client.Player.MapChannel.ClientList.Remove(client);
            client.Player.MapChannel = destination;
            client.Player.RuntimeMapChannel = destination;
            client.Player.MapContextId = destination.MapInfo.MapContextId;
            destination.ClientList.Add(client);
            CellManager.Instance.AddToWorld(client);
        }

        /// <summary>Every row of the two missions and the three new bosses: what Down has to leave as it found.</summary>
        private static string[] Dump(SqliteWorldContext world)
        {
            var rows = new List<string>();
            var connection = world.Database.GetDbConnection();

            if (connection.State != System.Data.ConnectionState.Open)
                connection.Open();

            var queries = MissionTables.Select(table => (table, $"select * from {table} where mission_id in ({string.Join(", ", Missions)})"))
                .Concat(CreatureTables.Select(table => (table, $"select * from {table} where id in ({string.Join(", ", Valverde.NewBosses.Select(boss => boss.Id).Append(Valverde.Strider))})")))
                .Append(("creature_action", $"select * from creature_action where id in ({string.Join(", ", Valverde.Attacks.Select(attack => attack.Id))})"));

            foreach (var (table, query) in queries)
            {
                using var command = connection.CreateCommand();
                command.CommandText = query;
                using var reader = command.ExecuteReader();

                while (reader.Read())
                    rows.Add(table + ": " + string.Join(" | ", Enumerable.Range(0, reader.FieldCount)
                        .Select(index => reader.GetName(index) + "=" + (reader.IsDBNull(index) ? "NULL" : Convert.ToString(reader.GetValue(index), System.Globalization.CultureInfo.InvariantCulture)))));
            }

            rows.Sort(StringComparer.Ordinal);
            return rows.ToArray();
        }

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "navmesh")))
                directory = directory.Parent;

            return directory?.FullName ?? AppContext.BaseDirectory;
        }

        private static T WithDatabase<TContext, T>(Func<TContext, T> body) where TContext : DbContext
        {
            var path = Path.Combine(AppContext.BaseDirectory, "TestDatabases", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);

            try
            {
                using var context = (TContext)(object)PersistenceIntegrationTests.CreateContext(typeof(TContext), Path.Combine(path, "database"));
                return body(context);
            }
            finally
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

                try
                {
                    Directory.Delete(path, true);
                }
                catch (IOException)
                {
                }
            }
        }

        #endregion
    }
}
