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
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Mission.Server;
    using Rasa.Services.Preloader;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Structures.World;
    using Rasa.Test.Database;
    using Rasa.Test.Missions.Wilderness;

    using OneOffs = Rasa.Services.Preloader.TargetsOfOpportunitySeed.OneOffs;

    /// <summary>
    /// The titles that are one of a kind and are a Targets of Opportunity objective's
    /// (TargetsOfOpportunitySeed.OneOffs): Wilderness Spelunker, Mires Explorer, the Undertaker
    /// and Palisades Stalker Killer. The two Wanderers are the waypoints' (WaypointTitlesTests).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class OneOffTitlesTests
    {
        private const uint Wilderness = OneOffs.WildernessMissionId;
        private const uint Caves = OneOffs.VisitAllCaves;
        private const uint Spelunker = OneOffs.Spelunker;

        private const uint Mires = OneOffs.MiresMissionId;
        private const uint Operations = OneOffs.MiresOperations;
        private const uint Explorer = OneOffs.MiresExplorer;
        private const uint MiresMap = 1759;

        private const uint HowlingMaw = 1752;
        private const uint InfectedForeans = 74;        // "Kill 100 Infected Foreans"
        private const uint Undertaker = 724;
        private const uint HowlingMawMap = 2051;
        private const uint InfectedGunner = 25607;      // Redshirt_Forean_Gunner_Infected

        private const uint Palisades = 1809;
        private const uint Stalkers = 64;               // "Kill 5 Stalkers", of mission 1630
        private const uint StalkerKiller = 510;
        private const uint PalisadesMap = 1244;

        private const uint Stalker = TargetsOfOpportunitySeed.Flag.Stalker;
        private const uint Thrax = TargetsOfOpportunitySeed.Flag.Thrax;

        /// <summary>The last world and character migrations before Oneoff_titles.</summary>
        private const string WorldBefore = "20261117000000_Hold_captive_pierre";
        private const string CharBefore = "20261116000000_Wilderness_targets_kill_rules";

        private static readonly uint[] Missions = { Wilderness, Mires, HowlingMaw, Palisades };

        private static readonly string[] MissionTables =
        {
            "mission_channel_policy", "mission_repeat_policy", "mission_scene_binding", "mission_evidence",
            "mission_action", "mission_trigger", "mission_scenario_step", "mission_scenario",
            "mission_spawn", "mission_spawn_group", "mission_indicator", "mission_area",
            "mission_reward_item", "mission_reward_definition", "mission_objective_transition",
            "mission_objective_definition", "mission_prerequisite", "mission_content_definition"
        };

        #region The seed

        [TestMethod]
        public void WhatIsAddedIsSixObjectivesOfThreeMissionsAndATitleOnAFourth()
        {
            var added = OneOffs.AddedObjectives;

            CollectionAssert.AreEquivalent(new[] { Mires, HowlingMaw, Palisades }, added.Keys.ToArray());
            CollectionAssert.AreEqual(new uint[] { 40, 56, 57, 58 }, added[Mires]);
            CollectionAssert.AreEqual(new[] { InfectedForeans }, added[HowlingMaw]);
            CollectionAssert.AreEqual(new[] { Stalkers }, added[Palisades]);
            Assert.HasCount(6, OneOffs.Objectives);

            // None of them is an objective the mission had, nor takes the place of one in its log.
            foreach (var (mission, objectives) in added)
            {
                var zone = TargetsOfOpportunitySeed.Zones.Single(candidate => candidate.MissionId == mission);
                var had = zone.Kills.Select(kill => kill.ObjectiveId).Append(zone.AllObjectiveId).ToArray();

                Assert.IsFalse(objectives.Intersect(had).Any(), zone.Name);

                var ordinals = OneOffs.Objectives.Where(row => (uint)row[0] == mission).Select(row => (uint)row[9]).ToArray();

                CollectionAssert.AreEqual(Enumerable.Range(zone.Kills.Count + 2, objectives.Length).Select(value => (uint)value).ToArray(), ordinals, zone.Name);
            }

            // Nor is a title one that something else gives.
            var given = TargetsOfOpportunitySeed.Zones.Append(TargetsOfOpportunitySeed.Wilderness).SelectMany(zone => zone.Kills).Select(kill => kill.TitleId)
                .Concat(WaypointTitles.Zones.Select(zone => zone.TitleId)).ToHashSet();

            foreach (var title in new[] { Spelunker, Explorer, Undertaker, StalkerKiller })
                Assert.IsFalse(given.Contains(title), $"{title}");

            CollectionAssert.AreEqual(new[] { Undertaker, StalkerKiller }, OneOffs.Kills.Select(kill => kill.Kill.TitleId).ToArray());
            CollectionAssert.AreEqual(new uint[] { 2115, 2107, 2125 }, OneOffs.Operations.Select(operation => operation.MapContextId).ToArray());

            foreach (var operation in OneOffs.Operations)
                Assert.IsTrue(TargetsOfOpportunitySeed.Zones.Single(zone => zone.MissionId == Mires).Maps.Contains(operation.MapContextId), operation.Name);

            // The comments fit their columns, and the SQL carries nothing MySQL would read as an escape.
            foreach (var row in OneOffs.Objectives.Concat(OneOffs.Transitions).Concat(OneOffs.KillTriggers).Concat(OneOffs.AreaTriggers)
                .Concat(OneOffs.Actions).Concat(OneOffs.Areas))
                Assert.IsTrue(((string)row[^1]).Length <= 64, (string)row[^1]);

            foreach (var row in OneOffs.Evidence)
                Assert.IsTrue(((string)row[^1]).Length <= 256 && ((string)row[7]).Length <= 256, (string)row[^1]);

            foreach (var statement in OneOffs.WorldUp.Concat(OneOffs.WorldDown).Concat(OneOffs.CharUp).Concat(OneOffs.CharDown))
                Assert.IsFalse(statement.Contains('\\'), statement);
        }

        [TestMethod]
        public void ABindingIsTheMissionsOwnAndWhatIsAdded()
        {
            string Json(MissionSceneDefinition scene) => JsonSerializer.Serialize(scene, MissionContentCodec.Options);
            TargetsOfOpportunitySeed.Zone Zone(uint mission) => TargetsOfOpportunitySeed.Zones.Single(zone => zone.MissionId == mission);

            // The Mires: the three kills as they were, the operations' objective their sum and its title.
            var mires = OneOffs.Scene(Mires);

            Assert.AreEqual(Explorer, mires.Titles[Operations]);
            CollectionAssert.AreEqual(new uint[] { 56, 57, 58 }, mires.ObjectiveAggregations[Operations].ChildObjectiveIds.ToArray());
            Assert.AreEqual(3u, mires.ObjectiveAggregations[Operations].TargetCount);
            Assert.AreEqual(0u, mires.ObjectiveAggregations[Operations].CounterId);
            CollectionAssert.AreEqual(new uint[] { 56, 57, 58 }, mires.HiddenObjectiveIds);
            Assert.IsFalse(mires.Credit.ContainsKey(Operations) || mires.ObjectiveRequirements.ContainsKey(Operations), "entering is one's own, and where is the area's");

            mires.Titles.Remove(Operations);
            mires.ObjectiveAggregations = null;
            mires.HiddenObjectiveIds = null;
            Assert.AreEqual(Json(TargetsOfOpportunitySeed.Scene(Zone(Mires))), Json(mires));

            // A kill added has what the mission's own kills have.
            foreach (var (mission, objective, title, shown) in new[] { (HowlingMaw, InfectedForeans, Undertaker, true), (Palisades, Stalkers, StalkerKiller, false) })
            {
                var scene = OneOffs.Scene(mission);

                Assert.AreEqual(title, scene.Titles[objective]);
                Assert.AreEqual(MissionCreditMode.NearbyParty, scene.Credit[objective].Mode);
                Assert.AreEqual(TargetsOfOpportunitySeed.SquadRadius, scene.Credit[objective].Radius);
                CollectionAssert.AreEqual(Zone(mission).Maps.ToArray(), ((MapRequirement)scene.ObjectiveRequirements[objective]).Maps.ToArray());

                if (shown)
                    Assert.IsNull(scene.HiddenObjectiveIds);
                else
                    CollectionAssert.AreEqual(new[] { objective }, scene.HiddenObjectiveIds);

                scene.Titles.Remove(objective);
                scene.Credit.Remove(objective);
                scene.ObjectiveRequirements.Remove(objective);
                scene.HiddenObjectiveIds = null;
                Assert.AreEqual(Json(TargetsOfOpportunitySeed.Scene(Zone(mission))), Json(scene));
            }

            // The Wilderness: the mission's binding and one title more.
            var wilderness = OneOffs.WildernessScene();

            Assert.AreEqual(Spelunker, wilderness.Titles[Caves]);
            wilderness.Titles.Remove(Caves);
            Assert.AreEqual(Json(WildernessTargetsKillRules.Scene()), Json(wilderness));
        }

        #endregion

        #region The world database

        [TestMethod]
        public void TheMigrationAddsItsRowsAndDownLeavesTheMissionsAsTheyWere()
        {
            WithDatabase<SqliteWorldContext, int>(world =>
            {
                world.GetService<IMigrator>().Migrate(WorldBefore);
                var before = Dump(world);

                // Its own is the next, and then what is dated after it.
                Assert.AreEqual("20261118000000_Oneoff_titles", world.Database.GetPendingMigrations().First());

                world.Database.Migrate();

                foreach (var (mission, objectives) in OneOffs.AddedObjectives)
                {
                    var zone = TargetsOfOpportunitySeed.Zones.Single(candidate => candidate.MissionId == mission);

                    CollectionAssert.AreEquivalent(
                        zone.Kills.Select(kill => kill.ObjectiveId).Append(zone.AllObjectiveId).Concat(objectives).ToArray(),
                        world.Set<MissionObjectiveDefinitionEntry>().AsNoTracking().Where(row => row.MissionId == mission).Select(row => row.ObjectiveId).ToArray(),
                        zone.Name);
                    Assert.AreEqual(
                        JsonSerializer.Serialize(OneOffs.Scene(mission), MissionContentCodec.Options),
                        world.Set<MissionSceneBindingEntry>().AsNoTracking().Single(row => row.MissionId == mission).Bindings, zone.Name);
                    Assert.AreEqual(4, world.Set<MissionEvidenceEntry>().AsNoTracking().Count(row => row.MissionId == mission), zone.Name);
                }

                Assert.AreEqual(
                    JsonSerializer.Serialize(OneOffs.WildernessScene(), MissionContentCodec.Options),
                    world.Set<MissionSceneBindingEntry>().AsNoTracking().Single(row => row.MissionId == Wilderness).Bindings);

                var areas = world.Set<MissionAreaEntry>().AsNoTracking().Where(row => row.MissionId == Mires).OrderBy(row => row.AreaId).ToArray();

                CollectionAssert.AreEqual(new uint[] { 56, 57, 58 }, areas.Select(area => area.AreaId).ToArray());
                CollectionAssert.AreEqual(new uint[] { 2115, 2107, 2125 }, areas.Select(area => area.MapContextId).ToArray());

                // Each the whole of a map the Mires have a way into.
                foreach (var area in areas)
                {
                    Assert.AreEqual(MissionAreaShape.Map, area.Shape);
                    Assert.IsNull(area.Radius);
                    Assert.IsTrue(world.Set<MapLinkEntry>().AsNoTracking().Any(link => link.MapContextId == MiresMap && link.DestMapContextId == area.MapContextId && link.Enabled != 0),
                        $"{area.AreaId}");
                }

                // The kills are of what the world has: a class, a flag.
                var classes = world.Set<EntityClassEntry>().AsNoTracking().Select(row => row.Id).ToHashSet();
                var flags = world.Set<CreatureClassFlagEntry>().AsNoTracking().Select(row => row.FlagId).Distinct().ToHashSet();

                foreach (var added in OneOffs.Kills)
                    foreach (var subject in added.Kill.Subjects)
                        Assert.IsTrue((added.Kill.EventKind == MissionProgressEventKind.CreatureFlagKilled ? flags : classes).Contains(subject), $"{added.Kill.Title}: {subject}");

                // The other battlefields' missions are as they were.
                Assert.AreEqual(14, world.MissionContentDefinitionEntries.AsNoTracking().Count(row => row.ContentRevision == TargetsOfOpportunitySeed.Revision && row.Enabled));
                Assert.AreEqual(59 + 14 + 6, world.Set<MissionObjectiveDefinitionEntry>().AsNoTracking().Count(row => row.ContentRevision == TargetsOfOpportunitySeed.Revision));

                world.GetService<IMigrator>().Migrate(WorldBefore);

                CollectionAssert.AreEqual(before, Dump(world));
                return 0;
            });
        }

        #endregion

        #region The character database

        [TestMethod]
        public void ACharacterHoldingAMissionIsGivenItsNewObjectivesAndKeepsTheirCounts()
        {
            WithDatabase<SqliteCharContext, int>(characters =>
            {
                characters.GetService<IMigrator>().Migrate(CharBefore);

                characters.GameAccountEntries.Add(new GameAccountEntry
                {
                    Id = 1, Email = "oneoffs@example.invalid", Name = "Oneoffs", FamilyName = "Fixture", SelectedSlot = 1,
                    CanSkipBootcamp = false, CreatedAt = DateTime.UtcNow, LastLogin = DateTime.UtcNow, LastIp = "127.0.0.1", Level = 0
                });
                foreach (var id in new uint[] { 1, 2 })
                    characters.CharacterEntries.Add(new CharacterEntry
                    {
                        Id = id, AccountId = 1, Slot = (byte)id, Name = $"Holder{id}", Race = 1, Class = 1, Scale = 1, Level = 40,
                        MapContextId = 1220, RunState = 1, LastLogin = DateTime.UtcNow, CreatedAt = DateTime.UtcNow, LastPvPClan = DateTime.UtcNow
                    });
                characters.SaveChanges();

                // Character 1 holds the three missions as they were, and has seen every cave.
                // Character 2 holds the Divide's, and the Wilderness's with caves yet to see.
                characters.CharacterMissionEntries.Add(Held(1, HowlingMaw, TargetsOfOpportunitySeed.Revision, MissionObjectiveState.Incomplete, 58, 60, 71, 72, 73, 75));
                characters.CharacterMissionEntries.Add(Held(1, Mires, TargetsOfOpportunitySeed.Revision, MissionObjectiveState.Incomplete, 68, 69, 70));
                characters.CharacterMissionEntries.Add(Held(1, Palisades, TargetsOfOpportunitySeed.Revision, MissionObjectiveState.Incomplete, 62, 63));
                characters.CharacterMissionEntries.Add(Held(1, Wilderness, WildernessTargetsKillRules.Revision, MissionObjectiveState.Completed, Caves));
                characters.CharacterMissionEntries.Add(Held(2, 1582, TargetsOfOpportunitySeed.Revision, MissionObjectiveState.Incomplete, 3, 5));
                characters.CharacterMissionEntries.Add(Held(2, Wilderness, WildernessTargetsKillRules.Revision, MissionObjectiveState.Incomplete, Caves));
                characters.SaveChanges();
                characters.ChangeTracker.Clear();

                characters.Database.Migrate();
                Check();

                // Run again, it changes nothing.
                foreach (var statement in OneOffs.CharUp)
                    characters.Database.ExecuteSqlRaw(statement);

                Check();

                void Check()
                {
                    (uint Objective, byte State)[] Objectives(uint character, uint mission) => characters.CharacterMissionObjectiveEntries.AsNoTracking()
                        .Where(row => row.CharacterId == character && row.MissionId == mission).OrderBy(row => row.ObjectiveId).ToArray()
                        .Select(row => (row.ObjectiveId, row.ObjectiveState)).ToArray();
                    (uint Objective, uint Value)[] Counters(uint character, uint mission) => characters.CharacterMissionObjectiveCounterEntries.AsNoTracking()
                        .Where(row => row.CharacterId == character && row.MissionId == mission).OrderBy(row => row.ObjectiveId).ToArray()
                        .Select(row => (row.ObjectiveId, row.CounterValue)).ToArray();
                    const byte incomplete = (byte)MissionObjectiveState.Incomplete;

                    CollectionAssert.AreEqual(new (uint, byte)[] { (58, incomplete), (60, incomplete), (71, incomplete), (72, incomplete), (73, incomplete), (74, incomplete), (75, incomplete) },
                        Objectives(1, HowlingMaw));
                    CollectionAssert.AreEqual(new (uint, uint)[] { (58, 7), (60, 7), (71, 7), (72, 7), (73, 7), (74, 0), (75, 7) }, Counters(1, HowlingMaw));

                    CollectionAssert.AreEqual(new (uint, byte)[] { (40, incomplete), (56, incomplete), (57, incomplete), (58, incomplete), (68, incomplete), (69, incomplete), (70, incomplete) },
                        Objectives(1, Mires));
                    CollectionAssert.AreEqual(new (uint, uint)[] { (40, 0), (68, 7), (69, 7), (70, 7) }, Counters(1, Mires), "an operation entered has no count of its own");

                    CollectionAssert.AreEqual(new (uint, byte)[] { (62, incomplete), (63, incomplete), (64, incomplete) }, Objectives(1, Palisades));
                    CollectionAssert.AreEqual(new (uint, uint)[] { (62, 7), (63, 7), (64, 0) }, Counters(1, Palisades));

                    // A mission that gains nothing is as it was.
                    CollectionAssert.AreEqual(new (uint, uint)[] { (3, 7), (5, 7) }, Counters(2, 1582));
                    Assert.HasCount(1, Objectives(1, Wilderness));

                    // The caves all seen before the title was theirs to give.
                    CollectionAssert.AreEqual(new[] { (1u, Spelunker) },
                        characters.CharacterTitleEntries.AsNoTracking().ToArray().Select(row => (row.CharacterId, row.TitleId)).ToArray());
                }

                // Back: the rows go, the title stays.
                characters.GetService<IMigrator>().Migrate(CharBefore);

                Assert.AreEqual(6 + 3 + 2 + 1 + 2 + 1, characters.CharacterMissionObjectiveEntries.AsNoTracking().Count());
                Assert.IsFalse(characters.CharacterMissionObjectiveEntries.AsNoTracking().Any(row =>
                    row.MissionId == HowlingMaw && row.ObjectiveId == InfectedForeans || row.MissionId == Mires && row.ObjectiveId <= 58 ||
                    row.MissionId == Palisades && row.ObjectiveId == Stalkers));
                Assert.AreEqual(6 + 3 + 2 + 1 + 2 + 1, characters.CharacterMissionObjectiveCounterEntries.AsNoTracking().Count());
                Assert.AreEqual(Spelunker, characters.CharacterTitleEntries.AsNoTracking().Single().TitleId);
                return 0;
            });

            static CharacterMissionEntry Held(uint characterId, uint missionId, string revision, MissionObjectiveState state, params uint[] objectives)
            {
                var held = new CharacterMissionEntry(characterId, missionId, (uint)MissionState.Active) { ContentRevision = revision };

                foreach (var objectiveId in objectives)
                {
                    var objective = new CharacterMissionObjectiveEntry
                    {
                        CharacterId = characterId, MissionId = missionId, ObjectiveId = objectiveId, ObjectiveState = (byte)state
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

        #endregion

        #region The missions in the world

        [TestMethod]
        public void TheMissionsStandUpWithWhatIsAdded()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var missions = harness.Manager.LoadedMissions;

            foreach (var mission in Missions)
                Assert.IsTrue(missions[mission].IsOperational, $"{mission}: {missions[mission].OperationalDiagnostic}");

            // The Undertaker: a kill counter like the mission's others, by class.
            var foreans = missions[HowlingMaw].Objectives[InfectedForeans];

            Assert.AreEqual(Undertaker, foreans.TitleId);
            Assert.IsTrue(foreans.IsVisible);
            Assert.IsFalse(foreans.IsRequired.Value);
            Assert.AreEqual(MissionProgressEventKind.CreatureClassKilled, foreans.ProgressRule.Kind);
            CollectionAssert.AreEquivalent(new uint[] { 25607, 25618, 25882 }, foreans.ProgressRule.Subjects.ToArray());
            Assert.AreEqual(100u, foreans.Counters[0].TargetValue);
            Assert.AreEqual(17301u, foreans.ClientCounterTextIds[0]);
            Assert.AreEqual(MissionCreditMode.NearbyParty, foreans.CreditPolicy.Mode);
            CollectionAssert.AreEqual(new uint[] { 2051, 2136, 2162 }, ((MapRequirement)missions[HowlingMaw].ObjectiveRequirements[InfectedForeans]).Maps.ToArray());

            // Palisades Stalker Killer: counted, and no objective of the log.
            var stalkers = missions[Palisades].Objectives[Stalkers];

            Assert.AreEqual(StalkerKiller, stalkers.TitleId);
            Assert.IsFalse(stalkers.IsVisible);
            Assert.IsFalse(stalkers.IsRequired.Value);
            Assert.AreEqual(MissionProgressEventKind.CreatureFlagKilled, stalkers.ProgressRule.Kind);
            CollectionAssert.AreEqual(new[] { Stalker }, stalkers.ProgressRule.Subjects.ToArray());
            Assert.AreEqual(5u, stalkers.Counters[0].TargetValue);

            // Mires Explorer: the operations' objective, the sum of the three entered.
            var operations = missions[Mires].Objectives[Operations];

            Assert.AreEqual(Explorer, operations.TitleId);
            Assert.IsTrue(operations.IsAggregate);
            Assert.IsTrue(operations.IsVisible);
            Assert.IsFalse(operations.IsRequired.Value);
            Assert.AreEqual(3u, operations.Counters[0].TargetValue);
            Assert.AreEqual(14748u, operations.ClientCounterTextIds[0], "Operations Completed");

            foreach (var operation in OneOffs.Operations)
            {
                var entered = missions[Mires].Objectives[operation.ObjectiveId];

                Assert.IsFalse(entered.IsVisible, operation.Name);
                Assert.IsNull(entered.TitleId, operation.Name);
                Assert.AreEqual(MissionProgressEventKind.AreaEntered, entered.GetExecutableTransitionsOrLegacyDefault().Single().ProgressRule.Kind, operation.Name);
                Assert.IsTrue(harness.Manager.TryGetAreaDefinition(Mires, operation.AreaId, out var area), operation.Name);
                Assert.AreEqual(operation.MapContextId, area.MapContextId, operation.Name);
                Assert.AreEqual(MissionAreaShape.Map, area.Shape, operation.Name);
            }

            // The Wilderness's caves: the objective that was there, and now its title.
            var caves = missions[Wilderness].Objectives[Caves];

            Assert.AreEqual(Spelunker, caves.TitleId);
            Assert.IsTrue(caves.IsAggregate);
            Assert.AreEqual(6u, caves.Counters[0].TargetValue);

            // What a mission shows of itself: the Stalkers and the operations entered are not in it.
            CollectionAssert.AreEquivalent(new uint[] { 62, 63, 81 },
                missions[Palisades].CreateInfo(MissionState.Active, false, missions[Palisades].CreateInitialObjectiveLogs())
                    .ObjectivesList.Select(objective => objective.ObjectiveId).ToArray());
            CollectionAssert.AreEquivalent(new uint[] { 40, 68, 69, 70, 72 },
                missions[Mires].CreateInfo(MissionState.Active, false, missions[Mires].CreateInitialObjectiveLogs())
                    .ObjectivesList.Select(objective => objective.ObjectiveId).ToArray());
        }

        [TestMethod]
        public void AHundredInfectedForeansOnHowlingMawMakeAnUndertaker()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            Take(harness, HowlingMaw, HowlingMawMap);

            // A Forean who is not infected is no part of it: the class is counted, not the species.
            Assert.IsFalse(harness.Manager.Credit.RecordKill(harness.Client, Kill(900, 6043, 97), Vector3.Zero));

            for (var kills = 1u; kills <= 99; kills++)
                Assert.IsTrue(harness.Manager.Credit.RecordKill(harness.Client, Kill(900, kills % 2 == 0 ? InfectedGunner : 25618), Vector3.Zero));

            var packets = harness.Drain();
            var last = packets.OfType<UpdateObjectiveCounterPacket>().Last();

            Assert.AreEqual((HowlingMaw, InfectedForeans, 0u, 99u, 0u, 100u),
                (last.MissionId, last.ObjectiveId, last.CounterId, last.CounterValue, last.InitialValue, last.TargetValue));
            Assert.IsEmpty(packets.OfType<TitleAddedPacket>().ToArray());

            Assert.IsTrue(harness.Manager.Credit.RecordKill(harness.Client, Kill(900, 25882), Vector3.Zero));

            packets = harness.Drain();
            Assert.AreEqual(InfectedForeans, packets.OfType<ObjectiveCompletedPacket>().Single().ObjectiveId);
            Assert.AreEqual(Undertaker, packets.OfType<TitleAddedPacket>().Single().TitleId);
            Assert.IsTrue(harness.Client.Player.Titles.Contains(Undertaker));
            Assert.AreEqual(MissionState.Active, harness.Client.Player.Missions[HowlingMaw].State);
        }

        [TestMethod]
        public void TheFifthStalkerOfThePalisadesGivesItsTitleAndNothingOfTheCountIsShown()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            Take(harness, Palisades, PalisadesMap);

            for (var kills = 1u; kills <= 4; kills++)
                Assert.IsTrue(harness.Manager.Credit.RecordKill(harness.Client, Kill(540002, 3781, Stalker), Vector3.Zero));

            Assert.AreEqual(4U, harness.Client.Player.Missions[Palisades].Objectives[Stalkers].Counters[0]);
            Assert.IsEmpty(harness.Drain().Where(packet => packet is UpdateObjectiveCounterPacket or ObjectiveCompletedPacket or TitleAddedPacket).ToArray(),
                "the client has no objective 64 of this mission to show a count on");

            // A Stalker of another battlefield is not the Palisades'.
            Arrive(harness, BootcampRuntimeTestHarness.BootcampMapContextId);
            Assert.IsFalse(harness.Manager.Credit.RecordKill(harness.Client, Kill(540002, 3781, Stalker), Vector3.Zero));
            Arrive(harness, PalisadesMap);
            harness.Drain();

            Assert.IsTrue(harness.Manager.Credit.RecordKill(harness.Client, Kill(540002, 3781, Stalker), Vector3.Zero));

            var packets = harness.Drain();
            Assert.AreEqual(StalkerKiller, packets.OfType<TitleAddedPacket>().Single().TitleId);
            Assert.IsEmpty(packets.Where(packet => packet is UpdateObjectiveCounterPacket or ObjectiveCompletedPacket).ToArray());
            Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[Palisades].Objectives[Stalkers].State);

            using var unit = harness.Context.CreateChar();
            CollectionAssert.Contains(unit.CharacterTitles.Get(harness.Client.Player.Id), StalkerKiller);
        }

        [TestMethod]
        public void EnteringEachOperationOfTheMiresMakesAnExplorer()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            harness.Client.MissionAreaService = new MissionAreaService(() => harness.Manager);
            Take(harness, Mires, MiresMap);

            // On the Mires themselves nothing is entered.
            ManifestationManager.Instance.AssignPlayer(harness.Client);
            Assert.AreEqual(0U, harness.Client.Player.Missions[Mires].Objectives[Operations].Counters[0]);
            harness.Drain();

            // Into the Fluxite Mines, as a map change puts a player there (MapChannelManager: AssignPlayer).
            Enter(2115);

            var counter = harness.Drain().OfType<UpdateObjectiveCounterPacket>().Single();
            Assert.AreEqual((Mires, Operations, 0u, 1u, 0u, 3u),
                (counter.MissionId, counter.ObjectiveId, counter.CounterId, counter.CounterValue, counter.InitialValue, counter.TargetValue));
            Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[Mires].Objectives[56].State);

            // Out and in again: an operation is entered once.
            Enter(MiresMap);
            Enter(2115);
            Assert.AreEqual(1U, harness.Client.Player.Missions[Mires].Objectives[Operations].Counters[0]);

            Enter(2125);
            var packets = harness.Drain();
            Assert.AreEqual(2U, packets.OfType<UpdateObjectiveCounterPacket>().Single().CounterValue);
            Assert.IsEmpty(packets.OfType<ObjectiveCompletedPacket>().ToArray(), "the three entered are not objectives the log shows");
            Assert.IsEmpty(packets.OfType<TitleAddedPacket>().ToArray());

            Enter(2107);
            packets = harness.Drain();
            Assert.AreEqual(3U, packets.OfType<UpdateObjectiveCounterPacket>().Single().CounterValue);
            Assert.AreEqual(Operations, packets.OfType<ObjectiveCompletedPacket>().Single().ObjectiveId);
            Assert.AreEqual(Explorer, packets.OfType<TitleAddedPacket>().Single().TitleId);
            Assert.IsTrue(IndexOf<ObjectiveCompletedPacket>(packets) < IndexOf<TitleAddedPacket>(packets), "the objective, then what it gave");
            Assert.IsTrue(harness.Client.Player.Titles.Contains(Explorer));

            using (var unit = harness.Context.CreateChar())
                CollectionAssert.Contains(unit.CharacterTitles.Get(harness.Client.Player.Id), Explorer);

            Enter(2107);
            Assert.IsEmpty(harness.Drain().OfType<TitleAddedPacket>().ToArray());

            void Enter(uint mapContextId)
            {
                Arrive(harness, mapContextId);
                ManifestationManager.Instance.AssignPlayer(harness.Client);
            }
        }

        [TestMethod]
        public void AMissionTakenBeforeIsKeptWithItsCountsOnceItHasTheNewObjectives()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            Take(harness, HowlingMaw, HowlingMawMap);

            for (var kills = 0; kills < 3; kills++)
                Assert.IsTrue(harness.Manager.Credit.RecordKill(harness.Client, Kill(3, 20757, Thrax), Vector3.Zero));

            // As a character who took the mission before it had objective 74 holds it; then the
            // character database's half of the migration.
            using (var characters = harness.Context.Open())
            {
                foreach (var statement in OneOffs.CharDown)
                    characters.Database.ExecuteSqlRaw(statement);

                Assert.IsFalse(characters.CharacterMissionObjectiveEntries.AsNoTracking().Any(row => row.MissionId == HowlingMaw && row.ObjectiveId == InfectedForeans));

                foreach (var statement in OneOffs.CharUp)
                    characters.Database.ExecuteSqlRaw(statement);
            }

            harness.ReconnectFresh();

            var log = harness.Client.Player.Missions[HowlingMaw];
            Assert.AreEqual(MissionState.Active, log.State);
            Assert.AreEqual(3U, log.Objectives[60].Counters[0], "The Dreadnaught's Thrax");
            Assert.AreEqual(0U, log.Objectives[InfectedForeans].Counters[0]);
            Assert.AreEqual(MissionObjectiveState.Incomplete, log.Objectives[InfectedForeans].State);

            Arrive(harness, HowlingMawMap);
            harness.Drain();
            Assert.IsTrue(harness.Manager.Credit.RecordKill(harness.Client, Kill(900, InfectedGunner), Vector3.Zero));
            Assert.AreEqual(1U, harness.Drain().OfType<UpdateObjectiveCounterPacket>().Single().CounterValue);
        }

        #endregion

        #region The caves of the Wilderness

        [TestMethod]
        public void TheLastCaveOfTheWildernessMakesASpelunker()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            harness.SpawnWorld(196);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, harness.Npc(196).EntityId, Wilderness));
            harness.Drain();

            foreach (var areaId in new uint[] { 49, 50, 51, 52, 53 })
                Walk(areaId);

            Assert.AreEqual(5U, harness.Client.Player.Missions[Wilderness].Objectives[Caves].Counters[0]);
            Assert.IsEmpty(harness.Drain().OfType<TitleAddedPacket>().ToArray(), "five of six");

            Walk(541);

            var packets = harness.Drain();
            Assert.AreEqual(Caves, packets.OfType<ObjectiveCompletedPacket>().Single().ObjectiveId);
            Assert.AreEqual(Spelunker, packets.OfType<TitleAddedPacket>().Single().TitleId);
            Assert.IsTrue(IndexOf<ObjectiveCompletedPacket>(packets) < IndexOf<TitleAddedPacket>(packets), "the objective, then what it gave");
            Assert.IsTrue(harness.Client.Player.Titles.Contains(Spelunker));

            using (var unit = harness.CreateChar())
                CollectionAssert.Contains(unit.CharacterTitles.Get(harness.Client.Player.Id), Spelunker);

            // The cave's other mouth is the same cave, and gives nothing twice.
            Walk(542);
            Assert.IsEmpty(harness.Drain().OfType<TitleAddedPacket>().ToArray());

            void Walk(uint areaId)
            {
                Assert.IsTrue(harness.Manager.TryGetAreaDefinition(Wilderness, areaId, out var area));
                var before = area.Position + new Vector3(8, 0, 0);
                var after = area.Position + new Vector3(6.5f, 0, 0);

                harness.MoveTo(before);
                harness.Client.Movement = new Rasa.Models.Movement(before, 1, 0, Vector2.Zero);
                harness.Client.Player.MoveBudget = 10;
                Assert.IsTrue(harness.Client.HandleMovement(new Rasa.Models.Movement(after, 1, 0, Vector2.Zero)));
            }
        }

        [TestMethod]
        public void ArrivingInsideAnAreaWithAShapeIsNotEnteringIt()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            harness.SpawnWorld(196);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, harness.Npc(196).EntityId, Wilderness));
            Assert.IsTrue(harness.Manager.TryGetAreaDefinition(Wilderness, 49, out var cave));
            Assert.AreEqual(MissionAreaShape.Cylinder, cave.Shape);

            // Set down in the cave's mouth, as a map change or a login sets a character down:
            // only a map that is an area is entered by arriving. The cave is walked into.
            harness.MoveTo(cave.Position);

            Assert.IsFalse(harness.Client.MissionAreaService.RecordArrival(harness.Client));
            Assert.AreEqual(0U, harness.Client.Player.Missions[Wilderness].Objectives[Caves].Counters[0]);
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[Wilderness].Objectives[49].State);
        }

        #endregion

        /// <summary>Onto the battlefield, and its mission taken from the radio that offers it there.</summary>
        private static void Take(BootcampRuntimeTestHarness.Harness harness, uint missionId, uint mapContextId)
        {
            Arrive(harness, mapContextId);
            harness.Manager.OfferArrivalMissions(harness.Client);
            Assert.IsTrue(harness.Manager.TryAcceptRadioMission(harness.Client, missionId));
            harness.Drain();
        }

        /// <summary>A kill as CreatureManager.KillEvents names it: the creature row, its class, the flags of its class.</summary>
        private static MissionProgressEvent[] Kill(uint creatureId, uint classId, params uint[] flags) =>
            new[] { MissionProgressEvent.Creature(creatureId), MissionProgressEvent.CreatureClass(classId) }
                .Concat(flags.Select(MissionProgressEvent.CreatureFlag)).ToArray();

        /// <summary>The player onto another of the harness's maps, as a map change leaves them.</summary>
        private static void Arrive(BootcampRuntimeTestHarness.Harness harness, uint mapContextId)
        {
            var client = harness.Client;

            if (mapContextId != BootcampRuntimeTestHarness.BootcampMapContextId && harness.Maps.FindByContextId(mapContextId) == null)
                harness.Maps.MapChannelArray.Add(mapContextId, new MapChannel
                {
                    MapInfo = new MapInfo(mapContextId, "battlefield_fixture", 1556, 0),
                    ClientList = new List<Client>(),
                    PlayerLimit = 128
                });

            var destination = mapContextId == BootcampRuntimeTestHarness.BootcampMapContextId
                ? harness.BootcampMap
                : harness.Maps.FindByContextId(mapContextId);

            CellManager.Instance.RemoveFromWorld(client);
            client.Player.MapChannel.ClientList.Remove(client);
            client.Player.MapChannel = destination;
            client.Player.RuntimeMapChannel = destination;
            client.Player.MapContextId = destination.MapInfo.MapContextId;
            destination.ClientList.Add(client);
            CellManager.Instance.AddToWorld(client);
        }

        private static int IndexOf<T>(IReadOnlyList<Rasa.Packets.PythonPacket> packets)
        {
            for (var i = 0; i < packets.Count; i++)
                if (packets[i] is T)
                    return i;

            return -1;
        }

        /// <summary>Every row of the four missions in every mission table: what Down has to leave as it found.</summary>
        private static string[] Dump(SqliteWorldContext world)
        {
            var rows = new List<string>();
            var connection = world.Database.GetDbConnection();

            if (connection.State != System.Data.ConnectionState.Open)
                connection.Open();

            foreach (var table in MissionTables)
            {
                using var command = connection.CreateCommand();
                command.CommandText = $"select * from {table} where mission_id in ({string.Join(", ", Missions)})";
                using var reader = command.ExecuteReader();

                while (reader.Read())
                    rows.Add(table + ": " + string.Join(" | ", Enumerable.Range(0, reader.FieldCount)
                        .Select(index => reader.GetName(index) + "=" + (reader.IsDBNull(index) ? "NULL" : Convert.ToString(reader.GetValue(index), System.Globalization.CultureInfo.InvariantCulture)))));
            }

            rows.Sort(StringComparer.Ordinal);
            return rows.ToArray();
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
    }
}
