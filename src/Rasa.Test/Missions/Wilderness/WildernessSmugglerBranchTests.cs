using System;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
using Rasa.Missions.Definitions;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.MapChannel.Server;
using Rasa.Packets.Mission.Server;
using Rasa.Structures;

namespace Rasa.Test.Missions.Wilderness
{
    [TestClass]
    [DoNotParallelize]
    public class WildernessSmugglerBranchTests
    {
        private const uint SmugglingMission = 623;
        private const uint SuspicionMission = 791;
        private const uint StimDustTemplate = 2226;

        [TestMethod]
        public void BetrayalRollsBackBothAssignmentsAndParcelsWhenFailurePersistenceThrows()
        {
            using var harness = WildernessTwinPillarsTests.Create();
            harness.SpawnWorld(209, 207);
            var moore = harness.Npc(209);
            var taylor = harness.Npc(207);
            Assert.IsNotNull(moore, "Shared World must expose Private Moore (creature 131, spawn 209) as a public NPC.");
            Assert.IsNotNull(taylor, "Shared World must expose Commander Taylor (creature 129, spawn 207) as a public NPC.");
            Assert.AreEqual(131U, moore.DbId);
            Assert.AreEqual(129U, taylor.DbId);
            var npcs = new NpcManager(harness, harness.Manager);

            Accept(harness, npcs, moore, SmugglingMission);
            var assignment = harness.Client.Player.Missions[SmugglingMission];
            AssertParcels(harness, 3, assignment.AssignmentId, assignment.Generation);
            Accept(harness, npcs, taylor, SuspicionMission);
            AssertParcels(harness, 3, assignment.AssignmentId, assignment.Generation);
            var before = harness.Context.ReadRewardTotals();
            OpenChoice(harness, taylor);
            var choice = new PerformNPCChoicePacket
            {
                EntityId = taylor.EntityId,
                MissionId = SuspicionMission,
                ObjectiveId = 2,
                PlayerFlagId = 1,
                ChoiceIdx = 1
            };
            var injected = false;
            harness.Context.AfterSave = database =>
            {
                if (!database.CharacterMissionEntries.Local.Any(mission =>
                    mission.MissionId == SmugglingMission && mission.MissionState == (uint)MissionState.Failed))
                    return;
                injected = true;
                throw new DbUpdateException("Injected failure after persisting the betrayed smuggler assignment.");
            };

            try
            {
                npcs.PerformNPCChoice(harness.Client, choice);
            }
            finally
            {
                harness.Context.AfterSave = null;
            }

            Assert.IsTrue(injected, "The native choice must reach the real smuggler-failure persistence path.");
            Assert.AreEqual(before, harness.Context.ReadRewardTotals());
            Assert.AreEqual(MissionState.Active, harness.Client.Player.Missions[SmugglingMission].State);
            Assert.AreEqual(MissionObjectiveState.Incomplete,
                harness.Client.Player.Missions[SuspicionMission].Objectives[2].State);
            AssertDurableStates(harness, MissionState.Active, MissionObjectiveState.Incomplete);
            AssertParcels(harness, 3, assignment.AssignmentId, assignment.Generation);
            var rejectedPackets = harness.Drain();
            Assert.IsFalse(rejectedPackets.OfType<MissionFailedPacket>().Any());
            Assert.IsFalse(rejectedPackets.OfType<MissionRewardedPacket>().Any());
            Assert.IsFalse(rejectedPackets.OfType<ObjectiveCompletedPacket>().Any(),
                "A failed betrayal must not publish a committed choice before its parcel cleanup succeeds.");

            OpenChoice(harness, taylor);
            npcs.PerformNPCChoice(harness.Client, choice);

            Assert.AreEqual(MissionState.Failed, harness.Client.Player.Missions[SmugglingMission].State);
            Assert.AreEqual(MissionObjectiveState.Completed,
                harness.Client.Player.Missions[SuspicionMission].Objectives[2].State);
            AssertDurableStates(harness, MissionState.Failed, MissionObjectiveState.Completed);
            AssertParcels(harness, 0, assignment.AssignmentId, assignment.Generation);
            Assert.AreEqual(before.Experience, harness.Context.ReadRewardTotals().Experience);
            Assert.AreEqual(before.Credits, harness.Context.ReadRewardTotals().Credits);
            Assert.IsFalse(harness.Drain().OfType<MissionRewardedPacket>().Any(),
                "Choosing the betrayal branch must not also pay Moore's delivery reward.");

            var afterBetrayal = harness.Context.ReadRewardTotals();
            npcs.PerformNPCChoice(harness.Client, choice);
            harness.MoveTo(moore.Position);
            WildernessTwinPillarsTests.OpenNativeNpcConversation(harness, moore);
            var delivery = new CompleteNPCMissionPacket
            {
                EntityId = moore.EntityId,
                MissionId = SmugglingMission,
                SelectionIdx = 0
            };
            npcs.CompleteNPCMission(harness.Client, delivery);
            npcs.CompleteNPCMission(harness.Client, delivery);
            Assert.AreEqual(afterBetrayal, harness.Context.ReadRewardTotals());
            AssertDurableStates(harness, MissionState.Failed, MissionObjectiveState.Completed);
            Assert.IsFalse(harness.Drain().OfType<MissionRewardedPacket>().Any());
        }

