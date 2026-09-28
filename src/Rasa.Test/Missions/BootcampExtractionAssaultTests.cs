using System;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.MapChannel.Server;
using Rasa.Services.Preloader.Missions;
using Rasa.Structures;
using Rasa.Structures.Char;

namespace Rasa.Test.Missions
{
    [TestClass]
    [DoNotParallelize]
    public class BootcampExtractionAssaultTests
    {
        [TestMethod]
        public void CombatInterruptsAndResumesTheUphillRouteWithoutRecordingADeath()
        {
            using var harness = StartAssault();
            var enemy = Attackers(harness)[0];
            BehaviorManager.Instance.SetActionFighting(enemy, harness.Client.Player.EntityId);

            harness.Manager.TickScenarios(harness.Client);

            Assert.AreEqual(BehaviorManager.BehaviorActionFighting, enemy.Controller.CurrentAction);
            using (var verify = harness.Context.CreateChar())
                Assert.HasCount(0, verify.CharacterMissions.Runtime.ActorStates(enemy.SpawnPool.SceneRunId));
            BehaviorManager.Instance.SetActionAnchor(enemy, enemy.Position);
            harness.Manager.TickScenarios(harness.Client);
            Assert.AreEqual(BehaviorManager.BehaviorActionScriptedMove, enemy.Controller.CurrentAction);
            Assert.IsFalse(enemy.Controller.ScriptedMove.Arrived);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void EveryAssaultEnemyMustDieBeforeVanCheckInIncludingSoldierFinalBlows(bool retry)
        {
            using var harness = StartAssault(retry);
            var missionId = retry ? 2005U : 1995U;
            Arrive(harness);
            var van = Van(harness);
            harness.MovePlayerTo(van);
            var soldier = Soldiers(harness)[0];
            var enemies = Attackers(harness);
            Assert.HasCount(6, enemies);
            foreach (var enemy in enemies.Take(5))
                ActorManager.Instance.Damage(harness.BootcampMap, enemy, 100000, soldier);
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, van.EntityId, missionId, 4, 1));
            Assert.IsFalse(harness.Client.Player.Missions[missionId].Completeable);
            Assert.IsNull(harness.Client.PendingTransfer);

