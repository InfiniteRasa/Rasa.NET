using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions.Scenes
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Game.Missions;
    using Rasa.Game.Missions.World;
    using Rasa.Managers;
    using Rasa.Missions.Scenes;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Mission.Server;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Structures.World;

    [TestClass]
    [DoNotParallelize]
    public class SceneApplicationTests
    {
        [TestMethod]
        [DataRow(false, 0, 1)]
        [DataRow(false, 1, 1)]
        [DataRow(true, 0, 1)]
        [DataRow(true, 1, 1)]
        [DataRow(true, 0, 2)]
        [DataRow(true, 1, 2)]
        [DataRow(true, 2, 2)]
        public void ItemRewardAndIndependentObjectiveConvergeTheFinalSceneAssignment(
            bool itemCounter, int objectivePosition, int grants)
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var scene = MissionDialogueTestContent.Install(harness.WorldContext);
            var world = harness.WorldContext;
            for (uint index = 0; index < grants; index++)
            {
                var itemClassId = index == 0 ? 3147U : 4001U;
                var reward = world.Set<MissionRewardDefinitionEntry>().Single(entry =>
                    entry.MissionId == 339 && entry.RewardId == 401 + index);
                reward.Credits = 0;
                world.Add(new MissionRewardItemEntry
                {
                    MissionId = 339, ContentRevision = MissionDialogueTestContent.Revision,
                    RewardId = 401 + index, ItemId = 1, Kind = MissionRewardItemKind.Fixed,
                    ItemTemplateId = 28 + index, Quantity = index + 1
                });
                world.Add(new MissionObjectiveDefinitionEntry
                {
                    MissionId = 339, ContentRevision = MissionDialogueTestContent.Revision, ObjectiveId = 9 + index,
                    ClientNameTextId = 4318, ClientBodyTextId = 4318, Ordinal = 2 + index,
                    InitialState = (byte)MissionObjectiveState.Incomplete, IsRequired = true
                });
                world.Add(new MissionObjectiveTransitionEntry
                {
                    MissionId = 339, ContentRevision = MissionDialogueTestContent.Revision, ObjectiveId = 9 + index,
                    TransitionId = 1, FromState = (byte)MissionObjectiveState.Incomplete,
                    ToState = (byte)MissionObjectiveState.Completed
                });
                world.Add(new MissionTriggerEntry
                {
                    MissionId = 339, ContentRevision = MissionDialogueTestContent.Revision, ObjectiveId = 9 + index,
                    TransitionId = 1, TriggerId = 1, Kind = MissionTriggerKind.ProgressEvent,
                    EventKind = (byte)MissionProgressEventKind.ItemAcquired, SubjectId = itemClassId,
                    InitialValue = itemCounter ? 0U : null, TargetValue = itemCounter ? index + 1 : null
                });
                harness.Context.AddRewardTemplate(28 + index, itemClassId);
            }
            world.Add(new MissionActionEntry
            {
                MissionId = 339, ContentRevision = MissionDialogueTestContent.Revision, ObjectiveId = 9,
                TransitionId = 1, ActionId = 1, Kind = MissionActionKind.SetPlayerFlag,
                PlayerFlagId = 901, PlayerFlagValue = 0
            });
            var intents = Enumerable.Range(0, grants).Select(index =>
                (CharacterIntent)new GrantRewardIntent($"item-{index}", 339, 401 + (uint)index)).ToList();
            intents.Insert(objectivePosition, new ObjectiveIntent("independent", 339, 8, MissionObjectiveState.Completed));
            intents.Add(new SetCharacterFlagIntent("final-flag", 901, 7));
            intents.Add(new SetCharacterFlagIntent("other-flag", 902, 1));
            scene.Sequences[900] = new() { Character = intents };
            MissionDialogueTestContent.SaveScene(world, scene);
            harness.Manager.LoadMissions();
            Assert.IsTrue(harness.Manager.LoadedMissions[339].IsOperational,
                harness.Manager.LoadedMissions[339].OperationalDiagnostic);
            var giver = harness.AddNpc(77);
            var receiver = harness.AddNpc(88);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, giver.EntityId, 339));
            var assignment = harness.Context.ReadMission(339);
            string runId;
            using (var unit = harness.Context.CreateChar())
                runId = unit.CharacterMissions.Runtime.ScenesForCharacter(harness.Client.Player.Id)
                    .Single(run => run.MissionId == 339).RunId;
            harness.Drain();
            var before = harness.Context.ReadRewardTotals();

            Assert.IsTrue(harness.Manager.Scenes.Submit(runId,
                new SceneObservation(SceneEventKind.Signal, 1, SequenceId: 900)));

            var durable = harness.Context.ReadMission(339);
            var progress = harness.Context.ReadProgress(339).Missions[339];
            Assert.AreEqual(assignment.AssignmentId, durable.AssignmentId);
            Assert.AreEqual(assignment.Generation, durable.Generation);
            Assert.IsTrue(durable.Completeable);
            var objectiveIds = Enumerable.Range(8, grants + 1).Select(value => (uint)value).ToArray();
            foreach (var objective in objectiveIds)
                Assert.AreEqual((byte)MissionObjectiveState.Completed, progress.Objectives[objective].State);
            var runtime = harness.Client.Player.Missions[339];
            Assert.AreEqual(MissionObjectiveState.Completed, runtime.Objectives[8].State,
                "An earlier item progress snapshot cannot undo the later independent objective.");
            foreach (var objective in objectiveIds)
                Assert.AreEqual(MissionObjectiveState.Completed, runtime.Objectives[objective].State);
            Assert.IsTrue(runtime.Completeable);
            Assert.AreEqual(7U, harness.Client.Player.PlayerFlags[901]);
            Assert.AreEqual(1U, harness.Client.Player.PlayerFlags[902]);
            var packets = harness.Drain().ToList();
            CollectionAssert.AreEquivalent(objectiveIds,
                packets.OfType<ObjectiveCompletedPacket>().Where(packet => packet.MissionId == 339)
                    .Select(packet => packet.ObjectiveId).ToArray());
            Assert.IsTrue(packets.OfType<MissionCompleteablePacket>().Last(packet => packet.MissionId == 339).IsCompleteable);
            Assert.AreEqual(1, packets.OfType<PlayerFlagsPacket>().Count());
            Assert.IsTrue(packets.FindIndex(packet => packet is PlayerFlagsPacket) <
                packets.FindIndex(packet => packet is ObjectiveCompletedPacket));
            if (itemCounter)
            {
                for (uint index = 0; index < grants; index++)
                {
                    var itemClassId = index == 0 ? 3147U : 4001U;
                    Assert.AreEqual(index + 1, progress.Objectives[9 + index].ItemCounters[itemClassId]);
                    Assert.AreEqual(index + 1, runtime.Objectives[9 + index].ItemCounters[itemClassId]);
                }
                CollectionAssert.AreEqual(Enumerable.Range(1, grants).Select(value => (uint)value).ToArray(),
                    packets.OfType<UpdateObjectiveItemCounterPacket>().Select(packet => packet.CounterValue).ToArray());
            }
            var after = harness.Context.ReadRewardTotals();
            Assert.AreEqual(before.Experience, after.Experience);
            Assert.AreEqual(before.Credits, after.Credits);
            Assert.AreEqual(before.Prestige, after.Prestige);
            Assert.AreEqual(before.ItemCount + (grants == 1 ? 1 : 3), after.ItemCount);
            Assert.IsTrue(harness.Manager.CompleteOfferedMission(harness.Client, receiver.EntityId, 339, null, null),
                "The committed scene must be immediately turn-in eligible without hydration.");
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, receiver.EntityId, 339, null, null));
            Assert.AreEqual(after, harness.Context.ReadRewardTotals());
            Assert.AreEqual((uint)MissionState.Completed, harness.Context.ReadMission(339).MissionState);
        }

        [TestMethod]
        public void ReconstructionRetryPreservesTheConcretePoseRecoveryInsteadOfRestartingItsOriginalRoute()
        {
            using var fixture = new SharedActorRepeatFixture(resumeAtDestination: true);
            var context = fixture.Context;
            var start = fixture.Guide.Position;
            var original = ReconstructWithFailedAcknowledgement(fixture, "route", "Running",
                BehaviorManager.BehaviorActionScriptedMove, restoredPose: true);
            var destination = SceneRouteController.Position(fixture.RootBindings.Routes["outbound"].Points.Last().Position);
            Assert.AreEqual(destination, fixture.Guide.Position);
            Assert.IsTrue(BehaviorManager.Instance.RestoreScriptedPose(context.Map, fixture.Guide, start, 0));

            fixture.Now = fixture.Now.AddSeconds(1);
            context.Manager.Scenes.Tick(context.Map);

            Assert.AreEqual(destination, fixture.Guide.Position, "Retry the reconstructed pose, not the original moving route.");
            Assert.IsTrue(fixture.Guide.Controller.ScriptedMove.Arrived);
            Assert.IsFalse(fixture.Guide.IsRunning);
            using var verify = context.CreateChar();
            var effect = verify.CharacterMissions.Runtime.Effects(fixture.RootId)
                .Single(entry => entry.OperationKey == original.OperationKey);
            Assert.AreEqual("Applied", effect.Status);
            Assert.IsInstanceOfType<RestoreActorPoseIntent>(System.Text.Json.JsonSerializer.Deserialize<WorldIntent>(effect.Payload));
        }

        [TestMethod]
        [DataRow(true, "assignment")]
        [DataRow(false, "assignment")]
        [DataRow(true, "assignment-generation")]
        [DataRow(false, "assignment-generation")]
        [DataRow(true, "source-generation")]
        [DataRow(false, "source-generation")]
        [DataRow(true, "participant")]
        [DataRow(false, "participant")]
        [DataRow(true, "participant-generation")]
        [DataRow(false, "participant-generation")]
        [DataRow(true, "run-generation")]
        [DataRow(false, "run-generation")]
        [DataRow(true, "run-retired")]
        [DataRow(false, "run-retired")]
        [DataRow(true, "rebound-source-generation")]
        [DataRow(false, "rebound-source-generation")]
        [DataRow(true, "rebound-assignment-generation")]
        [DataRow(false, "rebound-assignment-generation")]
        [DataRow(true, "effect-version")]
        [DataRow(false, "effect-version")]
        [DataRow(true, "effect-status")]
        public void ReconstructionRetryRejectsChangedDurableIdentityInsteadOfRebinding(bool route, string change)
        {
            using var fixture = new SharedActorRepeatFixture();
            var context = fixture.Context;
            var original = ReconstructWithFailedAcknowledgement(fixture, route ? "route" : "follow",
                route ? "Running" : "Applied",
                route ? BehaviorManager.BehaviorActionScriptedMove : BehaviorManager.BehaviorActionFollow);
            var independentMove = fixture.Independent.Controller.ScriptedMove;
            string rootStatus;
            using (var unit = context.CreateChar())
            {
                var store = unit.CharacterMissions.Runtime;
                var assignment = unit.CharacterMissions.GetByCharacterAndMission(1, 321);
                var source = store.Scene(original.SourceRunId);
                var participant = store.Participants(source.RunId).Single();
                var root = store.Scene(fixture.RootId);
                rootStatus = root.Status;
                var effect = store.Effects(fixture.RootId).Single(entry => entry.OperationKey == original.OperationKey);
                unit.ExecuteTransaction(() =>
                {
                    switch (change)
                    {
                        case "assignment": assignment.AssignmentId = Guid.NewGuid().ToString("N"); break;
                        case "assignment-generation": assignment.Generation++; break;
                        case "source-generation": source.Generation++; break;
                        case "participant": participant.Active = false; break;
                        case "participant-generation": participant.AssignmentGeneration++; break;
                        case "run-generation": root.Generation++; break;
                        case "run-retired": root.Status = "Resetting"; break;
                        case "rebound-source-generation":
                            source.Generation++;
                            effect.SourceGeneration = source.Generation;
                            break;
                        case "rebound-assignment-generation":
                            assignment.Generation++;
                            participant.AssignmentGeneration = assignment.Generation;
                            effect.SourceAssignmentGeneration = assignment.Generation;
                            break;
                        case "effect-version": effect.Version++; break;
                        case "effect-status": effect.Status = "Applied"; break;
                        default: throw new ArgumentOutOfRangeException(nameof(change));
                    }
                });
            }

            fixture.Now = fixture.Now.AddSeconds(1);
            context.Manager.Scenes.Tick(context.Map);

            Assert.IsNull(fixture.Guide.Controller.ScriptedMove);
            Assert.IsTrue(fixture.Guide.Controller.ActionFollow.HasAnchor);
            Assert.AreEqual(0U, fixture.Guide.SpawnPool.FollowOwnerCharacterId);
            Assert.AreSame(independentMove, fixture.Independent.Controller.ScriptedMove);
            using (var unit = context.CreateChar())
                unit.ExecuteTransaction(() =>
                {
                    var store = unit.CharacterMissions.Runtime;
                    var assignment = unit.CharacterMissions.GetByCharacterAndMission(1, 321);
                    assignment.AssignmentId = original.SourceAssignmentId;
                    assignment.Generation = original.SourceAssignmentGeneration.Value;
                    store.Scene(original.SourceRunId).Generation = original.SourceGeneration.Value;
                    var participant = store.Participants(original.SourceRunId).Single();
                    participant.Active = true;
                    participant.AssignmentGeneration = original.SourceAssignmentGeneration.Value;
                    var root = store.Scene(fixture.RootId);
                    root.Generation = original.Generation;
                    root.Status = rootStatus;
                });
            fixture.Now = fixture.Now.AddSeconds(1);
            context.Manager.Scenes.Tick(context.Map);
            Assert.IsNull(fixture.Guide.Controller.ScriptedMove, "An invalidated retry cannot reappear when the old identity is restored.");
            Assert.IsTrue(fixture.Guide.Controller.ActionFollow.HasAnchor);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void AssignmentReplacementRetiresAReconstructionRetryWithoutStoppingTheNewAttemptsRoute(bool route)
        {
            using var fixture = new SharedActorRepeatFixture();
            var context = fixture.Context;
            var original = ReconstructWithFailedAcknowledgement(fixture, route ? "route" : "follow",
                route ? "Running" : "Applied",
                route ? BehaviorManager.BehaviorActionScriptedMove : BehaviorManager.BehaviorActionFollow);
            var independentMove = fixture.Independent.Controller.ScriptedMove;
            Assert.IsTrue(context.Manager.TryFailMission(context.Client, 321));
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, fixture.Giver.EntityId, 321));
            Assert.IsTrue(context.Manager.Scenes.ExecuteNamed(context.Client, 321, "route"));
            var currentMove = fixture.Guide.Controller.ScriptedMove;
            Assert.IsNotNull(currentMove);

            fixture.Now = fixture.Now.AddSeconds(1);
            context.Manager.Scenes.Tick(context.Map);
            context.Manager.Scenes.CompleteAssignmentCancellation(new[] { original.SourceRunId });

            Assert.AreSame(currentMove, fixture.Guide.Controller.ScriptedMove);
            Assert.AreSame(independentMove, fixture.Independent.Controller.ScriptedMove);
            Assert.IsTrue(CreatureGameplayRules.IsInvulnerable(fixture.Guide));
            using var verify = context.CreateChar();
            var effects = verify.CharacterMissions.Runtime.Effects(fixture.RootId);
            Assert.AreEqual("Cancelled", effects.Single(effect => effect.OperationKey == original.OperationKey).Status);
            Assert.AreEqual("Running", effects.Single(effect => effect.SourceAssignmentId == context.ReadMission(321).AssignmentId).Status);
            Assert.AreEqual(1U, verify.CharacterMissions.Runtime.Scene(fixture.RootId).Generation);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void RepeatedReconstructionAcknowledgementFailuresStayStoppedUntilTheExactRetryCommits(bool route)
        {
            using var fixture = new SharedActorRepeatFixture();
            var context = fixture.Context;
            var action = route ? BehaviorManager.BehaviorActionScriptedMove : BehaviorManager.BehaviorActionFollow;
            var original = ReconstructWithFailedAcknowledgement(fixture, route ? "route" : "follow",
                route ? "Running" : "Applied", action);
            var injected = false;
            context.BeforeCommand = command =>
            {
                if (!injected && command.Contains("\"mission_world_effect\"", StringComparison.Ordinal) &&
                    fixture.Guide.Controller.CurrentAction == action &&
                    (route || fixture.Guide.SpawnPool.FollowOwnerCharacterId == 1))
                {
                    injected = true;
                    throw new Microsoft.Data.Sqlite.SqliteException("Injected second transient acknowledgement failure.", 5);
                }
            };
            try
            {
                fixture.Now = fixture.Now.AddSeconds(1);
                context.Manager.Scenes.Tick(context.Map);
            }
            finally
            {
                context.BeforeCommand = null;
            }
            Assert.IsTrue(injected);
            Assert.IsNull(fixture.Guide.Controller.ScriptedMove);
            Assert.IsTrue(fixture.Guide.Controller.ActionFollow.HasAnchor);
            using (var unit = context.CreateChar())
                Assert.AreEqual(original.Status, unit.CharacterMissions.Runtime.Effects(fixture.RootId)
                    .Single(effect => effect.OperationKey == original.OperationKey).Status);

            fixture.Now = fixture.Now.AddSeconds(1);
            context.Manager.Scenes.Tick(context.Map);

            if (route)
                Assert.IsNotNull(fixture.Guide.Controller.ScriptedMove);
            else
            {
                Assert.IsFalse(fixture.Guide.Controller.ActionFollow.HasAnchor);
                Assert.AreEqual(1U, fixture.Guide.SpawnPool.FollowOwnerCharacterId);
            }
        }

        [TestMethod]
        [DataRow("route", "Running", BehaviorManager.BehaviorActionScriptedMove, 3U, BehaviorManager.BehaviorActionFollow)]
        [DataRow("route", "Running", BehaviorManager.BehaviorActionScriptedMove, 4U, BehaviorManager.BehaviorActionFighting)]
        [DataRow("follow", "Applied", BehaviorManager.BehaviorActionFollow, 4U, BehaviorManager.BehaviorActionFighting)]
        [DataRow("follow", "Applied", BehaviorManager.BehaviorActionFollow, 5U, BehaviorManager.BehaviorActionScriptedMove)]
        [DataRow("attack", "Applied", BehaviorManager.BehaviorActionFighting, 3U, BehaviorManager.BehaviorActionFollow)]
        [DataRow("attack", "Applied", BehaviorManager.BehaviorActionFighting, 5U, BehaviorManager.BehaviorActionScriptedMove)]
        public void NewRootCommandSupersedesAStoppedReconstructionRetryAcrossControlKinds(
            string control, string status, byte oldAction, uint sequence, byte newAction)
        {
            using var fixture = new SharedActorRepeatFixture();
            var context = fixture.Context;
            ReconstructWithFailedAcknowledgement(fixture, control, status, oldAction);
            var replayed = false;
            context.BeforeCommand = command =>
            {
                if (command.Contains("\"mission_world_effect\"", StringComparison.Ordinal) &&
                    fixture.Guide.Controller.CurrentAction == oldAction &&
                    (control != "follow" || fixture.Guide.SpawnPool.FollowOwnerCharacterId == 1))
                    replayed = true;
            };
            try
            {
                Assert.IsTrue(context.Manager.Scenes.Submit(fixture.RootId,
                    new SceneObservation(SceneEventKind.Signal, 1, SequenceId: sequence)));
            }
            finally
            {
                context.BeforeCommand = null;
            }
            Assert.IsFalse(replayed, "Once a newer root command commits, retry must not revive the superseded world operation.");
            Assert.AreEqual(newAction, fixture.Guide.Controller.CurrentAction);
            var move = fixture.Guide.Controller.ScriptedMove;
            var target = fixture.Guide.Controller.ActionFighting.TargetEntityId;
            var follow = fixture.Guide.Controller.ActionFollow.FollowTargetId;

            fixture.Now = fixture.Now.AddSeconds(1);
            context.Manager.Scenes.Tick(context.Map);

            Assert.AreEqual(newAction, fixture.Guide.Controller.CurrentAction);
            Assert.AreSame(move, fixture.Guide.Controller.ScriptedMove);
            Assert.AreEqual(target, fixture.Guide.Controller.ActionFighting.TargetEntityId);
            Assert.AreEqual(follow, fixture.Guide.Controller.ActionFollow.FollowTargetId);
        }

        [TestMethod]
        [DataRow("route", "Running", BehaviorManager.BehaviorActionScriptedMove)]
        [DataRow("follow", "Applied", BehaviorManager.BehaviorActionFollow)]
        [DataRow("attack", "Applied", BehaviorManager.BehaviorActionFighting)]
        public void ReconstructionAcknowledgementFailureRetriesOnlyTheStoppedOperationOnAnOrdinaryTick(
            string control, string status, byte expectedAction)
        {
            using var fixture = new SharedActorRepeatFixture();
            var context = fixture.Context;
            var original = ReconstructWithFailedAcknowledgement(fixture, control, status, expectedAction);
            var independentMove = fixture.Independent.Controller.ScriptedMove;
            CreatureManager.Instance.SetScenarioInteractionEnabled(context.Map, fixture.Independent, true);
            fixture.Now = fixture.Now.AddMilliseconds(999);
            context.Manager.Scenes.Tick(context.Map);
            Assert.IsNull(fixture.Guide.Controller.ScriptedMove);
            Assert.IsTrue(fixture.Guide.Controller.ActionFollow.HasAnchor);

            fixture.Now = fixture.Now.AddMilliseconds(1);
            context.Manager.Scenes.Tick(context.Map);

            Assert.AreEqual(expectedAction, fixture.Guide.Controller.CurrentAction,
                "An ordinary retry must recover a stopped durable Running/Applied effect, not only Pending effects.");
            if (control == "route")
                Assert.IsNotNull(fixture.Guide.Controller.ScriptedMove);
            else if (control == "follow")
            {
                Assert.IsFalse(fixture.Guide.Controller.ActionFollow.HasAnchor);
                Assert.AreEqual(context.Client.Player.EntityId, fixture.Guide.Controller.ActionFollow.FollowTargetId);
                Assert.AreEqual(1U, fixture.Guide.SpawnPool.FollowOwnerCharacterId);
            }
            else
                Assert.AreEqual(fixture.Independent.EntityId, fixture.Guide.Controller.ActionFighting.TargetEntityId);
            Assert.AreSame(independentMove, fixture.Independent.Controller.ScriptedMove);
            Assert.IsTrue(fixture.Independent.IsInteractable, "Do not replay unrelated already-applied interaction state.");
            using var verify = context.CreateChar();
            var current = verify.CharacterMissions.Runtime.Effects(fixture.RootId)
                .Single(effect => effect.OperationKey == original.OperationKey);
            Assert.AreEqual(status, current.Status);
            Assert.AreEqual(original.Version, current.Version);
            Assert.AreEqual(original.Generation, current.Generation);
            Assert.AreEqual(original.Payload, current.Payload);
            Assert.AreEqual(original.SourceRunId, current.SourceRunId);
            Assert.AreEqual(original.SourceGeneration, current.SourceGeneration);
            Assert.AreEqual(original.SourceAssignmentId, current.SourceAssignmentId);
            Assert.AreEqual(original.SourceAssignmentGeneration, current.SourceAssignmentGeneration);
            Assert.AreEqual("Pending", verify.CharacterMissions.Runtime.Timer(fixture.RootId, "root-wait").Disposition);
        }

        [TestMethod]
        public void LateForwardedWorldReceiptCannotReviveRetiredWorkOrStopTheNewRoute()
        {
            using var fixture = new SharedActorRepeatFixture();
            var context = fixture.Context;
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, fixture.Giver.EntityId, 321));
            context.Manager.Scenes.Detach(1, context.Map);
            Action<string, SceneObservation> deliver = null;
            var scenes = new SceneApplication(context, context.Manager, new ManifestationManager(context),
                worldFactory: observe =>
                {
                    deliver = observe;
                    return new SceneWorldAdapter(context.Manager.PublicActors, observe);
                }, utcNow: () => fixture.Now);
            scenes.Bind(321, "data.sequence", fixture.MissionBindings);
            scenes.Attach(context.Client, fixture.RootId, fixture.RootBindings);
            try
            {
                Assert.IsTrue(scenes.ExecuteNamed(context.Client, 321, "route"));
                string oldOperation;
                using (var unit = context.CreateChar())
                    oldOperation = unit.CharacterMissions.Runtime.Effects(fixture.RootId)
                        .Single(effect => effect.SourceRunId != null).OperationKey;
                var result = new SceneObservation(SceneEventKind.RouteCompleted, 1,
                    Role: "guide", OperationKey: oldOperation);
                deliver(fixture.RootId, result);
                Assert.IsTrue(context.Manager.TryFailMission(context.Client, 321));
                Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, fixture.Giver.EntityId, 321));
                Assert.IsTrue(scenes.ExecuteNamed(context.Client, 321, "route"));
                var currentMove = fixture.Guide.Controller.ScriptedMove;
                Assert.IsNotNull(currentMove);

                deliver(fixture.RootId, result);

                Assert.AreSame(currentMove, fixture.Guide.Controller.ScriptedMove);
                using var verify = context.CreateChar();
                Assert.AreEqual("Cancelled", verify.CharacterMissions.Runtime.Effects(fixture.RootId)
                    .Single(effect => effect.OperationKey == oldOperation).Status);
                Assert.AreEqual(2, verify.CharacterMissions.Runtime.Effects(fixture.RootId).Count(effect => effect.Status == "Running"));
            }
            finally
            {
                scenes.LeaseReset(fixture.RootId);
            }
        }

        [TestMethod]
        public void ForwardedRouteAcknowledgementRechecksItsSourceAfterPersistenceAndRetriesSafely()
        {
            using var fixture = new SharedActorRepeatFixture();
            var context = fixture.Context;
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, fixture.Giver.EntityId, 321));
            var assignment = context.ReadMission(321);
            fixture.DeferGuideSpawn();
            Assert.IsTrue(context.Manager.Scenes.ExecuteNamed(context.Client, 321, "route"));
            fixture.RestoreGuideSpawn();
            var injected = false;
            context.AfterSave = database =>
            {
                if (!injected && database.Set<MissionWorldEffectEntry>().Local.Any(effect =>
                    effect.SourceAssignmentId == assignment.AssignmentId && effect.Status == "Running"))
                {
                    injected = true;
                    database.Database.ExecuteSqlRaw(
                        "UPDATE character_mission SET generation = generation + 1 WHERE character_id = 1 AND mission_id = 321");
                }
            };

            context.Manager.Scenes.Reconcile(fixture.RootId);

            context.AfterSave = null;
            Assert.IsTrue(injected);
            Assert.IsNull(fixture.Guide.Controller.ScriptedMove, "An unacknowledged route cannot keep moving after its source guard rejects.");
            Assert.AreEqual(assignment.Generation, context.ReadMission(321).Generation,
                "The acknowledgement and injected source change must roll back together.");
            using (var unit = context.CreateChar())
                Assert.AreEqual("Pending", unit.CharacterMissions.Runtime.Effects(fixture.RootId)
                    .Single(effect => effect.SourceAssignmentId == assignment.AssignmentId).Status);
            fixture.Now = fixture.Now.AddSeconds(1);
            context.Manager.Scenes.Tick(context.Map);
            Assert.IsNotNull(fixture.Guide.Controller.ScriptedMove);
            using var verify = context.CreateChar();
            Assert.AreEqual("Running", verify.CharacterMissions.Runtime.Effects(fixture.RootId)
                .Single(effect => effect.SourceAssignmentId == assignment.AssignmentId).Status);
        }

        [TestMethod]
        public void AuthoredActorDefeatQueuesOneDurableSequenceWithoutARewardRecipient()
        {
            using var context = MissionTestContext.WithDefinitions(321);
            var app = Application(context, new WorldBoundary());
            var bindings = new SceneBindings("test",
                new Dictionary<string, SceneActorDefinition>
                { ["hostile"] = new("hostile", SceneActorKind.Creature, 77, new ScenePosition(0, 0, 0)) },
                new Dictionary<string, SceneRoute>(),
                new Dictionary<uint, SceneSequence>
                {
                    [0] = new(),
                    [1] = new(characterIntents: new CharacterIntent[]
                    { new SetQualificationIntent("defeat-qualification", (byte)CharacterQualificationKey.BootcampComplete, true) })
                }, defeatSequences: new Dictionary<string, uint> { ["hostile"] = 1 });
            var id = app.Start(context.Client, "data.sequence", bindings);
            var creature = context.AddNpc(77);
            creature.SpawnPool = new SpawnPool
            {
                SceneRunId = id, SceneActorRole = "hostile", SceneGeneration = 1,
                ScenarioOwnerCharacterId = context.Client.Player.Id
            };

            app.RecordDefeat(context.Map, creature, null);
            app.RecordDefeat(context.Map, creature, null);

            Assert.IsTrue(context.Client.Player.StartingExperienceCompleted);
            using var verify = context.CreateChar();
            Assert.HasCount(1, verify.CharacterMissions.Runtime.ActorStates(id));
            var input = verify.CharacterMissions.Runtime.Messages(id).Single();
            Assert.AreEqual("Handled", input.Status);
            Assert.AreEqual(1U, input.SequenceId);
        }

        [TestMethod]
        public void QualificationChangesConvergeTheCharacterRequirementFactAfterCommit()
        {
            using var context = MissionTestContext.WithDefinitions(321);
            var app = Application(context, new WorldBoundary());
            var bindings = new SceneBindings("test",
                new Dictionary<string, SceneActorDefinition>(),
                new Dictionary<string, SceneRoute>(),
                new Dictionary<uint, SceneSequence>
                {
                    [0] = new(characterIntents: new CharacterIntent[]
                    {
                        new SetQualificationIntent("graduate", (byte)CharacterQualificationKey.BootcampComplete, true)
                    }),
                    [1] = new(characterIntents: new CharacterIntent[]
                    {
                        new SetQualificationIntent("remove-graduation", (byte)CharacterQualificationKey.BootcampComplete, false)
                    })
                });
            var id = app.Start(context.Client, "data.sequence", bindings);
            Assert.IsTrue(context.Client.Player.StartingExperienceCompleted);
            using (var unit = context.CreateChar())
                Assert.IsTrue(Rasa.Game.Missions.Persistence.MissionRequirementFactsAdapter
                    .HasCompletedStartingExperience(unit, context.Client.Player.Id));

            Assert.IsTrue(app.Submit(id, new SceneObservation(SceneEventKind.Signal, 1, SequenceId: 1)));
            Assert.IsFalse(context.Client.Player.StartingExperienceCompleted);
            using (var unit = context.CreateChar())
                Assert.IsFalse(Rasa.Game.Missions.Persistence.MissionRequirementFactsAdapter
                    .HasCompletedStartingExperience(unit, context.Client.Player.Id));
        }

        [TestMethod]
        public void ResettingScenesCannotReattachOrExecuteTheirOldGeneration()
        {
            using var context = MissionTestContext.WithDefinitions(321);
            var app = Application(context, new WorldBoundary());
            var id = app.Start(context.Client, "data.sequence", Bindings());
            using (var unit = context.CreateChar())
                unit.ExecuteTransaction(() =>
                {
                    var scene = unit.CharacterMissions.Runtime.Scene(id);
                    scene.Status = "Resetting"; scene.Generation++; scene.Version++;
                });
            app.Attach(context.Client, id, Bindings());
            Assert.IsFalse(app.Submit(id, new SceneObservation(SceneEventKind.Signal, 1, SequenceId: 1)));
            Assert.IsFalse(app.Tick(context.Map));
        }

        [TestMethod]
        public void ReacceptedMissionDispatchesOnlyToItsNewAssignmentScene()
        {
            using var context = MissionTestContext.WithDefinitions(321);
            var app = Application(context, new WorldBoundary());
            app.Bind(321, "data.sequence", Bindings());
            var giver = context.AddNpc(77);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            Assert.IsTrue(app.Execute(context.Client, 321, 0, started: true));
            Assert.IsTrue(context.Manager.TryAbandon(context.Client, 321));
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            Assert.IsTrue(app.Execute(context.Client, 321, 0, started: true));
            Assert.IsTrue(app.Execute(context.Client, 321, 1));
            using var unit = context.CreateChar();
            var assignment = unit.CharacterMissions.GetByCharacterAndMission(1, 321);
            var current = unit.CharacterMissions.Runtime.AssignmentScene(assignment.AssignmentId);
            Assert.AreEqual(2, unit.CharacterMissions.Runtime.Scenes(1, 321).Count);
            Assert.AreEqual(1, unit.CharacterMissions.Runtime.Messages(current.RunId).Count(message => message.Status == "Handled"));
        }

        [TestMethod]
        public void EffectAcknowledgmentCannotResurrectACancelledGeneration()
        {
            using var context = MissionTestContext.WithDefinitions(321);
            var world = new WorldBoundary
            {
                BeforeApply = run =>
                {
                    using var unit = context.CreateChar();
                    unit.ExecuteTransaction(() =>
                    {
                        var scene = unit.CharacterMissions.Runtime.Scene(run.Id);
                        scene.Generation++; scene.Version++; scene.Status = "Resetting";
                        foreach (var effect in unit.CharacterMissions.Runtime.Effects(run.Id))
                            effect.Status = "Cancelled";
                    });
                }
            };
            var app = Application(context, world);
            var id = app.Start(context.Client, "data.sequence", Bindings());
            using var verify = context.CreateChar();
            Assert.AreEqual("Cancelled", verify.CharacterMissions.Runtime.Effects(id).Single().Status);
        }

        [TestMethod]
        public void FailedWorldApplicationRemainsPendingAndRetriesWithoutRecommittingTheScene()
        {
            using var context = MissionTestContext.WithDefinitions(321);
            var world = new WorldBoundary { Fail = true };
            var app = Application(context, world);
            var id = app.Start(context.Client, "data.sequence", Bindings());
            using (var unit = context.CreateChar())
            {
                Assert.AreEqual("Pending", unit.CharacterMissions.Runtime.Effects(id).Single().Status);
                Assert.AreEqual(1L, unit.CharacterMissions.Runtime.Scene(id).Version);
            }
            world.Fail = false;
            app.Reconcile(id);
            using (var unit = context.CreateChar())
            {
                Assert.AreEqual("Applied", unit.CharacterMissions.Runtime.Effects(id).Single().Status);
                Assert.AreEqual(1L, unit.CharacterMissions.Runtime.Scene(id).Version);
            }
        }

        [TestMethod]
        public void DeferredWorldApplicationRetriesWithoutLoggingAFailureOrRecommittingTheScene()
        {
            using var context = MissionTestContext.WithDefinitions(321);
            var now = DateTime.UnixEpoch;
            var world = new WorldBoundary { Defer = true };
            var app = Application(context, world, () => now);
            var originalOutput = Console.Out;
            using var output = new StringWriter();
            Console.SetOut(output);
            try
            {
                var id = app.Start(context.Client, "data.sequence", Bindings());
                for (var retry = 0; retry < 3; retry++)
                {
                    now = now.AddSeconds(1);
                    app.Tick(context.Map);
                }
                using (var unit = context.CreateChar())
                {
                    var effect = unit.CharacterMissions.Runtime.Effects(id).Single();
                    Assert.AreEqual("Pending", effect.Status);
                    Assert.IsNull(effect.Failure);
                    Assert.AreEqual(1L, unit.CharacterMissions.Runtime.Scene(id).Version);
                }
                world.Defer = false;
                now = now.AddSeconds(1);

                app.Tick(context.Map);

                using var verify = context.CreateChar();
                Assert.AreEqual("Applied", verify.CharacterMissions.Runtime.Effects(id).Single().Status);
                Assert.AreEqual(1L, verify.CharacterMissions.Runtime.Scene(id).Version);
            }
            finally
            {
                Console.SetOut(originalOutput);
            }
            Assert.IsFalse(output.ToString().Contains("remains pending", StringComparison.Ordinal), output.ToString());
        }

        [TestMethod]
        public void IdleSceneTicksDoNotOpenCharacterStorage()
        {
            using var context = MissionTestContext.WithDefinitions(321);
            var app = Application(context, new WorldBoundary());
            app.Start(context.Client, "data.sequence", Bindings());
            var baseline = context.CharUnitsCreated;
            for (var tick = 0; tick < 1000; tick++)
                app.Tick(context.Map);
            Assert.AreEqual(baseline, context.CharUnitsCreated);
        }

        [TestMethod]
        public void ReconnectPreservesWallClockAndResumesAnActiveSceneWait()
        {
            using var context = MissionTestContext.WithDefinitions(321);
            var now = DateTime.UnixEpoch;
            var world = new WorldBoundary();
            var bindings = Bindings(timed: true);
            var app = Application(context, world, () => now);
            var id = app.Start(context.Client, "data.sequence", bindings);
            now = now.AddSeconds(1);
            app.Detach(1, context.Map);
            now = now.AddSeconds(100);
            app = Application(context, world, () => now);
            app.Attach(context.Client, id, bindings);
            using var unit = context.CreateChar();
            var timers = unit.CharacterMissions.Runtime.Timers(id);
            Assert.AreEqual(DateTime.UnixEpoch.AddSeconds(600), timers.Single(timer => timer.Name == "deadline").DueAtUtc);
            Assert.AreEqual(now.AddSeconds(1), timers.Single(timer => timer.Name == "dialogue").DueAtUtc);
            Assert.AreEqual("Pending", timers.Single(timer => timer.Name == "dialogue").Disposition);
        }

        [TestMethod]
        public void JournalCleanupDoesNotDeleteSceneCheckpointsOrWorldReceipts()
        {
            using var context = MissionTestContext.WithDefinitions(321);
            context.SeedMission(1, 321, (uint)MissionState.Completed, false);
            context.ReloadPlayerMissions();
            var app = Application(context, new WorldBoundary());
            var id = app.Start(context.Client, "data.sequence", Bindings(), 321);
            Assert.IsTrue(context.Manager.TryClear(context.Client, 321));
            using var unit = context.CreateChar();
            Assert.IsNotNull(unit.CharacterMissions.Runtime.Scene(id));
            Assert.AreEqual("Applied", unit.CharacterMissions.Runtime.Effects(id).Single().Status);
            Assert.AreEqual(1, unit.CharacterMissions.Runtime.History(1).Count);
        }

        private static MissionWorldEffectEntry ReconstructWithFailedAcknowledgement(
            SharedActorRepeatFixture fixture, string control, string status, byte expectedAction, bool restoredPose = false)
        {
            var context = fixture.Context;
            fixture.Guide.Faction = Factions.Bane;
            fixture.Independent.Faction = Factions.AFS;
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, fixture.Giver.EntityId, 321));
            Assert.IsTrue(context.Manager.Scenes.ExecuteNamed(context.Client, 321, control));
            Assert.IsTrue(context.Manager.Scenes.Submit(fixture.RootId,
                new SceneObservation(SceneEventKind.Signal, 1, SequenceId: 6)));
            MissionWorldEffectEntry original;
            using (var unit = context.CreateChar())
            {
                original = unit.CharacterMissions.Runtime.Effects(fixture.RootId).Single(effect => effect.SourceRunId != null);
                Assert.AreEqual(status, original.Status, "The failed acknowledgement must start from durable non-Pending work.");
            }
            context.Manager.Scenes.Detach(1, context.Map);
            fixture.DeferGuideSpawn();
            fixture.RestoreGuideSpawn();
            fixture.Guide.Faction = Factions.Bane;
            var injected = false;
            context.BeforeCommand = command =>
            {
                if (!injected && command.Contains("\"mission_world_effect\"", StringComparison.Ordinal) &&
                    fixture.Guide.Controller.CurrentAction == expectedAction &&
                    (control != "follow" || fixture.Guide.SpawnPool.FollowOwnerCharacterId == 1))
                {
                    injected = true;
                    throw new Microsoft.Data.Sqlite.SqliteException("Injected transient reconstruction acknowledgement failure.", 5);
                }
            };
            var previousOutput = Console.Out;
            using var output = new StringWriter();
            Console.SetOut(output);
            try
            {
                context.Manager.Scenes.Attach(context.Client, fixture.RootId, fixture.RootBindings);
            }
            finally
            {
                context.BeforeCommand = null;
                Console.SetOut(previousOutput);
            }
            Assert.IsTrue(injected, "Inject only after the real world adapter has restarted the target control.");
            StringAssert.Contains(output.ToString(), $"effect {original.OperationKey} acknowledgement failed");
            if (restoredPose)
                Assert.IsTrue(fixture.Guide.Controller.ScriptedMove?.Arrived == true);
            else
            {
                Assert.IsNull(fixture.Guide.Controller.ScriptedMove);
                Assert.IsTrue(fixture.Guide.Controller.ActionFollow.HasAnchor, "The unacknowledged world operation must be stopped.");
            }
            using var verify = context.CreateChar();
            Assert.AreEqual(status, verify.CharacterMissions.Runtime.Effects(fixture.RootId)
                .Single(effect => effect.OperationKey == original.OperationKey).Status,
                "A failed acknowledgement cannot change the previously committed durable status.");
            return original;
        }

        private static SceneApplication Application(MissionTestContext context, WorldBoundary world, Func<DateTime> now = null) =>
            new(context, context.Manager, new ManifestationManager(context),
                worldFactory: _ => world, utcNow: now ?? (() => DateTime.UnixEpoch));

        private static SceneBindings Bindings(bool timed = false) => new("test",
            new Dictionary<string, SceneActorDefinition>
            { ["guide"] = new("guide", SceneActorKind.Creature, 77, new ScenePosition(0, 0, 0)) },
            new Dictionary<string, SceneRoute>(),
            new Dictionary<uint, SceneSequence>
            {
                [0] = new(new WorldIntent[] { new EnsureActorIntent("guide", "guide") },
                    timers: timed ? new[]
                    {
                        new SequenceTimer("deadline", 600000, 1),
                        new SequenceTimer("dialogue", 2000, 2, SceneClockPolicy.ActiveScene)
                    } : Array.Empty<SequenceTimer>()),
                [1] = new(),
                [2] = new()
            });

        private sealed class WorldBoundary : ISceneWorld
        {
            internal bool Fail { get; set; }
            internal bool Defer { get; set; }
            internal Action<SceneRun> BeforeApply { get; set; }
            public void Attach(SceneRun run, SceneBindings bindings, Client owner, MapChannel map) { }
            public WorldEffectResult Apply(SceneRun run, WorldIntent intent)
            {
                BeforeApply?.Invoke(run);
                return Fail ? WorldEffectResult.Failed("Injected world failure") :
                    Defer ? WorldEffectResult.Deferred() : WorldEffectResult.Applied();
            }
            public void Tick(MapChannel map, DateTime utcNow) { }
            public void Detach(string runId) { }
            public void Pause(string runId) { }
            public void CancelOperation(string runId, uint generation, string operationKey) { }
            public void Terminate(SceneRun run, MapChannel map) { }
        }
    }
}
