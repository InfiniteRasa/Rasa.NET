using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Missions.Runtime;
using Rasa.Structures;
using Rasa.Structures.World;
using Rasa.Structures.Char;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Game.Missions.Persistence;
using Rasa.Test.Missions.Encounters;
using Rasa.Packets.Mission.Server;
using System.Numerics;
using System.Threading.Tasks;
using Rasa.Game.Missions.World;
using Rasa.Test.Missions.Scenes;

namespace Rasa.Test.Missions
{
    [TestClass]
    [DoNotParallelize]
    public class MissionRepeatabilityTests
    {
        [TestMethod]
        [DataRow("2026-09-25T05:59:59Z", "2026-09-24T06:00:00Z")]
        [DataRow("2026-09-25T06:00:00Z", "2026-09-25T06:00:00Z")]
        [DataRow("2026-09-25T06:00:01Z", "2026-09-25T06:00:00Z")]
        [DataRow("2026-09-26T00:00:00Z", "2026-09-25T06:00:00Z")]
        public void DailyWindowUsesAuthoredUtcReset(string now, string expected)
        {
            var policy = new MissionRepeatPolicy(MissionRepeatKind.Daily, ResetSecondUtc: 21600);
            Assert.AreEqual(Utc(expected), policy.WindowStartUtc(Utc(now)));
        }

        [TestMethod]
        [DataRow(MissionRepeatKind.Once, 1, -1)]
        [DataRow(MissionRepeatKind.Immediate, -1, 0)]
        [DataRow(MissionRepeatKind.Cooldown, -1, -1)]
        [DataRow(MissionRepeatKind.Cooldown, 0, -1)]
        [DataRow(MissionRepeatKind.Cooldown, 5, 0)]
        [DataRow(MissionRepeatKind.Daily, -1, -1)]
        [DataRow(MissionRepeatKind.Daily, -1, 86400)]
        [DataRow(MissionRepeatKind.Daily, 5, 0)]
        [DataRow((MissionRepeatKind)99, -1, -1)]
        public void InvalidPolicyCannotBecomeOperational(MissionRepeatKind kind, int cooldown, int reset)
        {
            var policy = new MissionRepeatPolicy(kind, cooldown < 0 ? null : (uint)cooldown,
                reset < 0 ? null : (uint)reset);
            Assert.ThrowsExactly<MissionRuleException>(policy.Validate);
            Assert.IsFalse(Definition(policy).IsOperational);
        }

        [TestMethod]
        [DataRow(-1, false)]
        [DataRow(0, true)]
        [DataRow(1, true)]
        public void CooldownStartsAtRewardCommit(int offset, bool allowed)
        {
            var reward = Utc("2026-09-25T12:00:00Z");
            var policy = new MissionRepeatPolicy(MissionRepeatKind.Cooldown, CooldownSeconds: 60);
            Assert.AreEqual(allowed, policy.Allows(reward.AddSeconds(60 + offset), true, reward));
        }

        [TestMethod]
        public void MissingPolicyIsOnceAndAllCopiesPreservePolicy()
        {
            Assert.AreEqual(MissionRepeatKind.Once, Definition().RepeatPolicy.Kind);
            var policy = new MissionRepeatPolicy(MissionRepeatKind.Immediate);
            var mission = Definition(policy);
            Assert.AreEqual(policy, mission.WithWorldMetadata(mission).RepeatPolicy);
            Assert.AreEqual(policy, mission.WithDialogue(mission.Dialogue).RepeatPolicy);
            Assert.AreEqual(policy, mission.WithItems(mission.Items.Values, mission.AcceptanceItems).RepeatPolicy);
            Assert.AreEqual(policy, mission.WithPolicies(new Dictionary<uint, MissionCreditPolicy>(), null).RepeatPolicy);
            Assert.AreEqual(policy, mission.DisableOperational("test").RepeatPolicy);
        }

        [TestMethod]
        public void ImmediateRepeatAtomicallyReplacesTerminalJournalAndRewardsEachAttemptOnce()
        {
            using var context = RepeatContext(new(MissionRepeatKind.Immediate));
            var giver = context.AddNpc(77);
            var receiver = context.AddNpc(88);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            var first = context.ReadMission(321);
            Assert.IsFalse(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            Assert.IsTrue(context.Manager.CompleteOfferedMission(context.Client, receiver.EntityId, 321, null, null));
            var firstPaid = context.ReadRewardTotals();
            Assert.IsFalse(context.Manager.CompleteOfferedMission(context.Client, receiver.EntityId, 321, null, null));
            Assert.AreEqual(firstPaid, context.ReadRewardTotals());

            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321),
                "An eligible repeat replaces the terminal journal without a manual clear.");
            var second = context.ReadMission(321);
            Assert.AreNotEqual(first.AssignmentId, second.AssignmentId);
            Assert.IsTrue(context.Manager.CompleteOfferedMission(context.Client, receiver.EntityId, 321, null, null));
            Assert.AreEqual(firstPaid.Credits + 7, context.ReadRewardTotals().Credits);
            using var unit = context.CreateChar();
            var history = unit.CharacterMissions.Runtime.History(1);
            Assert.AreEqual(2, history.Count);
            CollectionAssert.AreEquivalent(new[] { first.AssignmentId, second.AssignmentId },
                history.Select(row => row.AssignmentId).ToArray());
            Assert.IsTrue(history.All(row => row.Rewarded));
        }

