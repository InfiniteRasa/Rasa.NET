extern alias RasaGame;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions
{
    using ClientState = RasaGame::Rasa.Data.ClientState;
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Missions.Definitions;
    using Rasa.Missions.Runtime;
    using Rasa.Missions.Scenes;
    using Rasa.Models;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Structures;
    using Rasa.Structures.Char;

    [TestClass]
    [DoNotParallelize]
    public class MissionInteractionAdmissionTests
    {
        public static IEnumerable<object[]> LateNpcAuthorityCases()
        {
            foreach (var action in new[] { "accept", "completion", "legacy" })
                foreach (var reader in action == "accept"
                    ? new[] { "inventory", "facts", "source" } : new[] { "inventory", "facts" })
                    foreach (var change in new[] { "disabled", "removed", "moved", "session", "valid" })
                        yield return new object[] { action, reader, change };
        }

        [TestMethod]
        [DynamicData(nameof(LateNpcAuthorityCases))]
        public void NpcAcceptanceAndRewardRevalidateAfterLateReaders(string action, string reader, string change)
        {
            var requirement = new FlagRequirement(910, 1);
            var definition = new Mission(321, "Final NPC authority", 321, 77, 88, 1, 1, 2, false, false,
                new[]
                {
                    new MissionObjectiveDefinition(1, 1001, 1002, Array.Empty<uint?>(), 0,
                        MissionObjectiveState.Completed, true, null, null, null,
                        Array.Empty<uint>(), Array.Empty<uint>(), Array.Empty<MissionIndicator>())
                }, true, requirement: requirement, turnInRequirement: requirement,
                items: new[]
                {
                    new MissionItemBinding("starter", 28, MissionItemScope.AssignmentIssued, 1,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove,
                        MissionItemCleanupDisposition.Remove)
                },
                acceptanceItems: new[] { new IssueMissionItemIntent("starter", 321, "starter", 28, 1) });
            using var context = MissionTestContext.WithCustomDefinitions(
                new Dictionary<uint, Mission> { [321] = definition },
                new Dictionary<uint, MissionRewardDefinition>
                {
                    [321] = new(0, new Dictionary<CurencyType, int> { [CurencyType.Credits] = 7 },
                        new[] { new MissionRewardItem(29, 2) }, null)
                });
            context.AddRewardTemplate(28, 3147);
            context.AddRewardTemplate(29, 3147);
            using (var unit = context.CreateChar())
                unit.CharacterFlags.Set(1, 910, 1);
            context.Client.Player.PlayerFlags[910] = 1;
            var giver = context.AddNpc(77);
            var target = action == "accept" ? giver : context.AddNpc(88);
            if (action != "accept")
                Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            if (action == "legacy")
            {
                using var unit = context.CreateChar();
                unit.ExecuteTransaction(() =>
                {
                    var mission = unit.CharacterMissions.GetByCharacterAndMission(1, 321);
                    mission.MissionState = (uint)MissionState.Success;
                    mission.Completeable = false;
                });
                context.ReloadPlayerMissions();
            }
            context.Drain();
            Open(context, target.EntityId);
            var session = context.Client.MissionConversation;
            var before = context.ReadRewardTotals();
            var inventory = context.Client.Player.Inventory.PersonalInventory.ToArray();
            var assignment = action == "accept" ? null : context.ReadMission(321);
            var saved = false;
            var itemWriteSeen = false;
            var factsRead = false;
            var inTransaction = false;
            var crossed = false;
            context.BeforeQuery = database => inTransaction = database.Database.CurrentTransaction != null;
            context.AfterSave = _ => saved = itemWriteSeen;
            context.AfterCommand = sql =>
            {
                if (sql.Contains("INSERT INTO \"character_mission_item\"", StringComparison.Ordinal) ||
                    sql.Contains("INSERT INTO \"character_inventory\"", StringComparison.Ordinal))
                    itemWriteSeen = true;
                if (!inTransaction || !saved || crossed || !sql.StartsWith("SELECT", StringComparison.Ordinal))
                    return;
                var flagRead = sql.Contains("FROM \"character_flag\"", StringComparison.Ordinal);
                var invalidate = reader switch
                {
                    "inventory" => sql.Contains("FROM \"character_mission_item\"", StringComparison.Ordinal),
                    "facts" => flagRead,
                    "source" => factsRead && sql.Contains("FROM \"character\"", StringComparison.Ordinal),
                    _ => throw new ArgumentOutOfRangeException(nameof(reader))
                };
                factsRead |= flagRead;
                if (!invalidate)
                    return;
                crossed = true;
                switch (change)
                {
                    case "disabled": target.IsInteractable = false; break;
                    case "removed": EntityManager.Instance.UnregisterEntity(target.EntityId); break;
                    case "moved": target.Position = new Vector3(6, 0, 0); break;
                    case "session": context.Client.InvalidateMissionSession(); break;
                }
            };
            bool accepted;
            try { accepted = Apply(); }
            finally
            {
                context.BeforeQuery = null;
                context.AfterSave = null;
                context.AfterCommand = null;
            }
            Assert.IsTrue(crossed, $"The {reader} reader must run in the transaction after the actual item flush.");
            Assert.AreEqual(change == "valid", accepted);
            if (change != "valid")
            {
                Assert.AreEqual(before, context.ReadRewardTotals());
                CollectionAssert.AreEqual(inventory, context.Client.Player.Inventory.PersonalInventory);
                using (var unit = context.CreateChar())
                {
                    var current = unit.CharacterMissions.GetByCharacterAndMission(1, 321);
                    if (assignment == null)
                    {
                        Assert.IsNull(current);
                        Assert.IsFalse(context.Client.Player.Missions.ContainsKey(321));
                        Assert.IsEmpty(unit.CharacterMissionProgress.Get(1).Missions);
                    }
                    else
                    {
                        Assert.AreEqual(assignment.AssignmentId, current.AssignmentId);
                        Assert.AreEqual(assignment.Generation, current.Generation);
                        Assert.AreEqual(assignment.MissionState, current.MissionState);
                        Assert.AreEqual(assignment.Completeable, current.Completeable);
                        Assert.IsFalse(unit.CharacterMissions.Runtime.WasRewarded(assignment.AssignmentId));
                        Assert.IsFalse(unit.CharacterMissions.Runtime.HasReceipt(
                            assignment.AssignmentId, assignment.Generation, "mission-reward"));
                    }
                    Assert.AreEqual(assignment == null ? 0 : 1, unit.CharacterMissionItems.GetOwned(1).Count);
                    Assert.AreEqual(1U, unit.CharacterFlags.Get(1)[910]);
                    Assert.IsEmpty(unit.CharacterMissions.Runtime.History(1));
                }
                Assert.IsEmpty(context.Drain(), "Invalid authority cannot publish acceptance, cleanup or rewards.");
                Assert.IsNull(context.Client.MissionConversation);
                target.IsInteractable = true;
                target.Position = Vector3.Zero;
                if (change == "removed")
                    EntityManager.Instance.RegisterEntity(target.EntityId, EntityType.Creature);
                Open(context, target.EntityId);
                Assert.AreNotSame(session, context.Client.MissionConversation);
                Assert.IsTrue(Apply());
            }
            Assert.AreEqual((uint)(action == "accept" ? MissionState.Active : MissionState.Completed),
                context.ReadMission(321).MissionState);
            Assert.AreEqual(before.Credits + (action == "accept" ? 0 : 7), context.ReadRewardTotals().Credits);
            Assert.AreEqual(before.ItemCount + 1, context.ReadRewardTotals().ItemCount);
            var after = context.ReadRewardTotals();
            context.Drain();
            Assert.IsFalse(Apply());
            Assert.AreEqual(after, context.ReadRewardTotals());
            Assert.IsEmpty(context.Drain());

            bool Apply() => action switch
            {
                "accept" => context.Manager.TryAcceptNpcMission(context.Client, target.EntityId, 321),
                "completion" => context.Manager.TryCompleteNpcMission(context.Client, target.EntityId, 321, null, null),
                "legacy" => context.Manager.TryRewardNpcMission(context.Client, target.EntityId, 321, null, null),
                _ => throw new ArgumentOutOfRangeException(nameof(action))
            };
        }

        [TestMethod]
        public void AcceptanceRequiresAnActuallyOpenedMissionOffer()
        {
            using var context = MissionTestContext.WithDefinitions(321);
            var giver = context.AddNpc(77);

            Assert.IsFalse(context.Manager.TryAcceptNpcMission(context.Client, giver.EntityId, 321));
            using (var unit = context.CreateChar())
                Assert.IsNull(unit.CharacterMissions.GetByCharacterAndMission(context.Client.Player.Id, 321));
            Assert.AreEqual(0, context.Drain().Count);

            Open(context, giver.EntityId);
            Assert.IsTrue(context.Manager.TryAcceptNpcMission(context.Client, giver.EntityId, 321));
        }

        [TestMethod]
        [DataRow("outside")]
        [DataRow("vertical")]
        [DataRow("nan-player")]
        [DataRow("infinite-target")]
        [DataRow("dead-player")]
        [DataRow("dead-npc")]
        [DataRow("dying-player")]
        [DataRow("dying-npc")]
        [DataRow("disabled")]
        [DataRow("foreign-owner")]
        [DataRow("foreign-map")]
        [DataRow("despawned")]
        [DataRow("unregistered-player")]
        [DataRow("transfer")]
        [DataRow("logout")]
        [DataRow("pending-logout")]
        [DataRow("removal")]
        public void OpeningRejectsUnavailablePlayersAndTargets(string reason)
        {
            using var context = MissionTestContext.WithDefinitions(321);
            var giver = context.AddNpc(77);
            switch (reason)
            {
                case "outside": giver.Position = new Vector3(5.001f, 0, 0); break;
                case "vertical": giver.Position = new Vector3(0, 5.001f, 0); break;
                case "nan-player": context.Client.Player.Position = new Vector3(float.NaN, 0, 0); break;
                case "infinite-target": giver.Position = new Vector3(0, float.PositiveInfinity, 0); break;
                case "dead-player": context.Client.Player.State = CharacterState.Dead; break;
                case "dead-npc": giver.State = CharacterState.Dead; break;
                case "dying-player": context.Client.Player.State = CharacterState.Dying; break;
                case "dying-npc": giver.State = CharacterState.Dying; break;
                case "disabled": giver.IsInteractable = false; break;
                case "foreign-owner": giver.SpawnPool = new SpawnPool { ScenarioOwnerCharacterId = 999 }; break;
                case "foreign-map": giver.RuntimeMapChannel = new MapChannel { MapInfo = context.Map.MapInfo }; break;
                case "despawned": context.RemoveNpcFromWorld(giver); break;
                case "unregistered-player": EntityManager.Instance.UnregisterPlayer(context.Client.Player.EntityId); break;
                case "transfer": context.Client.PendingTransfer = new PlayerTransfer { OriginMap = context.Map }; break;
                case "logout": context.Client.State = ClientState.LoggedIn; break;
                case "pending-logout": context.Client.Player.LogoutActive = true; break;
                case "removal": context.Client.Player.RemoveFromMap = true; break;
            }

            new NpcManager(context, context.Manager).RequestNpcConverse(context.Client,
                new RequestNPCConversePacket { EntityId = giver.EntityId });

            Assert.AreEqual(0, context.Drain().OfType<ConversePacket>().Count(), reason);
            Assert.IsFalse(context.Manager.TryAcceptNpcMission(context.Client, giver.EntityId, 321), reason);
            using var unit = context.CreateChar();
            Assert.IsNull(unit.CharacterMissions.GetByCharacterAndMission(context.Client.Player.Id, 321), reason);
        }

        [TestMethod]
        public void ConversationAllowsTheExactFiveMetreThreeDimensionalBoundary()
        {
            using var context = MissionTestContext.WithDefinitions(321);
            var giver = context.AddNpc(77, position: new Vector3(3, 4, 0));

            Open(context, giver.EntityId);

            Assert.IsTrue(context.Manager.TryAcceptNpcMission(context.Client, giver.EntityId, 321));
        }

        [TestMethod]
        public void ObjectiveCompletionRequiresItsOwnOfferedTopic()
        {
            using var context = MissionTestContext.WithObjectiveMission();
            var giver = context.AddNpc(77);
            var npc = context.AddNpc(500, npcPackageId: 700);
            Open(context, giver.EntityId);
            Assert.IsTrue(context.Manager.TryAcceptNpcMission(context.Client, giver.EntityId, 321));
            context.Drain();

            Assert.IsFalse(context.Manager.TryCompleteNpcObjective(context.Client, npc.EntityId, 321, 5, 11));
            Assert.AreEqual(MissionObjectiveState.Incomplete, context.Client.Player.Missions[321].Objectives[5].State);
            Assert.AreEqual(0, context.Drain().Count);

            Open(context, npc.EntityId);
            Assert.IsTrue(context.Manager.TryCompleteNpcObjective(context.Client, npc.EntityId, 321, 5, 11));
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void TurnInAndLegacyRewardRequireAnOpenedOffer(bool legacy)
        {
            using var context = MissionTestContext.WithCompletableMission(321);
            if (legacy)
            {
                using var unit = context.CreateChar();
                unit.ExecuteTransaction(() =>
                {
                    var row = unit.CharacterMissions.GetByCharacterAndMission(1, 321);
                    row.MissionState = (uint)MissionState.Success;
                    row.Completeable = false;
                });
                context.ReloadPlayerMissions();
            }

            Assert.IsFalse(Grant(context, legacy));
            Assert.AreEqual(100, context.Client.Player.Credits[CurencyType.Credits]);
            Assert.AreEqual(0, context.Drain().Count);

            Open(context, context.Receiver.EntityId);
            Assert.IsTrue(Grant(context, legacy));
        }

        [TestMethod]
        [DataRow("objective", "assignment")]
        [DataRow("objective", "generation")]
        [DataRow("objective", "revision")]
        [DataRow("completion", "assignment")]
        [DataRow("completion", "generation")]
        [DataRow("completion", "revision")]
        [DataRow("reward", "assignment")]
        [DataRow("reward", "generation")]
        [DataRow("reward", "revision")]
        public void CallbackCannotUseADifferentDurableAssignment(string action, string changed)
        {
            using var context = action == "objective"
                ? MissionTestContext.WithObjectiveMission()
                : MissionTestContext.WithCompletableMission(321);
            var npc = context.Receiver;
            if (action == "objective")
            {
                var giver = context.AddNpc(77);
                Open(context, giver.EntityId);
                Assert.IsTrue(context.Manager.TryAcceptNpcMission(context.Client, giver.EntityId, 321));
                npc = context.AddNpc(500, npcPackageId: 700);
                context.Drain();
            }
            if (action == "reward")
            {
                using var unit = context.CreateChar();
                unit.ExecuteTransaction(() =>
                {
                    var row = unit.CharacterMissions.GetByCharacterAndMission(1, 321);
                    row.MissionState = (uint)MissionState.Success;
                    row.Completeable = false;
                });
                context.ReloadPlayerMissions();
            }
            Open(context, npc.EntityId);
            using (var unit = context.CreateChar())
                unit.ExecuteTransaction(() =>
                {
                    var row = unit.CharacterMissions.GetByCharacterAndMission(1, 321);
                    if (changed == "assignment") row.AssignmentId = Guid.NewGuid().ToString("N");
                    if (changed == "generation") row.Generation++;
                    if (changed == "revision") row.ContentRevision = "different-release";
                });

            var accepted = action == "objective"
                ? context.Manager.TryCompleteNpcObjective(context.Client, npc.EntityId, 321, 5, 11)
                : Grant(context, action == "reward");

            Assert.IsFalse(accepted, $"{action}: {changed}");
            Assert.AreEqual(100, context.Client.Player.Credits[CurencyType.Credits]);
            if (action == "objective")
                Assert.AreEqual((byte)MissionObjectiveState.Incomplete,
                    context.ReadProgress(321).Missions[321].Objectives[5].State);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void TargetIsRevalidatedAfterReadsInsideTheMutationTransaction(bool lateRead)
        {
            using var context = MissionTestContext.WithDefinitions(321);
            var giver = context.AddNpc(77);
            Open(context, giver.EntityId);
            var raced = false;
            context.BeforeQuery = database =>
            {
                if (!lateRead && !raced && database.Database.CurrentTransaction != null)
                {
                    raced = true;
                    giver.Position = new Vector3(20, 0, 0);
                }
            };
            context.BeforeCommand = command =>
            {
                if (lateRead && !raced && command.Contains("COUNT(", StringComparison.OrdinalIgnoreCase))
                {
                    raced = true;
                    giver.Position = new Vector3(20, 0, 0);
                }
            };

            Assert.IsFalse(context.Manager.TryAcceptNpcMission(context.Client, giver.EntityId, 321));

            Assert.IsTrue(raced);
            using var verify = context.CreateChar();
            Assert.IsNull(verify.CharacterMissions.GetByCharacterAndMission(1, 321));
            Assert.AreEqual(0, context.Drain().Count);
        }

        [TestMethod]
        [DataRow("transfer")]
        [DataRow("disconnect")]
        [DataRow("character-switch")]
        [DataRow("map-removal")]
        [DataRow("logout-cancel")]
        [DataRow("target-removal")]
        public void LeavingAndReturningNeverRestoresTheOldConversation(string change)
        {
            using var context = MissionTestContext.WithDefinitions(321);
            var giver = context.AddNpc(77);
            var client = context.Client;
            Open(context, giver.EntityId);
            switch (change)
            {
                case "transfer":
                    client.PendingTransfer = new PlayerTransfer { OriginMap = context.Map };
                    client.PendingTransfer = null;
                    break;
                case "disconnect":
                    client.State = ClientState.Disconnected;
                    client.State = ClientState.Ingame;
                    break;
                case "character-switch":
                    var oldPlayer = client.Player;
                    client.Player = new Manifestation();
                    client.Player = oldPlayer;
                    break;
                case "map-removal":
                    CellManager.Instance.RemoveFromWorld(client);
                    CellManager.Instance.AddToWorld(client);
                    break;
                case "logout-cancel":
                    MapChannelManager.Instance.RequestLogout(client);
                    MapChannelManager.Instance.CancelLogoutRequest(client);
                    break;
                case "target-removal":
                    EntityManager.Instance.UnregisterEntity(giver.EntityId);
                    EntityManager.Instance.RegisterEntity(giver.EntityId, EntityType.Creature);
                    break;
            }
            context.Drain();

            Assert.IsFalse(context.Manager.TryAcceptNpcMission(client, giver.EntityId, 321), change);
            Open(context, giver.EntityId);
            Assert.IsTrue(context.Manager.TryAcceptNpcMission(client, giver.EntityId, 321));
        }

        [TestMethod]
        public void AcceptedMovementOutOfRangeInvalidatesBeforeThePlayerReturns()
        {
            using var context = MissionTestContext.WithDefinitions(321);
            var giver = context.AddNpc(77);
            var client = context.Client;
            client.MissionAreaService = null;
            Open(context, giver.EntityId);
            client.Player.MoveBudget = 20;
            client.Player.MoveBudgetTick = Environment.TickCount64;
            Assert.IsTrue(client.HandleMovement(new Movement(new Vector3(6, 0, 0), Vector2.Zero)));
            client.Player.MoveBudget = 20;
            client.Player.MoveBudgetTick = Environment.TickCount64;
            Assert.IsTrue(client.HandleMovement(new Movement(Vector3.Zero, Vector2.Zero)));
            context.Drain();

            Assert.IsFalse(context.Manager.TryAcceptNpcMission(client, giver.EntityId, 321));
            Open(context, giver.EntityId);
            Assert.IsTrue(context.Manager.TryAcceptNpcMission(client, giver.EntityId, 321));
        }

        [TestMethod]
        public void RejectedMovementDoesNotDiscardAStillValidConversation()
        {
            using var context = MissionTestContext.WithDefinitions(321);
            var giver = context.AddNpc(77);
            Open(context, giver.EntityId);

            Assert.IsFalse(context.Client.HandleMovement(new Movement(new Vector3(200, 0, 0), Vector2.Zero)));

            Assert.IsTrue(context.Manager.TryAcceptNpcMission(context.Client, giver.EntityId, 321));
        }

        [TestMethod]
        public void OpeningADifferentConversationDoesNotOfferTheOldNpcTopics()
        {
            using var context = MissionTestContext.WithDefinitions(321);
            var giver = context.AddNpc(77);
            var other = context.AddNpc(100);
            Open(context, giver.EntityId);
            Open(context, other.EntityId);

            Assert.IsFalse(context.Manager.TryAcceptNpcMission(context.Client, giver.EntityId, 321));
            Assert.IsFalse(context.Manager.TryAcceptNpcMission(context.Client, other.EntityId, 321));
        }

        [TestMethod]
        public void NewlyCompletableStateDoesNotAddATurnInTopicToAnOpenObjectiveConversation()
        {
            using var context = MissionTestContext.WithObjectiveMission();
            var giver = context.AddNpc(77);
            var receiver = context.AddNpc(88, npcPackageId: 700);
            Open(context, giver.EntityId);
            Assert.IsTrue(context.Manager.TryAcceptNpcMission(context.Client, giver.EntityId, 321));
            context.Drain();
            Open(context, receiver.EntityId);
            using (var unit = context.CreateChar())
                unit.ExecuteTransaction(() =>
                {
                    foreach (var row in unit.CharacterMissionProgress.GetTracked(1, 321).Values)
                        row.ObjectiveState = (byte)MissionObjectiveState.Completed;
                    unit.CharacterMissions.GetByCharacterAndMission(1, 321).Completeable = true;
                });
            context.ReloadPlayerMissions();

            Assert.IsFalse(context.Manager.TryCompleteNpcMission(context.Client, receiver.EntityId, 321, 0, null));
            Assert.AreEqual(0, context.Drain().Count);
            Open(context, receiver.EntityId);
            Assert.IsTrue(context.Manager.TryCompleteNpcMission(context.Client, receiver.EntityId, 321, 0, null));
        }

        [TestMethod]
        public void AbandonAndReacceptCannotAuthorizeOldContinueButAFreshConversationCan()
        {
            using var context = MissionTestContext.WithObjectiveMission();
            var giver = context.AddNpc(77);
            var npc = context.AddNpc(500, npcPackageId: 700);
            Open(context, giver.EntityId);
            Assert.IsTrue(context.Manager.TryAcceptNpcMission(context.Client, giver.EntityId, 321));
            context.Drain();
            Open(context, npc.EntityId);
            var other = context.CreateCompetingClient();
            Assert.IsTrue(context.Manager.TryAbandon(other, 321));
            MissionTestContext.Drain(other);
            Open(context, giver.EntityId, other);
            Assert.IsTrue(context.Manager.TryAcceptNpcMission(other, giver.EntityId, 321));
            context.Drain();

            Assert.IsFalse(context.Manager.TryCompleteNpcObjective(context.Client, npc.EntityId, 321, 5, 11));
            Assert.AreEqual(0, context.Drain().Count);
            Open(context, npc.EntityId);
            Assert.IsTrue(context.Manager.TryCompleteNpcObjective(context.Client, npc.EntityId, 321, 5, 11),
                "Native input has no nonce; identical bytes after a legitimately reopened offer are valid.");
        }

        [TestMethod]
        [DataRow("acceptance")]
        [DataRow("objective")]
        [DataRow("completion")]
        public void FailedDurableWritesPublishNothingAndTheSameConversationCanRetry(string action)
        {
            using var context = action == "completion"
                ? MissionTestContext.WithCompletableMission(321)
                : MissionTestContext.WithObjectiveMission();
            var npc = context.Receiver ?? context.AddNpc(77);
            if (action == "objective")
            {
                Open(context, npc.EntityId);
                Assert.IsTrue(context.Manager.TryAcceptNpcMission(context.Client, npc.EntityId, 321));
                context.Drain();
                npc = context.AddNpc(500, npcPackageId: 700);
            }
            Open(context, npc.EntityId);
            Func<bool> callback = action switch
            {
                "acceptance" => () => context.Manager.TryAcceptNpcMission(context.Client, npc.EntityId, 321),
                "objective" => () => context.Manager.TryCompleteNpcObjective(context.Client, npc.EntityId, 321, 5, 11),
                _ => () => context.Manager.TryCompleteNpcMission(context.Client, npc.EntityId, 321, 0, null)
            };
            var saves = context.SaveAttempts;
            context.AfterSave = _ => throw new DbUpdateException("Injected interaction commit failure.");

            Assert.IsFalse(callback());

            Assert.AreEqual(saves + 1, context.SaveAttempts);
            Assert.AreEqual(0, context.Drain().Count);
            Assert.AreEqual(100, context.Client.Player.Credits[CurencyType.Credits]);
            context.AfterSave = null;
            Assert.IsTrue(callback(), "A failed write must not consume a valid offered topic.");
        }

        [TestMethod]
        [DataRow("range")]
        [DataRow("dead")]
        [DataRow("disabled")]
        [DataRow("owner")]
        [DataRow("package")]
        [DataRow("replacement")]
        public void ObjectiveCallbacksRevalidateTheTargetAfterOpening(string changed)
        {
            using var context = MissionTestContext.WithObjectiveMission();
            var giver = context.AddNpc(77);
            var npc = context.AddNpc(500, npcPackageId: 700);
            Open(context, giver.EntityId);
            Assert.IsTrue(context.Manager.TryAcceptNpcMission(context.Client, giver.EntityId, 321));
            context.Drain();
            Open(context, npc.EntityId);
            switch (changed)
            {
                case "range": npc.Position = new Vector3(0, 0, 5.001f); break;
                case "dead": npc.State = CharacterState.Dead; break;
                case "disabled": npc.IsInteractable = false; break;
                case "owner": npc.SpawnPool = new SpawnPool { ScenarioOwnerCharacterId = 999 }; break;
                case "package": npc.Npc.NpcPackageId = 701; break;
                case "replacement":
                    EntityManager.Instance.Creatures[npc.EntityId] = new Creature
                    {
                        EntityId = npc.EntityId, Npc = npc.Npc, DbId = npc.DbId,
                        MapContextId = npc.MapContextId, RuntimeMapChannel = npc.RuntimeMapChannel,
                        Position = npc.Position, State = npc.State
                    };
                    break;
            }
            try
            {
                Assert.IsFalse(context.Manager.TryCompleteNpcObjective(context.Client, npc.EntityId, 321, 5, 11));
                Assert.AreEqual((byte)MissionObjectiveState.Incomplete,
                    context.ReadProgress(321).Missions[321].Objectives[5].State);
                Assert.AreEqual(0, context.Drain().Count);
            }
            finally
            {
                if (changed == "replacement")
                    EntityManager.Instance.Creatures[npc.EntityId] = npc;
            }
        }

        private static bool Grant(MissionTestContext context, bool legacy) => legacy
            ? context.Manager.TryRewardNpcMission(context.Client, context.Receiver.EntityId, 321, 0, null)
            : context.Manager.TryCompleteNpcMission(context.Client, context.Receiver.EntityId, 321, 0, null);

        private static void Open(MissionTestContext context, ulong entityId, Client client = null)
        {
            client ??= context.Client;
            new NpcManager(context, context.Manager).RequestNpcConverse(client,
                new RequestNPCConversePacket { EntityId = entityId });
            Assert.AreEqual(1, MissionTestContext.Drain(client).OfType<ConversePacket>().Count());
        }
    }
}
