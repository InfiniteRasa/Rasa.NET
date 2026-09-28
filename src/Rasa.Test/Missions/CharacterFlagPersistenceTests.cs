using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Missions.Scenes;
using Rasa.Missions.Runtime;
using Rasa.Structures;
using Rasa.Structures.Char;
using Rasa.Structures.World;

namespace Rasa.Test.Missions
{
    [TestClass]
    [DoNotParallelize]
    public class CharacterFlagPersistenceTests
    {
        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void CompositeScenePublishesTheFinalFlagValuesInTransactionOrder(bool typedFlagLast)
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            AddFlagAction(harness, 901, 7);
            var giver = harness.AddNpc(BootcampRuntimeTestHarness.MajorMcAllisterCreatureId);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, giver.EntityId, 1990));
            using var publication = new MissionScenarioPlan();
            var adapter = new Rasa.Game.Missions.Persistence.SceneCharacterAdapter(
                harness.Manager, new ManifestationManager(harness.Context));
            var run = new SceneRun("flags", "test", "data.sequence", 1, 1, 0, "{}", SceneStatus.Running, harness.Client.Player.Id);
            using (var unit = harness.Context.CreateChar())
                unit.ExecuteTransaction(() =>
                {
                    if (!typedFlagLast)
                        adapter.Apply(harness.Client, run, new SetCharacterFlagIntent("typed", 901, 9), unit, publication);
                    publication.AddProgressPlan(harness.Manager.PlanProgress(harness.Client,
                        new[] { MissionProgressEvent.Area(1990, 430) }, unit));
                    if (typedFlagLast)
                        adapter.Apply(harness.Client, run, new SetCharacterFlagIntent("typed", 901, 9), unit, publication);
                    adapter.Apply(harness.Client, run, new SetCharacterFlagIntent("other", 902, 0), unit, publication);
                });

            publication.ApplyRuntime(harness.Client, new ManifestationManager(harness.Context), harness.Manager);

            Assert.AreEqual(typedFlagLast ? 9U : 7U, harness.Client.Player.PlayerFlags[901]);
            Assert.IsTrue(harness.Client.Player.PlayerFlags.ContainsKey(902));
            Assert.AreEqual(0U, harness.Client.Player.PlayerFlags[902]);
            using var verify = harness.Context.CreateChar();
            CollectionAssert.AreEquivalent(verify.CharacterFlags.Get(harness.Client.Player.Id).ToArray(),
                harness.Client.Player.PlayerFlags.ToArray());
        }

        [TestMethod]
        public void FlagsStoreZeroAndUnsignedValuesAndCascadeOnlyForTheirCharacter()
        {
            using var context = MissionTestContext.WithDefinitions(321);
            context.SeedCharacter(2, 1, 2, (byte)Race.Human);
            using (var unit = context.CreateChar())
            {
                unit.CharacterFlags.Set(1, 91, 0);
                unit.CharacterFlags.Set(1, 92, uint.MaxValue);
                unit.CharacterFlags.Set(1, CharacterFlagIds.BootcampComplete, 1);
                unit.CharacterFlags.Set(2, 91, 7);
            }
            using (var verify = context.CreateChar())
            {
                Assert.AreEqual(0U, verify.CharacterFlags.GetValue(1, 91));
                Assert.IsTrue(verify.CharacterFlags.HasValue(1, 91, 0));
                Assert.IsFalse(verify.CharacterFlags.HasValue(1, 999, 0));
                Assert.IsNull(verify.CharacterFlags.GetValue(1, 999));
                Assert.AreEqual(uint.MaxValue, verify.CharacterFlags.GetValue(1, 92));
                Assert.IsTrue(verify.CharacterFlags.HasValue(1, CharacterFlagIds.BootcampComplete));
                Assert.AreEqual(7U, verify.CharacterFlags.GetValue(2, 91));
                verify.CharacterFlags.Set(1, 91, 5);
                Assert.AreEqual(3, verify.CharacterFlags.Get(1).Count);
                verify.ExecuteTransaction(() => verify.Characters.Delete(1));
            }
            using var remaining = context.CreateChar();
            Assert.AreEqual(0, remaining.CharacterFlags.Get(1).Count);
            Assert.AreEqual(7U, remaining.CharacterFlags.GetValue(2, 91));
        }

        [TestMethod]
        public void FlagWritesAndRemovalRollBackWithTheOwningTransaction()
        {
            using var context = MissionTestContext.WithDefinitions(321);
            using (var unit = context.CreateChar())
                unit.CharacterFlags.Set(1, 91, 7);
            using (var unit = context.CreateChar())
                Assert.ThrowsExactly<DbUpdateException>(() => unit.ExecuteTransaction(() =>
                {
                    unit.CharacterFlags.Remove(1, 91);
                    unit.CharacterFlags.Set(1, 92, 0);
                    throw new DbUpdateException("Injected flag transaction failure.");
                }));
            using var verify = context.CreateChar();
            Assert.AreEqual(7U, verify.CharacterFlags.GetValue(1, 91));
            Assert.IsNull(verify.CharacterFlags.GetValue(1, 92));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => verify.CharacterFlags.Set(1, 0, 1));
        }

        [TestMethod]
        public void FreshCharacterSchemaHasFlagsAndNoQualificationTable()
        {
            using var context = MissionTestContext.WithDefinitions(321);
            using var database = context.Open();
            database.Database.OpenConnection();
            using var command = database.Database.GetDbConnection().CreateCommand();
            command.CommandText = "select count(*) from sqlite_master where type='table' and name='character_flag'";
            Assert.AreEqual(1L, Convert.ToInt64(command.ExecuteScalar()));
            command.CommandText = "select count(*) from sqlite_master where type='table' and name='character_qualification'";
            Assert.AreEqual(0L, Convert.ToInt64(command.ExecuteScalar()));
        }

        [TestMethod]
        public void GenericSceneFlagsPersistSetZeroAndRemovalAndOnlyPublishAfterCommit()
        {
            using var context = MissionTestContext.WithDefinitions(321);
            var bindings = new SceneBindings("test", new Dictionary<string, SceneActorDefinition>(),
                new Dictionary<string, SceneRoute>(), new Dictionary<uint, SceneSequence>
                {
                    [0] = new(characterIntents: new CharacterIntent[] { new SetCharacterFlagIntent("set", 904, 7) }),
                    [1] = new(characterIntents: new CharacterIntent[] { new SetCharacterFlagIntent("zero", 904, 0) }),
                    [2] = new(characterIntents: new CharacterIntent[] { new SetCharacterFlagIntent("remove", 904, null) })
                });
            var id = context.Manager.Scenes.Start(context.Client, "data.sequence", bindings);
            Assert.AreEqual(7U, context.Client.Player.PlayerFlags[904]);
            context.BeforeSave = _ => throw new DbUpdateException("Injected scene flag failure.");
            Assert.IsFalse(context.Manager.Scenes.Submit(id,
                new SceneObservation(SceneEventKind.Signal, 1, SequenceId: 1)));
            Assert.AreEqual(7U, context.Client.Player.PlayerFlags[904]);
            context.BeforeSave = null;
            Assert.IsTrue(context.Manager.Scenes.Submit(id,
                new SceneObservation(SceneEventKind.Signal, 1, SequenceId: 1)));
            Assert.AreEqual(0U, context.Client.Player.PlayerFlags[904]);
            using (var verify = context.CreateChar())
                Assert.AreEqual(0U, verify.CharacterFlags.GetValue(1, 904));
            Assert.IsTrue(context.Manager.Scenes.Submit(id,
                new SceneObservation(SceneEventKind.Signal, 1, SequenceId: 2)));
            Assert.IsFalse(context.Client.Player.PlayerFlags.ContainsKey(904));
            using var removed = context.CreateChar();
            Assert.IsNull(removed.CharacterFlags.GetValue(1, 904));
        }

        [TestMethod]
        public void FailedMissionTransitionRollsBackItsFlagAndRuntimeCache()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            AddFlagAction(harness, 901, 7);
            var giver = harness.AddNpc(BootcampRuntimeTestHarness.MajorMcAllisterCreatureId);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, giver.EntityId, 1990));
            harness.Context.AfterSave = _ => throw new DbUpdateException("Injected mission flag failure.");

            Assert.IsFalse(harness.Manager.RecordProgress(harness.Client, MissionProgressEvent.Area(1990, 430)));

            Assert.IsFalse(harness.Client.Player.PlayerFlags.ContainsKey(901));
            using (var verify = harness.Context.CreateChar())
                Assert.IsNull(verify.CharacterFlags.GetValue(harness.Client.Player.Id, 901));
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[1990].Objectives[1].State);
            harness.Context.AfterSave = null;
            Assert.IsTrue(harness.Manager.RecordProgress(harness.Client, MissionProgressEvent.Area(1990, 430)));
            Assert.AreEqual(7U, harness.Client.Player.PlayerFlags[901]);
        }

        [TestMethod]
        public void NpcObjectiveFlagsCommitAlongsideDialogueProgress()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            AddFlagAction(harness, 902, 0, 1992, 4);
            harness.SeedMission(harness.Client.Player.Id, 1990, (uint)MissionState.Completed, false);
            var giver = harness.AddNpc(BootcampRuntimeTestHarness.MajorMcAllisterCreatureId);
            var delessio = harness.AddNpc(BootcampRuntimeTestHarness.CaptainDelessioCreatureId, 2560);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, giver.EntityId, 1992));

            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, delessio.EntityId, 1992, 4, 1));

            Assert.AreEqual(0U, harness.Client.Player.PlayerFlags[902]);
            using var verify = harness.Context.CreateChar();
            Assert.AreEqual(0U, verify.CharacterFlags.GetValue(harness.Client.Player.Id, 902));
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void FailureTransitionFlagsPersistAndConvergeWithoutClearingOtherFlags(bool abandon)
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            AddFlagAction(harness, 902, 7, 1995, 1, 2);
            ConradCorpseDialogueTests.FindMissingSoldiers(harness);
            harness.UseObjectAndRecover(ConradCorpseDialogueTests.Corpse(harness));
            using (var unit = harness.Context.CreateChar())
                unit.CharacterFlags.Set(harness.Client.Player.Id, 903, 0);
            harness.Client.Player.PlayerFlags[903] = 0;
            if (abandon)
                Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, 1995));
            else
            {
                harness.UtcNow += TimeSpan.FromSeconds(601);
                Assert.IsTrue(harness.Manager.EvaluateDeadlines(harness.Client));
            }

            Assert.AreEqual(7U, harness.Client.Player.PlayerFlags[902]);
            Assert.AreEqual(0U, harness.Client.Player.PlayerFlags[903]);
            using var verify = harness.Context.CreateChar();
            Assert.AreEqual(7U, verify.CharacterFlags.GetValue(harness.Client.Player.Id, 902));
        }

        [TestMethod]
        [DataRow(0U)]
        [DataRow(CharacterFlagIds.BootcampComplete)]
        public void MissionFlagActionsCannotWriteInvalidOrReservedServerIds(uint id)
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            harness.WorldContext.MissionActionEntries.Add(new MissionActionEntry
            {
                MissionId = 1990, ContentRevision = "deployment_11", ObjectiveId = 1, TransitionId = 1,
                ActionId = 99, Sequence = 99, Kind = MissionActionKind.SetPlayerFlag,
                PlayerFlagId = id, PlayerFlagValue = 1
            });
            harness.WorldContext.SaveChanges();

            var report = harness.Manager.LoadMissions();

            Assert.IsTrue(report.BlocksReadiness);
            Assert.IsTrue(report.Diagnostics.Any(diagnostic => diagnostic.Code == "reserved-player-flag-id"));
        }

        [TestMethod]
        public void AFlagOnlyInTheRuntimeCacheCannotAuthorizeDurableMissionAcceptance()
        {
            using var harness = BootcampRuntimeTestHarness.Create(configureScenes: scenes =>
                scenes[1990].Requirement = new FlagRequirement(903, 1));
            var giver = harness.AddNpc(BootcampRuntimeTestHarness.MajorMcAllisterCreatureId);
            harness.Client.Player.PlayerFlags[903] = 1;

            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, giver.EntityId, 1990),
                "Acceptance must recheck persisted flags, not trust an unsaved runtime cache.");
        }

        [TestMethod]
        public void ACommittedMissionFlagSurvivesAFreshCharacterAndManager()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            AddFlagAction(harness, 901, 7);
            var giver = harness.AddNpc(BootcampRuntimeTestHarness.MajorMcAllisterCreatureId);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, giver.EntityId, 1990));
            Assert.IsTrue(harness.Manager.RecordProgress(harness.Client, MissionProgressEvent.Area(1990, 430)));
            Assert.AreEqual(7U, harness.Client.Player.PlayerFlags[901]);

            harness.ReconnectFresh();

            Assert.IsTrue(harness.Client.Player.PlayerFlags.TryGetValue(901, out var value),
                "Mission flags must be restored from character storage on reconnect.");
            Assert.AreEqual(7U, value);
        }

        private static void AddFlagAction(BootcampRuntimeTestHarness.Harness harness, uint id, uint value,
            uint mission = 1990, uint objective = 1, uint transition = 1)
        {
            harness.WorldContext.MissionActionEntries.Add(new MissionActionEntry
            {
                MissionId = mission, ContentRevision = "deployment_11", ObjectiveId = objective, TransitionId = transition,
                ActionId = 99, Sequence = 99, Kind = MissionActionKind.SetPlayerFlag,
                PlayerFlagId = id, PlayerFlagValue = value, Comment = "Persistent flag regression"
            });
            harness.WorldContext.SaveChanges();
            Assert.IsFalse(harness.Manager.LoadMissions().BlocksReadiness);
        }
    }
}