        [TestMethod]
        public void TerminalRepeatPublishesClearThenGainOnlyAfterCommit()
        {
            using var context = RepeatContext(new(MissionRepeatKind.Immediate));
            var giver = context.AddNpc(77);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            Assert.IsTrue(context.Manager.CompleteOfferedMission(context.Client, context.AddNpc(88).EntityId, 321, null, null));
            var old = context.Client.Player.Missions[321];
            context.Drain();
            context.BeforeSave = _ =>
            {
                Assert.AreSame(old, context.Client.Player.Missions[321]);
                Assert.AreEqual(0, context.Drain().Count);
            };

            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));

            CollectionAssert.AreEqual(new[] { typeof(MissionClearedPacket), typeof(MissionGainedPacket) },
                context.Drain().Where(packet => packet is MissionClearedPacket or MissionGainedPacket)
                    .Select(packet => packet.GetType()).ToArray());
        }

        [TestMethod]
        public void AFailedRepeatKeepsLifetimeSuccessAndLatestFailureSeparateAcrossRelog()
        {
            using var context = RepeatContext(new(MissionRepeatKind.Immediate),
                dependent: Definition(requirement: new MissionStateRequirement(321), missionId: 322));
            var giver = context.AddNpc(77);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            Assert.IsTrue(context.Manager.CompleteOfferedMission(context.Client, context.AddNpc(88).EntityId, 321, null, null));
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            Assert.IsTrue(context.Manager.TryFailMission(context.Client, 321));
            Assert.IsTrue(context.Manager.TryClear(context.Client, 321));
            using (var unit = context.CreateChar())
            {
                context.Manager.HydrateAndClearInvalid(context.Client.Player, unit);
                Assert.IsTrue(unit.CharacterMissions.Runtime.EverSucceeded(1, 321));
                Assert.AreEqual((uint)MissionState.Failed, unit.CharacterMissions.Runtime.LatestTerminal(1, 321).Outcome);
            }
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 322),
                "A lifetime prerequisite cannot be masked by a later failure.");
        }

        [TestMethod]
        [DataRow("progress")]
        [DataRow("fail")]
        [DataRow("abandon")]
        public void StaleRuntimeCannotMutateReplacementWithIdenticalMissionAndObjectiveIds(string command)
        {
            using var context = MissionTestContext.WithProgressMission(
                MissionProgressRule.IncrementCounterOnExactSubject(MissionProgressEventKind.CreatureKilled, 55, 0, 0, 5),
                counters: new Dictionary<uint, MissionObjectiveCounterDefinition> { [0] = new(0, 0, 5) });
            var giver = context.AddNpc(77);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            var stale = context.CreateCompetingClient();
            Assert.IsTrue(context.Manager.TryAbandon(context.Client, 321));
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            var replacement = context.ReadMission(321);
            Assert.IsFalse(command switch
            {
                "progress" => context.Manager.RecordProgress(stale, MissionProgressEvent.Creature(55)),
                "fail" => context.Manager.TryFailMission(stale, 321),
                _ => context.Manager.TryAbandon(stale, 321)
            });
            Assert.AreEqual(replacement.AssignmentId, context.ReadMission(321).AssignmentId);
            Assert.AreEqual((uint)MissionState.Active, context.ReadMission(321).MissionState);
            Assert.AreEqual(0U, context.ReadProgress(321).Missions[321].Objectives[1].Counters[0]);
        }

        [TestMethod]
        public void PendingSuccessMustBeSettledNotClearedOrRepeated()
        {
            using var context = RepeatContext(new(MissionRepeatKind.Immediate));
            context.SeedMission(1, 321, (uint)MissionState.Success, false);
            context.ReloadPlayerMissions();
            Assert.IsFalse(context.Manager.TryClear(context.Client, 321));
            Assert.IsFalse(context.Manager.AcceptOfferedMission(context.Client, context.AddNpc(77).EntityId, 321));
            Assert.IsTrue(context.Manager.CompleteOfferedMission(context.Client, context.AddNpc(88).EntityId, 321, null, null));
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, context.AddNpc(77).EntityId, 321));
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void FailureAndAbandonmentAreRecordedWithoutConsumingDailyEntitlement(bool abandon)
        {
            using var context = RepeatContext(new(MissionRepeatKind.Daily, ResetSecondUtc: 0));
            var giver = context.AddNpc(77);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            var first = context.ReadMission(321);
            Assert.IsTrue(abandon ? context.Manager.TryAbandon(context.Client, 321) :
                context.Manager.TryFailMission(context.Client, 321));
            using (var unit = context.CreateChar())
            {
                var history = unit.CharacterMissions.Runtime.History(1).Single();
                Assert.AreEqual(first.AssignmentId, history.AssignmentId);
                Assert.IsFalse(history.Rewarded);
                Assert.IsNull(history.RewardedAtUtc);
                Assert.IsNull(history.RewardWindowStartUtc);
            }
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            Assert.AreNotEqual(first.AssignmentId, context.ReadMission(321).AssignmentId);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void CommittedAbandonmentImmediatelyAdmitsItsBranchWithoutErasingLifetimeRewardHistory(bool failFirst)
        {
            var now = Utc("2026-09-25T12:00:00Z");
            using var context = RepeatContext(new(MissionRepeatKind.Immediate), () => now,
                Definition(requirement: new MissionStateRequirement(321, MissionState.NotAssigned), missionId: 322));
            var giver = context.AddNpc(77);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            Assert.IsTrue(context.Manager.CompleteOfferedMission(context.Client, context.AddNpc(88).EntityId, 321, null, null));
            var rewardTime = context.Client.Player.MissionRewardTimes[321];
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            Assert.IsTrue(context.Manager.TryFailMission(context.Client, 321));
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            var abandoning = context.ReadMission(321);
            now = now.AddMinutes(1);
            context.BeforeSave = database =>
            {
                Assert.AreEqual(MissionState.Failed, context.Client.Player.MissionHistory[321],
                    "History must not publish before the abandonment commits.");
                Assert.AreEqual(rewardTime, context.Client.Player.MissionRewardTimes[321]);
                if (failFirst && database.Set<CharacterMissionHistoryEntry>().Local
                    .Any(row => row.AssignmentId == abandoning.AssignmentId && row.Outcome == (uint)MissionState.NotAssigned))
                    throw new DbUpdateException("Injected abandonment history failure.");
            };

            Assert.AreEqual(!failFirst, context.Manager.TryAbandon(context.Client, 321));

            context.BeforeSave = null;
            if (failFirst)
            {
                Assert.AreEqual(MissionState.Active, context.Client.Player.Missions[321].State);
                Assert.AreEqual(MissionState.Failed, context.Client.Player.MissionHistory[321]);
                using (var unit = context.CreateChar())
                    Assert.AreEqual((uint)MissionState.Failed, unit.CharacterMissions.Runtime.LatestTerminal(1, 321).Outcome);
                Assert.IsTrue(context.Manager.TryAbandon(context.Client, 321));
            }
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 322),
                "The committed abandonment branch must be available without hydration or relog.");
            Assert.AreEqual(MissionState.NotAssigned, context.Client.Player.MissionHistory[321]);
            Assert.IsTrue(context.Client.Player.MissionSuccessHistory.Contains(321));
            Assert.AreEqual(rewardTime, context.Client.Player.MissionRewardTimes[321]);
            using var verify = context.CreateChar();
            Assert.AreEqual((uint)MissionState.NotAssigned, verify.CharacterMissions.Runtime.LatestTerminal(1, 321).Outcome);
            Assert.IsTrue(verify.CharacterMissions.Runtime.EverSucceeded(1, 321));
            Assert.AreEqual(rewardTime.Ticks, verify.CharacterMissions.Runtime.LastRewardedAtUtc(1, 321).Value.Ticks);
        }

        [TestMethod]
        public void WorldPolicyIsOptionalRevisionScopedAndSurvivesSceneMetadataCopies()
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            Assert.IsFalse(harness.Manager.LoadMissions().BlocksReadiness);
            Assert.IsTrue(harness.Manager.LoadedMissions.Values.Where(mission => mission.IsOperational)
                .All(mission => mission.RepeatPolicy.Kind == MissionRepeatKind.Once && mission.Shareable == false));
            var mission = harness.Manager.LoadedMissions[1990];
            harness.WorldContext.Add(new MissionRepeatPolicyEntry
            {
                MissionId = mission.MissionId, ContentRevision = mission.ContentRevision,
                Kind = MissionRepeatKind.Daily, ResetSecondUtc = 21600
            });
            harness.WorldContext.SaveChanges();

            Assert.IsFalse(harness.Manager.LoadMissions().BlocksReadiness);

            var loaded = harness.Manager.LoadedMissions[1990];
            Assert.AreEqual(new MissionRepeatPolicy(MissionRepeatKind.Daily, ResetSecondUtc: 21600), loaded.RepeatPolicy);
            Assert.AreEqual(mission.Dialogue.Count, loaded.Dialogue.Count);
            Assert.AreEqual(mission.Items.Count, loaded.Items.Count);
            Assert.AreEqual(mission.AcceptanceItems.Count, loaded.AcceptanceItems.Count);
        }

        [TestMethod]
        public void InvalidStoredRepeatPolicyBlocksReadinessEvenIfStorageChecksWereBypassed()
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            harness.WorldContext.Database.OpenConnection();
            harness.WorldContext.Database.ExecuteSqlRaw("PRAGMA ignore_check_constraints = ON");
            harness.WorldContext.Add(new MissionRepeatPolicyEntry
            {
                MissionId = 1990, ContentRevision = harness.Manager.LoadedMissions[1990].ContentRevision,
                Kind = MissionRepeatKind.Daily, ResetSecondUtc = 86400
            });
            harness.WorldContext.SaveChanges();
            harness.WorldContext.Database.ExecuteSqlRaw("PRAGMA ignore_check_constraints = OFF");
            var report = harness.Manager.LoadMissions();
            Assert.IsTrue(report.BlocksReadiness);
            Assert.IsFalse(harness.Manager.LoadedMissions[1990].IsOperational);
        }

        [TestMethod]
        [DataRow(MissionRepeatKind.Cooldown)]
        [DataRow(MissionRepeatKind.Daily)]
        public void RewardTimestampAndWindowAreFinalizedAfterInventoryPersistence(MissionRepeatKind kind)
        {
            var now = Utc("2026-09-25T05:59:59Z");
            var policy = kind == MissionRepeatKind.Daily
                ? new MissionRepeatPolicy(kind, ResetSecondUtc: 21600)
                : new MissionRepeatPolicy(kind, CooldownSeconds: 60);
            using var context = RepeatContext(policy, () => now);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, context.AddNpc(77).EntityId, 321));
            var crossed = false;
            context.AfterSave = database =>
            {
                if (!crossed && database.Set<CharacterMissionHistoryEntry>().Local.Any(row => row.Rewarded))
                {
                    crossed = true;
                    now = Utc("2026-09-25T06:00:00Z");
                }
            };
            Assert.IsTrue(context.Manager.CompleteOfferedMission(context.Client, context.AddNpc(88).EntityId, 321, null, null));
            Assert.IsTrue(crossed);
            using var unit = context.CreateChar();
            var history = unit.CharacterMissions.Runtime.History(1).Single();
            Assert.AreEqual(now.Ticks, history.RewardedAtUtc.Value.Ticks);
            if (kind == MissionRepeatKind.Daily)
                Assert.AreEqual(now.Ticks, history.RewardWindowStartUtc.Value.Ticks);
        }

        [TestMethod]
        public void DailyResetDuringLateEnlistedInventoryValidationRollsBackAndRetriesInTheNewWindow()
        {
            var now = Utc("2026-09-25T05:59:59Z");
            var dependent = new Mission(322, "Observe Daily completion", 322, 77, 88, 1, 1, 2, false, false,
                new[]
                {
                    new MissionObjectiveDefinition(1, 1001, 1002, Array.Empty<uint?>(), 0,
                        MissionObjectiveState.Incomplete, true, null, null,
                        Array.Empty<MissionObjectiveConversation>(), Array.Empty<uint>(), Array.Empty<uint>(),
                        Array.Empty<MissionIndicator>(),
                        MissionProgressRule.CompleteOnExactSubject(MissionProgressEventKind.MissionCompleted, 321))
                }, true, items: new[]
                {
                    new MissionItemBinding("key", 28, MissionItemScope.AssignmentIssued, 1,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove,
                        MissionItemCleanupDisposition.Remove)
                });
            using var context = RepeatContext(new(MissionRepeatKind.Daily, ResetSecondUtc: 21600),
                () => now, dependent);
            var giver = context.AddNpc(77);
            var receiver = context.AddNpc(88);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 322));
            var assignment = context.ReadMission(321);
            var finalized = false;
            var crossed = false;
            context.AfterSave = database => finalized |= database.Set<CharacterMissionHistoryEntry>().Local
                .Any(row => row.MissionId == 321 && row.RewardWindowStartUtc.HasValue);
            context.AfterCommand = command =>
            {
                if (!crossed && finalized && command.StartsWith("SELECT", StringComparison.Ordinal) &&
                    command.Contains("FROM \"character_mission_item\"", StringComparison.Ordinal))
                {
                    crossed = true;
                    now = Utc("2026-09-25T06:00:00Z");
                }
            };

            Assert.IsFalse(context.Manager.CompleteOfferedMission(context.Client, receiver.EntityId, 321, null, null));

            Assert.IsTrue(crossed, "Reset must occur in the late inventory participant's final database reads.");
            Assert.AreEqual(100, context.ReadRewardTotals().Credits);
            Assert.AreEqual((uint)MissionState.Active, context.ReadMission(321).MissionState);
            Assert.AreEqual((byte)MissionObjectiveState.Incomplete, context.ReadProgress(322).Missions[322].Objectives[1].State);
            Assert.AreEqual(MissionObjectiveState.Incomplete, context.Client.Player.Missions[322].Objectives[1].State);
            using (var unit = context.CreateChar())
            {
                Assert.AreEqual(0, unit.CharacterMissions.Runtime.History(1).Count);
                Assert.IsFalse(unit.CharacterMissions.Runtime.HasReceipt(assignment.AssignmentId,
                    assignment.Generation, "mission-reward"));
            }

            context.AfterSave = null;
            context.AfterCommand = null;
            Assert.IsTrue(context.Manager.CompleteOfferedMission(context.Client, receiver.EntityId, 321, null, null));
            Assert.AreEqual(107, context.ReadRewardTotals().Credits);
            Assert.AreEqual(MissionObjectiveState.Completed, context.Client.Player.Missions[322].Objectives[1].State);
            using var verify = context.CreateChar();
            var reward = verify.CharacterMissions.Runtime.History(1).Single();
            Assert.AreEqual(Utc("2026-09-25T06:00:00Z").Ticks, reward.RewardWindowStartUtc.Value.Ticks);
            Assert.AreEqual(now.Ticks, reward.RewardedAtUtc.Value.Ticks);
            Assert.IsFalse(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
        }

        [TestMethod]
        public void AssignmentChangeDuringFinalRewardTimestampWriteRollsBackEverything()
        {
            var now = Utc("2026-09-25T06:00:00Z");
            using var context = RepeatContext(new(MissionRepeatKind.Cooldown, CooldownSeconds: 60), () => now);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, context.AddNpc(77).EntityId, 321));
            var original = context.ReadMission(321);
            var writes = 0;
            context.AfterSave = database =>
            {
                if (!database.Set<CharacterMissionHistoryEntry>().Local.Any(row => row.Rewarded))
                    return;
                if (++writes == 1)
                    now = now.AddSeconds(1);
                else if (writes == 2)
                    database.Database.ExecuteSqlRaw(
                        "UPDATE character_mission SET generation = generation + 1 WHERE character_id = 1 AND mission_id = 321");
            };
            Assert.IsFalse(context.Manager.CompleteOfferedMission(context.Client, context.AddNpc(88).EntityId, 321, null, null));
            Assert.AreEqual(2, writes);
            Assert.AreEqual(original.Generation, context.ReadMission(321).Generation);
            Assert.AreEqual(100, context.ReadRewardTotals().Credits);
            using var unit = context.CreateChar();
            Assert.AreEqual(0, unit.CharacterMissions.Runtime.History(1).Count);
        }

        [TestMethod]
        [DataRow(MissionRepeatKind.Cooldown, -1, false)]
        [DataRow(MissionRepeatKind.Cooldown, 0, true)]
        [DataRow(MissionRepeatKind.Cooldown, 1, true)]
        [DataRow(MissionRepeatKind.Daily, -1, false)]
        [DataRow(MissionRepeatKind.Daily, 0, true)]
        [DataRow(MissionRepeatKind.Daily, 1, true)]
        public void DurableEligibilitySurvivesRelogAtUtcBoundary(MissionRepeatKind kind, int offset, bool allowed)
        {
            var now = Utc("2026-09-25T06:00:00Z");
            var policy = kind == MissionRepeatKind.Cooldown ? new MissionRepeatPolicy(kind, CooldownSeconds: 60) :
                new MissionRepeatPolicy(kind, ResetSecondUtc: 21600);
            using var context = RepeatContext(policy, () => now);
            var giver = context.AddNpc(77);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            Assert.IsTrue(context.Manager.CompleteOfferedMission(context.Client, context.AddNpc(88).EntityId, 321, null, null));
            Assert.IsTrue(context.Manager.TryClear(context.Client, 321));
            now = kind == MissionRepeatKind.Cooldown ? now.AddSeconds(60 + offset) : now.AddDays(1).AddSeconds(offset);
            var restarted = new MissionApplication(context, context.Manager.LoadedMissions,
                new Dictionary<uint, MissionRewardDefinition>(), new ManifestationManager(context), utcNow: () => now);
            using (var unit = context.CreateChar())
                restarted.HydrateAndClearInvalid(context.Client.Player, unit);
            Assert.AreEqual(allowed, restarted.AcceptOfferedMission(context.Client, giver.EntityId, 321));
        }

        [TestMethod]
        public void DailyAttemptStartedBeforeResetConsumesTheWindowOfItsReward()
        {
            var now = Utc("2026-09-25T23:59:59Z");
            using var context = RepeatContext(new(MissionRepeatKind.Daily, ResetSecondUtc: 0), () => now);
            var giver = context.AddNpc(77);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            now = now.AddSeconds(2);
            Assert.IsTrue(context.Manager.CompleteOfferedMission(context.Client, context.AddNpc(88).EntityId, 321, null, null));
            Assert.IsFalse(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            using var unit = context.CreateChar();
            Assert.AreEqual(Utc("2026-09-26T00:00:00Z").Ticks,
                unit.CharacterMissions.Runtime.History(1).Single().RewardWindowStartUtc.Value.Ticks);
        }

        [TestMethod]
        public void DailyWindowHasADatabaseUniqueGuardButNonDailyOutcomesDoNotCollide()
        {
            using var context = RepeatContext(new(MissionRepeatKind.Daily, ResetSecondUtc: 0));
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, context.AddNpc(77).EntityId, 321));
            Assert.IsTrue(context.Manager.CompleteOfferedMission(context.Client, context.AddNpc(88).EntityId, 321, null, null));
            using var unit = context.CreateChar();
            var first = unit.CharacterMissions.Runtime.History(1).Single();
            Assert.ThrowsExactly<DbUpdateException>(() => unit.ExecuteTransaction(() =>
                unit.CharacterMissions.Runtime.Add(new CharacterMissionHistoryEntry
                {
                    CharacterId = 1, MissionId = 321, AssignmentId = Guid.NewGuid().ToString("N"), AssignmentGeneration = 2,
                    CompletedAtUtc = first.CompletedAtUtc, Rewarded = true, RewardedAtUtc = first.RewardedAtUtc,
                    RewardWindowStartUtc = first.RewardWindowStartUtc
                })));
            unit.ExecuteTransaction(() =>
            {
                foreach (var generation in new[] { 2U, 3U })
                    unit.CharacterMissions.Runtime.Add(new CharacterMissionHistoryEntry
                    {
                        CharacterId = 1, MissionId = 321, AssignmentId = Guid.NewGuid().ToString("N"),
                        AssignmentGeneration = generation, CompletedAtUtc = DateTime.UtcNow, Outcome = 2
                    });
            });
            Assert.AreEqual(3, unit.CharacterMissions.Runtime.History(1).Count);
        }

        [TestMethod]
        public void OldOpenedTurnInCannotClaimNextAttemptWithoutFreshHydrationAndSession()
        {
            using var context = MissionTestContext.WithCustomDefinitions(
                new Dictionary<uint, Mission> { [321] = Definition(new(MissionRepeatKind.Immediate)) },
                new Dictionary<uint, MissionRewardDefinition> { [321] = new(0, null, null, null) });
            var giver = context.AddNpc(77);
            var receiver = context.AddNpc(88);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            var stale = context.CreateCompetingClient();
            Assert.IsTrue(context.Manager.OpenNpcConversation(stale, receiver.EntityId));
            Assert.IsTrue(context.Manager.CompleteOfferedMission(context.Client, receiver.EntityId, 321, null, null));
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            var paid = context.ReadRewardTotals();
            Assert.IsFalse(context.Manager.TryCompleteNpcMission(stale, receiver.EntityId, 321, null, null));
            Assert.IsFalse(context.Manager.TryCompleteNpcMission(stale, receiver.EntityId, 321, null, null));
            Assert.AreEqual(paid, context.ReadRewardTotals());
            Assert.IsTrue(context.Manager.OpenNpcConversation(stale, receiver.EntityId));
            Assert.AreEqual(context.ReadMission(321).AssignmentId, stale.Player.Missions[321].AssignmentId);
            Assert.IsTrue(context.Manager.TryCompleteNpcMission(stale, receiver.EntityId, 321, null, null));
        }

        [TestMethod]
        public void OldEmptyOfferCannotBeReplayedAfterAnotherAttemptWasCleared()
        {
            using var context = RepeatContext(new(MissionRepeatKind.Immediate));
            var giver = context.AddNpc(77);
            Assert.IsTrue(context.Manager.OpenNpcConversation(context.Client, giver.EntityId));
            var old = context.Client.MissionConversation;
            Assert.IsTrue(context.Manager.TryAcceptNpcMission(context.Client, giver.EntityId, 321));
            Assert.IsTrue(context.Manager.CompleteOfferedMission(context.Client, context.AddNpc(88).EntityId, 321, null, null));
            Assert.IsTrue(context.Manager.TryClear(context.Client, 321));
            context.Client.MissionConversation = old;
            Assert.IsFalse(context.Manager.TryAcceptNpcMission(context.Client, giver.EntityId, 321));
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
        }

        [TestMethod]
        public void CompetingCallersOnSecondAttemptCommitOneNewRewardAndOneReceipt()
        {
            using var context = RepeatContext(new(MissionRepeatKind.Immediate));
            var giver = context.AddNpc(77);
            var receiver = context.AddNpc(88);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            Assert.IsTrue(context.Manager.CompleteOfferedMission(context.Client, receiver.EntityId, 321, null, null));
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            var competitor = context.CreateCompetingClient();
            Assert.IsTrue(context.Manager.OpenNpcConversation(context.Client, receiver.EntityId));
            Assert.IsTrue(context.Manager.OpenNpcConversation(competitor, receiver.EntityId));
            var results = new bool[2];
            Parallel.Invoke(
                () => results[0] = context.Manager.TryCompleteNpcMission(context.Client, receiver.EntityId, 321, null, null),
                () => results[1] = context.Manager.TryCompleteNpcMission(competitor, receiver.EntityId, 321, null, null));
            Assert.AreEqual(1, results.Count(result => result));
            Assert.AreEqual(114, context.ReadRewardTotals().Credits);
            using var unit = context.CreateChar();
            var history = unit.CharacterMissions.Runtime.History(1);
            Assert.AreEqual(2, history.Count);
            Assert.IsTrue(history.All(row => unit.CharacterMissions.Runtime.HasReceipt(
                row.AssignmentId, row.AssignmentGeneration, "mission-reward")));
        }

        [TestMethod]
        [DataRow(MissionState.Failed, 29, true)]
        [DataRow(MissionState.Failed, 30, false)]
        [DataRow(MissionState.Completed, 29, true)]
        [DataRow(MissionState.Completed, 30, false)]
        public void TerminalReplacementCountsOnlyTheNewActiveSlot(MissionState terminal, int active, bool allowed)
        {
            using var context = RepeatContext(new(MissionRepeatKind.Immediate));
            var giver = context.AddNpc(77);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            var first = context.ReadMission(321);
            Assert.IsTrue(terminal == MissionState.Failed ? context.Manager.TryFailMission(context.Client, 321) :
                context.Manager.CompleteOfferedMission(context.Client, context.AddNpc(88).EntityId, 321, null, null));
            for (uint id = 1000; id < 1000 + active; id++)
                context.SeedMission(1, id, (uint)MissionState.Active, false);
            Assert.AreEqual(allowed, context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            Assert.AreEqual(allowed ? MissionState.Active : terminal, context.Client.Player.Missions[321].State);
            Assert.AreEqual(allowed, context.ReadMission(321).AssignmentId != first.AssignmentId);
        }

        [TestMethod]
        public void FailedReplacementSaveRollsBackJournalHistoryAndFreshAcceptanceItems()
        {
            using var context = ItemContext();
            var giver = context.AddNpc(77);
            var first = context.ReadMission(321);
            Assert.IsTrue(context.Manager.CompleteOfferedMission(context.Client, context.AddNpc(88).EntityId, 321, null, null));
            context.BeforeSave = database =>
            {
                if (database.ChangeTracker.Entries<CharacterMissionEntry>().Any(entry =>
                    entry.State == EntityState.Added && entry.Entity.AssignmentId != first.AssignmentId))
                    throw new DbUpdateException("Injected replacement failure.");
            };
            Assert.IsFalse(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            context.BeforeSave = null;
            Assert.AreEqual(first.AssignmentId, context.ReadMission(321).AssignmentId);
            using (var unit = context.CreateChar())
            {
                Assert.AreEqual(1, unit.CharacterMissions.Runtime.History(1).Count);
                Assert.AreEqual(0, unit.CharacterMissionItems.GetOwned(1).Count);
            }
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            using var verify = context.CreateChar();
            Assert.AreEqual(context.ReadMission(321).AssignmentId, verify.CharacterMissionItems.GetOwned(1).Single().AssignmentId);
        }

        [TestMethod]
        public void RepeatItemsUseFreshProvenanceAndOldOperationsCannotReachThem()
        {
            using var context = ItemContext();
            var old = context.ReadMission(321);
            Assert.IsTrue(context.Manager.CompleteOfferedMission(context.Client, context.AddNpc(88).EntityId, 321, null, null));
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, context.AddNpc(77).EntityId, 321));
            var current = context.ReadMission(321);
            using var unit = context.CreateChar();
            Assert.ThrowsExactly<GameplayRejectionException>(() => unit.ExecuteTransaction(() =>
                new MissionItemPlanner(context.Client, unit, context.Manager).Apply(
                    new ConsumeMissionItemIntent("old-cost", 321, "key", 1, MissionItemScope.AssignmentIssued),
                    old.AssignmentId, old.Generation)));
            Assert.AreEqual(current.AssignmentId, unit.CharacterMissionItems.GetOwned(1).Single().AssignmentId);
            Assert.IsNotNull(unit.CharacterMissionItems.GetReceipt(1, old.AssignmentId, "accept-key"));
            Assert.IsNotNull(unit.CharacterMissionItems.GetReceipt(1, current.AssignmentId, "accept-key"));
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void ReplacingAFailedRepeatCancelsOnlyItsForwardedRouteAndPreservesTheExperienceRoot(bool sourceAlreadyResetting)
        {
            using var fixture = new SharedActorRepeatFixture();
            var context = fixture.Context;
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, fixture.Giver.EntityId, 321));
            var old = context.ReadMission(321);
            Assert.IsTrue(context.Manager.Scenes.ExecuteNamed(context.Client, 321, "route"));
            var unrelatedMove = fixture.Independent.Controller.ScriptedMove;
            Assert.IsNotNull(unrelatedMove);
            Assert.IsNotNull(fixture.Guide.Controller.ScriptedMove);
            string checkpoint;
            using (var unit = context.CreateChar())
            {
                checkpoint = unit.CharacterMissions.Runtime.Scene(fixture.RootId).Checkpoint;
                Assert.AreEqual(2, unit.CharacterMissions.Runtime.Effects(fixture.RootId).Count(effect => effect.Status == "Running"));
            }
            Assert.IsTrue(context.Manager.TryFailMission(context.Client, 321));
            if (sourceAlreadyResetting)
                using (var unit = context.CreateChar())
                    unit.ExecuteTransaction(() =>
                    {
                        var source = unit.CharacterMissions.Runtime.AssignmentScene(old.AssignmentId);
                        source.Status = "Resetting";
                        source.Generation++;
                        source.Version++;
                    });

            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, fixture.Giver.EntityId, 321));

            Assert.IsNull(fixture.Guide.Controller.ScriptedMove,
                "A replacement must stop the old attempt's route even though its actor belongs to the experience.");
            Assert.AreEqual(6.5f, fixture.Guide.RunSpeed);
            Assert.AreSame(unrelatedMove, fixture.Independent.Controller.ScriptedMove);
            Assert.IsTrue(MapInstanceScope.Contains(context.Map, fixture.Guide));
            Assert.IsTrue(CreatureGameplayRules.IsInvulnerable(fixture.Guide), "P5's current root policy must survive retirement.");
            Assert.AreNotEqual(old.AssignmentId, context.ReadMission(321).AssignmentId);
            using var verify = context.CreateChar();
            var root = verify.CharacterMissions.Runtime.Scene(fixture.RootId);
            Assert.AreEqual(1U, root.Generation);
            Assert.AreEqual(checkpoint, root.Checkpoint);
            Assert.AreEqual("Pending", verify.CharacterMissions.Runtime.Timer(fixture.RootId, "root-wait").Disposition);
            var effects = verify.CharacterMissions.Runtime.Effects(fixture.RootId);
            Assert.AreEqual("Running", effects.Single(effect => effect.OperationKey == "root-route").Status);
            Assert.AreEqual("Cancelled", effects.Single(effect => effect.OperationKey.StartsWith("shared-", StringComparison.Ordinal)).Status);
        }

        [TestMethod]
        [DataRow("follow", false)]
        [DataRow("attack", false)]
        [DataRow("follow", true)]
        [DataRow("attack", true)]
        public void ReplacingRepeatRetiresForwardedControlsWithoutClearingNewerRootControls(string control, bool rootSupersedes)
        {
            using var fixture = new SharedActorRepeatFixture();
            var context = fixture.Context;
            fixture.Guide.Faction = Factions.Bane;
            fixture.Independent.Faction = Factions.AFS;
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, fixture.Giver.EntityId, 321));
            var old = context.ReadMission(321);
            Assert.IsTrue(context.Manager.Scenes.ExecuteNamed(context.Client, 321, control));
            Assert.AreEqual(control == "follow" ? BehaviorManager.BehaviorActionFollow : BehaviorManager.BehaviorActionFighting,
                fixture.Guide.Controller.CurrentAction);
            var independentMove = fixture.Independent.Controller.ScriptedMove;
            if (rootSupersedes)
                Assert.IsTrue(context.Manager.Scenes.Submit(fixture.RootId,
                    new SceneObservation(SceneEventKind.Signal, 1, SequenceId: 3)));
            Assert.IsTrue(context.Manager.TryFailMission(context.Client, 321));

            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, fixture.Giver.EntityId, 321));

            Assert.AreEqual(BehaviorManager.BehaviorActionFollow, fixture.Guide.Controller.CurrentAction);
            Assert.AreEqual(rootSupersedes ? 1U : 0U, fixture.Guide.SpawnPool.FollowOwnerCharacterId);
            Assert.AreEqual(!rootSupersedes, fixture.Guide.Controller.ActionFollow.HasAnchor);
            if (!rootSupersedes)
            {
                Assert.AreEqual(0UL, fixture.Guide.Controller.ActionFollow.FollowTargetId);
                Assert.AreEqual(0UL, fixture.Guide.Controller.ActionFighting.TargetEntityId);
            }
            Assert.AreSame(independentMove, fixture.Independent.Controller.ScriptedMove);
            Assert.IsTrue(CreatureGameplayRules.IsInvulnerable(fixture.Guide));
            using var verify = context.CreateChar();
            Assert.AreEqual("Cancelled", verify.CharacterMissions.Runtime.Effects(fixture.RootId)
                .Single(effect => effect.SourceAssignmentId == old.AssignmentId).Status);
        }

        [TestMethod]
        public void RepeatCancelsOldSceneTimersAndDoesNotCopyFlagsOrSceneCredit()
        {
            var now = DateTime.UnixEpoch;
            using var context = RepeatContext(new(MissionRepeatKind.Immediate), () => now);
            context.Manager.Scenes.Bind(321, "data.sequence", new SceneBindings("unversioned",
                new Dictionary<string, SceneActorDefinition>(), new Dictionary<string, SceneRoute>(),
                new Dictionary<uint, SceneSequence>
                {
                    [0] = new(timers: new[] { new SequenceTimer("late", 2000, 1) }),
                    [1] = new(characterIntents: new CharacterIntent[] { new SetEntitlementIntent("old-entitlement", true) })
                }));
            var giver = context.AddNpc(77);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            string old;
            using (var unit = context.CreateChar())
            {
                old = unit.CharacterMissions.Runtime.Scenes(1, 321).Single().RunId;
                unit.CharacterFlags.Set(1, 9001, 5);
            }
            Assert.IsTrue(context.Manager.CompleteOfferedMission(context.Client, context.AddNpc(88).EntityId, 321, null, null));
            now = now.AddSeconds(1);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            now = now.AddSeconds(1);
            context.Manager.Scenes.Tick(context.Map);
            Assert.IsFalse(context.Manager.Scenes.Submit(old, new SceneObservation(SceneEventKind.Signal, 1, SequenceId: 1)));
            Assert.IsFalse(context.Client.AccountEntry.CanSkipBootcamp);
            using var verify = context.CreateChar();
            Assert.AreEqual("Cancelled", verify.CharacterMissions.Runtime.Timer(old, "late").Disposition);
            Assert.IsTrue(verify.CharacterFlags.HasValue(1, 9001, 5), "Persistent flags are not attempt progress.");
        }

        [TestMethod]
        public void StaleGroupCandidateCannotFreezeCreditOntoANewerDurableAttempt()
        {
            using var context = MissionTestContext.WithProgressMission(
                MissionProgressRule.IncrementCounterOnExactSubject(MissionProgressEventKind.CreatureKilled, 55, 0, 0, 5),
                counters: new Dictionary<uint, MissionObjectiveCounterDefinition> { [0] = new(0, 0, 5) },
                creditPolicy: new MissionCreditPolicy(MissionCreditMode.NearbyParty, 20),
                repeatPolicy: new(MissionRepeatKind.Immediate));
            var member = context.CreateAdditionalClient(2);
            var giver = context.AddNpc(77);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            Assert.IsTrue(context.Manager.AcceptOfferedMission(member, giver.EntityId, 321));
            var stale = member.Player.Missions[321];
            Assert.IsTrue(context.Manager.TryFailMission(member, 321));
            Assert.IsTrue(context.Manager.AcceptOfferedMission(member, giver.EntityId, 321));
            stale.State = MissionState.Active;
            member.Player.Missions[321] = stale;
            using var party = new GroupMissionCreditTests.PartyScope(context.Client, member);
            Assert.IsTrue(context.Manager.Credit.Record(context.Client, MissionProgressEvent.Creature(55), Vector3.Zero));
            using var unit = context.CreateChar();
            Assert.AreEqual(0, unit.CharacterMissions.Runtime.Deliveries(2).Count);
        }

        [TestMethod]
        public void StaleRuntimeCannotQueueNamedInputForNewScene()
        {
            using var context = RepeatContext(new(MissionRepeatKind.Immediate));
            context.Manager.Scenes.Bind(321, "data.sequence", new SceneBindings("unversioned",
                new Dictionary<string, SceneActorDefinition>(), new Dictionary<string, SceneRoute>(),
                new Dictionary<uint, SceneSequence>
                {
                    [0] = new(),
                    [1] = new(characterIntents: new CharacterIntent[] { new SetEntitlementIntent("stale-input", true) })
                }, new Dictionary<string, uint> { ["late"] = 1 }));
            var giver = context.AddNpc(77);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            var stale = context.CreateCompetingClient();
            Assert.IsTrue(context.Manager.TryAbandon(context.Client, 321));
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            Assert.IsFalse(context.Manager.Scenes.ExecuteNamed(stale, 321, "late"));
            using var unit = context.CreateChar();
            Assert.IsFalse(unit.GameAccounts.Get(1).CanSkipBootcamp);
        }

        [TestMethod]
        public void QueuedProgressPublicationCannotOverwriteANewAttempt()
        {
            using var context = MissionTestContext.WithProgressMission(
                MissionProgressRule.IncrementCounterOnExactSubject(MissionProgressEventKind.CreatureKilled, 55, 0, 0, 5),
                counters: new Dictionary<uint, MissionObjectiveCounterDefinition> { [0] = new(0, 0, 5) });
            var giver = context.AddNpc(77);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            MissionProgressPublicationPlan queued = null;
            using (var unit = context.CreateChar())
                unit.ExecuteTransaction(() => queued = context.Manager.PlanProgress(context.Client,
                    new[] { MissionProgressEvent.Creature(55) }, unit));
            Assert.IsTrue(context.Manager.TryAbandon(context.Client, 321));
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            context.Drain();

            queued.Publish(context.Client);

            Assert.AreEqual(0U, context.Client.Player.Missions[321].Objectives[1].Counters[0]);
            Assert.AreEqual(0, context.Drain().Count);
        }

        [TestMethod]
        public void NewAttemptStartsFromAuthoredCountersAndHydrationKeepsExactIdentity()
        {
            using var context = MissionTestContext.WithProgressMission(
                MissionProgressRule.IncrementCounterOnExactSubject(MissionProgressEventKind.CreatureKilled, 55, 0, 2, 5),
                counters: new Dictionary<uint, MissionObjectiveCounterDefinition> { [0] = new(0, 2, 5) },
                repeatPolicy: new(MissionRepeatKind.Immediate), reward: new(0, null, null, null));
            var giver = context.AddNpc(77);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            var first = context.ReadMission(321);
            for (var count = 0; count < 3; count++)
                Assert.IsTrue(context.Manager.RecordProgress(context.Client, MissionProgressEvent.Creature(55)));
            Assert.IsTrue(context.Manager.CompleteOfferedMission(context.Client, context.AddNpc(88).EntityId, 321, null, null));
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            context.ReloadPlayerMissions();
            var second = context.ReadMission(321);
            var runtime = context.Client.Player.Missions[321];
            Assert.AreEqual(2U, runtime.Objectives[1].Counters[0]);
            Assert.AreEqual(MissionObjectiveState.Incomplete, runtime.Objectives[1].State);
            Assert.IsFalse(runtime.Completeable);
            Assert.AreEqual(first.Generation + 1, second.Generation);
            Assert.IsTrue(runtime.MatchesAssignment(second.AssignmentId, second.Generation, second.ContentRevision));
        }

        [TestMethod]
        public void LegacyHydrationPinsStoredRevisionSeparatelyFromTheLoadedDefinition()
        {
            using var context = RepeatContext(new(MissionRepeatKind.Immediate));
            context.SeedMission(1, 321, (uint)MissionState.Active, true);
            context.ReloadPlayerMissions();
            var row = context.ReadMission(321);
            Assert.AreEqual("legacy", row.ContentRevision);
            Assert.IsTrue(context.Client.Player.Missions[321].MatchesAssignment(row.AssignmentId, row.Generation, "legacy"));
            var receiver = context.AddNpc(88);
            Assert.IsTrue(context.Manager.OpenNpcConversation(context.Client, receiver.EntityId));
            var topic = context.Client.MissionConversation.Topics.Single();
            Assert.AreEqual("legacy", topic.AssignmentRevision);
            Assert.AreEqual("unversioned", topic.ContentRevision);
            Assert.IsTrue(context.Manager.TryCompleteNpcMission(context.Client, receiver.EntityId, 321, null, null));
        }

        [TestMethod]
        public void RepeatWaitsForExistingPublicLeaseToReturnRatherThanOverridingIt()
        {
            using var context = RepeatContext(new(MissionRepeatKind.Immediate));
            var actor = context.AddNpc(77);
            actor.SpawnPool = new SpawnPool
            {
                DbId = 77, MapContextId = context.Map.MapInfo.MapContextId,
                RuntimeMapChannel = context.Map, Position = actor.Position
            };
            context.Map.SpawnPools.Add(actor.SpawnPool);
            var giver = context.AddNpc(77);
            context.Manager.PublicActors.Bind(new PublicEncounterBinding(321, 77, "escort", "example.escort"));
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            var old = context.Manager.PublicActors.Handle(context.Map, 77);
            Assert.IsTrue(context.Manager.CompleteOfferedMission(context.Client, context.AddNpc(88).EntityId, 321, null, null));
            Assert.IsFalse(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            Assert.AreEqual(old, context.Manager.PublicActors.Handle(context.Map, 77));
            context.Manager.PublicActors.Tick(context.Map);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            Assert.AreNotEqual(old.RunId, context.Manager.PublicActors.Handle(context.Map, 77).RunId);
            Assert.IsFalse(context.Manager.PublicActors.TryResolve(context.Map, old, out _));
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void PreparedPublicSourceRechecksPersistedLeaseAndSceneGeneration(bool changeScene)
        {
            using var context = RepeatContext(new(MissionRepeatKind.Immediate));
            var actor = context.AddNpc(77);
            actor.SpawnPool = new SpawnPool
            {
                DbId = 77, MapContextId = context.Map.MapInfo.MapContextId,
                RuntimeMapChannel = context.Map, Position = actor.Position
            };
            context.Map.SpawnPools.Add(actor.SpawnPool);
            context.Manager.PublicActors.Bind(new PublicEncounterBinding(321, 77, "escort", "example.escort"));
            var injected = false;
            context.AfterSave = database =>
            {
                var lease = database.Set<MissionActorLeaseEntry>().Local.SingleOrDefault();
                if (injected || lease == null)
                    return;
                injected = true;
                if (changeScene)
                    database.Database.ExecuteSqlInterpolated(
                        $"UPDATE mission_scene SET generation = generation + 1 WHERE run_id = {lease.RunId}");
                else
                    database.Database.ExecuteSqlInterpolated(
                        $"UPDATE mission_actor_lease SET generation = generation + 1 WHERE run_id = {lease.RunId}");
            };
            Assert.IsFalse(context.Manager.AcceptOfferedMission(context.Client, actor.EntityId, 321));
            Assert.IsTrue(injected);
            Assert.IsNull(context.Manager.PublicActors.Handle(context.Map, 77));
            Assert.IsTrue(actor.IsInteractable);
            using var unit = context.CreateChar();
            Assert.IsNull(unit.CharacterMissions.GetByCharacterAndMission(1, 321));
            Assert.AreEqual(0, unit.CharacterMissions.Runtime.Scenes(1, 321).Count);
        }

        private static MissionTestContext ItemContext()
        {
            var mission = Definition(new(MissionRepeatKind.Immediate)).WithItems(new[]
            {
                new MissionItemBinding("key", 28, MissionItemScope.AssignmentIssued, 1,
                    MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove)
            }, new[] { new IssueMissionItemIntent("accept-key", 321, "key", 28, 1) });
            var context = MissionTestContext.WithCustomDefinitions(new Dictionary<uint, Mission> { [321] = mission },
                new Dictionary<uint, MissionRewardDefinition> { [321] = new(0, null, null, null) });
            context.AddRewardTemplate(28, 3147);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, context.AddNpc(77).EntityId, 321));
            return context;
        }

        private static MissionTestContext RepeatContext(MissionRepeatPolicy policy, Func<DateTime> utcNow = null,
            Mission dependent = null) =>
            MissionTestContext.WithCustomDefinitions((dependent == null ? new[] { Definition(policy) } :
                    new[] { Definition(policy), dependent }).ToDictionary(mission => mission.MissionId),
                new Dictionary<uint, MissionRewardDefinition>
                {
                    [321] = new(0, new Dictionary<CurencyType, int> { [CurencyType.Credits] = 7 },
                        Array.Empty<MissionRewardItem>(), Array.Empty<MissionRewardItem>())
                }, utcNow);

        private static Mission Definition(MissionRepeatPolicy policy = null, MissionRequirement requirement = null,
            uint missionId = 321) =>
            new(missionId, "Repeat fixture", missionId, 77, 88, 1, 1, 2, false, false,
                Array.Empty<MissionObjectiveDefinition>(), true, requirement: requirement, repeatPolicy: policy);

        private static DateTime Utc(string value) => DateTime.Parse(value, null,
            System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal);
    }
}
