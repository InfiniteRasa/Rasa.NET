using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game.Missions.Persistence;
using Rasa.Managers;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Packets.Mission.Server;
using Rasa.Structures;
using Rasa.Structures.Char;

namespace Rasa.Test.Missions
{
    [TestClass]
    [DoNotParallelize]
    public class MissionTurnInItemCostTests
    {
        private const uint MissionId = 901;
        private const uint TemplateId = 28;
        private const string CostKey = "mission-turn-in:delivery-cost";

        [TestMethod]
        public void CompletableMissionRejectsTurnInWhenAuthoritativeInventoryCannotPayItsCost()
        {
            const uint missionId = 901;
            const uint templateId = 28;
            var scene = JsonSerializer.Deserialize<MissionSceneDefinition>(
                """
                {
                  "items": [
                    {
                      "itemKey": "delivery-cost",
                      "itemTemplateId": 28,
                      "scope": "CharacterOwned",
                      "maximumQuantity": 2,
                      "completion": "Retain",
                      "failure": "Retain",
                      "abandonment": "Retain",
                      "turnInQuantity": 2
                    }
                  ]
                }
                """, MissionContentCodec.Options);
            var mission = new Mission(missionId, "Delivery cost", missionId, 77, 88, 1, 1, 1, false, false,
                Array.Empty<MissionObjectiveDefinition>(), true, items: scene.Items);
            var reward = new MissionRewardDefinition(0,
                new Dictionary<CurencyType, int> { [CurencyType.Credits] = 7 }, null, null);
            using var context = MissionTestContext.WithCustomDefinitions(
                new Dictionary<uint, Mission> { [missionId] = mission },
                new Dictionary<uint, MissionRewardDefinition> { [missionId] = reward });
            context.AddRewardTemplate(templateId, 3147);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(
                context.Client, context.AddNpc(77).EntityId, missionId));
            using (var grant = new InventoryManager.InventoryGrant())
            {
                using (var unit = context.CreateChar())
                    unit.ExecuteTransaction(() => grant.PlanAndSave(context.Client,
                        new[] { new InventoryManager.InventoryItemGrant(templateId, 1) }, unit));
                grant.Publish(context.Client);
            }
            var held = context.Client.Player.Inventory.PersonalInventory
                .Where(id => id != 0).Select(EntityManager.Instance.GetItem).Single();
            Assert.AreEqual(1U, held.StackSize);
            Assert.IsNull(held.MissionOwnership);
            Assert.IsTrue(context.Client.Player.Missions[missionId].Completeable);
            Assert.IsTrue(context.ReadMission(missionId).Completeable);
            var before = context.ReadRewardTotals();
            var receiver = context.AddNpc(88);
            Assert.IsTrue(context.Manager.OpenNpcConversation(context.Client, receiver.EntityId));
            var conversation = context.Client.MissionConversation;
            context.Drain();

            Assert.IsFalse(context.Manager.TryCompleteNpcMission(
                context.Client, receiver.EntityId, missionId, null, null),
                "A completable mission must not reward when its two-item turn-in cost has only one real item.");

            Assert.AreEqual(before, context.ReadRewardTotals());
            Assert.AreEqual(MissionState.Active, context.Client.Player.Missions[missionId].State);
            Assert.IsTrue(context.Client.Player.Missions[missionId].Completeable);
            Assert.AreEqual((uint)MissionState.Active, context.ReadMission(missionId).MissionState);
            Assert.AreEqual(1U, held.StackSize);
            Assert.IsTrue(context.Client.Player.Inventory.PersonalInventory.Contains(held.EntityId));
            Assert.AreSame(conversation, context.Client.MissionConversation);
            Assert.IsFalse(context.Drain().OfType<MissionRewardedPacket>().Any());
            using var verify = context.CreateChar();
            Assert.IsFalse(verify.CharacterMissions.Runtime.WasRewarded(
                context.Client.Player.Missions[missionId].AssignmentId));
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void SuccessfulRewardChargesOnceAndClearingCannotChargeAgain(bool legacySuccess)
        {
            using var context = Create();
            Grant(context, 3);
            if (legacySuccess)
            {
                using var unit = context.CreateChar();
                unit.ExecuteTransaction(() =>
                {
                    unit.CharacterMissions.SetState(1, MissionId, (uint)MissionState.Success);
                    unit.CharacterMissions.SetCompletable(1, MissionId, false);
                });
                context.ReloadPlayerMissions();
            }
            var held = Held(context);
            var assignment = context.Client.Player.Missions[MissionId];
            var receiver = OpenTurnIn(context);
            var before = context.ReadRewardTotals();
            context.BeforeSave = _ =>
            {
                Assert.AreEqual(3U, held.StackSize);
                Assert.AreEqual(before.Credits, context.Client.Player.Credits[CurencyType.Credits]);
                Assert.IsFalse(context.Drain().OfType<MissionRewardedPacket>().Any());
            };

            Assert.IsTrue(context.Manager.TryCompleteNpcMission(
                context.Client, receiver.EntityId, MissionId, null, null));

            context.BeforeSave = null;
            Assert.AreEqual(1U, held.StackSize);
            Assert.IsNull(held.MissionOwnership);
            Assert.AreEqual(before.Credits + 7, context.ReadRewardTotals().Credits);
            Assert.AreEqual(before.ItemCount - 2, context.ReadRewardTotals().ItemCount);
            var receipt = Receipt(context, assignment.AssignmentId);
            Assert.IsNotNull(receipt);
            Assert.AreEqual(assignment.Generation, receipt.Generation);
            var cost = JsonSerializer.Deserialize<CharacterIntent>(receipt.Payload, MissionContentCodec.Options)
                as ConsumeMissionItemIntent;
            Assert.IsNotNull(cost);
            Assert.AreEqual(2U, cost.Quantity);
            Assert.AreEqual(MissionItemScope.CharacterOwned, cost.Scope);
            Assert.AreEqual(1, context.Drain().OfType<MissionRewardedPacket>().Count());
            Assert.IsFalse(context.Manager.TryCompleteNpcMission(
                context.Client, receiver.EntityId, MissionId, null, null));
            context.ReloadPlayerMissions();
            Assert.IsFalse(context.Manager.CompleteOfferedMission(
                context.Client, receiver.EntityId, MissionId, null));
            Assert.IsTrue(context.Manager.TryClear(context.Client, MissionId));
            Assert.AreEqual(1U, held.StackSize);
            Assert.AreEqual(before.ItemCount - 2, context.ReadRewardTotals().ItemCount);
            Assert.IsNotNull(Receipt(context, assignment.AssignmentId));
        }

