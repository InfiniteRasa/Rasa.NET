using System;
using System.Linq;
using System.Numerics;

using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Packets.Inventory.Server;
    using Rasa.Packets.LootDispenser.Client;
    using Rasa.Packets.LootDispenser.Server;
    using Rasa.Packets.Manifestation.Server;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Mission.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Test.World;

    /// <summary>
    /// What a Bootcamp mission hands out before it is handed in - the equipment crate's rows of
    /// Gearing Up for Battle, Corporal DeSimone's promotion in Capture the Flag - is handed out
    /// once, whatever the player abandons and takes again.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class BootcampAbandonedAttemptTests
    {
        private const uint GearingUp = BootcampRuntimeTestHarness.MissionGearingUp;
        private const uint CaptureTheFlag = 1994;
        private const uint CrateObjective = 1;
        private const uint BriefingObjective = 4;
        private const uint PromotionExperience = 43000;
        private static readonly Vector3 CratePosition = new(398, 122, 173);
        private static readonly uint[] TemplateIds = { 13066, 13096, 13156, 13186, 13713, 28 };

        [TestMethod]
        public void AbandoningTellsTheClientTheGiverOffersTheMissionAgain()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var (mcAllister, delessio) = PrepareGearingUp(harness);
            Accept(harness, mcAllister);
            harness.Drain();
            Assert.IsFalse(harness.Manager.ClassifyNpcConversation(harness.Client.Player, mcAllister)
                .TryGetStatus(out _, out _), "An accepted mission is not offered.");

            Abandon(harness, GearingUp);

            var sent = WorldTestContext.Drain(harness.Client).Select(packet => packet.Message)
                .OfType<CallMethodMessage>().ToArray();
            Assert.AreEqual(1, sent.Count(method => method.Packet is MissionDiscardedPacket));
            var offered = sent.Where(method => method.EntityId == mcAllister.EntityId)
                .Select(method => method.Packet).OfType<NPCConversationStatusPacket>().ToArray();
            Assert.IsTrue(offered.Length > 0,
                "The giver showed no offer until the player went out of his sight and back, or logged in again.");
            Assert.AreEqual(ConversationStatus.Available, offered.Last().ConvoStatusId);
            // Delessio's briefing was the abandoned mission's objective: his status is withdrawn.
            Assert.AreEqual(ConversationStatus.None, sent.Where(method => method.EntityId == delessio.EntityId)
                .Select(method => method.Packet).OfType<NPCConversationStatusPacket>().Last().ConvoStatusId);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void ARetryFindsTheEmptiedCrateEmptyAndUsingItCompletesTheObjective(bool reconnect)
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var (mcAllister, delessio) = PrepareGearingUp(harness);
            Accept(harness, mcAllister);
            Brief(harness, delessio);
            LootAll(harness, UseCrate(harness));
            var owned = AssertOwnsOneOfEach(harness);

            Abandon(harness, GearingUp);
            if (reconnect)
                (mcAllister, delessio) = Reconnect(harness);
            Accept(harness, mcAllister);
            Brief(harness, delessio);

            var crate = Crate(harness);
            CellManager.Instance.UpdateVisibility(harness.Client);
            Assert.IsTrue(crate.IsEnabled, "The crate has to be usable while its objective is open.");
            Assert.AreEqual(UseObjectState.TdStateOpened, crate.StateId);
            Assert.AreEqual(0, harness.BootcampMap.LootDispensers[crate.LootDispenserEntityId].Remaining().Count,
                "Every row was taken in the attempt that was abandoned.");
            Assert.AreEqual(MissionObjectiveState.Incomplete,
                harness.Client.Player.Missions[GearingUp].Objectives[CrateObjective].State);
            harness.Drain();

            harness.UseObjectAndRecover(crate, actionArgId: DynamicObjectManager.FootlockerUseArgId);

            var packets = harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Completed,
                harness.Client.Player.Missions[GearingUp].Objectives[CrateObjective].State,
                "With nothing left to take, using the crate is the objective.");
            Assert.AreEqual(MissionObjectiveState.Incomplete,
                harness.Client.Player.Missions[GearingUp].Objectives[2].State);
            Assert.AreEqual(0, packets.OfType<LootCorpsePacket>().Count(), "There is no loot to show.");
            Assert.AreEqual(0, packets.OfType<InventoryAddItemPacket>().Count());
            Assert.IsFalse(crate.IsEnabled);
            Assert.AreEqual(owned, AssertOwnsOneOfEach(harness));

            // And it stays that way.
            LootDispenserManager.Instance.RequestLootAllFromCorpse(harness.Client,
                new RequestLootAllFromCorpsePacket { EntityId = crate.LootDispenserEntityId });
            harness.UseObjectAndRecover(crate, actionArgId: DynamicObjectManager.FootlockerUseArgId);
            Assert.AreEqual(owned, AssertOwnsOneOfEach(harness));
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void ARetryFindsOnlyTheRowsNoAttemptHasTaken(bool reconnect)
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var (mcAllister, delessio) = PrepareGearingUp(harness);
            Accept(harness, mcAllister);
            Brief(harness, delessio);
            var first = UseCrate(harness);
            LootDispenserManager.Instance.RequestLootItemFromCorpse(harness.Client,
                new RequestLootItemFromCorpsePacket
                {
                    EntityId = first.EntityId,
                    ItemId = first.LootItems.Single(item => item.ItemTemplateId == 13066).EntityId,
                    DestSlot = 0
                });
            Assert.AreEqual(1, harness.ReadOwnedTemplateCounts(TemplateIds).Values.Sum());

            Abandon(harness, GearingUp);
            if (reconnect)
                (mcAllister, delessio) = Reconnect(harness);
            Accept(harness, mcAllister);
            Brief(harness, delessio);
            var second = UseCrate(harness);

            Assert.AreEqual(5, second.Remaining().Count);
            Assert.IsFalse(second.Remaining().Any(item => item.ItemTemplateId == 13066),
                "The boots went in the abandoned attempt.");
            LootAll(harness, second);
            var owned = AssertOwnsOneOfEach(harness);
            Assert.AreEqual(MissionObjectiveState.Completed,
                harness.Client.Player.Missions[GearingUp].Objectives[CrateObjective].State);

            // A third attempt follows two that each took a part: nothing is left.
            Abandon(harness, GearingUp);
            if (reconnect)
                (mcAllister, delessio) = Reconnect(harness);
            Accept(harness, mcAllister);
            Brief(harness, delessio);
            var crate = Crate(harness);
            CellManager.Instance.UpdateVisibility(harness.Client);
            Assert.AreEqual(0, harness.BootcampMap.LootDispensers[crate.LootDispenserEntityId].Remaining().Count);
            harness.UseObjectAndRecover(crate, actionArgId: DynamicObjectManager.FootlockerUseArgId);
            Assert.AreEqual(MissionObjectiveState.Completed,
                harness.Client.Player.Missions[GearingUp].Objectives[CrateObjective].State);
            Assert.AreEqual(owned, AssertOwnsOneOfEach(harness));
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void ThePromotionIsPaidOnceAcrossAbandonedAttempts(bool reconnect)
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            harness.SeedMission(harness.Client.Player.Id, GearingUp, (uint)MissionState.Completed, false);
            var deSimone = harness.AddNpc(BootcampRuntimeTestHarness.CorporalDeSimoneCreatureId,
                BootcampRuntimeTestHarness.CorporalDeSimonePackageId);
            harness.MovePlayerTo(deSimone);
            CellManager.Instance.UpdateVisibility(harness.Client);
            var before = harness.Context.ReadRewardTotals().Experience;

            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, deSimone.EntityId, CaptureTheFlag));
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, deSimone.EntityId, CaptureTheFlag, 4, 1));
            Assert.AreEqual(before + PromotionExperience, harness.Context.ReadRewardTotals().Experience);
            Assert.AreEqual((byte)5, harness.Client.Player.Level);
            var cloneCredits = harness.Client.Player.CloneCredits;

            for (var attempt = 2; attempt <= 3; attempt++)
            {
                Abandon(harness, CaptureTheFlag);
                if (reconnect)
                {
                    harness.ReconnectFresh();
                    harness.Manager.PublishInitialState(harness.Client);
                    deSimone = BootcampRuntimeTestHarness.FindNpcByPackage(harness.BootcampMap,
                        BootcampRuntimeTestHarness.CorporalDeSimonePackageId);
                    Assert.IsNotNull(deSimone);
                    harness.MovePlayerTo(deSimone);
                    CellManager.Instance.UpdateVisibility(harness.Client);
                }
                harness.Drain();

                Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, deSimone.EntityId, CaptureTheFlag));
                Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, deSimone.EntityId, CaptureTheFlag, 4, 1));

                // The attempt goes on as the first did, without the pay.
                BootcampRuntimeTestHarness.AssertObjectiveStates(
                    harness.Client.Player.Missions[CaptureTheFlag],
                    (4U, MissionObjectiveState.Completed),
                    (2U, MissionObjectiveState.Incomplete),
                    (1U, MissionObjectiveState.Inactive),
                    (3U, MissionObjectiveState.Inactive));
                Assert.AreEqual(before + PromotionExperience, harness.Context.ReadRewardTotals().Experience,
                    $"Attempt {attempt} was paid the promotion again.");
                Assert.AreEqual((byte)5, harness.Client.Player.Level);
                Assert.AreEqual(cloneCredits, harness.Client.Player.CloneCredits);
                Assert.AreEqual(0, harness.Drain().OfType<LevelUpPacket>().Count());
            }
        }

        [TestMethod]
        public void AnAttemptFollowsOnlyTheUnfinishedOnesSinceTheLastSuccess()
        {
            using var context = MissionTestContext.WithDefinitions(321);
            var now = new DateTime(2026, 10, 5, 12, 0, 0, DateTimeKind.Utc);
            using (var db = context.Open())
            {
                // Abandoned, succeeded, failed, abandoned; and another mission's abandoned attempt.
                db.Add(History(321, "a1", 1, MissionState.NotAssigned, now));
                db.Add(History(321, "a2", 2, MissionState.Completed, now, rewarded: true));
                db.Add(History(321, "a3", 3, MissionState.Failed, now));
                db.Add(History(321, "a4", 4, MissionState.NotAssigned, now));
                db.Add(History(429, "b1", 1, MissionState.NotAssigned, now));
                db.Add(Receipt("a1", "legacy:step-before-success", now));
                db.Add(Receipt("a2", "legacy:step-of-success", now));
                db.Add(Receipt("a3", "legacy:step-of-failure", now));
                db.Add(Receipt("a4", "legacy:step-of-abandonment", now));
                db.Add(Receipt("b1", "legacy:step-of-other-mission", now));
                db.Add(new MissionSceneEntry { RunId = "r1", ScriptKey = "script", OwnerCharacterId = 1, MissionId = 321, AssignmentId = "a1", Status = "Resetting" });
                db.Add(new MissionSceneEntry { RunId = "r3", ScriptKey = "script", OwnerCharacterId = 1, MissionId = 321, AssignmentId = "a3", Status = "Resetting" });
                db.Add(new MissionSceneEntry { RunId = "r4", ScriptKey = "other-script", OwnerCharacterId = 1, MissionId = 321, AssignmentId = "a4", Status = "Resetting" });
                db.Add(Receipt("r1", "grant-before-success", now));
                db.Add(Receipt("r3", "grant-of-failure", now));
                db.Add(Receipt("r4", "grant-of-other-script", now));
                db.SaveChanges();
            }

            using var unit = context.CreateChar();
            var store = unit.CharacterMissions.Runtime;

            CollectionAssert.AreEquivalent(new[] { "a3", "a4" },
                store.UnfinishedAttempts(1, 321).Select(attempt => attempt.AssignmentId).ToArray());
            Assert.IsTrue(store.HadStepInUnfinishedAttempt(1, 321, "step-of-failure"));
            Assert.IsTrue(store.HadStepInUnfinishedAttempt(1, 321, "step-of-abandonment"));
            Assert.IsFalse(store.HadStepInUnfinishedAttempt(1, 321, "step-before-success"),
                "A success ends what went before it: a repeat of the mission starts over.");
            Assert.IsFalse(store.HadStepInUnfinishedAttempt(1, 321, "step-of-success"));
            Assert.IsFalse(store.HadStepInUnfinishedAttempt(1, 321, "step-of-other-mission"));
            Assert.IsFalse(store.HadStepInUnfinishedAttempt(1, 429, "step-of-abandonment"));
            Assert.IsTrue(store.GrantedInUnfinishedAttempt(1, 321, "script", "grant-of-failure"));
            Assert.IsFalse(store.GrantedInUnfinishedAttempt(1, 321, "script", "grant-before-success"));
            Assert.IsFalse(store.GrantedInUnfinishedAttempt(1, 321, "script", "grant-of-other-script"));
            Assert.IsFalse(store.GrantedInUnfinishedAttempt(2, 321, "script", "grant-of-failure"));
            Assert.AreEqual(0, store.UnfinishedAttempts(1, 322).Count);
        }

        private static CharacterMissionHistoryEntry History(
            uint missionId, string assignmentId, uint generation, MissionState outcome, DateTime now, bool rewarded = false) =>
            new()
            {
                CharacterId = 1, MissionId = missionId, AssignmentId = assignmentId, AssignmentGeneration = generation,
                ContentRevision = "deployment_11", CompletedAtUtc = now, Outcome = (uint)outcome, Rewarded = rewarded,
                RewardedAtUtc = rewarded ? now : null
            };

        private static MissionReceiptEntry Receipt(string owner, string key, DateTime now) =>
            new() { OwnerId = owner, Generation = 0, OperationKey = key, Kind = "Legacy", CreatedAtUtc = now };

        private static (Creature McAllister, Creature Delessio) PrepareGearingUp(BootcampRuntimeTestHarness.Harness harness)
        {
            harness.MovePlayerTo(CratePosition);
            CellManager.Instance.UpdateVisibility(harness.Client);
            var mcAllister = harness.AddNpc(BootcampRuntimeTestHarness.MajorMcAllisterCreatureId, position: CratePosition);
            var delessio = harness.AddNpc(BootcampRuntimeTestHarness.CaptainDelessioCreatureId,
                BootcampRuntimeTestHarness.CaptainDelessioPackageId, CratePosition);
            foreach (var npc in new[] { mcAllister, delessio })
                CreatureManager.Instance.CreateCreatureOnClient(harness.Client, npc);
            harness.SeedMission(harness.Client.Player.Id,
                BootcampRuntimeTestHarness.MissionInitiation, (uint)MissionState.Completed, false);
            return (mcAllister, delessio);
        }

        private static (Creature McAllister, Creature Delessio) Reconnect(BootcampRuntimeTestHarness.Harness harness)
        {
            harness.ReconnectFresh();
            harness.Manager.PublishInitialState(harness.Client);
            harness.MovePlayerTo(CratePosition);
            CellManager.Instance.UpdateVisibility(harness.Client);
            var mcAllister = BootcampRuntimeTestHarness.FindCreature(harness.BootcampMap,
                BootcampRuntimeTestHarness.MajorMcAllisterCreatureId);
            var delessio = BootcampRuntimeTestHarness.FindNpcByPackage(harness.BootcampMap,
                BootcampRuntimeTestHarness.CaptainDelessioPackageId);
            Assert.IsNotNull(mcAllister);
            Assert.IsNotNull(delessio);
            return (mcAllister, delessio);
        }

        private static void Accept(BootcampRuntimeTestHarness.Harness harness, Creature mcAllister)
        {
            harness.MovePlayerTo(mcAllister);
            CellManager.Instance.UpdateVisibility(harness.Client);
            Assert.IsTrue(harness.Manager.OpenNpcConversation(harness.Client, mcAllister.EntityId));
            new NpcManager(harness.Context, harness.Manager).AssignNPCMission(harness.Client,
                new AssignNPCMissionPacket { NpcEntityId = mcAllister.EntityId, MissionId = GearingUp });
            Assert.IsTrue(harness.Client.Player.Missions.TryGetValue(GearingUp, out var mission),
                "Major McAllister did not give the mission.");
            Assert.AreEqual(MissionState.Active, mission.State);
        }

        private static void Brief(BootcampRuntimeTestHarness.Harness harness, Creature delessio)
        {
            harness.MovePlayerTo(delessio);
            CellManager.Instance.UpdateVisibility(harness.Client);
            Assert.IsTrue(harness.Manager.OpenNpcConversation(harness.Client, delessio.EntityId));
            new NpcManager(harness.Context, harness.Manager).CompleteNPCObjective(harness.Client,
                new CompleteNPCObjectivePacket
                {
                    EntityId = delessio.EntityId,
                    MissionId = GearingUp,
                    ObjectiveId = BriefingObjective,
                    PlayerFlagId = 1
                });
            Assert.AreEqual(MissionObjectiveState.Incomplete,
                harness.Client.Player.Missions[GearingUp].Objectives[CrateObjective].State);
        }

        private static void Abandon(BootcampRuntimeTestHarness.Harness harness, uint missionId)
        {
            new NpcManager(harness.Context, harness.Manager).AbandonMission(harness.Client,
                new AbandonMissionPacket { MissionId = missionId });
            Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(missionId));
        }

        private static DynamicObject Crate(BootcampRuntimeTestHarness.Harness harness)
        {
            var crate = BootcampRuntimeTestHarness.FindScenarioObject(harness.BootcampMap, "bootcamp-equipment-crate");
            Assert.IsNotNull(crate);
            harness.MovePlayerTo(crate);
            return crate;
        }

        private static LootDispenser UseCrate(BootcampRuntimeTestHarness.Harness harness)
        {
            var crate = Crate(harness);
            CellManager.Instance.UpdateVisibility(harness.Client);
            harness.UseObjectAndRecover(crate, actionArgId: DynamicObjectManager.FootlockerUseArgId);
            var loot = harness.BootcampMap.LootDispensers[crate.LootDispenserEntityId];
            Assert.AreEqual(harness.Client.Player.EntityId, loot.CurrentLooter, "The crate did not open.");
            return loot;
        }

        private static void LootAll(BootcampRuntimeTestHarness.Harness harness, LootDispenser loot) =>
            LootDispenserManager.Instance.RequestLootAllFromCorpse(harness.Client,
                new RequestLootAllFromCorpsePacket { EntityId = loot.EntityId });

        /// <summary>The crate's six rows as the character holds them: one item of each, at the stack it came with.</summary>
        private static string AssertOwnsOneOfEach(BootcampRuntimeTestHarness.Harness harness)
        {
            using var db = harness.Context.Open();
            var owned = (from inventory in db.CharacterInventoryEntries.AsNoTracking()
                         join item in db.ItemEntries.AsNoTracking() on inventory.ItemId equals item.ItemId
                         where inventory.CharacterId == harness.Client.Player.Id
                         select new { item.ItemTemplateId, item.StackSize }).ToArray();
            foreach (var templateId in TemplateIds)
                Assert.AreEqual(1, owned.Count(row => row.ItemTemplateId == templateId), $"item template {templateId}");

            return string.Join(",", owned.Where(row => TemplateIds.Contains(row.ItemTemplateId))
                .OrderBy(row => row.ItemTemplateId).Select(row => $"{row.ItemTemplateId}x{row.StackSize}"));
        }
    }
}
