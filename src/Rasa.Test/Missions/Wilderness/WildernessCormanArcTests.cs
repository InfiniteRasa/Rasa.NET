using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Game.Handlers;
using Rasa.Managers;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Packets;
using Rasa.Packets.Inventory.Client;
using Rasa.Packets.LootDispenser.Client;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.MapChannel.Server;
using Rasa.Packets.Mission.Server;
using Rasa.Services.Preloader.Missions.Wilderness;
using Rasa.Structures;
using Rasa.Structures.Char;
using Rasa.Structures.World;
using static Rasa.Test.Missions.Wilderness.WildernessDaghdasUrnTests;

namespace Rasa.Test.Missions.Wilderness
{
    [TestClass]
    [DoNotParallelize]
    public class WildernessCormanArcTests
    {
        [TestMethod]
        public void EleanorIssuesTheUntimedFirstBatchAndOffersOrdersByRadio()
        {
            using var harness = CreateIntegratedHub();
            harness.SpawnWorld(173);
            var eleanor = harness.Npc(173);
            Assert.IsNotNull(eleanor, "Use Eleanor's existing migrated public spawn, not a fixture NPC.");
            Assert.AreEqual(94U, eleanor.DbId);
            harness.MoveTo(eleanor.Position);
            harness.Client.Player.Level = 15;
            using (var unit = harness.CreateChar())
                unit.ExecuteTransaction(() => unit.Characters.Get(harness.Client.Player.Id).Level = 15);
            SeedCompletedHistory(harness, 442, 444, 425, 679, 451, 682, 695);
            var npcs = new NpcManager(harness, harness.Manager);

            var beforeElixir = Open(harness, npcs, eleanor.EntityId);
            Assert.IsFalse(Offers(beforeElixir, 700),
                "The first vaccine batch must require completed Elixir Vitae.");
            Assert.IsFalse(Offers(beforeElixir, 820),
                "The replacement batch cannot bypass the Corman chain.");
            Assert.IsFalse(Offers(beforeElixir, 701),
                "Orders From High Command is a radio offer, not Eleanor's ordinary mission.");

            SeedCompletedHistory(harness, 698);
            var firstBatch = Open(harness, npcs, eleanor.EntityId);
            Assert.IsTrue(Offers(firstBatch, 700),
                "Completed Elixir Vitae must unlock native 700, the untimed initial vaccine batch.");
            Assert.IsFalse(Offers(firstBatch, 820),
                "Native 820 is recovery after loss of the first batch, not a higher-ID replacement offer.");
            Assert.IsFalse(Offers(firstBatch, 701));

            npcs.AssignNPCMission(harness.Client, new AssignNPCMissionPacket
            {
                NpcEntityId = eleanor.EntityId,
                MissionId = 700
            });
            harness.Tick();

            var packets = harness.Drain();
            Assert.AreEqual(1, packets.OfType<MissionGainedPacket>().Count(packet => packet.MissionId == 700));
            Assert.IsTrue(harness.Client.Player.Missions.TryGetValue(700, out var mission));
            Assert.AreEqual(MissionState.Active, mission.State);
            CollectionAssert.AreEquivalent(new uint[] { 1, 2, 3, 5, 6 }, mission.Objectives.Keys.ToArray(),
                "The first batch retains its native objectives and must not acquire 820's timed objective 7.");
            foreach (var recipient in new uint[] { 1, 2, 3 })
                Assert.AreEqual(MissionObjectiveState.Incomplete, mission.Objectives[recipient].State);
            Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(820));
            Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(701),
                "Radio notification must not auto-accept Orders From High Command.");
            Assert.AreEqual(1, packets.OfType<DispenseRadioMissionPacket>()
                .Count(packet => packet.MissionId == 701),
                "The first batch must trigger one native radio offer while the vaccines are carried.");
            Assert.AreEqual(0, packets.OfType<MissionGainedPacket>().Count(packet => packet.MissionId == 701));

            var vaccines = EntityManager.Instance.Items.Values
                .Where(item => item.ItemTemplateId == 2356 &&
                    harness.Client.Player.Inventory.PersonalInventory.Contains(item.EntityId))
                .ToArray();
            Assert.AreEqual(3U, vaccines.Aggregate(0U, (quantity, item) => quantity + item.StackSize),
                "Accepting 700 must put three real vaccine items in the character's inventory.");
            Assert.IsTrue(vaccines.All(item =>
                item.MissionOwnership?.CharacterId == harness.Client.Player.Id &&
                item.MissionOwnership.MissionId == 700 &&
                item.MissionOwnership.AssignmentId == mission.AssignmentId &&
                item.MissionOwnership.Generation == mission.Generation),
                "The first batch must belong to this exact 700 assignment, not unbound same-template inventory.");

            using var verify = harness.CreateChar();
            var assignment = verify.CharacterMissions.GetByCharacterAndMission(harness.Client.Player.Id, 700);
            Assert.IsNotNull(assignment);
            Assert.AreEqual(mission.AssignmentId, assignment.AssignmentId);
            Assert.AreEqual(mission.Generation, assignment.Generation);
            Assert.IsNull(verify.CharacterMissionDeadlines.Get(harness.Client.Player.Id, 700),
                "The ordinary initial batch is untimed.");
            var owned = verify.CharacterMissionItems.GetOwned(harness.Client.Player.Id)
                .Where(item => item.MissionId == 700 &&
                    item.AssignmentId == assignment.AssignmentId && item.Generation == assignment.Generation)
                .ToArray();
            Assert.AreEqual(3U, owned.Aggregate(0U, (quantity, item) => quantity + item.Quantity));
            Assert.IsTrue(owned.All(item => verify.Items.GetItem(item.ItemId).ItemTemplateId == 2356));