        [TestMethod]
        public void FailedRewardPlanningRollsBackTheCostAndReceiptBeforeRetry()
        {
            var reward = new MissionRewardDefinition(0,
                new Dictionary<CurencyType, int> { [CurencyType.Credits] = 7 }, null,
                new[] { new MissionRewardItem(29, 1) });
            using var context = Create(reward: reward);
            Grant(context, 3);
            var receiver = OpenTurnIn(context);
            var conversation = context.Client.MissionConversation;
            var assignmentId = context.Client.Player.Missions[MissionId].AssignmentId;
            var before = context.ReadRewardTotals();

            Assert.IsFalse(context.Manager.TryCompleteNpcMission(
                context.Client, receiver.EntityId, MissionId, 99, null));

            Assert.AreEqual(before, context.ReadRewardTotals());
            Assert.AreEqual(3U, Held(context).StackSize);
            Assert.IsNull(Receipt(context, assignmentId));
            Assert.AreSame(conversation, context.Client.MissionConversation);
            Assert.AreEqual(MissionState.Active, context.Client.Player.Missions[MissionId].State);
            Assert.IsFalse(context.Drain().OfType<MissionRewardedPacket>().Any());
            Assert.IsTrue(context.Manager.TryCompleteNpcMission(
                context.Client, receiver.EntityId, MissionId, 0, null));
            Assert.AreEqual(1U, Held(context).StackSize);
            Assert.IsNotNull(Receipt(context, assignmentId));
        }

