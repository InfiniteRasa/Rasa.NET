using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions.Wilderness
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

    /// <summary>
    /// Wilderness Targets of Opportunity (1449) was written twice, and runs once: the Wilderness
    /// missions' mission, with the kill rules of the other battlefields' Targets of Opportunity
    /// on its four kill objectives (WildernessTargetsKillRules).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class WildernessTargetsKillRulesTests
    {
        private const uint Mission = WildernessTargetsKillRules.MissionId;
        private const uint KillXanx = 3;
        private const uint BugZapper = 365;             // "Wilderness Bug Zapper"
        private const uint Xanx = TargetsOfOpportunitySeed.Flag.Xanx;

        /// <summary>The last world migration before Wilderness_targets_kill_rules.</summary>
        private const string WorldBefore = "20261110000000_Stock_personal_waypoints";

        /// <summary>The last character migration before Wilderness_targets_kill_rules.</summary>
        private const string CharBefore = "20261115000000_Add_control_point_clan";

        private static readonly string[] MissionTables =
        {
            "mission_channel_policy", "mission_repeat_policy", "mission_scene_binding", "mission_evidence",
            "mission_action", "mission_trigger", "mission_scenario_step", "mission_scenario",
            "mission_spawn", "mission_spawn_group", "mission_indicator", "mission_area",
            "mission_reward_item", "mission_reward_definition", "mission_objective_transition",
            "mission_objective_definition", "mission_prerequisite", "mission_content_definition"
        };

        #region What the rules are written against

        [TestMethod]
        public void TheSceneAndTheCreaturesAreWhatTheWildernessMigrationInstalls()
        {
            // The binding is one document and the rules rewrite it whole, so their copy of the
            // mission's own has to be the mission's own - on both providers.
            foreach (var migration in new Migration[]
            {
                new Rasa.Migrations.SqliteWorld.WildernessAliaOpening(),
                new Rasa.Migrations.MySqlWorld.WildernessAliaOpening()
            })
            {
                var inserts = migration.UpOperations.OfType<InsertDataOperation>().ToArray();
                var binding = inserts.Single(insert => insert.Table == "mission_scene_binding" && Value(insert, "mission_id") == Mission);

                Assert.AreEqual(
                    JsonSerializer.Serialize(WildernessTargetsKillRules.MissionScene(), MissionContentCodec.Options),
                    (string)binding.Values[0, Array.IndexOf(binding.Columns, "bindings")], migration.GetType().FullName);

                foreach (var kill in WildernessTargetsKillRules.Zone.Kills)
                {
                    var trigger = inserts.Single(insert => insert.Table == MissionTriggerEntry.TableName &&
                        Value(insert, "mission_id") == Mission && Value(insert, "objective_id") == kill.ObjectiveId);

                    Assert.AreEqual(1U, Value(trigger, "transition_id"), kill.Title);
                    Assert.AreEqual(1U, Value(trigger, "trigger_id"), kill.Title);
                    Assert.AreEqual((uint)MissionProgressEventKind.CreatureKilled, Value(trigger, "event_kind"), kill.Title);
                    Assert.AreEqual(WildernessTargetsKillRules.MissionCreatures[kill.ObjectiveId], Value(trigger, "subject_id"), kill.Title);
                    Assert.AreEqual(0U, Value(trigger, "counter_id"), kill.Title);
                    Assert.AreEqual(kill.Count, Value(trigger, "target_value"), "the count is the same on both: " + kill.Title);
                    Assert.HasCount(1, kill.Subjects, "one trigger a kill objective, one species: " + kill.Title);
                }
            }

            static uint Value(InsertDataOperation insert, string column) =>
                Convert.ToUInt32(insert.Values[0, Array.IndexOf(insert.Columns, column)]);
        }

        [TestMethod]
        public void TheStatementsCarryNothingEitherProviderWouldEscape()
        {
            var statements = WildernessTargetsKillRules.WorldUp.Concat(WildernessTargetsKillRules.WorldDown)
                .Concat(WildernessTargetsKillRules.CharUp).ToArray();

            foreach (var statement in statements)
                Assert.IsFalse(statement.Contains('\\'), statement);

            foreach (var scene in new[] { WildernessTargetsKillRules.Scene(), WildernessTargetsKillRules.MissionScene() })
                Assert.IsFalse(JsonSerializer.Serialize(scene, MissionContentCodec.Options).Contains('\''));

            // The scene with the rules is the mission's own and three things more.
            var mission = WildernessTargetsKillRules.MissionScene();
            var merged = WildernessTargetsKillRules.Scene();
            var kills = WildernessTargetsKillRules.Zone.Kills.Select(kill => kill.ObjectiveId).ToArray();

            CollectionAssert.AreEquivalent(kills, merged.Titles.Keys.ToArray());
            CollectionAssert.AreEquivalent(kills, merged.Credit.Keys.ToArray());
            CollectionAssert.AreEquivalent(kills, merged.ObjectiveRequirements.Keys.ToArray());
            Assert.IsNull(merged.Category, "the mission's definition row has its category");

            merged.Titles = mission.Titles;
            merged.Credit = mission.Credit;
            merged.ObjectiveRequirements = mission.ObjectiveRequirements;
            Assert.AreEqual(JsonSerializer.Serialize(mission, MissionContentCodec.Options), JsonSerializer.Serialize(merged, MissionContentCodec.Options));
        }

        #endregion

        #region The world database, in either order

        [TestMethod]
        public void ANewDatabaseAndOneThatHadTheOtherMissionEndTheSame()
        {
            // New: every migration in order. The seed no longer has 1449 under targets_1.
            var fresh = WithWorld(world =>
            {
                world.Database.Migrate();
                return Dump(world);
            });

            // One that ran Add_targets_of_opportunity while 1449 was one of its missions, and then
            // the Wilderness missions: both of them are there until the rules run.
            var upgraded = WithWorld(world =>
            {
                world.GetService<IMigrator>().Migrate(WorldBefore);

                foreach (var statement in TargetsOfOpportunitySeed.InsertStatementsOf(new[] { TargetsOfOpportunitySeed.Wilderness }))
                    world.Database.ExecuteSqlRaw(statement.Replace("{", "{{").Replace("}", "}}"));

                CollectionAssert.AreEquivalent(
                    new[] { WildernessTargetsKillRules.SupersededRevision, WildernessTargetsKillRules.Revision },
                    world.MissionContentDefinitionEntries.AsNoTracking().Where(row => row.MissionId == Mission && row.Enabled)
                        .Select(row => row.ContentRevision).ToArray(),
                    "what Game refuses to start on");

                world.Database.Migrate();
                return Dump(world);
            });

            CollectionAssert.AreEqual(fresh, upgraded);
            Assert.IsFalse(fresh.Any(row => row.Contains(WildernessTargetsKillRules.SupersededRevision) && !row.StartsWith("enabled: ")),
                "no row of 1449 is of the revision that went");
            Assert.IsTrue(fresh.Contains($"enabled: {Mission}@{WildernessTargetsKillRules.Revision}"));
            Assert.IsFalse(fresh.Contains($"enabled: {Mission}@{WildernessTargetsKillRules.SupersededRevision}"));

            WithWorld(world =>
            {
                world.Database.Migrate();

                Assert.AreEqual(WildernessTargetsKillRules.Revision,
                    world.MissionContentDefinitionEntries.AsNoTracking().Single(row => row.MissionId == Mission).ContentRevision);
                Assert.IsFalse(world.MissionContentDefinitionEntries.AsNoTracking().Where(row => row.Enabled)
                    .GroupBy(row => row.MissionId).Any(group => group.Count() > 1), "one enabled definition a mission");

                foreach (var kill in WildernessTargetsKillRules.Zone.Kills)
                {
                    var trigger = world.Set<MissionTriggerEntry>().AsNoTracking().Single(row => row.MissionId == Mission && row.ObjectiveId == kill.ObjectiveId);

                    Assert.AreEqual((byte)MissionProgressEventKind.CreatureFlagKilled, trigger.EventKind, kill.Title);
                    Assert.AreEqual(kill.Subjects.Single(), trigger.SubjectId, kill.Title);
                    Assert.AreEqual(kill.Count, trigger.TargetValue, kill.Title);
                }

                // The rules' binding, and the caves' title put on it since (Oneoff_titles).
                Assert.AreEqual(
                    JsonSerializer.Serialize(TargetsOfOpportunitySeed.OneOffs.WildernessScene(), MissionContentCodec.Options),
                    world.Set<MissionSceneBindingEntry>().AsNoTracking().Single(row => row.MissionId == Mission).Bindings);

                // The other fourteen battlefields' are untouched.
                Assert.AreEqual(14, world.MissionContentDefinitionEntries.AsNoTracking()
                    .Count(row => row.ContentRevision == TargetsOfOpportunitySeed.Revision && row.Enabled));
                return 0;
            });
        }

        [TestMethod]
        public void DownPutsTheMissionBackAsItWasWritten()
        {
            WithWorld(world =>
            {
                world.Database.Migrate();
                world.GetService<IMigrator>().Migrate(WorldBefore);

                foreach (var kill in WildernessTargetsKillRules.Zone.Kills)
                {
                    var trigger = world.Set<MissionTriggerEntry>().AsNoTracking().Single(row => row.MissionId == Mission && row.ObjectiveId == kill.ObjectiveId);

                    Assert.AreEqual((byte)MissionProgressEventKind.CreatureKilled, trigger.EventKind, kill.Title);
                    Assert.AreEqual(WildernessTargetsKillRules.MissionCreatures[kill.ObjectiveId], trigger.SubjectId, kill.Title);
                    Assert.AreEqual($"Objective {kill.ObjectiveId} trigger 1", trigger.Comment, kill.Title);
                }

                Assert.AreEqual(
                    JsonSerializer.Serialize(WildernessTargetsKillRules.MissionScene(), MissionContentCodec.Options),
                    world.Set<MissionSceneBindingEntry>().AsNoTracking().Single(row => row.MissionId == Mission).Bindings);
                Assert.AreEqual(WildernessTargetsKillRules.Revision,
                    world.MissionContentDefinitionEntries.AsNoTracking().Single(row => row.MissionId == Mission).ContentRevision,
                    "the other mission does not come back");
                return 0;
            });
        }

        #endregion

        #region The character database

        [TestMethod]
        public void ACharacterHoldingTheOtherMissionHasItTakenBackAndNothingElse()
        {
            WithDatabase<SqliteCharContext, int>(characters =>
            {
                characters.GetService<IMigrator>().Migrate(CharBefore);

                characters.GameAccountEntries.Add(new GameAccountEntry
                {
                    Id = 1, Email = "rules@example.invalid", Name = "Rules", FamilyName = "Fixture", SelectedSlot = 1,
                    CanSkipBootcamp = false, CreatedAt = DateTime.UtcNow, LastLogin = DateTime.UtcNow, LastIp = "127.0.0.1", Level = 0
                });
                foreach (var id in new uint[] { 1, 2 })
                    characters.CharacterEntries.Add(new CharacterEntry
                    {
                        Id = id, AccountId = 1, Slot = (byte)id, Name = $"Holder{id}", Race = 1, Class = 1, Scale = 1, Level = 9,
                        MapContextId = 1220, RunState = 1, LastLogin = DateTime.UtcNow, CreatedAt = DateTime.UtcNow, LastPvPClan = DateTime.UtcNow
                    });
                characters.SaveChanges();

                // Character 1: the Wilderness's and the Divide's, both of the revision that went
                // from the Wilderness. Character 2: 1449 as the Wilderness missions have it.
                characters.CharacterMissionEntries.Add(Held(1, Mission, WildernessTargetsKillRules.SupersededRevision, 3, 5, 58));
                characters.CharacterMissionEntries.Add(Held(1, 1582, TargetsOfOpportunitySeed.Revision, 3, 5, 69));
                characters.CharacterMissionEntries.Add(Held(2, Mission, WildernessTargetsKillRules.Revision, 3, 5, 58));
                characters.CharacterMissionOfferEntries.Add(Offer(2, Mission, WildernessTargetsKillRules.SupersededRevision));
                characters.CharacterMissionOfferEntries.Add(Offer(2, 1582, TargetsOfOpportunitySeed.Revision));
                characters.SaveChanges();
                characters.ChangeTracker.Clear();

                characters.Database.Migrate();

                CollectionAssert.AreEquivalent(new[] { (1U, 1582U), (2U, Mission) },
                    characters.CharacterMissionEntries.AsNoTracking().Select(row => new { row.CharacterId, row.MissionId }).ToArray()
                        .Select(row => (row.CharacterId, row.MissionId)).ToArray());
                CollectionAssert.AreEquivalent(new[] { (1U, 1582U), (2U, Mission) },
                    characters.CharacterMissionObjectiveEntries.AsNoTracking().Select(row => new { row.CharacterId, row.MissionId }).Distinct().ToArray()
                        .Select(row => (row.CharacterId, row.MissionId)).ToArray());
                CollectionAssert.AreEquivalent(new[] { (1U, 1582U), (2U, Mission) },
                    characters.CharacterMissionObjectiveCounterEntries.AsNoTracking().Select(row => new { row.CharacterId, row.MissionId }).Distinct().ToArray()
                        .Select(row => (row.CharacterId, row.MissionId)).ToArray());
                Assert.AreEqual(1582U, characters.CharacterMissionOfferEntries.AsNoTracking().Single().MissionId);
                Assert.AreEqual(7U, characters.CharacterMissionObjectiveCounterEntries.AsNoTracking()
                    .Single(row => row.CharacterId == 2 && row.ObjectiveId == 3).CounterValue, "a count under the mission that runs is kept");
                return 0;
            });

            static CharacterMissionEntry Held(uint characterId, uint missionId, string revision, params uint[] objectives)
            {
                var held = new CharacterMissionEntry(characterId, missionId, (uint)MissionState.Active) { ContentRevision = revision };

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

            static CharacterMissionOfferEntry Offer(uint characterId, uint missionId, string revision) => new()
            {
                CharacterId = characterId, MissionId = missionId, OfferId = Guid.NewGuid().ToString("N"), ContentRevision = revision,
                AccountId = 1, SourceKind = MissionOfferSourceKind.ServerEvent, SourceKey = MissionOfferSourceDefinition.MapArrivalKey,
                SourceInstanceId = "1220", CreatedAtUtc = DateTime.UtcNow, ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5)
            };
        }

        #endregion

        #region The mission in the world

        [TestMethod]
        public void TheMissionIsTheWildernessMissionsWithTheKillRulesOnItsKillObjectives()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var mission = harness.Manager.LoadedMissions[Mission];
            var zone = WildernessTargetsKillRules.Zone;

            Assert.IsTrue(mission.IsOperational, mission.OperationalDiagnostic);
            Assert.AreEqual(WildernessTargetsKillRules.Revision, mission.ContentRevision);
            Assert.AreEqual(118U, mission.MissionGiver, "Lt Col Cimoch gives it; it is not offered by radio");
            Assert.AreEqual(MissionChannel.Npc, mission.AcceptanceChannel);
            Assert.HasCount(26, mission.Objectives);
            Assert.AreEqual(10000044U, mission.CategoryId, "Battlefield (Wilderness), on its definition");
            Assert.IsNull(mission.ClientCategoryId);

            foreach (var kill in zone.Kills)
            {
                var objective = mission.Objectives[kill.ObjectiveId];

                Assert.AreEqual(kill.TitleId, objective.TitleId, kill.Title);
                Assert.AreEqual(MissionCreditMode.NearbyParty, objective.CreditPolicy.Mode, kill.Title);
                Assert.AreEqual(TargetsOfOpportunitySeed.SquadRadius, objective.CreditPolicy.Radius, kill.Title);
                var rule = objective.GetExecutableTransitionsOrLegacyDefault().Single().ProgressRule;
                Assert.AreEqual(MissionProgressEventKind.CreatureFlagKilled, rule.Kind, kill.Title);
                CollectionAssert.AreEquivalent(kill.Subjects.ToArray(), rule.Subjects.ToArray(), kill.Title);
                Assert.AreEqual(kill.Count, objective.Counters[0].TargetValue, kill.Title);
                Assert.AreEqual(kill.CounterTextId, objective.ClientCounterTextIds[0], kill.Title);
                CollectionAssert.AreEqual(zone.Maps.ToArray(), ((MapRequirement)mission.ObjectiveRequirements[kill.ObjectiveId]).Maps.ToArray(), kill.Title);
            }

            // The rest of the mission is as it was written: the officers by their rows, alone.
            var officer = mission.Objectives[20];
            Assert.IsNull(officer.TitleId);
            Assert.AreEqual(MissionCreditMode.Personal, officer.CreditPolicy.Mode);
            Assert.AreEqual(MissionProgressEventKind.CreatureKilled, officer.GetExecutableTransitionsOrLegacyDefault().Single().ProgressRule.Kind);
            Assert.IsFalse(mission.ObjectiveRequirements.ContainsKey(20));
            Assert.IsNotNull(mission.Objectives[58].Aggregation);
            Assert.IsNotNull(mission.Objectives[55].HistoryAggregation);
        }

        [TestMethod]
        public void AnyXanxOnTheBattlefieldCountsAndTheFortiethMakesABugZapper()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            harness.SpawnWorld(196);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, harness.Npc(196).EntityId, Mission));
            harness.Drain();

            // Not the one creature row the mission named (87): any creature of the species.
            for (var kills = 1; kills <= 39; kills++)
                Assert.IsTrue(harness.Manager.Credit.RecordKill(harness.Client, Kill(77, 7511, Xanx), Vector3.Zero));

            var packets = harness.Drain();
            var last = packets.OfType<UpdateObjectiveCounterPacket>().Last();
            Assert.AreEqual((Mission, KillXanx, 0u, 39u, 40u), (last.MissionId, last.ObjectiveId, last.CounterId, last.CounterValue, last.TargetValue));
            Assert.IsEmpty(packets.OfType<TitleAddedPacket>().ToArray());

            // Off the battlefield and its instances a Xanx is not the Wilderness's.
            var wilderness = harness.Client.Player.MapChannel;
            Arrive(harness.Client, Elsewhere(1148));
            Assert.IsFalse(harness.Manager.Credit.RecordKill(harness.Client, Kill(77, 7511, Xanx), Vector3.Zero));
            Assert.AreEqual(39U, harness.Client.Player.Missions[Mission].Objectives[KillXanx].Counters[0]);

            // In one of its instances it is.
            Arrive(harness.Client, Elsewhere(1416));
            Assert.IsTrue(harness.Manager.Credit.RecordKill(harness.Client, Kill(77, 7511, Xanx), Vector3.Zero));
            Arrive(harness.Client, wilderness);

            packets = harness.Drain();
            Assert.AreEqual(KillXanx, packets.OfType<ObjectiveCompletedPacket>().Single().ObjectiveId);
            Assert.AreEqual(BugZapper, packets.OfType<TitleAddedPacket>().Single().TitleId);
            Assert.IsTrue(harness.Client.Player.Titles.Contains(BugZapper));

            var log = harness.Client.Player.Missions[Mission];
            Assert.AreEqual(MissionObjectiveState.Completed, log.Objectives[KillXanx].State);
            Assert.AreEqual(MissionObjectiveState.Incomplete, log.Objectives[58].State, "one of the ten");
            Assert.AreEqual(MissionState.Active, log.State);
            Assert.IsFalse(log.Completeable);

            // The title is the character's, in the database.
            using var unit = harness.CreateChar();
            Assert.IsTrue(unit.CharacterTitles.Get(harness.Client.Player.Id).Contains(BugZapper));

            MapChannel Elsewhere(uint mapContextId) => new()
            {
                MapInfo = new MapInfo(mapContextId, "elsewhere_fixture", 1556, 0),
                ClientList = new List<Client>(),
                PlayerLimit = 128
            };
        }

        [TestMethod]
        public void TheCreatureRowAloneNoLongerCountsAndAThraxIsItsOwnObjectives()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            harness.SpawnWorld(196);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, harness.Npc(196).EntityId, Mission));
            harness.Drain();

            // A kill is named by its row, its class and its class's flags (CreatureManager.KillEvents);
            // the row is not what the kill objectives count by.
            Assert.IsFalse(harness.Manager.RecordProgress(harness.Client, MissionProgressEvent.Creature(87)));

            // A Thrax of any row: "Kill 200 Thrax Soldiers!".
            Assert.IsTrue(harness.Manager.Credit.RecordKill(harness.Client, Kill(57, 20757, TargetsOfOpportunitySeed.Flag.Thrax), Vector3.Zero));
            var thrax = harness.Drain().OfType<UpdateObjectiveCounterPacket>().Single();
            Assert.AreEqual((5U, 1U, 200U), (thrax.ObjectiveId, thrax.CounterValue, thrax.TargetValue));

            // An officer is still his own row: Overseer Glognar, objective 20, who is a Thrax too.
            Assert.IsTrue(harness.Manager.Credit.RecordKill(harness.Client, Kill(82, 10504, TargetsOfOpportunitySeed.Flag.Thrax), Vector3.Zero));
            var log = harness.Client.Player.Missions[Mission];
            Assert.AreEqual(MissionObjectiveState.Completed, log.Objectives[20].State);
            Assert.AreEqual(2U, log.Objectives[5].Counters[0], "and one of the two hundred");
            Assert.AreEqual(1U, log.Objectives[6].Counters[0], "one of the six officers");
        }

        #endregion

        /// <summary>The player onto another map, as a map change leaves them.</summary>
        private static void Arrive(Client client, MapChannel destination)
        {
            CellManager.Instance.RemoveFromWorld(client);
            client.Player.MapChannel.ClientList.Remove(client);
            client.Player.MapChannel = destination;
            client.Player.RuntimeMapChannel = destination;
            client.Player.MapContextId = destination.MapInfo.MapContextId;
            destination.ClientList.Add(client);
            CellManager.Instance.AddToWorld(client);
        }

        /// <summary>A kill as CreatureManager.KillEvents names it: the creature row, its class, the flags of its class.</summary>
        private static MissionProgressEvent[] Kill(uint creatureId, uint classId, params uint[] flags) =>
            new[] { MissionProgressEvent.Creature(creatureId), MissionProgressEvent.CreatureClass(classId) }
                .Concat(flags.Select(MissionProgressEvent.CreatureFlag)).ToArray();

        /// <summary>Every row of 1449 in every mission table, and what is enabled: what the two orders have to agree on.</summary>
        private static string[] Dump(SqliteWorldContext world)
        {
            var rows = new List<string>();
            var connection = world.Database.GetDbConnection();

            if (connection.State != System.Data.ConnectionState.Open)
                connection.Open();

            foreach (var table in MissionTables)
            {
                using var command = connection.CreateCommand();
                command.CommandText = $"select * from {table} where mission_id = {Mission}";
                using var reader = command.ExecuteReader();

                while (reader.Read())
                    rows.Add(table + ": " + string.Join(" | ", Enumerable.Range(0, reader.FieldCount)
                        .Select(index => reader.GetName(index) + "=" + (reader.IsDBNull(index) ? "NULL" : Convert.ToString(reader.GetValue(index), System.Globalization.CultureInfo.InvariantCulture)))));
            }

            rows.AddRange(world.MissionContentDefinitionEntries.AsNoTracking().Where(row => row.Enabled)
                .Select(row => new { row.MissionId, row.ContentRevision }).ToArray()
                .Select(row => $"enabled: {row.MissionId}@{row.ContentRevision}"));
            rows.Sort(StringComparer.Ordinal);
            return rows.ToArray();
        }

        private static T WithWorld<T>(Func<SqliteWorldContext, T> body) => WithDatabase(body);

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