            var offer = verify.MissionOffers.Get(harness.Client.Player.Id, 701);
            Assert.IsNotNull(offer, "The native radio packet needs a durable pending authorization.");
            Assert.AreEqual(MissionOfferState.Pending, offer.State);
            Assert.AreEqual(MissionOfferSourceKind.Scene, offer.SourceKind);
            Assert.AreEqual(harness.Client.MissionSessionId, offer.SessionId);
            Assert.AreEqual(assignment.AssignmentId, offer.SourceAssignmentId);
            Assert.AreEqual(assignment.Generation, offer.SourceAssignmentGeneration);
            Assert.AreEqual(offer.CreatedAtUtc.AddMinutes(5), offer.ExpiresAtUtc);
            var source = verify.CharacterMissions.Runtime.Scene(offer.SourceInstanceId);
            Assert.IsNotNull(source);
            Assert.AreEqual(700U, source.MissionId);
            Assert.AreEqual(assignment.AssignmentId, source.AssignmentId);
            Assert.AreEqual(source.Generation, offer.SourceGeneration);
            Assert.AreEqual(source.ScriptKey, offer.SourceKey);
            Assert.IsTrue(source.Status is "Running" or "Waiting",
                "The radio sender must be the live first-batch scene, not a retired source or dummy NPC.");
        }

        [TestMethod]
        public void ReplacementRequiresAFailedFirstBatchAndItsDeadlineDoesNotRestartAfterDelivery()
        {
            using var harness = CreateMedicineHarness();
            harness.SpawnWorld(173, 510004);
            SeedHistory(harness, 698);
            var eleanor = harness.Npc(173);
            Assert.IsNotNull(eleanor);
            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, eleanor.EntityId, 820));
            SeedFirstBatchFailure(harness);
            var acceptedAt = harness.UtcNow;
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, eleanor.EntityId, 820));
            Assert.AreEqual(3U, Owned(harness, 820, 2356));
            DateTime deadline;
            using (var unit = harness.CreateChar())
            {
                deadline = unit.CharacterMissionDeadlines.Get(harness.Client.Player.Id, 820).DueAtUtc;
                Assert.AreEqual(acceptedAt.AddSeconds(900), deadline);
                Assert.IsNull(unit.MissionOffers.Get(harness.Client.Player.Id, 701),
                    "The replacement batch must not issue the first-batch interruption.");
            }
            harness.Tick(30000);
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, harness.Npc(510004).EntityId, 820, 2, 1));
            harness.Tick();
            Assert.AreEqual(2U, Owned(harness, 820, 2356));
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, harness.Npc(510004).EntityId, 820, 2, 1));
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, eleanor.EntityId, 820, 5, 1));
            using var after = harness.CreateChar();
            Assert.AreEqual(deadline, after.CharacterMissionDeadlines.Get(harness.Client.Player.Id, 820).DueAtUtc);
        }

        [TestMethod]
        public void ReplacementConsumesOnePerRecipientAndCannotPayEitherMedicineVariantTwice()
        {
            using var harness = CreateMedicineHarness();
            harness.SpawnWorld(173, 203, 510004, 186);
            SeedHistory(harness, 698);
            SeedFirstBatchFailure(harness);
            var eleanor = harness.Npc(173);
            Assert.IsNotNull(eleanor);
            AssertRewardOffer(harness, eleanor, 820, 550, 13388, 12915);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, eleanor.EntityId, 820));
            DeliverMedicine(harness, 820);
            Assert.AreEqual(0U, Owned(harness, 820, 2356));
            var credits = harness.Client.Player.Credits[CurencyType.Credits];
            var experience = harness.Client.Player.Experience;
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, eleanor.EntityId, 820, 5, 1));
            harness.Tick();
            Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[820].Objectives[7].State);
            Assert.IsTrue(harness.Manager.CompleteOfferedMission(harness.Client, eleanor.EntityId, 820, 0));
            Assert.AreEqual(credits + 550, harness.Client.Player.Credits[CurencyType.Credits]);
            Assert.AreEqual(experience + 11000, harness.Client.Player.Experience);
            Assert.AreEqual(1U, Held(harness, 13388));
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, eleanor.EntityId, 820, 0));
            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, eleanor.EntityId, 700));
            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, eleanor.EntityId, 820));
            var after = Open(harness, new NpcManager(harness, harness.Manager), eleanor.EntityId);
            Assert.IsFalse(Offers(after, 700));
            Assert.IsFalse(Offers(after, 820));
            using var unit = harness.CreateChar();
            Assert.AreEqual(CharacterMissionDeadlineState.Satisfied,
                unit.CharacterMissionDeadlines.Get(harness.Client.Player.Id, 820).State);
            VerifyArmorReward(harness, 13388, 1, 250, CharacterClass.Soldier, 21);
        }

        [TestMethod]
        public void OpenEleanorDialogueCannotCompleteReplacementAtItsExactDeadlineWithoutATick()
        {
            using var harness = CreateMedicineHarness();
            harness.SpawnWorld(173, 203, 510004, 186);
            SeedHistory(harness, 698);
            SeedFirstBatchFailure(harness);
            var eleanor = harness.Npc(173);
            Assert.IsNotNull(eleanor);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, eleanor.EntityId, 820));
            DeliverMedicine(harness, 820);
            harness.MoveTo(eleanor.Position);
            var npcs = new NpcManager(harness, harness.Manager);
            Open(harness, npcs, eleanor.EntityId);
            using (var unit = harness.CreateChar())
                harness.UtcNow = DateTime.SpecifyKind(
                    unit.CharacterMissionDeadlines.Get(harness.Client.Player.Id, 820).DueAtUtc, DateTimeKind.Utc);
            var credits = harness.Client.Player.Credits[CurencyType.Credits];
            npcs.CompleteNPCObjective(harness.Client, new CompleteNPCObjectivePacket
            {
                EntityId = eleanor.EntityId, MissionId = 820, ObjectiveId = 5, PlayerFlagId = 1
            });
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[820].Objectives[5].State);
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, eleanor.EntityId, 820, 0));
            Assert.AreEqual(credits, harness.Client.Player.Credits[CurencyType.Credits]);
        }

        [TestMethod]
        public void ExpiredReplacementCleansOnlyItsBatchAndCannotExpireTheNextAssignment()
        {
            using var harness = CreateMedicineHarness();
            harness.SpawnWorld(173);
            SeedHistory(harness, 698);
            SeedFirstBatchFailure(harness);
            GrantUnbound(harness, 2356, 1);
            var eleanor = harness.Npc(173);
            Assert.IsNotNull(eleanor);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, eleanor.EntityId, 820));
            var oldAssignment = harness.Client.Player.Missions[820].AssignmentId;
            string oldRun;
            string timerName;
            uint oldGeneration;
            using (var unit = harness.CreateChar())
            {
                var run = unit.CharacterMissions.Runtime.AssignmentScene(oldAssignment);
                oldRun = run.RunId;
                oldGeneration = run.Generation;
                timerName = unit.CharacterMissions.Runtime.Timers(oldRun)
                    .Single(timer => timer.MissionId == 820 && timer.ObjectiveId == 7).Name;
            }
            harness.Tick(900000);
            Assert.AreEqual(MissionState.Failed, harness.Client.Player.Missions[820].State);
            Assert.AreEqual(0U, Owned(harness, 820, 2356));
            Assert.AreEqual(1U, Held(harness, 2356),
                "Expiry must preserve the unrelated copy of the same vaccine template.");
            var retryAt = harness.UtcNow;
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, eleanor.EntityId, 820));
            Assert.AreNotEqual(oldAssignment, harness.Client.Player.Missions[820].AssignmentId);
            Assert.AreEqual(3U, Owned(harness, 820, 2356));
            Assert.IsFalse(harness.Manager.Scenes.Submit(oldRun,
                new SceneObservation(SceneEventKind.ObjectiveDeadlineElapsed, oldGeneration, Name: timerName)));
            Assert.AreEqual(MissionState.Active, harness.Client.Player.Missions[820].State);
            using var after = harness.CreateChar();
            Assert.AreEqual(retryAt.AddSeconds(900), after.CharacterMissionDeadlines.Get(harness.Client.Player.Id, 820).DueAtUtc);
        }

        [TestMethod]
        [TestCategory("WildernessG2")]
        public void FullBagsCannotAcceptTheInitialBatchOrAuthorizeRadioFromUnboundVaccines()
        {
            using var harness = CreateIntegratedHub();
            harness.SpawnWorld(173);
            SeedHistory(harness, 698);
            var template = ItemManager.Instance.GetItemTemplateById(2356);
            var stack = EntityClassManager.Instance.GetClassInfo(template.Class).ItemClassInfo.StackSize;
            GrantUnbound(harness, 2356, checked(stack * 50));
            var eleanor = harness.Npc(173);
            Assert.IsNotNull(eleanor);
            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, eleanor.EntityId, 700));
            Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(700));
            using (var unit = harness.CreateChar())
            {
                Assert.IsNull(unit.CharacterMissions.GetByCharacterAndMission(harness.Client.Player.Id, 700));
                Assert.IsNull(unit.MissionOffers.Get(harness.Client.Player.Id, 701));
                Assert.AreEqual(0, unit.CharacterMissionItems.GetOwned(harness.Client.Player.Id).Count);
            }
            var filler = harness.Client.Player.Inventory.PersonalInventory.Where(id => id != 0)
                .Select(EntityManager.Instance.GetItem).First(item => item.ItemTemplateId == 2356);
            InventoryManager.Instance.PersonalInventory_DestroyItem(harness.Client,
                new PersonalInventory_DestroyItemPacket { EntityId = filler.EntityId, Quantity = filler.StackSize });
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, eleanor.EntityId, 700));
            harness.Tick();
            Assert.AreEqual(3U, Owned(harness, 700, 2356));
            using var after = harness.CreateChar();
            Assert.AreEqual(MissionOfferState.Pending, after.MissionOffers.Get(harness.Client.Player.Id, 701).State);
        }

        [TestMethod]
        [TestCategory("WildernessG2")]
        [DataRow(1, 26U)]
        [DataRow(2, 1U)]
        [DataRow(0, 27U)]
        public void NativeSkeevOutcomesPreserveExclusiveBatchConsequencesAndBeachamsTurnIn(int choice, uint flag)
        {
            using var harness = CreateIntegratedHub();
            harness.SpawnWorld(173, 171, 510002, 203, 510004, 186);
            var skeev = SpawnSkeev(harness);
            SeedHistory(harness, 698);
            GrantUnbound(harness, 2356, 1);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, harness.Npc(173).EntityId, 700));
            harness.Tick();
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, harness.Npc(510004).EntityId, 700, 2, 1));
            harness.Tick();
            Assert.AreEqual(2U, Owned(harness, 700, 2356));
            AcceptRadio(harness.Client, 701);
            Assert.IsTrue(harness.Client.Player.Missions.ContainsKey(701));
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, harness.Npc(171).EntityId, 701, 1, 1));
            harness.Tick();
            var npcs = new NpcManager(harness, harness.Manager);
            harness.MoveTo(skeev.Position);
            Open(harness, npcs, skeev.EntityId);
            npcs.PerformNPCChoice(harness.Client, new PerformNPCChoicePacket
            {
                EntityId = skeev.EntityId, MissionId = 701, ObjectiveId = 2, PlayerFlagId = 1, ChoiceIdx = 3
            });
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[701].Objectives[2].State);
            Assert.AreEqual(2U, Owned(harness, 700, 2356));
            if (choice == 0)
            {
                harness.Tick(60000);
                Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[701].Objectives[7].State,
                    "Ultimatum expiry must execute its authored continuation, not become a permanent mission-wide deadline block.");
                Assert.AreEqual(MissionState.Active, harness.Client.Player.Missions[701].State);
            }
            else
            {
                npcs.PerformNPCChoice(harness.Client, new PerformNPCChoicePacket
                {
                    EntityId = skeev.EntityId, MissionId = 701, ObjectiveId = 2, PlayerFlagId = 1, ChoiceIdx = choice
                });
                harness.Tick();
            }
            if (choice == 1)
            {
                Assert.AreEqual(MissionState.Failed, harness.Client.Player.Missions[700].State);
                Assert.AreEqual(0U, Owned(harness, 700, 2356));
                Assert.AreEqual(1U, Held(harness, 2356));
            }
            else
            {
                Assert.AreEqual(MissionState.Active, harness.Client.Player.Missions[700].State);
                Assert.AreEqual(2U, Owned(harness, 700, 2356));
                Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[701].Objectives[3].State);
                Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, harness.Npc(171).EntityId, 701, 8, flag));
                Kill(harness, skeev);
            }
            var burke = harness.Npc(171);
            harness.MoveTo(burke.Position);
            Open(harness, npcs, burke.EntityId);
            foreach (var wrong in new uint[] { 1, 26, 27 }.Where(candidate => candidate != flag))
            {
                npcs.CompleteNPCObjective(harness.Client, new CompleteNPCObjectivePacket
                {
                    EntityId = burke.EntityId, MissionId = 701, ObjectiveId = 8, PlayerFlagId = wrong
                });
                Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[701].Objectives[8].State,
                    "A native Burke flag from another outcome must not complete the report.");
            }
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, burke.EntityId, 701, 8, flag));
            var beacham = harness.Npc(510002);
            Assert.IsNotNull(beacham);
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, burke.EntityId, 701, 6, 1));
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, beacham.EntityId, 701, 6, 1));
            var credits = harness.Client.Player.Credits[CurencyType.Credits];
            Assert.IsTrue(harness.Manager.CompleteOfferedMission(harness.Client, beacham.EntityId, 701, null));
            Assert.AreEqual(credits + 2200, harness.Client.Player.Credits[CurencyType.Credits]);
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, beacham.EntityId, 701, null));

            var medicine = choice == 1 ? 820U : 700U;
            var eleanor = harness.Npc(173);
            if (medicine == 820)
                Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, eleanor.EntityId, 820));
            else
                Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, eleanor.EntityId, 820));
            DeliverMedicine(harness, medicine);
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, eleanor.EntityId, medicine, 5, 1));
            harness.Tick();
            Assert.IsTrue(harness.Manager.CompleteOfferedMission(harness.Client, eleanor.EntityId, medicine, 1));
            Assert.AreEqual(1U, Held(harness, 12915));
            Assert.AreEqual(1U, Held(harness, 2356), "Neither route may consume unrelated same-template inventory.");
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, eleanor.EntityId, medicine, 1));
            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, eleanor.EntityId, 700));
            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, eleanor.EntityId, 820));
            VerifyArmorReward(harness, 12915, 16, 234, CharacterClass.Specialist, 30);
        }

        [TestMethod]
        [TestCategory("WildernessG2")]
        public void SkeevDoesNotAcquireTargetsBeforeTheChoiceButActuallyFightsAfterRefusal()
        {
            using var harness = CreateIntegratedHub();
            harness.SpawnWorld(173, 171);
            var skeev = SpawnSkeev(harness);
            Assert.AreNotEqual(Factions.AFS, skeev.Faction, "The real hostile faction must not be changed to fake passivity.");
            ManifestationManager.Instance.UpdateStatsValues(harness.Client, true);
            harness.MoveTo(skeev.Position + new Vector3(0, 0, 2));
            var before = harness.Client.Player.Attributes[Attributes.Health].Current +
                harness.Client.Player.Attributes[Attributes.Armor].Current;
            for (var tick = 0; tick < 20; tick++)
                CombatTick(harness);
            Assert.AreNotEqual(BehaviorManager.BehaviorActionFighting, skeev.Controller.CurrentAction,
                "The canonical unleased Skeev must not autonomously acquire a nearby player or Burke.");
            Assert.AreEqual(before, harness.Client.Player.Attributes[Attributes.Health].Current +
                harness.Client.Player.Attributes[Attributes.Armor].Current);

            SeedHistory(harness, 698);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, harness.Npc(173).EntityId, 700));
            harness.Tick();
            AcceptRadio(harness.Client, 701);
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, harness.Npc(171).EntityId, 701, 1, 1));
            harness.Tick();
            harness.MoveTo(skeev.Position + new Vector3(0, 0, 2));
            for (var tick = 0; tick < 20; tick++)
                CombatTick(harness);
            Assert.AreNotEqual(BehaviorManager.BehaviorActionFighting, skeev.Controller.CurrentAction);
            Assert.AreEqual(3U, Owned(harness, 700, 2356));
            var npcs = new NpcManager(harness, harness.Manager);
            Open(harness, npcs, skeev.EntityId);
            npcs.PerformNPCChoice(harness.Client, new PerformNPCChoicePacket
            {
                EntityId = skeev.EntityId, MissionId = 701, ObjectiveId = 2, PlayerFlagId = 1, ChoiceIdx = 2
            });
            var combatBefore = harness.Client.Player.Attributes[Attributes.Health].Current +
                harness.Client.Player.Attributes[Attributes.Armor].Current;
            for (var tick = 0; tick < 80 && harness.Client.Player.Attributes[Attributes.Health].Current +
                 harness.Client.Player.Attributes[Attributes.Armor].Current >= combatBefore; tick++)
                CombatTick(harness);
            Assert.AreEqual(BehaviorManager.BehaviorActionFighting, skeev.Controller.CurrentAction);
            Assert.IsTrue(harness.Client.Player.Attributes[Attributes.Health].Current +
                harness.Client.Player.Attributes[Attributes.Armor].Current < combatBefore,
                "Refusal must start actual Skeev combat, not just change a quest state or appearance.");
            Assert.AreEqual(3U, Owned(harness, 700, 2356));
        }

        [TestMethod]
        [TestCategory("WildernessG2")]
        public void ExpiredRadioReissuesOnReconnectWithoutGivingAnOldSessionAuthority()
        {
            using var harness = CreateIntegratedHub();
            harness.SpawnWorld(173);
            SpawnSkeev(harness);
            SeedHistory(harness, 698);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, harness.Npc(173).EntityId, 700));
            harness.Tick();
            CharacterMissionOfferEntry original;
            using (var unit = harness.CreateChar())
                original = unit.MissionOffers.Get(harness.Client.Player.Id, 701);
            Assert.IsNotNull(original);
            harness.UtcNow = DateTime.SpecifyKind(original.ExpiresAtUtc, DateTimeKind.Utc);
            AcceptRadio(harness.Client, 701);
            Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(701));
            var reconnected = Reconnect(harness);
            try
            {
                harness.Manager.Scenes.Resume(reconnected);
                using (var unit = harness.CreateChar())
                {
                    var current = unit.MissionOffers.Get(reconnected.Player.Id, 701);
                    Assert.AreEqual(MissionOfferState.Pending, current.State);
                    Assert.AreNotEqual(original.OfferId, current.OfferId);
                    Assert.AreEqual(reconnected.MissionSessionId, current.SessionId);
                    Assert.AreEqual(original.SourceAssignmentId, current.SourceAssignmentId);
                    Assert.AreEqual(original.SourceAssignmentGeneration, current.SourceAssignmentGeneration);
                }
                AcceptRadio(harness.Client, 701);
                Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(701));
                AcceptRadio(reconnected, 701);
                Assert.AreEqual(MissionState.Active, reconnected.Player.Missions[701].State);
            }
            finally
            {
                CellManager.Instance.RemoveFromWorld(reconnected);
                harness.Map.ClientList.Remove(reconnected);
            }
        }

        [TestMethod]
        [TestCategory("WildernessG2")]
        public void RetiredFirstBatchSourceCannotOfferAgainstTheNextVaccineAssignment()
        {
            using var harness = CreateIntegratedHub();
            harness.SpawnWorld(173);
            SeedHistory(harness, 698);
            var eleanor = harness.Npc(173);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, eleanor.EntityId, 700));
            harness.Tick();
            CharacterMissionOfferEntry original;
            using (var unit = harness.CreateChar())
                original = unit.MissionOffers.Get(harness.Client.Player.Id, 701);
            Assert.IsNotNull(original);
            new NpcManager(harness, harness.Manager).AbandonMission(harness.Client,
                new AbandonMissionPacket { MissionId = 700 });
            Assert.AreEqual(0U, Owned(harness, 700, 2356));
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, eleanor.EntityId, 700));
            harness.Tick();
            Assert.AreNotEqual(original.SourceAssignmentId, harness.Client.Player.Missions[700].AssignmentId);
            Assert.IsFalse(harness.Manager.Offers.TryOffer(harness.Client, 701,
                new MissionOfferSourceIdentity(original.SourceKind, original.SourceKey, original.SourceInstanceId,
                    original.SourceGeneration, original.SourceAssignmentId, original.SourceAssignmentGeneration)));
            Assert.AreEqual(3U, Owned(harness, 700, 2356));
        }

        [TestMethod]
        [TestCategory("WildernessG2")]
        public void DeliveringTheFirstBatchInvalidatesItsStillPendingRadioOffer()
        {
            using var harness = CreateIntegratedHub();
            harness.SpawnWorld(173, 203, 510004, 186);
            SpawnSkeev(harness);
            SeedHistory(harness, 698);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, harness.Npc(173).EntityId, 700));
            harness.Tick();
            using (var unit = harness.CreateChar())
                Assert.AreEqual(MissionOfferState.Pending, unit.MissionOffers.Get(harness.Client.Player.Id, 701).State);
            DeliverMedicine(harness, 700);
            Assert.AreEqual(MissionState.Active, harness.Client.Player.Missions[700].State);
            Assert.AreEqual(0U, Owned(harness, 700, 2356));
            AcceptRadio(harness.Client, 701);
            Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(701),
                "An active medicine journal entry alone must not authorize radio after its vaccines are delivered.");
        }

        [TestMethod]
        public void ReplacementReconnectKeepsItsDeadlineAndDeliveredRecipient()
        {
            using var harness = CreateMedicineHarness();
            harness.SpawnWorld(173, 510004);
            SeedHistory(harness, 698);
            SeedFirstBatchFailure(harness);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, harness.Npc(173).EntityId, 820));
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, harness.Npc(510004).EntityId, 820, 2, 1));
            harness.Tick();
            DateTime due;
            using (var unit = harness.CreateChar())
                due = unit.CharacterMissionDeadlines.Get(harness.Client.Player.Id, 820).DueAtUtc;
            harness.Tick(30000);
            var reconnected = Reconnect(harness);
            try
            {
                harness.Manager.Scenes.Resume(reconnected);
                Assert.AreEqual(MissionObjectiveState.Completed, reconnected.Player.Missions[820].Objectives[2].State);
                using (var unit = harness.CreateChar())
                {
                    Assert.AreEqual(due, unit.CharacterMissionDeadlines.Get(reconnected.Player.Id, 820).DueAtUtc);
                    Assert.AreEqual(2U, unit.CharacterMissionItems.GetOwned(reconnected.Player.Id)
                        .Where(item => item.MissionId == 820).Aggregate(0U, (count, item) => count + item.Quantity));
                }
                harness.UtcNow = due;
                harness.Manager.Scenes.Tick(harness.Map);
                Assert.AreEqual(MissionState.Failed, reconnected.Player.Missions[820].State);
                using var after = harness.CreateChar();
                Assert.IsFalse(after.CharacterMissionItems.GetOwned(reconnected.Player.Id).Any(item => item.MissionId == 820));
            }
            finally
            {
                CellManager.Instance.RemoveFromWorld(reconnected);
                harness.Map.ClientList.Remove(reconnected);
            }
        }

        [TestMethod]
        [TestCategory("WildernessG2")]
        public void UltimatumReconnectKeepsSixtySecondsAndTimeoutPreservesTheFirstBatch()
        {
            using var harness = CreateIntegratedHub();
            harness.SpawnWorld(173, 171);
            var skeev = SpawnSkeev(harness);
            SeedHistory(harness, 698);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, harness.Npc(173).EntityId, 700));
            harness.Tick();
            AcceptRadio(harness.Client, 701);
            var began = harness.UtcNow;
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, harness.Npc(171).EntityId, 701, 1, 1));
            harness.Tick();
            DateTime due;
            using (var unit = harness.CreateChar())
            {
                due = unit.CharacterMissionDeadlines.Get(harness.Client.Player.Id, 701).DueAtUtc;
                Assert.AreEqual(began.AddSeconds(60), due);
            }
            harness.Tick(30000);
            var reconnected = Reconnect(harness);
            try
            {
                harness.Manager.Scenes.Resume(reconnected);
                using (var unit = harness.CreateChar())
                    Assert.AreEqual(due, unit.CharacterMissionDeadlines.Get(reconnected.Player.Id, 701).DueAtUtc);
                harness.UtcNow = due;
                harness.Manager.Scenes.Tick(harness.Map);
                Assert.AreEqual(MissionObjectiveState.Incomplete, reconnected.Player.Missions[701].Objectives[3].State);
                Assert.AreEqual(MissionState.Active, reconnected.Player.Missions[700].State);
                using (var unit = harness.CreateChar())
                    Assert.AreEqual(3U, unit.CharacterMissionItems.GetOwned(reconnected.Player.Id)
                        .Where(item => item.MissionId == 700).Aggregate(0U, (count, item) => count + item.Quantity));
                reconnected.SetWorldPosition(skeev.Position, reconnected.Player.Rotation);
                CellManager.Instance.UpdateVisibility(reconnected);
                skeev.Attributes[Attributes.Health].Current = 0;
                harness.Creatures.HandleCreatureKill(harness.Map, skeev, reconnected.Player);
                harness.Tick();
                Assert.IsFalse(harness.Manager.CompleteOfferedObjective(reconnected, harness.Npc(171).EntityId, 701, 8, 1));
                Assert.IsTrue(harness.Manager.CompleteOfferedObjective(reconnected, harness.Npc(171).EntityId, 701, 8, 27));
            }
            finally
            {
                CellManager.Instance.RemoveFromWorld(reconnected);
                harness.Map.ClientList.Remove(reconnected);
            }
        }

        [TestMethod]
        [TestCategory("WildernessG2")]
        public void CormanArcRunsFromQuarantineThroughTheOutdoorCouncilChainToBeacham()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            harness.SpawnWorld(218, 203, 173, 178, 169, 170, 174, 175, 176, 177, 191,
                156, 520065, 171, 510002, 510004, 186);
            var skeev = SpawnSkeev(harness);
            using (var unit = harness.CreateChar())
                unit.ExecuteTransaction(() => unit.CharacterFlags.Set(harness.Client.Player.Id, CharacterFlagIds.BootcampComplete, 1));
            harness.Client.Player.StartingExperienceCompleted = true;
            var victor = harness.Npc(218);
            var duncan = harness.Npc(203);
            var eleanor = harness.Npc(173);
            Assert.IsNotNull(victor);
            Assert.IsNotNull(duncan);
            Assert.IsNotNull(eleanor);

            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, victor.EntityId, 442));
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, duncan.EntityId, 442, 1, 1));
            Assert.AreEqual(1U, Owned(harness, 442, 749));
            var analyzer = harness.Map.DynamicObjects.SingleOrDefault(obj =>
                obj.SceneMissionId == 442 && obj.SceneActorRole == "blood-analyzer" &&
                obj.SceneOwnerCharacterId == harness.Client.Player.Id && obj.IsInWorld);
            Assert.IsNotNull(analyzer);
            Assert.AreEqual(7123U, (uint)analyzer.EntityClassId);
            Assert.IsNotNull(analyzer.MissionConversation);
            Assert.AreEqual(1486U, analyzer.MissionConversation.NpcPackageId);
            harness.MoveTo(new Vector3(-124.8f, 220.91783f, -477.2f));
            var analysisNpcs = new NpcManager(harness, harness.Manager);
            Open(harness, analysisNpcs, analyzer.EntityId);
            analysisNpcs.CompleteNPCObjective(harness.Client, new CompleteNPCObjectivePacket
            {
                EntityId = analyzer.EntityId, MissionId = 442, ObjectiveId = 2, PlayerFlagId = 1
            });
            Assert.AreEqual(0U, Owned(harness, 442, 749));
            Assert.IsTrue(harness.Manager.CompleteOfferedMission(harness.Client, victor.EntityId, 442, 0));
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, victor.EntityId, 444));
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, eleanor.EntityId, 444, 1, 1));
            Assert.IsTrue(harness.Manager.CompleteOfferedMission(harness.Client, eleanor.EntityId, 444, 0));

            var samuel = harness.Npc(178);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, eleanor.EntityId, 425));
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, samuel.EntityId, 425, 1, 1));
            var graal = LiveCreature(harness, 90, 169);
            Kill(harness, graal);
            var loot = harness.Map.LootDispensers[graal.CorpseLootEntityId];
            LootDispenserManager.Instance.RequestLootAllFromCorpse(harness.Client,
                new RequestLootAllFromCorpsePacket { EntityId = loot.EntityId });
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, samuel.EntityId, 425, 5, 1));
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, eleanor.EntityId, 425, 3, 1));
            Assert.IsTrue(harness.Manager.CompleteOfferedMission(harness.Client, eleanor.EntityId, 425, 0));
            var nula = harness.Npc(170);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, eleanor.EntityId, 679));
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, nula.EntityId, 679, 1, 1));
            Assert.IsTrue(harness.Manager.CompleteOfferedMission(harness.Client, nula.EntityId, 679, null));
            var doyan = harness.Npc(175);
            var todae = harness.Npc(174);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, nula.EntityId, 451));
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, doyan.EntityId, 451, 1, 1));
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, todae.EntityId, 451, 2, 1));
            Assert.IsTrue(harness.Manager.CompleteOfferedMission(harness.Client, todae.EntityId, 451, 0));

            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, todae.EntityId, 682));
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, harness.Npc(176).EntityId, 682, 2, 1));
            harness.Tick();
            var attackers = Creatures(harness).Where(creature => creature.DbId == 87 &&
                creature.SpawnPool?.ScenarioMissionId == 682 &&
                creature.SpawnPool.ScenarioOwnerCharacterId == harness.Client.Player.Id &&
                creature.State != CharacterState.Dead).ToArray();
            Assert.AreEqual(3, attackers.Length);
            foreach (var attacker in attackers)
                Kill(harness, attacker);
            for (var tick = 0; tick < 160 && harness.Manager.PublicActors.Handle(harness.Map, 176) != null; tick++)
            {
                harness.SpawnWorldAfter(250, 176);
                harness.Tick();
            }
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, harness.Npc(176).EntityId, 682, 5, 1));
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, harness.Npc(177).EntityId, 682, 4, 1));
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, doyan.EntityId, 682, 6, 1));
            Assert.IsTrue(harness.Manager.CompleteOfferedMission(harness.Client, doyan.EntityId, 682, 1));
            var gadfly = harness.Npc(191);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, doyan.EntityId, 695));
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, gadfly.EntityId, 695, 4, 1));
            Kill(harness, LiveCreature(harness, 80, 156));
            Kill(harness, LiveCreature(harness, 75, 156));
            Kill(harness, LiveCreature(harness, 79, 520065));
            for (var index = 1; index <= 5; index++)
                Use(harness, Object(harness, 695, $"tinctu-{index}"));
            Assert.IsTrue(harness.Manager.CompleteOfferedMission(harness.Client, gadfly.EntityId, 695, 0));
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, gadfly.EntityId, 698));
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, eleanor.EntityId, 698, 1, 1));
            Assert.IsTrue(harness.Manager.CompleteOfferedMission(harness.Client, eleanor.EntityId, 698, 0));

            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, eleanor.EntityId, 700));
            harness.Tick();
            AcceptRadio(harness.Client, 701);
            var burke = harness.Npc(171);
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, burke.EntityId, 701, 1, 1));
            harness.Tick();
            harness.MoveTo(skeev.Position);
            var npcs = new NpcManager(harness, harness.Manager);
            Open(harness, npcs, skeev.EntityId);
            npcs.PerformNPCChoice(harness.Client, new PerformNPCChoicePacket
            {
                EntityId = skeev.EntityId, MissionId = 701, ObjectiveId = 2, PlayerFlagId = 1, ChoiceIdx = 2
            });
            harness.Tick();
            Kill(harness, skeev);
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, burke.EntityId, 701, 8, 1));
            var beacham = harness.Npc(510002);
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, beacham.EntityId, 701, 6, 1));
            Assert.IsTrue(harness.Manager.CompleteOfferedMission(harness.Client, beacham.EntityId, 701, null));
            DeliverMedicine(harness, 700);
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, eleanor.EntityId, 700, 5, 1));
            Assert.IsTrue(harness.Manager.CompleteOfferedMission(harness.Client, eleanor.EntityId, 700, 0));
            using var history = harness.CreateChar();
            foreach (var mission in new uint[] { 442, 444, 425, 679, 451, 682, 695, 698, 700, 701 })
                Assert.IsTrue(history.CharacterMissions.Runtime.EverSucceeded(harness.Client.Player.Id, mission));
            Assert.IsFalse(history.CharacterMissions.Runtime.EverSucceeded(harness.Client.Player.Id, 820));
        }

        private static WildernessRuntimeTestHarness CreateMedicineHarness() => CreateIntegratedHub();

        private static void SeedFirstBatchFailure(WildernessRuntimeTestHarness harness)
        {
            using var unit = harness.CreateChar();
            unit.ExecuteTransaction(() => unit.CharacterMissions.Runtime.Archive(new CharacterMissionEntry(
                harness.Client.Player.Id, 700, (uint)MissionState.Failed)
            {
                ContentRevision = WildernessMissionDataV1.Revision
            }, harness.UtcNow));
            harness.Manager.HydrateAndClearInvalid(harness.Client.Player, unit);
        }

        private static void GrantUnbound(WildernessRuntimeTestHarness harness, uint template, uint quantity)
        {
            using var grant = new InventoryManager.InventoryGrant();
            using (var unit = harness.CreateChar())
                unit.ExecuteTransaction(() => grant.PlanAndSave(harness.Client,
                    new[] { new InventoryManager.InventoryItemGrant(template, quantity) }, unit));
            grant.Publish(harness.Client);
        }

        private static void DeliverMedicine(WildernessRuntimeTestHarness harness, uint mission)
        {
            foreach (var (objective, spawn) in new (uint, uint)[] { (1, 203), (2, 510004), (3, 186) })
            {
                if (harness.Client.Player.Missions[mission].Objectives[objective].State == MissionObjectiveState.Completed)
                    continue;
                var recipient = harness.Npc(spawn);
                Assert.IsNotNull(recipient);
                var before = Owned(harness, mission, 2356);
                Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, recipient.EntityId, mission, objective, 1));
                harness.Tick();
                Assert.AreEqual(before - 1, Owned(harness, mission, 2356));
                Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, recipient.EntityId, mission, objective, 1));
                Assert.AreEqual(before - 1, Owned(harness, mission, 2356));
            }
        }

        private static Creature SpawnSkeev(WildernessRuntimeTestHarness harness)
        {
            var template = harness.World.Set<CreatureEntry>().SingleOrDefault(entry => entry.ClassId == 28589 && entry.NameId == 6734);
            Assert.IsNotNull(template, "The coordinator's native Skeev World actor is required; do not substitute another boss.");
            Assert.AreEqual(530130U, template.Id);
            var spawn = harness.World.Set<SpawnPoolEntry>().SingleOrDefault(entry =>
                entry.MapContextId == 1220 && entry.Creature1Id == template.Id);
            Assert.IsNotNull(spawn, "Skeev requires one centrally allocated public spawn.");
            Assert.AreEqual(530130U, spawn.Id);
            Assert.IsTrue(Vector3.Distance(new Vector3(-400, 173.679004f, 178), spawn.Position) < 0.001f);
            Assert.AreEqual(-1.19028995, spawn.Rotation, 0.000001);
            harness.SpawnWorld(spawn.Id);
            var skeev = harness.Npc(spawn.Id);
            Assert.IsNotNull(skeev);
            Assert.AreEqual(595U, skeev.Npc.NpcPackageId);
            return skeev;
        }

        private static void AcceptRadio(Client client, uint mission)
        {
            var router = new PacketRouter<ClientPacketHandler, GameOpcode>();
            var handler = new ClientPacketHandler();
            handler.RegisterClient(client);
            router.RoutePacket(handler, new AssignRadioMissionPacket { MissionId = mission });
        }

        private static void CombatTick(WildernessRuntimeTestHarness harness)
        {
            harness.Tick();
            ActorActionManager.Instance.DoWork(harness.Map, 250);
            MissileManager.Instance.DoWork(harness.Map, 250);
        }

        private static Client Reconnect(WildernessRuntimeTestHarness harness)
        {
            var previous = harness.Client;
            CellManager.Instance.RemoveFromWorld(previous);
            harness.Map.ClientList.Remove(previous);
            var client = harness.Context.CreateCompetingClient(harness.Manager);
            client.Player.RuntimeMapChannel = harness.Map;
            client.Player.Race = previous.Player.Race;
            client.Player.Class = previous.Player.Class;
            foreach (var entry in previous.Player.Attributes)
                client.Player.Attributes[entry.Key] = new ActorAttributes(entry.Key, entry.Value.NormalMax,
                    entry.Value.CurrentMax, entry.Value.Current, entry.Value.RefreshAmount, entry.Value.RefreshPeriod);
            client.MissionAreaService = new MissionAreaService(() => harness.Manager);
            using (var unit = harness.CreateChar())
                harness.Manager.HydrateAndClearInvalid(client.Player, unit);
            InventoryManager.Instance.InitCharacterInventory(client);
            harness.Manager.PublishInitialState(client);
            return client;
        }

        private static ConversePacket Open(
            WildernessRuntimeTestHarness harness, NpcManager npcs, ulong entityId)
        {
            harness.Drain();
            npcs.RequestNpcConverse(harness.Client, new RequestNPCConversePacket
            {
                EntityId = entityId, ActionId = ActionId.Converse, ActionArgId = 1
            });
            var conversations = harness.Drain().OfType<ConversePacket>().ToArray();
            Assert.AreEqual(1, conversations.Length, "Eleanor must open a real native NPC conversation.");
            return conversations[0];
        }

        private static bool Offers(ConversePacket conversation, uint missionId) =>
            conversation.ConvoDataDict.TryGetValue(ConversationType.MissionDispense, out var value) &&
            value is Dictionary<uint, MissionInfo> offers && offers.ContainsKey(missionId);

        private static void SeedCompletedHistory(WildernessRuntimeTestHarness harness, params uint[] missionIds)
        {
            using var unit = harness.CreateChar();
            unit.ExecuteTransaction(() =>
            {
                foreach (var missionId in missionIds)
                    unit.CharacterMissions.Runtime.Archive(new CharacterMissionEntry(
                        harness.Client.Player.Id, missionId, (uint)MissionState.Completed)
                    {
                        ContentRevision = WildernessMissionDataV1.Revision
                    }, harness.UtcNow);
            });
            foreach (var missionId in missionIds)
            {
                harness.Client.Player.MissionHistory[missionId] = MissionState.Completed;
                harness.Client.Player.MissionSuccessHistory.Add(missionId);
            }
        }
    }
}
