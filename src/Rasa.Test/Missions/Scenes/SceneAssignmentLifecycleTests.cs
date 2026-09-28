using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions.Scenes
{
    using Rasa.Data;
    using Rasa.Game.Missions.World;
    using Rasa.Managers;
    using Rasa.Missions.Scenes;
    using Rasa.Packets.Mission.Server;
    using Rasa.Structures;
    using Rasa.Structures.Char;

    [TestClass]
    [DoNotParallelize]
    public class SceneAssignmentLifecycleTests
    {
        [TestMethod]
        [DataRow(3U, BehaviorManager.BehaviorActionFollow, "root-follow")]
        [DataRow(4U, BehaviorManager.BehaviorActionFighting, "root-attack")]
        [DataRow(5U, BehaviorManager.BehaviorActionScriptedMove, "root-guide-route")]
        public void ForwardedRouteRetirementPreservesNewerRootCommandsAcrossKinds(
            uint sequence, byte expectedAction, string rootOperation)
        {
            using var fixture = new SharedActorRepeatFixture();
            var context = fixture.Context;
            fixture.Guide.Faction = Factions.Bane;
            fixture.Independent.Faction = Factions.AFS;
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, fixture.Giver.EntityId, 321));
            var old = context.ReadMission(321);
            Assert.IsTrue(context.Manager.Scenes.ExecuteNamed(context.Client, 321, "route"));
            Assert.IsNotNull(fixture.Guide.Controller.ScriptedMove);
            var independentMove = fixture.Independent.Controller.ScriptedMove;
            Assert.IsTrue(context.Manager.Scenes.Submit(fixture.RootId,
                new SceneObservation(SceneEventKind.Signal, 1, SequenceId: sequence)));
            Assert.AreEqual(expectedAction, fixture.Guide.Controller.CurrentAction);
            var currentMove = fixture.Guide.Controller.ScriptedMove;
            var target = fixture.Guide.Controller.ActionFighting.TargetEntityId;
            var follow = fixture.Guide.Controller.ActionFollow.FollowTargetId;
            var hasAnchor = fixture.Guide.Controller.ActionFollow.HasAnchor;
            string sourceRun;
            string checkpoint;
            using (var unit = context.CreateChar())
            {
                sourceRun = unit.CharacterMissions.Runtime.AssignmentScene(old.AssignmentId).RunId;
                checkpoint = unit.CharacterMissions.Runtime.Scene(fixture.RootId).Checkpoint;
            }
            Assert.IsTrue(context.Manager.TryFailMission(context.Client, 321));

            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, fixture.Giver.EntityId, 321));
            AssertPreserved();
            context.Manager.Scenes.CompleteAssignmentCancellation(new[] { sourceRun });
            AssertPreserved();

            void AssertPreserved()
            {
                Assert.AreEqual(expectedAction, fixture.Guide.Controller.CurrentAction,
                    "Retiring an older route must not anchor a controller owned by a newer root command.");
                Assert.AreEqual(target, fixture.Guide.Controller.ActionFighting.TargetEntityId);
                Assert.AreEqual(follow, fixture.Guide.Controller.ActionFollow.FollowTargetId);
                Assert.AreEqual(hasAnchor, fixture.Guide.Controller.ActionFollow.HasAnchor);
                if (expectedAction == BehaviorManager.BehaviorActionScriptedMove)
                    Assert.AreSame(currentMove, fixture.Guide.Controller.ScriptedMove);
                else
                    Assert.IsNull(fixture.Guide.Controller.ScriptedMove, "Remove only the retired route's movement.");
                Assert.AreSame(independentMove, fixture.Independent.Controller.ScriptedMove);
                Assert.IsTrue(CreatureGameplayRules.IsInvulnerable(fixture.Guide));
                Assert.IsTrue(MapInstanceScope.Contains(context.Map, fixture.Guide));
                using var verify = context.CreateChar();
                var root = verify.CharacterMissions.Runtime.Scene(fixture.RootId);
                Assert.AreEqual(1U, root.Generation);
                Assert.AreEqual(checkpoint, root.Checkpoint);
                Assert.AreEqual("Pending", verify.CharacterMissions.Runtime.Timer(fixture.RootId, "root-wait").Disposition);
                var effects = verify.CharacterMissions.Runtime.Effects(fixture.RootId);
                Assert.AreEqual("Cancelled", effects.Single(effect => effect.SourceAssignmentId == old.AssignmentId).Status);
                Assert.AreEqual(expectedAction == BehaviorManager.BehaviorActionScriptedMove ? "Running" : "Applied",
                    effects.Single(effect => effect.OperationKey == rootOperation).Status);
                Assert.AreEqual("Running", effects.Single(effect => effect.OperationKey == "root-route").Status);
            }
        }

        [TestMethod]
        public void ForwardedRetirementIsAtomicAndLateCancellationCannotStopTheNewAttemptsRoute()
        {
            using var fixture = new SharedActorRepeatFixture();
            var context = fixture.Context;
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, fixture.Giver.EntityId, 321));
            Assert.IsTrue(context.Manager.Scenes.ExecuteNamed(context.Client, 321, "route"));
            var oldMove = fixture.Guide.Controller.ScriptedMove;
            var old = context.ReadMission(321);
            string sourceRun;
            using (var unit = context.CreateChar())
                sourceRun = unit.CharacterMissions.Runtime.AssignmentScene(old.AssignmentId).RunId;
            Assert.IsTrue(context.Manager.TryFailMission(context.Client, 321));
            context.BeforeSave = database =>
            {
                if (database.ChangeTracker.Entries<MissionWorldEffectEntry>().Any(entry =>
                    entry.State == EntityState.Modified && entry.Entity.SourceAssignmentId == old.AssignmentId &&
                    entry.Entity.Status == "Cancelled"))
                    throw new DbUpdateException("Injected forwarded retirement failure.");
            };

            Assert.IsFalse(context.Manager.AcceptOfferedMission(context.Client, fixture.Giver.EntityId, 321));

            context.BeforeSave = null;
            Assert.AreSame(oldMove, fixture.Guide.Controller.ScriptedMove);
            Assert.AreEqual(old.AssignmentId, context.ReadMission(321).AssignmentId);
            using (var unit = context.CreateChar())
                Assert.AreEqual("Running", unit.CharacterMissions.Runtime.ForwardedEffects(sourceRun).Single().Status);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, fixture.Giver.EntityId, 321));
            Assert.IsNull(fixture.Guide.Controller.ScriptedMove);
            Assert.IsTrue(context.Manager.Scenes.ExecuteNamed(context.Client, 321, "route"));
            var currentMove = fixture.Guide.Controller.ScriptedMove;
            Assert.IsNotNull(currentMove);

            context.Manager.Scenes.CompleteAssignmentCancellation(new[] { sourceRun });

            Assert.AreSame(currentMove, fixture.Guide.Controller.ScriptedMove,
                "Retiring an old operation must not cancel another operation on the same root actor.");
            Assert.IsNotNull(fixture.Independent.Controller.ScriptedMove);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void ForwardedRouteCallbacksRecheckSourceBeforeAndDuringCommit(bool changeDuringSave)
        {
            using var fixture = new SharedActorRepeatFixture();
            var context = fixture.Context;
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, fixture.Giver.EntityId, 321));
            Assert.IsTrue(context.Manager.Scenes.ExecuteNamed(context.Client, 321, "route"));
            string operation;
            long version;
            using (var unit = context.CreateChar())
            {
                operation = unit.CharacterMissions.Runtime.Effects(fixture.RootId)
                    .Single(effect => effect.OperationKey.StartsWith("shared-", StringComparison.Ordinal)).OperationKey;
                version = unit.CharacterMissions.Runtime.Scene(fixture.RootId).Version;
                if (!changeDuringSave)
                    unit.ExecuteTransaction(() => unit.CharacterMissions.GetByCharacterAndMission(1, 321).Generation++);
            }
            var injected = false;
            if (changeDuringSave)
                context.BeforeSave = database =>
                {
                    if (!injected && database.ChangeTracker.Entries<MissionSceneEntry>().Any(entry =>
                        entry.Entity.RunId == fixture.RootId && entry.State == EntityState.Modified))
                    {
                        injected = true;
                        database.Database.ExecuteSqlRaw(
                            "UPDATE character_mission SET generation = generation + 1 WHERE character_id = 1 AND mission_id = 321");
                    }
                };

            Assert.IsFalse(context.Manager.Scenes.Submit(fixture.RootId,
                new SceneObservation(SceneEventKind.RouteCompleted, 1, Role: "guide", OperationKey: operation)));

            context.BeforeSave = null;
            Assert.AreEqual(changeDuringSave, injected);
            using (var verify = context.CreateChar())
                Assert.AreEqual(version, verify.CharacterMissions.Runtime.Scene(fixture.RootId).Version);
            using (var database = context.Open())
                Assert.AreEqual(0, database.Set<MissionReceiptEntry>()
                    .Count(receipt => receipt.OwnerId == fixture.RootId && receipt.Kind == "WorldResult"));
            context.Manager.Scenes.Reconcile(fixture.RootId);
            if (!changeDuringSave)
                Assert.IsNull(fixture.Guide.Controller.ScriptedMove);
        }

        [TestMethod]
        public void AbandonAndReacceptCannotDeliverTheOldAssignmentsTimedGrantOrSignal()
        {
            var now = DateTime.UnixEpoch;
            using var context = MissionTestContext.WithProgressMission(
                MissionProgressRule.CompleteOnScenarioEvent(321, 1, 1), utcNow: () => now);
            var scenes = context.Manager.Scenes;
            scenes.Bind(321, "data.sequence", TimedGrant());
            var independent = scenes.Start(context.Client, "data.sequence", new SceneBindings("world",
                new Dictionary<string, SceneActorDefinition>(), new Dictionary<string, SceneRoute>(),
                new Dictionary<uint, SceneSequence>
                {
                    [0] = new(timers: new[] { new SequenceTimer("independent", 10000, 1) }),
                    [1] = new()
                }));
            var giver = context.AddNpc(77);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            string previous;
            using (var unit = context.CreateChar())
                previous = unit.CharacterMissions.Runtime.Scenes(1, 321).Single().RunId;
            now = now.AddSeconds(1);

            Assert.IsTrue(context.Manager.TryAbandon(context.Client, 321));
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            now = now.AddSeconds(1);
            scenes.Tick(context.Map);

            Assert.IsFalse(context.Client.AccountEntry.CanSkipBootcamp, "The abandoned timer must not grant account entitlement.");
            Assert.AreEqual(MissionObjectiveState.Incomplete, context.Client.Player.Missions[321].Objectives[1].State,
                "A prior assignment's signal must not advance the replacement assignment.");
            Assert.IsFalse(scenes.Submit(previous, new SceneObservation(SceneEventKind.Signal, 1, SequenceId: 1)));
            using var verify = context.CreateChar();
            Assert.IsFalse(verify.GameAccounts.Get(1).CanSkipBootcamp);
            Assert.IsTrue(verify.CharacterMissions.Runtime.Scene(previous).Generation > 1);
            Assert.AreEqual("Cancelled", verify.CharacterMissions.Runtime.Timer(previous, "grant").Disposition);
            Assert.AreEqual("Pending", verify.CharacterMissions.Runtime.Timer(independent, "independent").Disposition);
        }

        [TestMethod]
        public void FailedAbandonmentTransactionPreservesTheOriginalAssignmentAndItsWork()
        {
            var now = DateTime.UnixEpoch;
            using var context = MissionTestContext.WithProgressMission(
                MissionProgressRule.CompleteOnScenarioEvent(321, 1, 1), utcNow: () => now);
            context.Manager.Scenes.Bind(321, "data.sequence", TimedGrant());
            var giver = context.AddNpc(77);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            context.BeforeSave = database =>
            {
                if (database.ChangeTracker.Entries<MissionSceneEntry>().Any(entry => entry.State == EntityState.Modified))
                    throw new DbUpdateException("Injected cancellation persistence failure.");
            };

            Assert.IsFalse(context.Manager.TryAbandon(context.Client, 321));
            Assert.IsTrue(context.Client.Player.Missions.ContainsKey(321));
            using (var unit = context.CreateChar())
            {
                var scene = unit.CharacterMissions.Runtime.Scenes(1, 321).Single();
                Assert.AreEqual(1U, scene.Generation);
                Assert.AreEqual("Pending", unit.CharacterMissions.Runtime.Timer(scene.RunId, "grant").Disposition);
                Assert.IsNotNull(unit.CharacterMissions.GetByCharacterAndMission(1, 321));
            }
            context.BeforeSave = null;
            now = now.AddSeconds(2);
            context.Manager.Scenes.Tick(context.Map);
            Assert.IsTrue(context.Client.AccountEntry.CanSkipBootcamp);
            Assert.AreEqual(MissionObjectiveState.Completed, context.Client.Player.Missions[321].Objectives[1].State);
        }

        [TestMethod]
        public void AbandonmentCancelsPendingMessagesAndEffectsEvenWhenDiscardPublicationFails()
        {
            var now = DateTime.UnixEpoch;
            var failedPublication = false;
            using var context = MissionTestContext.WithProgressMission(
                MissionProgressRule.CompleteOnScenarioEvent(321, 1, 1), utcNow: () => now,
                beforeMissionPacketPublication: packet =>
                {
                    if (packet is MissionDiscardedPacket)
                    {
                        failedPublication = true;
                        throw new InvalidOperationException("Injected discard publication failure.");
                    }
                });
            context.Manager.Scenes.Bind(321, "data.sequence", TimedGrant(pendingEffect: true));
            var giver = context.AddNpc(77);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            context.BeforeSave = database =>
            {
                if (database.ChangeTracker.Entries<MissionSceneEntry>().Any(entry => entry.State == EntityState.Modified))
                    throw new DbUpdateException("Injected scene input handling failure.");
            };
            context.Manager.TryExecuteScenario(context.Client, 321, 1);
            string previous;
            using (var unit = context.CreateChar())
            {
                previous = unit.CharacterMissions.Runtime.Scenes(1, 321).Single().RunId;
                Assert.IsTrue(unit.CharacterMissions.Runtime.Messages(previous).Any(message => message.Status == "Pending"));
                Assert.IsTrue(unit.CharacterMissions.Runtime.Effects(previous).Any(effect => effect.Status == "Pending"));
            }
            context.BeforeSave = null;

            Assert.IsTrue(context.Manager.TryAbandon(context.Client, 321));
            Assert.IsTrue(failedPublication);
            now = now.AddSeconds(1);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            now = now.AddSeconds(1);
            context.Manager.Scenes.Tick(context.Map);

            Assert.IsFalse(context.Client.AccountEntry.CanSkipBootcamp);
            Assert.AreEqual(MissionObjectiveState.Incomplete, context.Client.Player.Missions[321].Objectives[1].State);
            using var verify = context.CreateChar();
            Assert.IsTrue(verify.CharacterMissions.Runtime.Messages(previous).All(message => message.Status == "Cancelled"));
            Assert.IsTrue(verify.CharacterMissions.Runtime.Effects(previous).All(effect => effect.Status == "Cancelled"));
            Assert.IsFalse(verify.CharacterMissions.Runtime.HasReceipt(previous, 1, "late-grant"));
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void SceneCommitsRecheckTheDurableAssignmentIdentityAndGeneration(bool generationOnly)
        {
            using var context = MissionTestContext.WithProgressMission(
                MissionProgressRule.CompleteOnScenarioEvent(321, 1, 1));
            context.Manager.Scenes.Bind(321, "data.sequence", TimedGrant());
            var giver = context.AddNpc(77);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            string runId;
            using (var unit = context.CreateChar())
            {
                runId = unit.CharacterMissions.Runtime.Scenes(1, 321).Single().RunId;
                unit.ExecuteTransaction(() =>
                {
                    var assignment = unit.CharacterMissions.GetByCharacterAndMission(1, 321);
                    if (generationOnly)
                        assignment.Generation++;
                    else
                        assignment.AssignmentId = Guid.NewGuid().ToString("N");
                });
            }

            Assert.IsFalse(context.Manager.Scenes.Submit(runId,
                new SceneObservation(SceneEventKind.Signal, 1, SequenceId: 1)));
            Assert.IsFalse(context.Client.AccountEntry.CanSkipBootcamp);
            Assert.AreEqual(MissionObjectiveState.Incomplete, context.Client.Player.Missions[321].Objectives[1].State);
        }

        private static SceneBindings TimedGrant(bool pendingEffect = false) =>
            new("unversioned", new Dictionary<string, SceneActorDefinition>
                { ["later"] = new("later", SceneActorKind.Object, (uint)EntityClasses.HumanBaseMale, new ScenePosition(0, 0, 0)) },
                new Dictionary<string, SceneRoute>(),
                new Dictionary<uint, SceneSequence>
                {
                    [0] = new(worldIntents: pendingEffect
                            ? new WorldIntent[] { new SetInteractionIntent("later", "later", false) } : Array.Empty<WorldIntent>(),
                        timers: new[] { new SequenceTimer("grant", 2000, 1) }),
                    [1] = new(characterIntents: new CharacterIntent[] { new SetEntitlementIntent("late-grant", true) },
                        signals: new[] { new SceneMissionSignal(321, 1, 1) })
                });
    }
}