        [TestMethod]
        [DataRow("123")]
        [DataRow("132")]
        [DataRow("213")]
        [DataRow("231")]
        [DataRow("312")]
        [DataRow("321")]
        public void DenialPreservesTheParcelsAndEveryDeliveryOrderPaysMooreOnlyOnce(string order)
        {
            using var harness = WildernessTwinPillarsTests.Create();
            var actors = SpawnContacts(harness);
            var npcs = new NpcManager(harness, harness.Manager);
            Accept(harness, npcs, actors.Moore, SmugglingMission);
            var assignment = harness.Client.Player.Missions[SmugglingMission];
            Accept(harness, npcs, actors.Taylor, SuspicionMission);
            var beforeDenial = harness.Context.ReadRewardTotals();
            Choose(harness, actors.Taylor, 2);
            Assert.AreEqual(MissionState.Failed, harness.Client.Player.Missions[SuspicionMission].State);
            Assert.AreEqual(MissionObjectiveState.Incomplete,
                harness.Client.Player.Missions[SuspicionMission].Objectives[3].State);
            Assert.IsFalse(harness.Context.ReadMission(SuspicionMission).Completeable);
            Assert.AreEqual(MissionState.Active, harness.Client.Player.Missions[SmugglingMission].State);
            Assert.AreEqual(beforeDenial, harness.Context.ReadRewardTotals());
            AssertParcels(harness, 3, assignment.AssignmentId, assignment.Generation);
            WildernessTwinPillarsTests.Reload(harness);

            for (var index = 0; index < order.Length; index++)
            {
                Deliver(harness, (uint)(order[index] - '0'));
                AssertParcels(harness, 2 - index, assignment.AssignmentId, assignment.Generation);
                Assert.AreEqual(index == 2 ? MissionObjectiveState.Incomplete : MissionObjectiveState.Inactive,
                    harness.Client.Player.Missions[SmugglingMission].Objectives[4].State);
                WildernessTwinPillarsTests.Reload(harness);
            }

            WildernessTwinPillarsTests.Talk(harness, actors.Moore, SmugglingMission, 4);
            WildernessTwinPillarsTests.Reward(harness, actors.Moore, SmugglingMission, null, 44919, 7000, 10000);
            var paid = harness.Context.ReadRewardTotals();
            harness.MoveTo(actors.Taylor.Position);
            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, actors.Taylor.EntityId, SuspicionMission));
            npcs.PerformNPCChoice(harness.Client, new PerformNPCChoicePacket
            {
                EntityId = actors.Taylor.EntityId,
                MissionId = SuspicionMission,
                ObjectiveId = 2,
                PlayerFlagId = 1,
                ChoiceIdx = 1
            });
            Assert.AreEqual(paid, harness.Context.ReadRewardTotals());
            Assert.AreEqual(0L, WildernessTwinPillarsTests.Held(harness, 44918));
            Assert.AreEqual(0L, WildernessTwinPillarsTests.Held(harness, 44917));
        }

        [TestMethod]
        [DataRow("123", 0, 0)]
        [DataRow("123", 1, 0)]
        [DataRow("123", 1, 1)]
        [DataRow("123", 2, 0)]
        [DataRow("123", 2, 1)]
        [DataRow("123", 2, 2)]
        [DataRow("132", 0, 0)]
        [DataRow("132", 1, 0)]
        [DataRow("132", 1, 1)]
        [DataRow("132", 2, 0)]
        [DataRow("132", 2, 1)]
        [DataRow("132", 2, 2)]
        [DataRow("213", 0, 0)]
        [DataRow("213", 1, 0)]
        [DataRow("213", 1, 1)]
        [DataRow("213", 2, 0)]
        [DataRow("213", 2, 1)]
        [DataRow("213", 2, 2)]
        [DataRow("231", 0, 0)]
        [DataRow("231", 1, 0)]
        [DataRow("231", 1, 1)]
        [DataRow("231", 2, 0)]
        [DataRow("231", 2, 1)]
        [DataRow("231", 2, 2)]
        [DataRow("312", 0, 0)]
        [DataRow("312", 1, 0)]
        [DataRow("312", 1, 1)]
        [DataRow("312", 2, 0)]
        [DataRow("312", 2, 1)]
        [DataRow("312", 2, 2)]
        [DataRow("321", 0, 0)]
        [DataRow("321", 1, 0)]
        [DataRow("321", 1, 1)]
        [DataRow("321", 2, 0)]
        [DataRow("321", 2, 1)]
        [DataRow("321", 2, 2)]
        public void BetrayalIsExclusiveForEveryAcceptanceAndPartialDeliveryOrdering(
            string order, int deliveredBeforeBetrayal, int acceptSuspicionAfter)
        {
            using var harness = WildernessTwinPillarsTests.Create();
            var actors = SpawnContacts(harness);
            var npcs = new NpcManager(harness, harness.Manager);
            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, actors.Taylor.EntityId, SuspicionMission));
            Accept(harness, npcs, actors.Moore, SmugglingMission);
            var assignment = harness.Client.Player.Missions[SmugglingMission];
            for (var delivered = 0; delivered <= deliveredBeforeBetrayal; delivered++)
            {
                if (delivered == acceptSuspicionAfter)
                    Accept(harness, npcs, actors.Taylor, SuspicionMission);
                AssertParcels(harness, 3 - delivered, assignment.AssignmentId, assignment.Generation);
                WildernessTwinPillarsTests.Reload(harness);
                if (delivered < deliveredBeforeBetrayal)
                    Deliver(harness, (uint)(order[delivered] - '0'));
            }

            Choose(harness, actors.Taylor, 1);

            Assert.AreEqual(MissionState.Failed, harness.Client.Player.Missions[SmugglingMission].State);
            Assert.AreEqual(MissionObjectiveState.Completed,
                harness.Client.Player.Missions[SuspicionMission].Objectives[2].State);
            AssertParcels(harness, 0, assignment.AssignmentId, assignment.Generation);
            AssertDurableStates(harness, MissionState.Failed, MissionObjectiveState.Completed);
            WildernessTwinPillarsTests.Reload(harness);
            RewardBetrayal(harness, actors.Taylor);
            var paid = harness.Context.ReadRewardTotals();
            harness.MoveTo(actors.Moore.Position);
            WildernessTwinPillarsTests.OpenNativeNpcConversation(harness, actors.Moore);
            Assert.IsFalse(harness.Manager.TryCompleteNpcMission(
                harness.Client, actors.Moore.EntityId, SmugglingMission, null, null));
            WildernessTwinPillarsTests.Reload(harness);
            Assert.IsFalse(harness.Manager.TryCompleteNpcMission(
                harness.Client, actors.Taylor.EntityId, SuspicionMission, null, null));
            Assert.AreEqual(paid, harness.Context.ReadRewardTotals());
            Assert.AreEqual(0L, WildernessTwinPillarsTests.Held(harness, 44919));
        }

        [TestMethod]
        public void AcceptedSuspicionCannotBetrayOrPayAfterTheLastOwnedParcelWasDelivered()
        {
            using var harness = WildernessTwinPillarsTests.Create();
            var actors = SpawnContacts(harness);
            var npcs = new NpcManager(harness, harness.Manager);
            Accept(harness, npcs, actors.Moore, SmugglingMission);
            Accept(harness, npcs, actors.Taylor, SuspicionMission);
            foreach (var recipient in new uint[] { 1, 2, 3 })
                Deliver(harness, recipient);
            var before = harness.Context.ReadRewardTotals();
            harness.MoveTo(actors.Taylor.Position);
            var menu = WildernessTwinPillarsTests.OpenNativeNpcConversation(harness, actors.Taylor);
            Assert.IsFalse(menu.ConvoDataDict.ContainsKey(ConversationType.ObjectiveChoice),
                "The native betrayal topic must disappear when the exact assignment has no parcels left.");

            Assert.IsFalse(harness.Manager.TryPerformNpcChoice(
                harness.Client, actors.Taylor.EntityId, SuspicionMission, 2, 1, 1));
            Assert.IsFalse(harness.Manager.TryCompleteNpcMission(
                harness.Client, actors.Taylor.EntityId, SuspicionMission, null, null));

            Assert.AreEqual(before, harness.Context.ReadRewardTotals());
            Assert.AreEqual(MissionState.Active, harness.Client.Player.Missions[SmugglingMission].State);
            Assert.AreEqual(MissionObjectiveState.Incomplete,
                harness.Client.Player.Missions[SuspicionMission].Objectives[2].State);
            Assert.IsFalse(harness.Drain().OfType<MissionRewardedPacket>().Any());
        }

        [TestMethod]
        public void UnownedStimDustDoesNotReplaceTheExhaustedSmugglingAssignmentsParcels()
        {
            using var harness = WildernessTwinPillarsTests.Create();
            var actors = SpawnContacts(harness);
            using (var grant = new InventoryManager.InventoryGrant())
            {
                using (var unit = harness.CreateChar())
                    unit.ExecuteTransaction(() => grant.PlanAndSave(harness.Client,
                        new[] { new InventoryManager.InventoryItemGrant(StimDustTemplate, 3) }, unit));
                grant.Publish(harness.Client);
            }
            WildernessTwinPillarsTests.Accept(harness, actors.Moore, SmugglingMission);
            Assert.AreEqual(6L, WildernessTwinPillarsTests.Held(harness, StimDustTemplate));
            foreach (var recipient in new uint[] { 1, 2, 3 })
                Deliver(harness, recipient);
            Assert.AreEqual(3L, WildernessTwinPillarsTests.Held(harness, StimDustTemplate));
            Assert.IsTrue(WildernessTwinPillarsTests.Inventory(harness)
                .Where(item => item.ItemTemplate.ItemTemplateId == StimDustTemplate)
                .All(item => item.MissionOwnership == null));
            harness.MoveTo(actors.Taylor.Position);

            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, actors.Taylor.EntityId, SuspicionMission));

            Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(SuspicionMission));
        }

        [TestMethod]
        public void FullQuestBagsRejectSmugglingAcceptanceWithoutAnAssignmentOrPartialParcels()
        {
            using var harness = WildernessTwinPillarsTests.Create();
            WildernessTwinPillarsTests.FillQuestInventory(harness);
            harness.SpawnWorld(209);
            var moore = WildernessTwinPillarsTests.RequireNpc(harness, 209, 131);
            var before = harness.Context.ReadRewardTotals();
            harness.MoveTo(moore.Position);

            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, moore.EntityId, SmugglingMission));

            Assert.AreEqual(before, harness.Context.ReadRewardTotals());
            Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(SmugglingMission));
            Assert.AreEqual(0L, WildernessTwinPillarsTests.Held(harness, StimDustTemplate));
            using var unit = harness.CreateChar();
            Assert.IsNull(unit.CharacterMissions.GetByCharacterAndMission(harness.Client.Player.Id, SmugglingMission));
            Assert.IsFalse(unit.CharacterMissionItems.GetOwned(harness.Client.Player.Id)
                .Any(item => item.MissionId == SmugglingMission));
        }

        [TestMethod]
        public void SuspicionAuthorsOnlyNativeChoicesOneAndTwoAndRejectsThree()
        {
            using var harness = WildernessTwinPillarsTests.Create();
            var actors = SpawnContacts(harness);
            WildernessTwinPillarsTests.Accept(harness, actors.Moore, SmugglingMission);
            WildernessTwinPillarsTests.Accept(harness, actors.Taylor, SuspicionMission);
            var topic = harness.Manager.LoadedMissions[SuspicionMission].Dialogue.Single(dialogue =>
                dialogue.ObjectiveId == 2 && dialogue.Kind == MissionDialogueKind.Choice);
            Assert.AreEqual(415U, topic.NpcPackageId);
            Assert.AreEqual(1U, topic.PlayerFlagId);
            CollectionAssert.AreEqual(new[] { 1, 2 }, topic.Choices.Keys.OrderBy(index => index).ToArray());
            var assignment = harness.Client.Player.Missions[SmugglingMission];
            var before = harness.Context.ReadRewardTotals();
            OpenChoice(harness, actors.Taylor);

            Assert.IsFalse(harness.Manager.TryPerformNpcChoice(
                harness.Client, actors.Taylor.EntityId, SuspicionMission, 2, 1, 3));

            Assert.AreEqual(before, harness.Context.ReadRewardTotals());
            AssertParcels(harness, 3, assignment.AssignmentId, assignment.Generation);
            AssertDurableStates(harness, MissionState.Active, MissionObjectiveState.Incomplete);
        }

        [TestMethod]
        public void StaleBetrayalCannotChargeAReplacementSmugglingAssignment()
        {
            using var harness = WildernessTwinPillarsTests.Create();
            var actors = SpawnContacts(harness);
            WildernessTwinPillarsTests.Accept(harness, actors.Moore, SmugglingMission);
            var original = harness.Client.Player.Missions[SmugglingMission];
            WildernessTwinPillarsTests.Accept(harness, actors.Taylor, SuspicionMission);
            OpenChoice(harness, actors.Taylor);
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, SmugglingMission));
            Assert.AreEqual(0L, WildernessTwinPillarsTests.Held(harness, StimDustTemplate));
            Assert.IsFalse(harness.Manager.TryPerformNpcChoice(
                harness.Client, actors.Taylor.EntityId, SuspicionMission, 2, 1, 1));
            WildernessTwinPillarsTests.Accept(harness, actors.Moore, SmugglingMission);
            var replacement = harness.Client.Player.Missions[SmugglingMission];
            Assert.AreNotEqual(original.AssignmentId, replacement.AssignmentId);
            var before = harness.Context.ReadRewardTotals();

            Assert.IsFalse(harness.Manager.TryPerformNpcChoice(
                harness.Client, actors.Taylor.EntityId, SuspicionMission, 2, 1, 1));

            Assert.AreEqual(before, harness.Context.ReadRewardTotals());
            Assert.AreEqual(MissionState.Active, replacement.State);
            AssertParcels(harness, 3, replacement.AssignmentId, replacement.Generation);
        }

        [TestMethod]
        [DataRow(623U, 44919U, 3)]
        [DataRow(791U, 44918U, 2)]
        [DataRow(791U, 44917U, 1)]
        public void SupportedMedicalRewardHealsAfterOneCommittedNativeConsumption(
            uint missionId, uint templateId, int argument)
        {
            using var harness = WildernessTwinPillarsTests.Create();
            var actors = SpawnContacts(harness);
            WildernessTwinPillarsTests.Accept(harness, actors.Moore, SmugglingMission);
            if (missionId == SmugglingMission)
            {
                foreach (var recipient in new uint[] { 1, 2, 3 })
                    Deliver(harness, recipient);
                WildernessTwinPillarsTests.Talk(harness, actors.Moore, SmugglingMission, 4);
                WildernessTwinPillarsTests.Reward(harness, actors.Moore, SmugglingMission, null, 44919, 7000, 10000);
            }
            else
            {
                WildernessTwinPillarsTests.Accept(harness, actors.Taylor, SuspicionMission);
                Choose(harness, actors.Taylor, 1);
                RewardBetrayal(harness, actors.Taylor);
            }

            var source = WildernessTwinPillarsTests.Inventory(harness)
                .Single(item => item.ItemTemplate.ItemTemplateId == templateId);
            Assert.AreEqual(1L, WildernessTwinPillarsTests.Held(harness, templateId));
            Assert.IsNull(source.MissionOwnership);
            var abilities = (AbilityManager)Activator.CreateInstance(typeof(AbilityManager),
                BindingFlags.Instance | BindingFlags.NonPublic, null,
                new object[] { harness, harness.Manager }, null);
            abilities.AbilityInit();
            Assert.IsTrue(abilities.TryGetItemAction(templateId, out var actionId, out var nativeArgument));
            Assert.AreEqual(ActionId.ConsumableMedpack, actionId);
            Assert.AreEqual((uint)argument, nativeArgument);
            bool registered;
            lock (Server.Clients)
            {
                registered = Server.Clients.Contains(harness.Client);
                if (!registered)
                    Server.Clients.Add(harness.Client);
            }
            try
            {
                Assert.AreEqual(Race.Human, harness.Client.Player.Race);
                var health = harness.Client.Player.Attributes[Attributes.Health];
                Assert.IsTrue(health.Current > 2);
                var wound = health.Current / 2;
                Assert.AreEqual(wound, ActorManager.Instance.Damage(
                    harness.Map, harness.Client.Player, wound, harness.Client.Player));
                var woundedHealth = health.Current;
                var before = harness.Context.ReadRewardTotals();
                var request = new RequestPerformAbilityPacket
                {
                    ActionId = ActionId.ConsumableMedpack,
                    ActionArgId = argument,
                    ItemId = source.EntityId,
                    Target = default
                };
                harness.Drain();

                abilities.RequestPerformAbility(harness.Client, new RequestPerformAbilityPacket
                {
                    ActionId = request.ActionId,
                    ActionArgId = argument == 1 ? 2 : 1,
                    ItemId = source.EntityId,
                    Target = default
                });

                Assert.IsFalse(harness.Map.PerformRecovery.Any(action =>
                    action.Actor == harness.Client.Player && action.ActionId == ActionId.ConsumableMedpack));
                Assert.IsTrue(harness.Drain().OfType<UserActionFailedPacket>().Any());
                Assert.AreEqual(before, harness.Context.ReadRewardTotals());
                Assert.AreEqual(woundedHealth, health.Current);

                var failed = RequestMedicalRecovery(harness, abilities, request);
                var injected = false;
                harness.Context.AfterSave = _ =>
                {
                    injected = true;
                    throw new DbUpdateException("Injected medical-item consumption persistence failure.");
                };
                try
                {
                    abilities.PerformRecovery(harness.Map, failed);
                }
                finally
                {
                    harness.Context.AfterSave = null;
                }
                Assert.IsTrue(injected);
                Assert.AreEqual(before, harness.Context.ReadRewardTotals());
                Assert.AreEqual(1L, WildernessTwinPillarsTests.Held(harness, templateId));
                Assert.AreEqual(woundedHealth, health.Current);
                Assert.IsFalse(harness.Client.Player.ActiveEffects.Values.Any(effect =>
                    effect.ActionId == ActionId.ConsumableMedpack));
                Assert.IsFalse(harness.Client.Player.ActionReuseUntil.ContainsKey(ActionId.ConsumableMedpack));
                Assert.IsTrue(harness.Drain().OfType<UserActionFailedPacket>().Any());

                var accepted = RequestMedicalRecovery(harness, abilities, request);
                Assert.AreEqual(1L, WildernessTwinPillarsTests.Held(harness, templateId),
                    "Requesting an item action must not consume it before native recovery.");
                abilities.PerformRecovery(harness.Map, accepted);

                Assert.AreEqual(0L, WildernessTwinPillarsTests.Held(harness, templateId));
                var consumed = harness.Context.ReadRewardTotals();
                Assert.AreEqual(before.ItemCount - 1, consumed.ItemCount,
                    "The source item and its matching reagent must be charged only once.");
                Assert.AreEqual(before.Credits, consumed.Credits);
                Assert.AreEqual(before.Experience, consumed.Experience);
                Assert.IsTrue(harness.Client.Player.ActiveEffects.Values.Any(effect =>
                    effect.ActionId == ActionId.ConsumableMedpack));
                GameEffectManager.Instance.DoWork(harness.Map, 0);
                Assert.IsTrue(health.Current > woundedHealth,
                    "The actual action419 effect must heal, not merely remove an inventory item.");
                var healed = health.Current;
                var effects = harness.Client.Player.ActiveEffects.Count;
                harness.Drain();

                abilities.PerformRecovery(harness.Map, accepted);
                abilities.RequestPerformAbility(harness.Client, request);

                Assert.AreEqual(consumed, harness.Context.ReadRewardTotals());
                Assert.AreEqual(healed, health.Current);
                Assert.AreEqual(effects, harness.Client.Player.ActiveEffects.Count);
                Assert.IsFalse(harness.Map.PerformRecovery.Any(action =>
                    action.Actor == harness.Client.Player && action.ActionId == ActionId.ConsumableMedpack));
                Assert.IsTrue(harness.Drain().OfType<UserActionFailedPacket>().Any());
            }
            finally
            {
                harness.Context.AfterSave = null;
                GameEffectManager.Instance.ClearEffects(harness.Map, harness.Client.Player);
                if (!registered)
                    lock (Server.Clients)
                        Server.Clients.Remove(harness.Client);
            }
        }

        private static ActionData RequestMedicalRecovery(WildernessRuntimeTestHarness harness,
            AbilityManager abilities, RequestPerformAbilityPacket request)
        {
            abilities.RequestPerformAbility(harness.Client, request);
            var pending = harness.Map.PerformRecovery.Where(action =>
                action.Actor == harness.Client.Player && action.ActionId == ActionId.ConsumableMedpack).ToArray();
            Assert.AreEqual(1, pending.Length, "The real native medical-item request must enqueue one recovery.");
            Assert.IsTrue(harness.Map.PerformRecovery.Remove(pending[0]));
            return pending[0];
        }

        private static (Creature Moore, Creature Taylor) SpawnContacts(WildernessRuntimeTestHarness harness)
        {
            harness.SpawnWorld(209, 207, 193, 182, 510005);
            return (WildernessTwinPillarsTests.RequireNpc(harness, 209, 131),
                WildernessTwinPillarsTests.RequireNpc(harness, 207, 129));
        }

        private static void Deliver(WildernessRuntimeTestHarness harness, uint objectiveId)
        {
            var (spawn, creature) = objectiveId switch
            {
                1 => (193U, 115U),
                2 => (182U, 105U),
                3 => (510005U, 510005U),
                _ => throw new System.ArgumentOutOfRangeException(nameof(objectiveId))
            };
            WildernessTwinPillarsTests.Talk(harness,
                WildernessTwinPillarsTests.RequireNpc(harness, spawn, creature), SmugglingMission, objectiveId);
        }

        private static void Choose(WildernessRuntimeTestHarness harness, Creature taylor, int index)
        {
            OpenChoice(harness, taylor);
            new NpcManager(harness, harness.Manager).PerformNPCChoice(harness.Client, new PerformNPCChoicePacket
            {
                EntityId = taylor.EntityId,
                MissionId = SuspicionMission,
                ObjectiveId = 2,
                PlayerFlagId = 1,
                ChoiceIdx = index
            });
        }

        private static void RewardBetrayal(WildernessRuntimeTestHarness harness, Creature taylor)
        {
            var conversation = WildernessTwinPillarsTests.OpenNativeNpcConversation(harness, taylor);
            var previews = (System.Collections.Generic.Dictionary<uint, RewardInfo>)
                conversation.ConvoDataDict[ConversationType.MissionComplete];
            var items = previews[SuspicionMission].FixedReward.FixedItems;
            CollectionAssert.AreEqual(new uint[] { 44917, 44918 },
                items.Select(item => item.ItemTemplateId).OrderBy(id => id).ToArray());
            Assert.IsTrue(items.All(item => item.Quantity == 1));
            WildernessTwinPillarsTests.Reward(harness, taylor, SuspicionMission, null, null, 3500, 700);
            Assert.AreEqual(1L, WildernessTwinPillarsTests.Held(harness, 44917));
            Assert.AreEqual(1L, WildernessTwinPillarsTests.Held(harness, 44918));
        }

        private static void Accept(WildernessRuntimeTestHarness harness, NpcManager npcs, Creature npc, uint missionId)
        {
            WildernessTwinPillarsTests.OpenNativeNpcConversation(harness, npc);
            npcs.AssignNPCMission(harness.Client, new AssignNPCMissionPacket
            {
                NpcEntityId = npc.EntityId,
                MissionId = missionId
            });
            Assert.IsTrue(harness.Client.Player.Missions.ContainsKey(missionId),
                $"Native NPC {npc.DbId} must offer and accept mission {missionId} without a synthetic assignment.");
            harness.Drain();
        }

        private static void OpenChoice(WildernessRuntimeTestHarness harness, Creature taylor)
        {
            harness.MoveTo(taylor.Position);
            harness.Drain();
            var conversation = WildernessTwinPillarsTests.OpenNativeNpcConversation(harness, taylor);
            Assert.IsTrue(conversation.ConvoDataDict.ContainsKey(ConversationType.ObjectiveChoice),
                "Taylor must expose the native 791.2 choice, not a completion-only shortcut.");
        }

        private static void AssertDurableStates(WildernessRuntimeTestHarness harness,
            MissionState smugglingState, MissionObjectiveState choiceState)
        {
            using var unit = harness.CreateChar();
            var smuggling = unit.CharacterMissions.GetByCharacterAndMission(harness.Client.Player.Id, SmugglingMission);
            Assert.IsNotNull(smuggling);
            Assert.AreEqual((uint)smugglingState, smuggling.MissionState);
            var suspicion = unit.CharacterMissions.GetByCharacterAndMission(harness.Client.Player.Id, SuspicionMission);
            Assert.IsNotNull(suspicion);
            Assert.AreEqual((uint)MissionState.Active, suspicion.MissionState);
            var objectives = unit.CharacterMissionProgress.GetTracked(harness.Client.Player.Id, SuspicionMission);
            Assert.AreEqual((byte)choiceState, objectives[2].ObjectiveState);
            var aggregateState = choiceState == MissionObjectiveState.Completed
                ? MissionObjectiveState.Completed : MissionObjectiveState.Incomplete;
            Assert.AreEqual((byte)aggregateState, objectives[3].ObjectiveState);
            Assert.AreEqual(aggregateState, harness.Client.Player.Missions[SuspicionMission].Objectives[3].State);
            Assert.AreEqual(choiceState == MissionObjectiveState.Completed, suspicion.Completeable);
        }

        private static void AssertParcels(WildernessRuntimeTestHarness harness, long quantity,
            string assignmentId, uint generation)
        {
            var items = harness.Client.Player.Inventory.PersonalInventory
                .Where(id => id != 0).Select(EntityManager.Instance.GetItem)
                .Where(item => item.ItemTemplate.ItemTemplateId == StimDustTemplate).ToArray();
            Assert.AreEqual(quantity, items.Sum(item => (long)item.StackSize));
            foreach (var item in items)
            {
                Assert.IsNotNull(item.MissionOwnership);
                Assert.AreEqual(SmugglingMission, item.MissionOwnership.MissionId);
                Assert.AreEqual(assignmentId, item.MissionOwnership.AssignmentId);
                Assert.AreEqual(generation, item.MissionOwnership.Generation);
            }
            using var unit = harness.CreateChar();
            var ownership = unit.CharacterMissionItems.GetOwned(harness.Client.Player.Id)
                .Where(item => item.MissionId == SmugglingMission || item.MissionId == SuspicionMission).ToArray();
            Assert.AreEqual(quantity, ownership.Sum(item => (long)item.Quantity));
            foreach (var item in ownership)
            {
                Assert.AreEqual(SmugglingMission, item.MissionId);
                Assert.AreEqual(assignmentId, item.AssignmentId);
                Assert.AreEqual(generation, item.Generation);
                Assert.AreEqual(StimDustTemplate, unit.Items.GetItem(item.ItemId).ItemTemplateId);
            }
        }
    }
}