        [TestMethod]
        public void FailureAfterSavingTheCostRollsBackInventoryAndReceiptTogether()
        {
            using var context = Create();
            Grant(context, 3);
            var held = Held(context);
            var receiver = OpenTurnIn(context);
            var conversation = context.Client.MissionConversation;
            var assignmentId = context.Client.Player.Missions[MissionId].AssignmentId;
            var before = context.ReadRewardTotals();
            var injected = false;
            context.AfterSave = database =>
            {
                if (database.ItemEntries.Local.Any(item => item.ItemId == held.Id && item.StackSize == 1) &&
                    database.Set<CharacterMissionItemReceiptEntry>().Local.Any(receipt => receipt.OperationKey == CostKey))
                {
                    injected = true;
                    throw new DbUpdateException("Injected failure after turn-in inventory and receipt writes.");
                }
            };

            Assert.IsFalse(context.Manager.TryCompleteNpcMission(
                context.Client, receiver.EntityId, MissionId, null, null));

            context.AfterSave = null;
            Assert.IsTrue(injected, "The failure must follow the actual cost write, not an earlier admission rejection.");
            Assert.AreEqual(before, context.ReadRewardTotals());
            Assert.AreEqual(3U, held.StackSize);
            Assert.IsNull(Receipt(context, assignmentId));
            Assert.AreSame(conversation, context.Client.MissionConversation);
            Assert.IsFalse(context.Drain().OfType<MissionRewardedPacket>().Any());
            Assert.IsTrue(context.Manager.TryCompleteNpcMission(
                context.Client, receiver.EntityId, MissionId, null, null));
            Assert.AreEqual(1U, held.StackSize);
            Assert.IsNotNull(Receipt(context, assignmentId));
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void FailureOrAbandonmentNeverChargesCharacterOwnedTurnInItems(bool abandon)
        {
            using var context = Create();
            Grant(context, 3);
            var before = context.ReadRewardTotals();
            var assignmentId = context.Client.Player.Missions[MissionId].AssignmentId;

            Assert.IsTrue(abandon
                ? context.Manager.TryAbandon(context.Client, MissionId)
                : context.Manager.TryFailMission(context.Client, MissionId));

            Assert.AreEqual(before, context.ReadRewardTotals());
            Assert.AreEqual(3U, Held(context).StackSize);
            Assert.IsNull(Receipt(context, assignmentId));
        }

        [TestMethod]
        public void DefaultTurnInMetadataDoesNotRequireItemsOrCreateACostReceipt()
        {
            var binding = new MissionItemBinding("delivery-cost", TemplateId, MissionItemScope.CharacterOwned, 3,
                MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                MissionItemCleanupDisposition.Retain);
            var json = JsonSerializer.Serialize(binding, MissionContentCodec.Options);
            Assert.IsFalse(json.Contains("\"turnInQuantity\"", StringComparison.Ordinal));
            Assert.IsFalse(json.Contains("\"drop\"", StringComparison.Ordinal));
            using var context = Create(turnInQuantity: 0);
            var assignmentId = context.Client.Player.Missions[MissionId].AssignmentId;
            var receiver = OpenTurnIn(context);

            Assert.IsTrue(context.Manager.TryCompleteNpcMission(
                context.Client, receiver.EntityId, MissionId, null, null));

            Assert.IsNull(Receipt(context, assignmentId));
            Assert.AreEqual(0L, context.ReadRewardTotals().ItemCount);
        }

        [TestMethod]
        public void AuthoredOperationsCannotPrepayAnAutomaticTurnInCost()
        {
            using var context = Create();
            Grant(context, 3);
            var assignment = context.Client.Player.Missions[MissionId];
            using var unit = context.CreateChar();

            Assert.ThrowsExactly<GameplayRejectionException>(() => unit.ExecuteTransaction(() =>
                new MissionItemPlanner(context.Client, unit, context.Manager).Apply(
                    new ConsumeMissionItemIntent(CostKey, MissionId, "delivery-cost", 2, MissionItemScope.CharacterOwned),
                    assignment.AssignmentId, assignment.Generation)));

            Assert.AreEqual(3U, Held(context).StackSize);
            Assert.IsNull(Receipt(context, assignment.AssignmentId));
        }

        private static MissionTestContext Create(uint turnInQuantity = 2, MissionRewardDefinition reward = null)
        {
            var binding = new MissionItemBinding("delivery-cost", TemplateId, MissionItemScope.CharacterOwned, 3,
                MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                MissionItemCleanupDisposition.Retain, turnInQuantity);
            var mission = new Mission(MissionId, "Delivery cost", MissionId, 77, 88, 1, 1, 1, false, false,
                Array.Empty<MissionObjectiveDefinition>(), true, items: new[] { binding });
            var context = MissionTestContext.WithCustomDefinitions(
                new Dictionary<uint, Mission> { [MissionId] = mission },
                new Dictionary<uint, MissionRewardDefinition>
                {
                    [MissionId] = reward ?? new MissionRewardDefinition(0,
                        new Dictionary<CurencyType, int> { [CurencyType.Credits] = 7 }, null, null)
                });
            context.AddRewardTemplate(TemplateId, 3147);
            context.AddRewardTemplate(29, 3148);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(
                context.Client, context.AddNpc(77).EntityId, MissionId));
            context.Drain();
            return context;
        }

        private static void Grant(MissionTestContext context, uint quantity)
        {
            using var grant = new InventoryManager.InventoryGrant();
            using (var unit = context.CreateChar())
                unit.ExecuteTransaction(() => grant.PlanAndSave(context.Client,
                    new[] { new InventoryManager.InventoryItemGrant(TemplateId, quantity) }, unit));
            grant.Publish(context.Client);
        }

        private static Creature OpenTurnIn(MissionTestContext context)
        {
            var receiver = context.AddNpc(88);
            Assert.IsTrue(context.Manager.OpenNpcConversation(context.Client, receiver.EntityId));
            context.Drain();
            return receiver;
        }

        private static Item Held(MissionTestContext context) => context.Client.Player.Inventory.PersonalInventory
            .Where(id => id != 0).Select(EntityManager.Instance.GetItem)
            .Single(item => item.ItemTemplate.ItemTemplateId == TemplateId);

        private static CharacterMissionItemReceiptEntry Receipt(MissionTestContext context, string assignmentId)
        {
            using var unit = context.CreateChar();
            return unit.CharacterMissionItems.GetReceipt(context.Client.Player.Id, assignmentId, CostKey);
        }
    }
}
