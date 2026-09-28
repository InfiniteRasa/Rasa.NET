extern alias RasaGame;

using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Structures;
    using Rasa.Structures.Char;

    [TestClass]
    [DoNotParallelize]
    public class BootcampBombRetryTests
    {
        private const uint MissingScoutAreaId = 435;
        private static readonly TimeSpan BombDeadline = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan FuseDelay = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan ArrivalDelay = TimeSpan.FromSeconds(2);

        [TestMethod]
        public void RetryAcceptsAfterTimeoutAndRebuildsWithoutResettingTheDeadline()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            StartTimedFinale(harness);
            harness.UseObjectAndRecover(FindScenarioObject(harness, "bootcamp-conrad-corpse"));
            harness.UtcNow += BombDeadline + TimeSpan.FromSeconds(1);
            Assert.IsTrue(harness.Manager.EvaluateDeadlines(harness.Client));

            var youngblood = BootcampRuntimeTestHarness.FindCreature(
                harness.BootcampMap,
                BootcampRuntimeTestHarness.CaptainYoungbloodCreatureId);
            Assert.IsNotNull(youngblood);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(
                harness.Client,
                youngblood.EntityId,
                2005));

            var beforeReconnect = ReadDeadline(harness, 2005);
            harness.ReconnectFresh();

            Assert.IsNotNull(FindScenarioObject(harness, "bootcamp-conrad-corpse"));
            Assert.IsNotNull(FindScenarioObject(harness, "bootcamp-dropship-debris"));
            var afterReconnect = ReadDeadline(harness, 2005);
            Assert.AreEqual(beforeReconnect.DueAtUtc, afterReconnect.DueAtUtc);
            Assert.AreEqual(CharacterMissionDeadlineState.Active, afterReconnect.State);
        }

        [TestMethod]
        public void RetryCannotBeAcceptedAfterBootcampCompletionWasAlreadyGranted()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var youngblood = harness.AddNpc(
                BootcampRuntimeTestHarness.CaptainYoungbloodCreatureId,
                2561);
            harness.SeedMission(1, 1995, (uint)MissionState.Failed, completeable: false);

            using (var unit = harness.Context.CreateChar())
            {
                unit.ExecuteTransaction(() =>
                {
                    if (!unit.CharacterFlags.HasValue(
                            harness.Client.Player.Id,
                            CharacterFlagIds.BootcampComplete))
                    {
                        unit.CharacterFlags.Add(
                            new CharacterFlagEntry(
                                harness.Client.Player.Id,
                                CharacterFlagIds.BootcampComplete));
                    }

                    unit.GameAccounts.UpdateCanSkipBootcamp(harness.Client.AccountEntry.Id, true);
                });
            }

            harness.Client.ReloadGameAccountEntry();

            Assert.IsFalse(harness.Manager.AcceptOfferedMission(
                harness.Client,
                youngblood.EntityId,
                2005));
        }

        [TestMethod]
        public void RetryUseAndDetonationDoNotDuplicateTheExitHandoff()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            StartTimedFinale(harness);
            harness.UseObjectAndRecover(FindScenarioObject(harness, "bootcamp-conrad-corpse"));
            harness.UtcNow += BombDeadline + TimeSpan.FromSeconds(1);
            Assert.IsTrue(harness.Manager.EvaluateDeadlines(harness.Client));

            var youngblood = BootcampRuntimeTestHarness.FindCreature(
                harness.BootcampMap,
                BootcampRuntimeTestHarness.CaptainYoungbloodCreatureId);
            Assert.IsNotNull(youngblood);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(
                harness.Client,
                youngblood.EntityId,
                2005));

            var dropship = FindScenarioObject(harness, "bootcamp-dropship-debris");
            harness.UseObjectAndRecover(dropship);
            harness.UseObjectAndRecover(dropship);

            harness.UtcNow += FuseDelay;
            Assert.IsTrue(harness.Manager.TickScenarios(harness.Client));
            Assert.AreEqual(
                MissionObjectiveState.Completed,
                harness.Client.Player.Missions[2005].Objectives[1].State);
            Assert.IsNull(BootcampRuntimeTestHarness.FindNpcByPackage(harness.BootcampMap, 2564));

            harness.UtcNow += ArrivalDelay;
            Assert.IsTrue(harness.Manager.TickScenarios(harness.Client));
            Assert.IsFalse(harness.Manager.TickScenarios(harness.Client));

            var vanValkenbergs = harness.BootcampMap.MapCellInfo.Cells.Values
                .SelectMany(cell => cell.CreatureList)
                .Where(creature => creature.Npc?.NpcPackageId == 2564)
                .ToArray();
            Assert.AreEqual(1, vanValkenbergs.Length);
            Assert.AreEqual(
                MissionObjectiveState.Inactive,
                harness.Client.Player.Missions[2005].Objectives[4].State);
            BootcampExtractionAssaultTests.DefeatAll(harness);
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[2005].Objectives[4].State);
        }

        [TestMethod]
        public void RetryReconnectDuringFusePreservesTheSatisfiedDeadlineAndPendingDetonation()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            StartTimedFinale(harness);
            harness.UseObjectAndRecover(FindScenarioObject(harness, "bootcamp-conrad-corpse"));
            harness.UtcNow += BombDeadline + TimeSpan.FromSeconds(1);
            Assert.IsTrue(harness.Manager.EvaluateDeadlines(harness.Client));

            var youngblood = BootcampRuntimeTestHarness.FindCreature(
                harness.BootcampMap,
                BootcampRuntimeTestHarness.CaptainYoungbloodCreatureId);
            Assert.IsNotNull(youngblood);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(
                harness.Client,
                youngblood.EntityId,
                2005));

            var deadline = ReadDeadline(harness, 2005);
            harness.UtcNow = deadline.DueAtUtc - FuseDelay;

            var dropship = FindScenarioObject(harness, "bootcamp-dropship-debris");
            harness.UseObjectAndRecover(dropship);
            Assert.AreEqual(CharacterMissionDeadlineState.Satisfied, ReadDeadline(harness, 2005).State);

            harness.UtcNow += FuseDelay - TimeSpan.FromMilliseconds(1);
            harness.ReconnectFresh();

            Assert.AreEqual(CharacterMissionDeadlineState.Satisfied, ReadDeadline(harness, 2005).State);
            Assert.AreEqual(
                MissionObjectiveState.Incomplete,
                harness.Client.Player.Missions[2005].Objectives[1].State);
            Assert.IsFalse(harness.Manager.EvaluateDeadlines(harness.Client));
            Assert.IsFalse(harness.Manager.TickScenarios(harness.Client));

            harness.UtcNow += TimeSpan.FromMilliseconds(2);
            Assert.IsTrue(harness.Manager.TickScenarios(harness.Client));
            Assert.AreEqual(
                MissionObjectiveState.Completed,
                harness.Client.Player.Missions[2005].Objectives[1].State);

            harness.UtcNow += ArrivalDelay;
            Assert.IsTrue(harness.Manager.TickScenarios(harness.Client));
            Assert.AreEqual(
                MissionObjectiveState.Inactive,
                harness.Client.Player.Missions[2005].Objectives[4].State);
            Assert.IsNotNull(BootcampRuntimeTestHarness.FindNpcByPackage(harness.BootcampMap, 2564));
        }

        [TestMethod]
        public void AbandoningRetryDuringActiveDeadlineFailsAndResetsMission2005OnceWithoutReofferingRetry()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var youngblood = AcceptRetryMission(harness);

            var dropship = FindScenarioObject(harness, "bootcamp-dropship-debris");
            Assert.IsTrue(dropship.IsEnabled);

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, 2005));

            AssertFailedRetryReset(
                harness,
                youngblood,
                dropship,
                expectedDeadlineState: CharacterMissionDeadlineState.Cancelled);

            Assert.IsFalse(harness.Manager.AcceptOfferedMission(
                harness.Client,
                youngblood.EntityId,
                2005));
            Assert.IsFalse(harness.Manager.TryAbandon(harness.Client, 2005));
            AssertResetScenarioRecordedOnce(harness, 2005, 4, stepCount: 4);
        }

        [TestMethod]
        public void AbandoningRetryDuringTheFuseFailsAndResetsMission2005OnceWithoutReofferingRetry()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var youngblood = AcceptRetryMission(harness);

            var dropship = FindScenarioObject(harness, "bootcamp-dropship-debris");
            harness.UseObjectAndRecover(dropship);
            Assert.IsFalse(dropship.IsEnabled);

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, 2005));

            AssertFailedRetryReset(
                harness,
                youngblood,
                dropship,
                expectedDeadlineState: CharacterMissionDeadlineState.Cancelled);

            Assert.IsFalse(harness.Manager.AcceptOfferedMission(
                harness.Client,
                youngblood.EntityId,
                2005));
            Assert.IsFalse(harness.Manager.TryAbandon(harness.Client, 2005));
            AssertResetScenarioRecordedOnce(harness, 2005, 4, stepCount: 4);
        }

        private static void StartTimedFinale(BootcampRuntimeTestHarness.Harness harness)
        {
            var youngblood = harness.AddNpc(
                BootcampRuntimeTestHarness.CaptainYoungbloodCreatureId,
                2561);

            harness.SeedMission(1, 1994, (uint)MissionState.Completed, completeable: true);

            Assert.IsTrue(harness.Manager.AcceptOfferedMission(
                harness.Client,
                youngblood.EntityId,
                1995));
            Assert.IsTrue(harness.Manager.RecordProgress(
                harness.Client,
                MissionProgressEvent.Area(1995, MissingScoutAreaId)));
            Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[1995].Objectives[2].State);
        }

        private static DynamicObject FindScenarioObject(
            BootcampRuntimeTestHarness.Harness harness,
            string key) =>
            BootcampRuntimeTestHarness.FindScenarioObject(harness.BootcampMap, key)
            ?? throw new AssertFailedException($"Missing scenario object {key}.");

        private static CharacterMissionDeadlineEntry ReadDeadline(
            BootcampRuntimeTestHarness.Harness harness,
            uint missionId)
        {
            using var unit = harness.Context.CreateChar();
            return unit.CharacterMissionDeadlines.Get(harness.Client.Player.Id, missionId)
                   ?? throw new AssertFailedException($"Missing mission deadline for {missionId}.");
        }

        private static Creature AcceptRetryMission(BootcampRuntimeTestHarness.Harness harness)
        {
            StartTimedFinale(harness);
            harness.UseObjectAndRecover(FindScenarioObject(harness, "bootcamp-conrad-corpse"));
            harness.UtcNow += BombDeadline + TimeSpan.FromSeconds(1);
            Assert.IsTrue(harness.Manager.EvaluateDeadlines(harness.Client));

            var youngblood = BootcampRuntimeTestHarness.FindCreature(
                harness.BootcampMap,
                BootcampRuntimeTestHarness.CaptainYoungbloodCreatureId)
                ?? throw new AssertFailedException("Missing Captain Youngblood.");
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(
                harness.Client,
                youngblood.EntityId,
                2005));
            return youngblood;
        }

        private static void AssertFailedRetryReset(
            BootcampRuntimeTestHarness.Harness harness,
            Creature youngblood,
            DynamicObject dropship,
            CharacterMissionDeadlineState expectedDeadlineState)
        {
            Assert.AreEqual(MissionState.Failed, harness.Client.Player.Missions[2005].State);
            Assert.AreEqual(
                MissionObjectiveState.Failed,
                harness.Client.Player.Missions[2005].Objectives[1].State);
            Assert.AreEqual(expectedDeadlineState, ReadDeadline(harness, 2005).State);
            Assert.IsTrue(dropship.IsEnabled);
            Assert.IsNotNull(FindScenarioObject(harness, "bootcamp-conrad-corpse"));
            Assert.IsNotNull(FindScenarioObject(harness, "bootcamp-dropship-debris"));
            Assert.IsNull(BootcampRuntimeTestHarness.FindNpcByPackage(harness.BootcampMap, 39));
            Assert.IsNull(BootcampRuntimeTestHarness.FindNpcByPackage(harness.BootcampMap, 50));
            Assert.IsNull(BootcampRuntimeTestHarness.FindNpcByPackage(
                harness.BootcampMap,
                BootcampRuntimeTestHarness.CorporalVanValkenbergPackageId));
            AssertResetScenarioRecordedOnce(harness, 2005, 4, stepCount: 4);

            var classification = harness.Manager.ClassifyNpcConversation(harness.Client.Player, youngblood);
            if (!classification.TryGetStatus(out _, out var missionIds))
                return;
            CollectionAssert.DoesNotContain(missionIds, 2005U);
        }

        private static void AssertResetScenarioRecordedOnce(
            BootcampRuntimeTestHarness.Harness harness,
            uint missionId,
            uint resetScenarioId,
            uint stepCount)
        {
            using var unit = harness.Context.CreateChar();
            var scene = unit.CharacterMissions.Runtime.Scenes(harness.Client.Player.Id, missionId).Single();
            Assert.AreEqual("Ended", scene.Status);
            using var checkpoint = System.Text.Json.JsonDocument.Parse(scene.Checkpoint);
            Assert.AreEqual(resetScenarioId, checkpoint.RootElement.GetProperty("sequence").GetUInt32());
            Assert.IsFalse(unit.CharacterMissions.Runtime.Timers(scene.RunId).Any(timer => timer.Disposition == "Pending"));
            var effects = unit.CharacterMissions.Runtime.Effects(scene.RunId);
            Assert.AreEqual(effects.Count, effects.Select(effect => (effect.Generation, effect.OperationKey)).Distinct().Count());
        }

        private static string[] ReadScenarioKeys(
            BootcampRuntimeTestHarness.Harness harness,
            uint missionId)
        {
            using var unit = harness.Context.CreateChar();
            return unit.CharacterMissionScenario.Get(harness.Client.Player.Id, missionId)
                .Select(entry => entry.StepKey)
                .ToArray();
        }
    }
}