            ActorManager.Instance.Damage(harness.BootcampMap, enemies[^1], 100000, soldier);

            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[missionId].Objectives[4].State);
            Assert.IsTrue(van.IsInteractable);
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, van.EntityId, missionId, 4, 1));
            Assert.IsTrue(harness.Client.Player.Missions[missionId].Completeable);
            Assert.IsNull(harness.Client.PendingTransfer, "Check-in alone must not board the player.");
            harness.Manager.Scenes.RecordDefeat(harness.BootcampMap, enemies[^1], null);
            using var unit = harness.Context.CreateChar();
            var run = unit.CharacterMissions.Runtime.Scenes(harness.Client.Player.Id, missionId).Single();
            Assert.HasCount(6, unit.CharacterMissions.Runtime.ActorStates(run.RunId));
            Assert.AreEqual(6, unit.CharacterMissions.Runtime.Messages(run.RunId)
                .Count(message => message.OperationKey.StartsWith("defeated-", StringComparison.Ordinal) && message.Status == "Handled"));
        }

        [TestMethod]
        [DataRow(false, "legacy", true)]
        [DataRow(false, "unversioned", true)]
        [DataRow(false, "other-release", false)]
        [DataRow(true, "legacy", true)]
        [DataRow(true, "unversioned", true)]
        [DataRow(true, "other-release", false)]
        public void VanConversationHonorsStoredRevisionCompatibilityAfterAssault(
            bool retry, string storedRevision, bool allowed)
        {
            using var harness = StartAssault(retry);
            Arrive(harness);
            DefeatAll(harness);
            var missionId = retry ? 2005U : 1995U;
            var van = Van(harness);
            Assert.IsTrue(van.IsInteractable);
            Assert.HasCount(0, Attackers(harness));
            string assignmentId = null;
            uint generation = 0;
            using (var unit = harness.Context.CreateChar())
                unit.ExecuteTransaction(() =>
                {
                    var assignment = unit.CharacterMissions.GetByCharacterAndMission(harness.Client.Player.Id, missionId);
                    assignmentId = assignment.AssignmentId;
                    generation = assignment.Generation;
                    assignment.ContentRevision = storedRevision;
                    var scene = unit.CharacterMissions.Runtime.Scene(van.SpawnPool.SceneRunId);
                    Assert.AreEqual(missionId, scene.MissionId);
                    Assert.AreEqual("deployment_11", scene.Release);
                });
            if (allowed)
            {
                using var unit = harness.Context.CreateChar();
                harness.Manager.Hydrate(harness.Client.Player,
                    unit.CharacterMissions.Get(harness.Client.Player.Id),
                    unit.CharacterMissionProgress.Get(harness.Client.Player.Id));
            }
            harness.MovePlayerTo(van);
            harness.Drain();
            var npcs = new NpcManager(harness.Context, harness.Manager);

            npcs.RequestNpcConverse(harness.Client, new RequestNPCConversePacket { EntityId = van.EntityId });

            var conversations = harness.Drain().OfType<ConversePacket>().ToArray();
            Assert.AreEqual(allowed ? 1 : 0, conversations.Length, $"Mission {missionId}, stored revision {storedRevision}");
            if (allowed)
            {
                var objective = ((System.Collections.Generic.List<CompleteableObjectives>)
                    conversations.Single().ConvoDataDict[ConversationType.ObjectiveComplete]).Single();
                Assert.AreEqual((int)missionId, objective.MissionId);
                Assert.AreEqual(4, objective.ObjectiveId);
                Assert.AreEqual(1, objective.PlayerFlagId);
            }

            npcs.CompleteNPCObjective(harness.Client, new CompleteNPCObjectivePacket
            {
                EntityId = van.EntityId, MissionId = missionId, ObjectiveId = 4, PlayerFlagId = 1
            });

            Assert.AreEqual(allowed ? MissionObjectiveState.Completed : MissionObjectiveState.Incomplete,
                harness.Client.Player.Missions[missionId].Objectives[4].State);
            Assert.AreEqual(allowed, harness.Client.Player.Missions[missionId].Completeable);
            Assert.IsNull(harness.Client.PendingTransfer);
            using var verify = harness.Context.CreateChar();
            var durable = verify.CharacterMissions.GetByCharacterAndMission(harness.Client.Player.Id, missionId);
            Assert.AreEqual(assignmentId, durable.AssignmentId);
            Assert.AreEqual(generation, durable.Generation);
            Assert.AreEqual(storedRevision, durable.ContentRevision, "Opening and Continue must not rewrite stored revisions.");
            Assert.AreEqual(allowed ? (byte)MissionObjectiveState.Completed : (byte)MissionObjectiveState.Incomplete,
                verify.CharacterMissionProgress.GetTracked(harness.Client.Player.Id, missionId)[4].ObjectiveState);
        }

        [TestMethod]
        [DataRow(false, "legacy")]
        [DataRow(false, "unversioned")]
        [DataRow(true, "legacy")]
        [DataRow(true, "unversioned")]
        public void VanConversationRequiresTheCurrentOperationalSceneRelease(
            bool retry, string storedRevision)
        {
            using var harness = StartAssault(retry);
            Arrive(harness);
            DefeatAll(harness);
            var missionId = retry ? 2005U : 1995U;
            var van = Van(harness);
            Assert.IsTrue(van.IsInteractable);
            using (var unit = harness.Context.CreateChar())
                unit.ExecuteTransaction(() =>
                {
                    unit.CharacterMissions.GetByCharacterAndMission(harness.Client.Player.Id, missionId)
                        .ContentRevision = storedRevision;
                    unit.CharacterMissions.Runtime.Scene(van.SpawnPool.SceneRunId).Release = storedRevision;
                });
            try
            {
                harness.MovePlayerTo(van);
                harness.Drain();
                var npcs = new NpcManager(harness.Context, harness.Manager);

                npcs.RequestNpcConverse(harness.Client, new RequestNPCConversePacket { EntityId = van.EntityId });

                Assert.HasCount(0, harness.Drain().OfType<ConversePacket>().ToArray(),
                    "A compatible assignment sentinel must not also make a stale scene release compatible.");
                npcs.CompleteNPCObjective(harness.Client, new CompleteNPCObjectivePacket
                {
                    EntityId = van.EntityId, MissionId = missionId, ObjectiveId = 4, PlayerFlagId = 1
                });
                Assert.AreEqual(MissionObjectiveState.Incomplete,
                    harness.Client.Player.Missions[missionId].Objectives[4].State);
                using var verify = harness.Context.CreateChar();
                Assert.AreEqual((byte)MissionObjectiveState.Incomplete,
                    verify.CharacterMissionProgress.GetTracked(harness.Client.Player.Id, missionId)[4].ObjectiveState);
            }
            finally
            {
                using var unit = harness.Context.CreateChar();
                unit.ExecuteTransaction(() => unit.CharacterMissions.Runtime.Scene(van.SpawnPool.SceneRunId).Release =
                    harness.Manager.LoadedMissions[missionId].ContentRevision);
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void VanContinueKeepsTheExactStoredRevisionCapturedAtOpening(bool retry)
        {
            using var harness = StartAssault(retry);
            Arrive(harness);
            DefeatAll(harness);
            var missionId = retry ? 2005U : 1995U;
            var van = Van(harness);
            using (var unit = harness.Context.CreateChar())
                unit.ExecuteTransaction(() => unit.CharacterMissions.GetByCharacterAndMission(harness.Client.Player.Id, missionId)
                    .ContentRevision = "legacy");
            harness.MovePlayerTo(van);
            harness.Drain();
            var npcs = new NpcManager(harness.Context, harness.Manager);
            npcs.RequestNpcConverse(harness.Client, new RequestNPCConversePacket { EntityId = van.EntityId });
            Assert.HasCount(1, harness.Drain().OfType<ConversePacket>().ToArray());
            using (var unit = harness.Context.CreateChar())
                unit.ExecuteTransaction(() => unit.CharacterMissions.GetByCharacterAndMission(harness.Client.Player.Id, missionId)
                    .ContentRevision = "unversioned");

            npcs.CompleteNPCObjective(harness.Client, new CompleteNPCObjectivePacket
            {
                EntityId = van.EntityId, MissionId = missionId, ObjectiveId = 4, PlayerFlagId = 1
            });

            Assert.AreEqual(MissionObjectiveState.Incomplete,
                harness.Client.Player.Missions[missionId].Objectives[4].State);
            Assert.HasCount(0, harness.Drain());
            using var verify = harness.Context.CreateChar();
            Assert.AreEqual((byte)MissionObjectiveState.Incomplete,
                verify.CharacterMissionProgress.GetTracked(harness.Client.Player.Id, missionId)[4].ObjectiveState);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void ReconnectPreservesDefeatedAttackersAndOnlyRestoresTheSurvivors(bool retry)
        {
            using var harness = StartAssault(retry);
            Arrive(harness);
            var defeatedRoles = Attackers(harness).Take(4).Select(enemy => enemy.SpawnPool.SceneActorRole).ToArray();
            foreach (var enemy in Attackers(harness).Where(enemy => defeatedRoles.Contains(enemy.SpawnPool.SceneActorRole)))
                ActorManager.Instance.Damage(harness.BootcampMap, enemy, 100000, harness.Client.Player);

            harness.ReconnectFresh();

            var survivors = Attackers(harness);
            Assert.HasCount(2, survivors);
            Assert.IsFalse(survivors.Any(enemy => defeatedRoles.Contains(enemy.SpawnPool.SceneActorRole)));
            Assert.IsFalse(Van(harness).IsInteractable);
            foreach (var enemy in survivors)
                ActorManager.Instance.Damage(harness.BootcampMap, enemy, 100000, Soldiers(harness)[0]);
            Assert.IsTrue(Van(harness).IsInteractable);
            Assert.AreEqual(MissionObjectiveState.Incomplete,
                harness.Client.Player.Missions[retry ? 2005U : 1995U].Objectives[4].State);
        }

        [TestMethod]
        public void ClearingAttackersBeforeTheShipArrivesStillWaitsForItsArrival()
        {
            using var harness = StartAssault();
            foreach (var enemy in Attackers(harness))
                ActorManager.Instance.Damage(harness.BootcampMap, enemy, 100000, harness.Client.Player);
            Assert.IsNull(Van(harness));
            Assert.IsFalse(harness.Client.Player.Missions[1995].Completeable);

            Arrive(harness);

            Assert.IsTrue(Van(harness).IsInteractable);
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[1995].Objectives[4].State);
            harness.ReconnectFresh();
            Assert.HasCount(0, Attackers(harness));
            Assert.IsTrue(Van(harness).IsInteractable);
        }

        [TestMethod]
        public void FailedPostKillSceneWriteRecoversTheCommittedDefeatAndUnlocksCheckIn()
        {
            using var harness = StartAssault();
            Arrive(harness);
            var enemies = Attackers(harness);
            foreach (var enemy in enemies.Take(5))
                ActorManager.Instance.Damage(harness.BootcampMap, enemy, 100000, Soldiers(harness)[0]);
            harness.Context.BeforeSave = database =>
            {
                if (database.ChangeTracker.Entries<MissionSceneEntry>().Any(entry => entry.State == EntityState.Modified))
                    throw new DbUpdateException("Injected post-defeat scene failure.");
            };
            ActorManager.Instance.Damage(harness.BootcampMap, enemies[^1], 100000, Soldiers(harness)[0]);
            Assert.IsFalse(Van(harness).IsInteractable);
            using (var unit = harness.Context.CreateChar())
            {
                var run = unit.CharacterMissions.Runtime.Scenes(harness.Client.Player.Id, 1995).Single();
                Assert.HasCount(6, unit.CharacterMissions.Runtime.ActorStates(run.RunId));
                Assert.AreEqual(1, unit.CharacterMissions.Runtime.Messages(run.RunId).Count(message => message.Status == "Pending"));
            }

            harness.Context.BeforeSave = null;
            harness.ReconnectFresh();

            Assert.HasCount(0, Attackers(harness));
            Assert.IsTrue(Van(harness).IsInteractable);
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[1995].Objectives[4].State);
        }

        [TestMethod]
        public void AbandoningAnAssaultCancelsItsActorsAndRoutesBeforeTheRetryBegins()
        {
            using var harness = StartAssault();
            Arrive(harness);
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, 1995));
            Assert.HasCount(0, Attackers(harness));
            Assert.HasCount(0, Soldiers(harness));
            Assert.IsNull(Van(harness));
            Assert.IsNull(BootcampRuntimeTestHarness.FindScenarioObject(harness.BootcampMap, "bootcamp-evacuation-ship"));
            harness.UtcNow += TimeSpan.FromSeconds(30);
            harness.Manager.TickScenarios(harness.Client);

            var youngblood = BootcampRuntimeTestHarness.FindNpcByPackage(harness.BootcampMap, 2561);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, youngblood.EntityId, 2005));
            harness.UseObjectAndRecover(BootcampRuntimeTestHarness.FindScenarioObject(harness.BootcampMap, "bootcamp-dropship-debris"));

            Assert.HasCount(6, Attackers(harness));
            Assert.IsTrue(Attackers(harness).All(enemy => enemy.SpawnPool.ScenarioMissionId == 2005));
        }

        [TestMethod]
        public void AssaultRunsUphillAndThenUsesRealCombatAgainstThePlayer()
        {
            using var harness = StartAssault();
            Arrive(harness);
            foreach (var soldier in Soldiers(harness))
                soldier.Actions.Clear();
            harness.Client.Player.Attributes[Attributes.Health].Current =
                harness.Client.Player.Attributes[Attributes.Health].CurrentMax = 100000;
            var enemy = Attackers(harness)[0];
            var initial = enemy.Position;
            var health = harness.Client.Player.Attributes[Attributes.Health].Current;

            for (var tick = 0; tick < 200 && harness.Client.Player.Attributes[Attributes.Health].Current == health; tick++)
                Advance(harness);

            Assert.IsTrue(Vector3.Distance(enemy.Position, harness.Client.Player.Position) <
                Vector3.Distance(initial, harness.Client.Player.Position) - 40);
            Assert.IsGreaterThan(initial.Y + 10, enemy.Position.Y);
            Assert.IsLessThan(health, harness.Client.Player.Attributes[Attributes.Health].Current,
                "The attackers must deal real damage after advancing, not only play a route.");
        }

        [TestMethod]
        public void ReinforcementSoldiersActivelyDamageTheAssaultWithoutPlayerAttacks()
        {
            using var harness = StartAssault();
            Arrive(harness);
            harness.Client.Player.Attributes[Attributes.Health].Current =
                harness.Client.Player.Attributes[Attributes.Health].CurrentMax = 100000;
            var enemies = Attackers(harness);
            var before = enemies.Sum(Durability);

            for (var tick = 0; tick < 200 && enemies.Sum(Durability) == before; tick++)
                Advance(harness);

            Assert.IsLessThan(before, enemies.Sum(Durability));
            Assert.IsTrue(Soldiers(harness).Any(soldier =>
                soldier.Controller.CurrentAction == BehaviorManager.BehaviorActionFighting));
            Assert.IsFalse(harness.Client.Player.Missions[1995].Completeable);
        }

        private static BootcampRuntimeTestHarness.Harness StartAssault(bool retry = false)
        {
            var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            try
            {
                var youngblood = BootcampCallingForReinforcementsTests.StartCrashSiteScene(harness);
                harness.UseObjectAndRecover(BootcampRuntimeTestHarness.FindScenarioObject(harness.BootcampMap, "bootcamp-conrad-corpse"));
                if (retry)
                {
                    harness.UtcNow += TimeSpan.FromSeconds(601);
                    Assert.IsTrue(harness.Manager.EvaluateDeadlines(harness.Client));
                    Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, youngblood.EntityId, 2005));
                }
                harness.UseObjectAndRecover(BootcampRuntimeTestHarness.FindScenarioObject(harness.BootcampMap, "bootcamp-dropship-debris"));
                return harness;
            }
            catch
            {
                harness.Dispose();
                throw;
            }
        }

        internal static void DefeatAll(BootcampRuntimeTestHarness.Harness harness)
        {
            foreach (var enemy in Attackers(harness))
                ActorManager.Instance.Damage(harness.BootcampMap, enemy, 100000, harness.Client.Player);
        }

        private static Creature[] Attackers(BootcampRuntimeTestHarness.Harness harness) =>
            Creatures(harness).Where(actor => actor.DbId == BootcampExtractionDataV5.AssaultTemplate &&
                actor.State != CharacterState.Dead).ToArray();
        private static Creature[] Soldiers(BootcampRuntimeTestHarness.Harness harness) =>
            Creatures(harness).Where(actor => actor.DbId == BootcampExtractionDataV5.SoldierTemplate).ToArray();
        private static Creature[] Creatures(BootcampRuntimeTestHarness.Harness harness) =>
            harness.BootcampMap.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList).Distinct().ToArray();
        private static Creature Van(BootcampRuntimeTestHarness.Harness harness) =>
            BootcampRuntimeTestHarness.FindNpcByPackage(harness.BootcampMap, 2564);
        private static int Durability(Creature creature) =>
            creature.Attributes[Attributes.Health].Current + creature.Attributes[Attributes.Armor].Current;

        private static void Arrive(BootcampRuntimeTestHarness.Harness harness)
        {
            harness.UtcNow += TimeSpan.FromSeconds(5);
            harness.Manager.TickScenarios(harness.Client);
            harness.UtcNow += TimeSpan.FromSeconds(2);
            harness.Manager.TickScenarios(harness.Client);
        }

        private static void Advance(BootcampRuntimeTestHarness.Harness harness)
        {
            harness.UtcNow += TimeSpan.FromMilliseconds(250);
            BehaviorManager.Instance.MapChannelThink(harness.BootcampMap, 250);
            MissileManager.Instance.DoWork(harness.BootcampMap, 250);
            harness.Manager.TickScenarios(harness.Client);
        }
    }
}
