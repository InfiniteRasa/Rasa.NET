using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Missions.Content;
using Rasa.Missions.Content.Wilderness;
using Rasa.Missions.Scenes;
using Rasa.Packets.Inventory.Client;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.MapChannel.Server;
using Rasa.Packets.Mission.Server;
using Rasa.Services.Preloader.Missions.Wilderness;
using Rasa.Structures;
using Rasa.Structures.Char;
using Rasa.Structures.World;

namespace Rasa.Test.Missions.Wilderness
{
    [TestClass]
    [DoNotParallelize]
    public class WildernessEscapeVelocityTests
    {
        [TestMethod]
        public void NativePierreAcceptanceStartsExactly420SecondsWithoutReleasingOrCompletingHer()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var acceptedAt = harness.UtcNow;
            var pierre = Accept(harness);

            Assert.AreEqual(3097U, pierre.NameId);
            Assert.IsNull(pierre.SpawnPool.ScenarioKey, "Pierre must remain the real, named public actor.");
            Assert.IsFalse(harness.Map.IsPrivateInstance);
            Assert.IsFalse(pierre.IsInteractable);
            Assert.IsNull(pierre.Controller.ScriptedMove, "Acceptance must not release Pierre through the forcefield.");
            Assert.IsNotNull(harness.Manager.PublicActors.Handle(harness.Map, pierre.SpawnPool.DbId));
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[666].Objectives[1].State);
            Assert.IsFalse(harness.Client.Player.Missions[666].Completeable);
            Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(665),
                "Discovery of Pierre at the cache must not require Cache of the Day.");
            using var unit = harness.CreateChar();
            var deadline = unit.CharacterMissionDeadlines.Get(harness.Client.Player.Id, 666);
            Assert.IsNotNull(deadline);
            Assert.AreEqual(acceptedAt.AddSeconds(420), deadline.DueAtUtc);
            Assert.AreEqual(CharacterMissionDeadlineState.Active, deadline.State);
        }

        [TestMethod]
        public void TalkingToRodriguezCannotReplaceReleaseEscortAndBoarding()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pierre = Accept(harness);
            var rodriguez = harness.Npc(172);
            Assert.IsNotNull(rodriguez);
            var npcs = new NpcManager(harness, harness.Manager);
            harness.MoveTo(rodriguez.Position);
            npcs.RequestNpcConverse(harness.Client, new RequestNPCConversePacket { EntityId = rodriguez.EntityId });
            npcs.CompleteNPCObjective(harness.Client, new CompleteNPCObjectivePacket
            {
                EntityId = rodriguez.EntityId, MissionId = 666, ObjectiveId = 1, PlayerFlagId = 1
            });
            npcs.CompleteNPCMission(harness.Client,
                new CompleteNPCMissionPacket { EntityId = rodriguez.EntityId, MissionId = 666, SelectionIdx = 0 });

            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[666].Objectives[1].State);
            Assert.IsFalse(harness.Client.Player.Missions[666].Completeable);
            Assert.IsNull(pierre.Controller.ScriptedMove);
        }

        [TestMethod]
        public void NativeForcefieldDestructionRequiresItsOwnerAndDoesNotCompleteTheEscort()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pierre = Accept(harness);
            var field = Forcefield(harness);
            var second = harness.Context.CreateAdditionalClient(2, manager: harness.Manager);
            try
            {
                second.SetWorldPosition(field.Position, 0);
                CellManager.Instance.UpdateVisibility(second);
                DamageForcefield(harness, field, 100, second);
                Assert.AreEqual(100U, field.CurrentHitPoints);
                Assert.IsNull(pierre.Controller.ScriptedMove);

                ReleaseForcefield(harness);

                Assert.IsFalse(field.IsEnabled);
                Assert.AreEqual(UseObjectState.FfStateFactionBDestroyed, field.StateId);
                Assert.IsNotNull(pierre.Controller.ScriptedMove);
                Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[666].Objectives[1].State);
                Assert.IsFalse(harness.Client.Player.Missions[666].Completeable);
                DamageForcefield(harness, field, 100);
                Assert.AreEqual(0U, field.CurrentHitPoints);
                Assert.AreEqual(UseObjectState.FfStateFactionBDestroyed, field.StateId);
            }
            finally
            {
                CellManager.Instance.RemoveFromWorld(second);
                harness.Map.ClientList.Remove(second);
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void RealPublicPierreMustReachTheLzAndThenBoardBeforeRodriguezCanReward(bool fullEquipmentBag)
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pierre = Accept(harness);
            var start = pierre.Position;
            var spawnId = pierre.SpawnPool.DbId;
            var oldLease = harness.Manager.PublicActors.Handle(harness.Map, spawnId);
            var binding = RescueBinding(harness);
            AssertGroundedRoute(harness, start, binding.Routes["landing-zone"]);
            var landing = Position(binding.Routes["landing-zone"].Points.Last().Position);
            AssertGroundedRoute(harness, landing, binding.Routes["boarding"]);
            var boarding = Position(binding.Routes["boarding"].Points.Last().Position);
            Assert.AreEqual(new Vector3(192.2f, 171.1f, -100.5f), boarding);
            Assert.IsTrue(Vector3.Distance(landing, boarding) > 0.5f,
                "Boarding must be a real movement stage, not a second callback at the L.Z. arrival point.");
            ReleaseForcefield(harness);
            DriveUntil(harness, pierre, () => Vector3.Distance(pierre.Position, landing) < 0.5f);
            Assert.IsFalse(harness.Client.Player.Missions[666].Completeable,
                "Reaching the L.Z. must not skip the separate boarding movement.");
            DriveUntil(harness, pierre, () => harness.Client.Player.Missions[666].Completeable);

            Assert.IsTrue(Vector3.Distance(start, pierre.Position) > 100);
            Assert.IsTrue(Vector2.Distance(new Vector2(boarding.X, boarding.Z),
                new Vector2(pierre.Position.X, pierre.Position.Z)) <= 0.6f);
            Assert.IsTrue(Math.Abs(pierre.Position.Y - boarding.Y) <= 0.35f,
                "The ground under the raised pad must not count as boarding.");
            Assert.AreSame(pierre, EntityManager.Instance.GetCreature(pierre.EntityId));
            Assert.AreEqual(630070U, pierre.SpawnPool.DbId);
            Assert.IsTrue(pierre.Attributes[Attributes.Health].Current > 0);
            Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[666].Objectives[1].State);
            using (var unit = harness.CreateChar())
                Assert.AreEqual(CharacterMissionDeadlineState.Satisfied,
                    unit.CharacterMissionDeadlines.Get(harness.Client.Player.Id, 666).State);
            AwaitRelease(harness, spawnId);
            Assert.IsFalse(harness.Manager.PublicActors.TryResolve(harness.Map, oldLease, out _));
            Assert.IsTrue(harness.Npc(spawnId).IsInteractable);
            var second = harness.Context.CreateAdditionalClient(2, manager: harness.Manager);
            try
            {
                second.SetWorldPosition(harness.Npc(spawnId).Position, 0);
                CellManager.Instance.UpdateVisibility(second);
                var npcs = new NpcManager(harness, harness.Manager);
                npcs.RequestNpcConverse(second, new RequestNPCConversePacket { EntityId = harness.Npc(spawnId).EntityId });
                npcs.AssignNPCMission(second, new AssignNPCMissionPacket
                {
                    NpcEntityId = harness.Npc(spawnId).EntityId, MissionId = 666
                });
                Assert.AreEqual(MissionState.Active, second.Player.Missions[666].State);
                var nextLease = harness.Manager.PublicActors.Handle(harness.Map, spawnId);
                Assert.AreNotEqual(oldLease.RunId, nextLease.RunId);

                if (fullEquipmentBag)
                {
                    var filler = ItemManager.Instance.GetItemTemplateById(28692);
                    var selected = ItemManager.Instance.GetItemTemplateById(13744);
                    Assert.AreEqual(selected.InventoryCategory, filler.InventoryCategory);
                    Assert.AreEqual(1U, EntityClassManager.Instance.GetClassInfo(filler.Class).ItemClassInfo.StackSize);
                    using var grant = new InventoryManager.InventoryGrant();
                    using (var unit = harness.CreateChar())
                        unit.ExecuteTransaction(() => grant.PlanAndSave(harness.Client,
                            new[] { new InventoryManager.InventoryItemGrant(28692, 50) }, unit));
                    grant.Publish(harness.Client);
                }
                var rodriguez = harness.Npc(172);
                harness.MoveTo(rodriguez.Position);
                npcs.RequestNpcConverse(harness.Client, new RequestNPCConversePacket { EntityId = rodriguez.EntityId });
                var before = harness.Context.ReadRewardTotals();
                harness.Drain();
                npcs.CompleteNPCMission(harness.Client,
                    new CompleteNPCMissionPacket { EntityId = rodriguez.EntityId, MissionId = 666, SelectionIdx = 0 });
                if (fullEquipmentBag)
                {
                    Assert.AreEqual(before, harness.Context.ReadRewardTotals());
                    Assert.AreEqual(MissionState.Active, harness.Client.Player.Missions[666].State);
                    Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[666].Objectives[1].State);
                    Assert.IsTrue(harness.Client.Player.Missions[666].Completeable);
                    using (var unit = harness.CreateChar())
                        Assert.AreEqual(CharacterMissionDeadlineState.Satisfied,
                            unit.CharacterMissionDeadlines.Get(harness.Client.Player.Id, 666).State);
                    Assert.AreEqual(nextLease, harness.Manager.PublicActors.Handle(harness.Map, spawnId),
                        "A failed reward must not affect the next player's reservation.");
                    var failedRewardPackets = harness.Drain();
                    Assert.IsFalse(failedRewardPackets.OfType<MissionRewardedPacket>().Any());
                    Assert.IsFalse(failedRewardPackets.OfType<MissionCompletedPacket>().Any());
                    var filler = harness.Client.Player.Inventory.PersonalInventory.Where(id => id != 0)
                        .Select(EntityManager.Instance.GetItem).First(item => item.ItemTemplate.ItemTemplateId == 28692);
                    InventoryManager.Instance.PersonalInventory_DestroyItem(harness.Client,
                        new PersonalInventory_DestroyItemPacket { EntityId = filler.EntityId, Quantity = 1 });
                    npcs.RequestNpcConverse(harness.Client, new RequestNPCConversePacket { EntityId = rodriguez.EntityId });
                    npcs.CompleteNPCMission(harness.Client,
                        new CompleteNPCMissionPacket { EntityId = rodriguez.EntityId, MissionId = 666, SelectionIdx = 0 });
                }
                Assert.AreEqual(MissionState.Completed, harness.Client.Player.Missions[666].State);
                Assert.AreEqual(before.Credits + 1400, harness.Context.ReadRewardTotals().Credits);
                Assert.AreEqual(before.Experience + 14000, harness.Context.ReadRewardTotals().Experience);
                using (var unit = harness.CreateChar())
                    Assert.AreEqual(1, unit.CharacterInventories.GetItems(harness.Client.AccountEntry.Id)
                        .Where(row => row.CharacterId == harness.Client.Player.Id)
                        .Select(row => unit.Items.GetItem(row.ItemId)).Count(item => item.ItemTemplateId == 13744));
                var rewarded = harness.Context.ReadRewardTotals();
                npcs.CompleteNPCMission(harness.Client,
                    new CompleteNPCMissionPacket { EntityId = rodriguez.EntityId, MissionId = 666, SelectionIdx = 0 });
                Assert.AreEqual(rewarded, harness.Context.ReadRewardTotals());
                Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count());
                Assert.AreEqual(nextLease, harness.Manager.PublicActors.Handle(harness.Map, spawnId),
                    "The first player's turn-in must not release the next player's public encounter.");
            }
            finally
            {
                CellManager.Instance.RemoveFromWorld(second);
                harness.Map.ClientList.Remove(second);
            }
        }

        [TestMethod]
        public void ThePlayerStandingOnTheBoardingPadDoesNotSubstituteForPierre()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pierre = Accept(harness);
            ReleaseForcefield(harness);
            var boarding = new Vector3(192.2f, 171.1f, -100.5f);

            harness.MoveTo(boarding);
            harness.Tick();

            Assert.IsTrue(Vector3.Distance(pierre.Position, boarding) > 100);
            Assert.IsFalse(harness.Client.Player.Missions[666].Completeable);
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[666].Objectives[1].State);
        }

        [TestMethod]
        [DataRow(0.5f, 0.3f, true)]
        [DataRow(0.601f, 0f, false)]
        [DataRow(0f, 0.351f, false)]
        public void BoardingDecisionUsesSeparateHorizontalAndFootHeightLimits(float horizontal, float height, bool allowed)
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pierre = Accept(harness);
            var binding = RescueBinding(harness);
            var lease = harness.Manager.PublicActors.Handle(harness.Map, pierre.SpawnPool.DbId);
            var run = new SceneRun(lease.RunId, WildernessMissionDataV1.Revision, binding.Script, 1,
                lease.Generation, 0, JsonSerializer.Serialize(
                    new WildernessEscapeVelocityScene.Checkpoint("boarding", harness.UtcNow.AddSeconds(420))),
                SceneStatus.Running, harness.Client.Player.Id, 666);
            var observation = new SceneObservation(SceneEventKind.RouteCompleted, lease.Generation,
                Role: "pierre", OperationKey: "board-dropship",
                Position: new ScenePosition(192.2f + horizontal, 171.1f + height, -100.5f));

            var decision = new WildernessEscapeVelocityScene().Handle(
                new SceneContext(run, binding.Bindings(WildernessMissionDataV1.Revision), harness.UtcNow), observation);

            Assert.AreEqual(allowed, decision.Signals.Any(signal => signal.MissionId == 666 &&
                signal.SequenceId == 1 && signal.EventId == 2));
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[666].Objectives[1].State,
                "A standalone spatial-policy decision is not a committed world arrival.");
        }

        [TestMethod]
        public void TheDepartureDeadlineWinsWhenBoardingMovementArrivesLate()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pierre = Accept(harness);
            var due = harness.UtcNow.AddSeconds(420);
            var landing = Position(RescueBinding(harness).Routes["landing-zone"].Points.Last().Position);
            ReleaseForcefield(harness);
            DriveUntil(harness, pierre, () => Vector3.Distance(pierre.Position, landing) < 0.5f);
            Assert.IsFalse(harness.Client.Player.Missions[666].Completeable);
            harness.UtcNow = due;

            harness.Tick(250);

            Assert.AreEqual(MissionState.Failed, harness.Client.Player.Missions[666].State);
            Assert.IsFalse(harness.Client.Player.Missions[666].Completeable);
            Assert.AreEqual(0, harness.Drain().OfType<MissionRewardedPacket>().Count());
        }

        [TestMethod]
        public void ReconnectingAt419SecondsPreservesTheOriginalDeadlineAndExpiresAt420()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            Accept(harness);
            var assignment = harness.Client.Player.Missions[666].AssignmentId;
            var due = harness.UtcNow.AddSeconds(420);
            harness.UtcNow = due.AddSeconds(-1);
            var reconnected = harness.Context.CreateCompetingClient(harness.Manager);
            harness.Manager.PublishInitialState(reconnected);
            Assert.AreEqual(assignment, reconnected.Player.Missions[666].AssignmentId);
            Assert.AreEqual(MissionState.Active, reconnected.Player.Missions[666].State);
            using (var unit = harness.CreateChar())
                Assert.AreEqual(due, unit.CharacterMissionDeadlines.Get(reconnected.Player.Id, 666).DueAtUtc);
            MissionTestContext.Drain(reconnected);

            harness.UtcNow = due;
            Assert.IsTrue(harness.Manager.EvaluateDeadlines(reconnected));
            Assert.IsFalse(harness.Manager.EvaluateDeadlines(reconnected));

            Assert.AreEqual(MissionState.Failed, reconnected.Player.Missions[666].State);
            Assert.IsFalse(reconnected.Player.Missions[666].Completeable);
            var packets = MissionTestContext.Drain(reconnected);
            Assert.AreEqual(1, packets.OfType<MissionFailedPacket>().Count());
            Assert.AreEqual(1, packets.OfType<ObjectiveFailedPacket>().Count());
            Assert.AreEqual(0, packets.OfType<MissionRewardedPacket>().Count());
            using var verify = harness.CreateChar();
            Assert.AreEqual(CharacterMissionDeadlineState.Expired,
                verify.CharacterMissionDeadlines.Get(reconnected.Player.Id, 666).State);
        }

        [TestMethod]
        public void PierreDeathWhileStillCaptiveFailsTheAttemptAndReleasesHerReplacement()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pierre = Accept(harness);
            var spawnId = pierre.SpawnPool.DbId;
            var oldLease = harness.Manager.PublicActors.Handle(harness.Map, spawnId);
            Assert.IsNull(pierre.Controller.ScriptedMove);

            pierre.Attributes[Attributes.Health].Current = 0;
            harness.Creatures.HandleCreatureKill(harness.Map, pierre, null);
            harness.Tick(1);

            Assert.AreEqual(MissionState.Failed, harness.Client.Player.Missions[666].State,
                "Public leased death must reach the rescue scene even before any route is running.");
            Assert.IsFalse(harness.Client.Player.Missions[666].Completeable);
            AwaitRelease(harness, spawnId);
            var replacement = harness.Npc(spawnId);
            Assert.IsNotNull(replacement);
            Assert.AreEqual(3097U, replacement.NameId);
            Assert.AreNotEqual(pierre.EntityId, replacement.EntityId);
            Assert.IsTrue(replacement.IsInteractable);
            Assert.IsFalse(harness.Manager.PublicActors.TryResolve(harness.Map, oldLease, out _));
            Assert.IsFalse(harness.Manager.Scenes.Submit(oldLease.RunId,
                new SceneObservation(SceneEventKind.RouteCompleted, oldLease.Generation,
                    Role: "pierre", OperationKey: "board-dropship")));
        }

        [TestMethod]
        public void PierreDeathDuringTheEscortFailsWithoutGrantingArrivalCredit()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pierre = Accept(harness);
            ReleaseForcefield(harness);
            var start = pierre.Position;
            DriveUntil(harness, pierre, () => Vector3.Distance(start, pierre.Position) > 2);

            pierre.Attributes[Attributes.Health].Current = 0;
            harness.Creatures.HandleCreatureKill(harness.Map, pierre, null);
            harness.Tick(1);

            Assert.AreEqual(MissionState.Failed, harness.Client.Player.Missions[666].State);
            Assert.IsFalse(harness.Client.Player.Missions[666].Completeable);
            Assert.AreEqual(0, harness.Drain().OfType<MissionRewardedPacket>().Count());
            AwaitRelease(harness, pierre.SpawnPool.DbId);
        }

        [TestMethod]
        public void ZeroHealthPierreCannotReceiveBoardingCreditBeforeDeathPublication()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pierre = Accept(harness);
            ReleaseForcefield(harness);
            var boarding = new Vector3(192.2f, 171.1f, -100.5f);
            DriveUntil(harness, pierre, () => Vector3.Distance(pierre.Position, boarding) < 2);
            Assert.IsFalse(harness.Client.Player.Missions[666].Completeable);
            pierre.Attributes[Attributes.Health].Current = 0;
            pierre.State = CharacterState.Dying;

            for (var tick = 0; tick < 8 && harness.Client.Player.Missions[666].State == MissionState.Active; tick++)
                harness.Tick();

            Assert.IsFalse(harness.Client.Player.Missions[666].Completeable,
                "A zero-health actor is not a living boarding passenger while its death notification is pending.");
            Assert.AreEqual(0, harness.Drain().OfType<MissionRewardedPacket>().Count());
        }

        [TestMethod]
        public void OwnerLossFailsAndReleasesPierreInsteadOfStrandingTheNextPlayer()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pierre = Accept(harness);
            var spawnId = pierre.SpawnPool.DbId;
            var oldLease = harness.Manager.PublicActors.Handle(harness.Map, spawnId);

            harness.Manager.Scenes.Detach(harness.Client, harness.Map);

            using (var unit = harness.CreateChar())
                Assert.AreEqual((uint)MissionState.Failed,
                    unit.CharacterMissions.GetByCharacterAndMission(harness.Client.Player.Id, 666).MissionState);
            AwaitRelease(harness, spawnId);
            Assert.IsFalse(harness.Manager.PublicActors.TryResolve(harness.Map, oldLease, out _));
            Assert.IsTrue(harness.Npc(spawnId).IsInteractable);
            var npcs = new NpcManager(harness, harness.Manager);
            npcs.AbandonMission(harness.Client, new AbandonMissionPacket { MissionId = 666 });
            var retryAt = harness.UtcNow;
            Accept(harness);
            var retry = harness.Manager.PublicActors.Handle(harness.Map, spawnId);
            Assert.AreNotEqual(oldLease.RunId, retry.RunId);
            using var verify = harness.CreateChar();
            Assert.AreEqual(retryAt.AddSeconds(420),
                verify.CharacterMissionDeadlines.Get(harness.Client.Player.Id, 666).DueAtUtc);
        }

        [TestMethod]
        public void CompetingPlayerCannotReservePierreAndAbandonmentAllowsTheNextAttempt()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pierre = Accept(harness);
            var oldLease = harness.Manager.PublicActors.Handle(harness.Map, pierre.SpawnPool.DbId);
            var second = harness.Context.CreateAdditionalClient(2, manager: harness.Manager);
            try
            {
                second.SetWorldPosition(pierre.Position, 0);
                CellManager.Instance.UpdateVisibility(second);
                var npcs = new NpcManager(harness, harness.Manager);
                npcs.RequestNpcConverse(second, new RequestNPCConversePacket { EntityId = pierre.EntityId });
                npcs.AssignNPCMission(second,
                    new AssignNPCMissionPacket { NpcEntityId = pierre.EntityId, MissionId = 666 });
                Assert.IsFalse(second.Player.Missions.ContainsKey(666));
                Assert.AreEqual(oldLease, harness.Manager.PublicActors.Handle(harness.Map, pierre.SpawnPool.DbId));
                npcs.AbandonMission(harness.Client, new AbandonMissionPacket { MissionId = 666 });
                AwaitRelease(harness, pierre.SpawnPool.DbId);

                npcs.RequestNpcConverse(second, new RequestNPCConversePacket { EntityId = pierre.EntityId });
                npcs.AssignNPCMission(second,
                    new AssignNPCMissionPacket { NpcEntityId = pierre.EntityId, MissionId = 666 });

                Assert.AreEqual(MissionState.Active, second.Player.Missions[666].State);
                var nextLease = harness.Manager.PublicActors.Handle(harness.Map, pierre.SpawnPool.DbId);
                Assert.AreNotEqual(oldLease.RunId, nextLease.RunId);
                Assert.IsFalse(harness.Manager.Scenes.Submit(oldLease.RunId,
                    new SceneObservation(SceneEventKind.Signal, oldLease.Generation, SequenceId: 1)));
                Assert.AreEqual(nextLease, harness.Manager.PublicActors.Handle(harness.Map, pierre.SpawnPool.DbId));
                Assert.IsFalse(second.Player.Missions[666].Completeable);
            }
            finally
            {
                CellManager.Instance.RemoveFromWorld(second);
                harness.Map.ClientList.Remove(second);
            }
        }

        [TestMethod]
        public void ADelayedForcefieldMissileCannotReleasePierreForANewAssignment()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pierre = Accept(harness);
            var oldAssignment = harness.Client.Player.Missions[666].AssignmentId;
            var field = Forcefield(harness);
            harness.MoveTo(field.Position);
            MissileManager.Instance.MissileLaunch(harness.Map,
                new ActionData(harness.Client.Player, ActionId.WeaponAttack, 133, 0)
            {
                TargetId = field.EntityId
            }, 100);
            Assert.AreEqual(1, harness.Map.QueuedMissiles.Count);
            var npcs = new NpcManager(harness, harness.Manager);
            npcs.AbandonMission(harness.Client, new AbandonMissionPacket { MissionId = 666 });
            AwaitRelease(harness, pierre.SpawnPool.DbId);
            Assert.AreEqual(MissionState.Failed, harness.Client.Player.Missions[666].State,
                "Abandoning the timed attempt preserves its failed journal entry until native dismissal.");
            npcs.AbandonMission(harness.Client, new AbandonMissionPacket { MissionId = 666 });
            Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(666),
                "Dismiss the failed attempt through the native handler before accepting a new assignment.");
            harness.UtcNow = harness.UtcNow.AddSeconds(20);
            var retryAt = harness.UtcNow;
            pierre = Accept(harness);
            Assert.AreNotEqual(oldAssignment, harness.Client.Player.Missions[666].AssignmentId);

            MissileManager.Instance.DoWork(harness.Map, 1000);

            Assert.IsNull(pierre.Controller.ScriptedMove);
            Assert.IsTrue(Forcefield(harness).IsEnabled);
            Assert.AreEqual(100U, Forcefield(harness).CurrentHitPoints);
            Assert.IsFalse(harness.Client.Player.Missions[666].Completeable);
            using var unit = harness.CreateChar();
            Assert.AreEqual(retryAt.AddSeconds(420), unit.CharacterMissionDeadlines.Get(harness.Client.Player.Id, 666).DueAtUtc,
                "Only a genuinely new assignment receives a new deadline; a stale recovery cannot restart it.");
        }

        private static Creature Accept(WildernessRuntimeTestHarness harness)
        {
            var definition = harness.World.CreatureEntries.SingleOrDefault(creature => creature.NameId == 3097);
            Assert.IsNotNull(definition, "The coordinator-owned World migration must provide native Sgt. Pierre.");
            Assert.AreEqual(630070U, definition.Id);
            Assert.AreEqual(6340U, definition.ClassId);
            var pool = harness.Map.SpawnPools.Single(spawn =>
                spawn.SpawnSlot.Any(slot => slot.CreatureId == definition.Id));
            Assert.AreEqual(630070U, pool.DbId);
            Assert.AreEqual(1, pool.SpawnSlot.Sum(slot => slot.CountMax));
            harness.SpawnWorld(pool.DbId, 172);
            var pierre = harness.Npc(pool.DbId);
            Assert.IsNotNull(pierre, "Native Pierre must spawn as a public NPC, not a test-only actor.");
            harness.MoveTo(pierre.Position);
            var npcs = new NpcManager(harness, harness.Manager);
            npcs.RequestNpcConverse(harness.Client, new RequestNPCConversePacket { EntityId = pierre.EntityId });
            var conversation = harness.Drain().OfType<ConversePacket>().Single();
            Assert.IsTrue(conversation.ConvoDataDict.TryGetValue(ConversationType.MissionDispense, out var offers));
            Assert.IsTrue(((Dictionary<uint, MissionInfo>)offers).ContainsKey(666));
            npcs.AssignNPCMission(harness.Client,
                new AssignNPCMissionPacket { NpcEntityId = pierre.EntityId, MissionId = 666 });
            Assert.IsTrue(harness.Client.Player.Missions.TryGetValue(666, out var mission));
            Assert.AreEqual(MissionState.Active, mission.State);
            var gained = harness.Drain().OfType<MissionGainedPacket>().Single();
            Assert.AreEqual(420U, gained.MissionInfo.ObjectivesList.Single(objective => objective.ObjectiveId == 1)
                .TimeRemaining.GetValueOrDefault());
            return pierre;
        }

        private static void AwaitRelease(WildernessRuntimeTestHarness harness, uint spawnId)
        {
            for (var tick = 0; tick < 1800 && harness.Manager.PublicActors.Handle(harness.Map, spawnId) != null; tick++)
            {
                harness.SpawnWorldAfter(250, spawnId);
                harness.Tick();
            }
            Assert.IsNull(harness.Manager.PublicActors.Handle(harness.Map, spawnId),
                "Pierre's reservation must be released through the real return/respawn path.");
        }

        private static DynamicObject Forcefield(WildernessRuntimeTestHarness harness) =>
            harness.Map.DynamicObjects.Single(obj => obj.SceneMissionId == 666 && obj.SceneActorRole == "forcefield");

        private static void ReleaseForcefield(WildernessRuntimeTestHarness harness)
        {
            var field = Forcefield(harness);
            Assert.AreEqual(20000003U, (uint)field.EntityClassId);
            Assert.AreEqual(UseObjectState.FfStateFactionBIntact, field.StateId);
            Assert.IsNull(field.MissionUseAction);
            DamageForcefield(harness, field, 99);
            Assert.AreEqual(1U, field.CurrentHitPoints);
            Assert.IsTrue(field.IsEnabled);
            DamageForcefield(harness, field, 1);
            Assert.AreEqual(0U, field.CurrentHitPoints);
            Assert.AreEqual(UseObjectState.FfStateFactionBDestroyed, field.StateId);
        }

        private static void DamageForcefield(WildernessRuntimeTestHarness harness, DynamicObject field,
            int damage, Rasa.Game.Client client = null)
        {
            client ??= harness.Client;
            client.SetWorldPosition(field.Position + new Vector3(0, 0, 1), 0);
            CellManager.Instance.UpdateVisibility(client);
            var action = new ActionData(client.Player, ActionId.WeaponAttack, 133, 0)
            {
                TargetId = field.EntityId
            };
            MissileManager.Instance.MissileLaunch(harness.Map, action, damage);
            MissileManager.Instance.DoWork(harness.Map, 1000);
        }

        private static MissionSceneDefinition RescueBinding(WildernessRuntimeTestHarness harness) =>
            JsonSerializer.Deserialize<MissionSceneDefinition>(
                harness.World.Set<MissionSceneBindingEntry>().Single(row => row.MissionId == 666 &&
                    row.ContentRevision == WildernessMissionDataV1.Revision).Bindings, MissionContentCodec.Options);

        private static Vector3 Position(ScenePosition position) => new(position.X, position.Y, position.Z);

        private static void AssertGroundedRoute(WildernessRuntimeTestHarness harness, Vector3 start, SceneRoute route)
        {
            foreach (var waypoint in route.Points)
            {
                var target = Position(waypoint.Position);
                var path = harness.Map.NavMesh.FindPath(start, target, out var complete);
                Assert.IsTrue(complete && path != null && path.Count > 0 &&
                    Vector3.Distance(path.Last(), target) < 0.5f && Math.Abs(path.Last().Y - target.Y) < 0.5f,
                    $"Route {route.Key} has no complete grounded path from {start} to {target}.");
                start = target;
            }
        }

        private static void DriveUntil(WildernessRuntimeTestHarness harness, Creature pierre, Func<bool> reached)
        {
            for (var tick = 0; tick < 1600 && !reached() &&
                 harness.Client.Player.Missions[666].State == MissionState.Active; tick++)
            {
                harness.MoveTo(pierre.Position + new Vector3(0, 0, 1));
                harness.Tick();
            }
            Assert.IsTrue(reached(), $"Pierre stopped at {pierre.Position}; mission state " +
                $"{harness.Client.Player.Missions[666].State}, objective {harness.Client.Player.Missions[666].Objectives[1].State}.");
        }
    }
}
