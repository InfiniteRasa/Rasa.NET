using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
using Rasa.Models;
using Rasa.Packets.Inventory.Client;
using Rasa.Packets.LootDispenser.Client;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.MapChannel.Server;
using Rasa.Services.Preloader.Missions.Wilderness;
using Rasa.Structures;
using Rasa.Structures.Char;

namespace Rasa.Test.Missions.Wilderness
{
    [TestClass]
    [DoNotParallelize]
    public class WildernessElohPinholeTests
    {
        [TestMethod]
        public void RiverReconRequiresEarlierHistoryAndCannotReplacePatrolReconWithRogersDialogue()
        {
            using var harness = CreateHarness();
            harness.SpawnWorld(100, 101);
            var rogers = harness.Npc(100);
            var witherspoon = harness.Npc(101);
            Assert.IsNotNull(rogers, "River Recon must use the real migrated Rogers.");
            Assert.IsNotNull(witherspoon);
            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, rogers.EntityId, 429),
                "River Recon must retain its Forming Alliances prerequisite.");

            CompleteEarlierHistory(harness, 479);
            AssertRewardOffer(harness, rogers, 429, 1000, Array.Empty<uint>(), new uint[] { 630, 3315 });

            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, rogers.EntityId, 429),
                "Rogers must offer native River Recon after completed Forming Alliances history.");
            var mission = harness.Client.Player.Missions[429];
            Assert.AreEqual(MissionObjectiveState.Incomplete, mission.Objectives[5].State);
            Assert.AreEqual(MissionObjectiveState.Inactive, mission.Objectives[4].State);
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, rogers.EntityId, 429, 5, 1),
                "The native Rogers reminder must not replace entering the lost-patrol recon region.");
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, rogers.EntityId, 429, 4, 1),
                "The Witherspoon handoff must remain locked until the patrol is reconnoitered.");
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, witherspoon.EntityId, 429, 4, 1));
            Assert.IsFalse(mission.Completeable);
            Assert.IsTrue(harness.Manager.TryGetAreaDefinition(429, 5, out var area));
            Assert.IsNotNull(area.Radius);
            var radius = (float)area.Radius.Value;
            Visit(harness, rogers.Position, radius);
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[429].Objectives[5].State);
            Visit(harness, area.Position + new Vector3(0, 12, 0), radius);
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[429].Objectives[5].State,
                "Passing above the patrol region must not perform the ground-level recon.");
            Visit(harness, area.Position, radius);
            Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[429].Objectives[5].State);
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[429].Objectives[4].State);
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, rogers.EntityId, 429, 4, 1));
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, witherspoon.EntityId, 429, 4, 1));
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, rogers.EntityId, 429, 0));
            Assert.IsTrue(harness.Manager.CompleteOfferedMission(harness.Client, witherspoon.EntityId, 429, 0));
            Assert.AreEqual(1U, HeldQuantity(harness, 630));
            Assert.AreEqual(0U, HeldQuantity(harness, 3315));
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, witherspoon.EntityId, 429, 0));
        }

        [TestMethod]
        public void FieldReportsRequireHughThenOinginThenWoodBeforeWitherspoonPaysOnce()
        {
            using var harness = CreateHarness();
            harness.SpawnWorld(101, 216, 217, 199);
            var witherspoon = harness.Npc(101);
            var hugh = harness.Npc(216);
            var oingin = harness.Npc(217);
            var wood = harness.Npc(199);
            Assert.IsNotNull(witherspoon);
            Assert.IsNotNull(hugh);
            Assert.IsNotNull(oingin);
            Assert.IsNotNull(wood);
            CompleteEarlierHistory(harness, 429);
            AssertRewardOffer(harness, witherspoon, 431, 500, Array.Empty<uint>(), new uint[] { 20548, 4019 });

            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, witherspoon.EntityId, 431));
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, oingin.EntityId, 431, 2, 1));
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, wood.EntityId, 431, 3, 1));
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, witherspoon.EntityId, 431, 1, 1));
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, hugh.EntityId, 431, 1, 1));
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, hugh.EntityId, 431, 1, 1));
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[431].Objectives[2].State);
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, witherspoon.EntityId, 431, 0));

            var reconnected = harness.Context.CreateCompetingClient(harness.Manager);
            harness.Manager.PublishInitialState(reconnected);
            Assert.AreEqual(MissionObjectiveState.Completed, reconnected.Player.Missions[431].Objectives[1].State);
            Assert.AreEqual(MissionObjectiveState.Incomplete, reconnected.Player.Missions[431].Objectives[2].State);
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, oingin.EntityId, 431, 2, 1));
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, wood.EntityId, 431, 3, 1));
            Assert.IsTrue(harness.Client.Player.Missions[431].Completeable);
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, wood.EntityId, 431, 0));

            var credits = harness.Client.Player.Credits[CurencyType.Credits];
            var experience = harness.Client.Player.Experience;
            Assert.IsTrue(harness.Manager.CompleteOfferedMission(harness.Client, witherspoon.EntityId, 431, 0));
            Assert.AreEqual(credits + 500, harness.Client.Player.Credits[CurencyType.Credits]);
            Assert.AreEqual(experience + 2500, harness.Client.Player.Experience);
            Assert.AreEqual(1U, HeldQuantity(harness, 20548));
            Assert.AreEqual(0U, HeldQuantity(harness, 4019));
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, witherspoon.EntityId, 431, 0));
            Assert.AreEqual(1U, HeldQuantity(harness, 20548));
        }

        [TestMethod]
        [DataRow(82U, 580014U)]
        [DataRow(83U, 580015U)]
        [DataRow(84U, 580016U)]
        public void SnipeHuntRequiresSixEligibleSnipersAndOneRetrievedOverseerDatapad(uint overseerId, uint overseerSpawn)
        {
            using var harness = CreateHarness();
            harness.SpawnWorld(101, 630046, 630047, 630048, 630049, 630050, 630051, 630071, overseerSpawn);
            var witherspoon = harness.Npc(101);
            Assert.IsNotNull(witherspoon);
            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, witherspoon.EntityId, 433));
            CompleteEarlierHistory(harness, 431);
            AssertRewardOffer(harness, witherspoon, 433, 900, Array.Empty<uint>(), new uint[] { 20399, 3603 });
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, witherspoon.EntityId, 433));
            var drop = harness.Manager.LoadedMissions[433].Items["overseer-datapad"].Drop;
            Assert.AreEqual(7U, drop.ObjectiveId);
            Assert.AreEqual(1U, drop.Quantity);
            CollectionAssert.AreEquivalent(new uint[] { 82, 83, 84 }, drop.CreatureIds.ToArray());

            var ordinary = harness.Map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList)
                .Single(creature => creature.SpawnPool?.DbId == 630071);
            Assert.AreEqual(630071U, ordinary.DbId);
            Assert.AreEqual(7120U, (uint)ordinary.EntityClass);
            Kill(harness, ordinary);
            Assert.AreEqual(0U, harness.Client.Player.Missions[433].Objectives[1].Counters[0],
                "An ordinary Lightbender with the same native body must not count as an authored sniper.");
            Assert.IsFalse(harness.Map.LootDispensers[ordinary.CorpseLootEntityId].LootItems
                .Any(item => item.ItemTemplateId == 2239));

            var spawns = new uint[] { 630046, 630047, 630048, 630049, 630050, 630051 };
            for (var index = 0; index < spawns.Length; index++)
            {
                var sniper = harness.Map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList)
                    .Single(creature => creature.SpawnPool?.DbId == spawns[index]);
                AssertSniperProfile(sniper);
                Kill(harness, sniper);
                Assert.AreEqual((uint)index + 1, harness.Client.Player.Missions[433].Objectives[1].Counters[0]);
                if (index < 5)
                    Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[433].Objectives[1].State);
            }
            Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[433].Objectives[1].State);
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[433].Objectives[7].State);
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, witherspoon.EntityId, 433, 1));

            var overseer = harness.Map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList)
                .Single(creature => creature.SpawnPool?.DbId == overseerSpawn);
            Assert.AreEqual(overseerId, overseer.DbId);
            Kill(harness, overseer);
            var loot = harness.Map.LootDispensers[overseer.CorpseLootEntityId];
            Assert.AreEqual(1, loot.LootItems.Count(item => item.ItemTemplateId == 2239));
            Assert.AreEqual(0U, HeldQuantity(harness, 2239));
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[433].Objectives[7].State,
                "The named kill alone must not replace physically retrieving its datapad.");
            LootDispenserManager.Instance.RequestLootAllFromCorpse(harness.Client,
                new RequestLootAllFromCorpsePacket { EntityId = loot.EntityId });
            Assert.AreEqual(1U, HeldQuantity(harness, 2239));
            Assert.AreEqual(1U, harness.Client.Player.Missions[433].Objectives[7].ItemCounters[10211]);
            Assert.IsTrue(harness.Client.Player.Missions[433].Completeable);
            LootDispenserManager.Instance.RequestLootAllFromCorpse(harness.Client,
                new RequestLootAllFromCorpsePacket { EntityId = loot.EntityId });
            Assert.AreEqual(1U, HeldQuantity(harness, 2239));

            var experience = harness.Client.Player.Experience;
            var credits = harness.Client.Player.Credits[CurencyType.Credits];
            Assert.IsTrue(harness.Manager.CompleteOfferedMission(harness.Client, witherspoon.EntityId, 433, 1));
            Assert.AreEqual(experience + 6000, harness.Client.Player.Experience);
            Assert.AreEqual(credits + 900, harness.Client.Player.Credits[CurencyType.Credits]);
            Assert.AreEqual(0U, HeldQuantity(harness, 2239));
            Assert.AreEqual(1U, HeldQuantity(harness, 3603));
            Assert.AreEqual(0U, HeldQuantity(harness, 20399));
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, witherspoon.EntityId, 433, 1));
        }

        [TestMethod]
        public void RendezvousUsesWitherspoonAndTheRealRandolphInsteadOfJennings()
        {
            using var harness = CreateHarness();
            harness.SpawnWorld(101, 213, 208);
            var witherspoon = harness.Npc(101);
            var jennings = harness.Npc(213);
            var randolph = harness.Npc(208);
            Assert.IsNotNull(witherspoon);
            Assert.IsNotNull(jennings);
            Assert.IsNotNull(randolph);
            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, witherspoon.EntityId, 434));
            CompleteEarlierHistory(harness, 432);
            AssertRewardOffer(harness, witherspoon, 434, 600, new uint[] { 44918, 44918 }, Array.Empty<uint>());

            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, jennings.EntityId, 434));
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, witherspoon.EntityId, 434));
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, witherspoon.EntityId, 434, 1, 1));
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, jennings.EntityId, 434, 1, 1));
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, randolph.EntityId, 434, 1, 1));
            Assert.IsTrue(harness.Manager.CompleteOfferedMission(harness.Client, randolph.EntityId, 434, null));
            Assert.AreEqual(2U, HeldQuantity(harness, 44918));
            Assert.AreEqual(0U, HeldQuantity(harness, 111247));
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, randolph.EntityId, 434, null));
            Assert.AreEqual(2U, HeldQuantity(harness, 44918));
        }

        [TestMethod]
        public void NativeMissileDestructionRequiresThreeDistinctHarvestersAndIsAssignmentScoped()
        {
            using var harness = CreateHarness();
            harness.SpawnWorld(213);
            var jennings = harness.Npc(213);
            Assert.IsNotNull(jennings);
            AssertRewardOffer(harness, jennings, 432, 600, new uint[] { 44917, 44918 }, Array.Empty<uint>());
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, jennings.EntityId, 432));
            var first = Object(harness, 432, "gas-harvester-1");
            Assert.AreEqual(7906U, (uint)first.EntityClassId);
            Assert.AreEqual(UseObjectState.IdesStateIntact, first.StateId);

            harness.Drain();
            Hit(harness, first, 99);
            Assert.AreEqual(1U, first.CurrentHitPoints);
            CollectionAssert.AreEqual(new[] { UseObjectState.IdesState50pHealth, UseObjectState.IdesState25pHealth },
                harness.Drain().OfType<UsePacket>().Select(use => use.CurState).ToArray(), "1 of 100: down to 25%, a state at a time");
            Assert.AreEqual(0U, harness.Client.Player.Missions[432].Objectives[1].Counters[0],
                "A damaged but surviving harvester must not count as destroyed.");
            Hit(harness, first, 1);
            Assert.AreEqual(0U, first.CurrentHitPoints);
            Assert.AreEqual(UseObjectState.StateDestroyed, harness.Drain().OfType<UsePacket>().Single().CurState,
                "25% to destroyed: vfx_arch_bane_gen_obj_gasharvester_explode.pkg, then the wreck");
            Assert.AreEqual(1U, harness.Client.Player.Missions[432].Objectives[1].Counters[0]);
            Assert.AreEqual(UseObjectState.StateDestroyed, first.StateId);
            Assert.IsFalse(first.IsEnabled);
            Hit(harness, first, 100);
            Use(harness, first, 1);
            Assert.AreEqual(1U, harness.Client.Player.Missions[432].Objectives[1].Counters[0],
                "Neither repeated damage nor use of one harvester may count again.");

            var second = harness.Context.CreateAdditionalClient(2, manager: harness.Manager);
            second.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0);
            second.Player.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0);
            try
            {
                Assert.IsTrue(harness.Manager.AcceptOfferedMission(second, jennings.EntityId, 432));
                var next = Object(harness, 432, "gas-harvester-2");
                Hit(harness, next, 100, second);
                Assert.AreEqual(0U, second.Player.Missions[432].Objectives[1].Counters[0]);
                Assert.AreEqual(1U, harness.Client.Player.Missions[432].Objectives[1].Counters[0]);
                Assert.IsTrue(next.IsEnabled);
                Hit(harness, Object(harness, 432, "gas-harvester-1", second), 100, second);
                Assert.AreEqual(1U, second.Player.Missions[432].Objectives[1].Counters[0]);
                Assert.AreEqual(1U, harness.Client.Player.Missions[432].Objectives[1].Counters[0]);

                Hit(harness, next, 100);
                Assert.AreEqual(2U, harness.Client.Player.Missions[432].Objectives[1].Counters[0]);
                Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, jennings.EntityId, 432, null));
                Hit(harness, Object(harness, 432, "gas-harvester-3"), 100);
                Assert.AreEqual(3U, harness.Client.Player.Missions[432].Objectives[1].Counters[0]);
                Assert.IsTrue(harness.Manager.CompleteOfferedMission(harness.Client, jennings.EntityId, 432, null));
                Assert.AreEqual(1U, HeldQuantity(harness, 44917));
                Assert.AreEqual(1U, HeldQuantity(harness, 44918));
                Assert.AreEqual(MissionObjectiveState.Incomplete, second.Player.Missions[432].Objectives[1].State);
            }
            finally
            {
                CellManager.Instance.RemoveFromWorld(second);
                harness.Map.ClientList.Remove(second);
            }
        }

        [TestMethod]
        public void FiveDistinctSurveyUnitsPersistAcrossReconnectBeforeRealRogersDelivery()
        {
            using var harness = CreateHarness();
            harness.SpawnWorld(205, 100);
            var maxwell = harness.Npc(205);
            var rogers = harness.Npc(100);
            Assert.IsNotNull(maxwell);
            Assert.IsNotNull(rogers);
            CompleteEarlierHistory(harness, 506);
            AssertRewardOffer(harness, maxwell, 508, 750, Array.Empty<uint>(), new uint[] { 20697, 35933 });
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, maxwell.EntityId, 508));
            var assignment = harness.Client.Player.Missions[508].AssignmentId;
            var first = Object(harness, 508, "survey-unit-1");
            Assert.AreEqual(7827U, (uint)first.EntityClassId);
            Assert.AreEqual(UseObjectState.TsState0, first.StateId);

            Use(harness, first, 1);
            Assert.AreEqual(0U, OwnedQuantity(harness, 508));
            Assert.AreEqual(0U, harness.Client.Player.Missions[508].Objectives[7].Counters[0],
                "The native Surveyor Unit requires argument 3, not the generic footlocker argument.");
            Use(harness, first, 3);
            for (var repeat = 0; repeat < 5; repeat++)
                Use(harness, first, 3);
            Assert.AreEqual(1U, OwnedQuantity(harness, 508));
            Assert.AreEqual(1U, harness.Client.Player.Missions[508].Objectives[7].Counters[0]);
            Assert.AreEqual(UseObjectState.TsState1, first.StateId);
            Assert.AreEqual(MissionObjectiveState.Inactive, harness.Client.Player.Missions[508].Objectives[8].State);
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, rogers.EntityId, 508, 8, 1));
            Use(harness, Object(harness, 508, "survey-unit-4"), 3);

            var reconnected = Reconnect(harness);
            Assert.AreEqual(assignment, reconnected.Player.Missions[508].AssignmentId);
            Assert.AreEqual(2U, OwnedQuantity(harness, 508, reconnected));
            Assert.AreEqual(2U, reconnected.Player.Missions[508].Objectives[7].Counters[0]);
            Use(harness, first, 3, reconnected);
            Assert.AreEqual(2U, reconnected.Player.Missions[508].Objectives[7].Counters[0]);
            foreach (var unit in new[] { 5, 2 })
                Use(harness, Object(harness, 508, $"survey-unit-{unit}", reconnected), 3, reconnected);
            Assert.AreEqual(4U, reconnected.Player.Missions[508].Objectives[7].Counters[0]);
            Assert.AreEqual(MissionObjectiveState.Inactive, reconnected.Player.Missions[508].Objectives[8].State);
            Use(harness, Object(harness, 508, "survey-unit-3", reconnected), 3, reconnected);
            Assert.AreEqual(5U, reconnected.Player.Missions[508].Objectives[7].Counters[0]);
            Assert.AreEqual(5U, OwnedQuantity(harness, 508, reconnected));
            Assert.AreEqual(MissionObjectiveState.Completed, reconnected.Player.Missions[508].Objectives[7].State);
            Assert.AreEqual(MissionObjectiveState.Incomplete, reconnected.Player.Missions[508].Objectives[8].State);
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(reconnected, maxwell.EntityId, 508, 8, 1));
            Assert.IsFalse(reconnected.Player.Missions[508].Completeable);

            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(reconnected, rogers.EntityId, 508, 8, 1));
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(reconnected, rogers.EntityId, 508, 99));
            Assert.AreEqual(5U, OwnedQuantity(harness, 508, reconnected));
            Assert.IsTrue(harness.Manager.CompleteOfferedMission(reconnected, rogers.EntityId, 508, 1));
            Assert.AreEqual(0U, OwnedQuantity(harness, 508, reconnected));
            Assert.AreEqual(1U, HeldQuantity(harness, 35933, reconnected));
            Assert.AreEqual(0U, HeldQuantity(harness, 20697, reconnected));
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(reconnected, rogers.EntityId, 508, 1));
            Assert.AreEqual(1U, HeldQuantity(harness, 35933, reconnected));
        }

        [TestMethod]
        public void OneCharactersSurveyUnitCannotIssueDataToAnotherAssignment()
        {
            using var harness = CreateHarness();
            harness.SpawnWorld(205);
            var maxwell = harness.Npc(205);
            Assert.IsNotNull(maxwell);
            CompleteEarlierHistory(harness, 506);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, maxwell.EntityId, 508));
            var first = Object(harness, 508, "survey-unit-1");
            var second = harness.Context.CreateAdditionalClient(2, manager: harness.Manager);
            try
            {
                CompleteEarlierHistory(harness, second, 506);
                Assert.IsTrue(harness.Manager.AcceptOfferedMission(second, maxwell.EntityId, 508));
                Use(harness, first, 3, second);
                Assert.AreEqual(0U, OwnedQuantity(harness, 508, second));
                Assert.AreEqual(0U, second.Player.Missions[508].Objectives[7].Counters[0]);
                Assert.AreEqual(0U, harness.Client.Player.Missions[508].Objectives[7].Counters[0]);
                Assert.IsTrue(first.IsEnabled);
                Use(harness, Object(harness, 508, "survey-unit-1", second), 3, second);
                Assert.AreEqual(1U, OwnedQuantity(harness, 508, second));
                Assert.AreEqual(0U, OwnedQuantity(harness, 508));
                Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[508].Objectives[2].State);
            }
            finally
            {
                CellManager.Instance.RemoveFromWorld(second);
                harness.Map.ClientList.Remove(second);
            }
        }

        [TestMethod]
        public void FullBagsDoNotConsumeASurveyUnitAndARepeatUseCanRetryAfterFreeingSpace()
        {
            using var harness = CreateHarness();
            harness.SpawnWorld(205);
            CompleteEarlierHistory(harness, 506);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, harness.Npc(205).EntityId, 508));
            using (var grant = new InventoryManager.InventoryGrant())
            {
                using (var unit = harness.CreateChar())
                    unit.ExecuteTransaction(() => grant.PlanAndSave(harness.Client,
                        new[] { new InventoryManager.InventoryItemGrant(741, 250) }, unit));
                grant.Publish(harness.Client);
            }
            Assert.AreEqual(50, harness.Client.Player.Inventory.PersonalInventory.Count(id => id != 0));
            var first = Object(harness, 508, "survey-unit-1");
            Use(harness, first, 3);
            Assert.AreEqual(0U, OwnedQuantity(harness, 508));
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[508].Objectives[2].State);
            Assert.AreEqual(0U, harness.Client.Player.Missions[508].Objectives[7].Counters[0]);
            Assert.AreEqual(UseObjectState.TsState0, first.StateId);
            Assert.IsTrue(first.IsEnabled);

            var filler = harness.Client.Player.Inventory.PersonalInventory
                .Where(id => id != 0).Select(EntityManager.Instance.GetItem).First();
            InventoryManager.Instance.PersonalInventory_DestroyItem(harness.Client,
                new PersonalInventory_DestroyItemPacket { EntityId = filler.EntityId, Quantity = filler.StackSize });
            Use(harness, first, 3);
            Assert.AreEqual(1U, OwnedQuantity(harness, 508));
            Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[508].Objectives[2].State);
            Assert.AreEqual(1U, harness.Client.Player.Missions[508].Objectives[7].Counters[0]);
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, 508));
            Assert.AreEqual(245U, HeldQuantity(harness, 741),
                "Assignment cleanup must preserve the unrelated character-owned filler stacks.");
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void TerminalSurveyCleanupRejectsOldObjectCallbacksOnTheReplacementAssignment(bool fail)
        {
            using var harness = CreateHarness();
            harness.SpawnWorld(205);
            var maxwell = harness.Npc(205);
            Assert.IsNotNull(maxwell);
            CompleteEarlierHistory(harness, 506);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, maxwell.EntityId, 508));
            var assignment = harness.Client.Player.Missions[508].AssignmentId;
            var first = Object(harness, 508, "survey-unit-1");
            var stale = Object(harness, 508, "survey-unit-2");
            Use(harness, first, 3);
            Assert.AreEqual(1U, OwnedQuantity(harness, 508));
            if (fail)
                Assert.IsTrue(harness.Manager.TryFailMission(harness.Client, 508));
            else
                Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, 508));
            Assert.AreEqual(0U, HeldQuantity(harness, 741));
            Use(harness, stale, 3);
            Assert.AreEqual(0U, HeldQuantity(harness, 741));

            if (fail)
            {
                // The client has dropped the failed mission from its log and has no way to
                // dismiss it: the new assignment below takes the failed one's place.
                Assert.AreEqual(MissionState.Failed, harness.Client.Player.Missions[508].State);
            }
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, maxwell.EntityId, 508));
            Assert.AreNotEqual(assignment, harness.Client.Player.Missions[508].AssignmentId);
            Use(harness, stale, 3);
            Assert.AreEqual(0U, OwnedQuantity(harness, 508));
            Assert.AreEqual(0U, harness.Client.Player.Missions[508].Objectives[7].Counters[0]);
            Use(harness, Object(harness, 508, "survey-unit-2"), 3);
            Assert.AreEqual(1U, OwnedQuantity(harness, 508));
            Assert.AreEqual(1U, harness.Client.Player.Missions[508].Objectives[7].Counters[0]);
        }

        [TestMethod]
        public void SonicEmanatorUsesItsOwnersItemWithoutHarmingTheActualTreebackHerd()
        {
            using var harness = CreateHarness();
            harness.SpawnWorld(204, 580057, 630043, 630044, 630045);
            var richards = harness.Npc(204);
            Assert.IsNotNull(richards);
            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, richards.EntityId, 436));
            CompleteEarlierHistory(harness, 506);
            AssertRewardOffer(harness, richards, 436, 500, new uint[] { 44919, 44918 }, Array.Empty<uint>());
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, richards.EntityId, 436));
            Assert.AreEqual(1U, HeldQuantity(harness, 2230));
            var emanator = Object(harness, 436, "sonic-emanator");
            Assert.AreEqual(10127U, (uint)emanator.EntityClassId);
            Assert.AreEqual(UseObjectState.IaStateActive, emanator.StateId);
            Assert.IsTrue(Vector3.Distance(new Vector3(460, 288.54892f, 589), emanator.Position) < 0.01f);
            var herd = harness.Map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList)
                .Where(creature => creature.SpawnPool?.DbId is 630043 or 630044 or 630045).ToArray();
            Assert.AreEqual(3, herd.Length);
            Assert.IsTrue(herd.All(creature => creature.DbId == 630043 && (uint)creature.EntityClass == 6038 &&
                Vector3.Distance(creature.Position, emanator.Position) < 20));
            var health = herd.ToDictionary(creature => creature.EntityId, creature => creature.Attributes[Attributes.Health].Current);
            var miasma = harness.Map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList)
                .First(creature => creature.DbId == 88 && creature.State != CharacterState.Dead);
            Kill(harness, miasma);
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[436].Objectives[1].State);
            Assert.AreEqual(1U, HeldQuantity(harness, 2230));

            var second = harness.Context.CreateAdditionalClient(2, manager: harness.Manager);
            try
            {
                CompleteEarlierHistory(harness, second, 506);
                Assert.IsTrue(harness.Manager.AcceptOfferedMission(second, richards.EntityId, 436));
                Use(harness, emanator, 1, second);
                Assert.AreEqual(1U, OwnedQuantity(harness, 436));
                Assert.AreEqual(1U, OwnedQuantity(harness, 436, second));
                Assert.AreEqual(MissionObjectiveState.Incomplete, second.Player.Missions[436].Objectives[1].State);
                Use(harness, emanator, 1);
                Assert.AreEqual(0U, OwnedQuantity(harness, 436));
                Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[436].Objectives[1].State);
                Assert.AreEqual(UseObjectState.IaStateAccepted, emanator.StateId);
                Assert.IsFalse(emanator.IsEnabled);
                Use(harness, emanator, 1);
                foreach (var treeback in herd)
                {
                    Assert.AreEqual(health[treeback.EntityId], treeback.Attributes[Attributes.Health].Current);
                    Assert.AreNotEqual(CharacterState.Dead, treeback.State);
                }
                Assert.IsTrue(harness.Manager.CompleteOfferedMission(harness.Client, richards.EntityId, 436, null));
                Assert.AreEqual(1U, HeldQuantity(harness, 44919));
                Assert.AreEqual(1U, HeldQuantity(harness, 44918));
                Assert.AreEqual(1U, OwnedQuantity(harness, 436, second));
                Assert.AreEqual(MissionObjectiveState.Incomplete, second.Player.Missions[436].Objectives[1].State);
                Assert.IsTrue(harness.Manager.TryAbandon(second, 436));
                Assert.AreEqual(0U, HeldQuantity(harness, 2230, second));
            }
            finally
            {
                CellManager.Instance.RemoveFromWorld(second);
                harness.Map.ClientList.Remove(second);
            }
        }

        [TestMethod]
        public void MamaMiasmaRequiresThreeNativeCavernEggLayersRatherThanOrdinaryMiasmas()
        {
            using var harness = CreateHarness();
            harness.SpawnWorld(204, 580057, 630040, 630041, 630042);
            var richards = harness.Npc(204);
            Assert.IsNotNull(richards);
            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, richards.EntityId, 506));
            CompleteEarlierHistory(harness, 422);
            AssertRewardOffer(harness, richards, 506, 750, Array.Empty<uint>(), new uint[] { 35784, 20250 });
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, richards.EntityId, 506));
            var ordinary = harness.Map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList)
                .First(creature => creature.DbId == 88 && creature.State != CharacterState.Dead);
            Kill(harness, ordinary);
            Assert.AreEqual(0U, harness.Client.Player.Missions[506].Objectives[1].Counters[0]);

            var spawns = new uint[] { 630040, 630041, 630042 };
            for (var index = 0; index < spawns.Length; index++)
            {
                var eggLayer = harness.Map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList)
                    .Single(creature => creature.SpawnPool?.DbId == spawns[index]);
                Assert.AreEqual(630040U, eggLayer.DbId);
                Assert.AreEqual(10240U, (uint)eggLayer.EntityClass);
                Assert.AreEqual(0U, eggLayer.NameId, "Use the native Miasma Egg-Layer class display, not an invented NPC name.");
                Assert.IsNull(eggLayer.Npc);
                Assert.IsTrue(harness.Map.NavMesh.IsUnderground(eggLayer.Position));
                Kill(harness, eggLayer);
                Assert.AreEqual((uint)index + 1, harness.Client.Player.Missions[506].Objectives[1].Counters[0]);
                if (index < 2)
                {
                    Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[506].Objectives[1].State);
                    Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, richards.EntityId, 506, 0));
                }
            }
            Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[506].Objectives[1].State);
            var experience = harness.Client.Player.Experience;
            var credits = harness.Client.Player.Credits[CurencyType.Credits];
            Assert.IsTrue(harness.Manager.CompleteOfferedMission(harness.Client, richards.EntityId, 506, 0));
            Assert.AreEqual(experience + 5000, harness.Client.Player.Experience);
            Assert.AreEqual(credits + 750, harness.Client.Player.Credits[CurencyType.Credits]);
            Assert.AreEqual(1U, HeldQuantity(harness, 35784));
            Assert.AreEqual(0U, HeldQuantity(harness, 20250));
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, richards.EntityId, 506, 0));
        }

        internal static WildernessRuntimeTestHarness CreateHarness() =>
            WildernessRuntimeTestHarness.Create();

        internal static void AssertSniperProfile(Creature sniper)
        {
            Assert.AreEqual(630046U, sniper.DbId);
            Assert.AreEqual(7120U, (uint)sniper.EntityClass);
            Assert.AreEqual(406U, sniper.NameId);
            Assert.IsNull(sniper.Npc);
            Assert.IsTrue(sniper.AppearanceData.TryGetValue(EquipmentData.Weapon, out var weapon));
            Assert.AreEqual(7119U, weapon.Class);
            var action = sniper.Actions.Single(candidate => candidate.Id == 630052);
            Assert.AreEqual(1U, (uint)action.ActionId);
            Assert.AreEqual(149U, action.ActionArgId);
            Assert.AreEqual(60d, action.RangeMax);
            Assert.IsTrue(action.RangeMin < action.RangeMax && action.MaxDamage >= action.MinDamage && action.MaxDamage > 0);
            Assert.IsFalse(sniper.Actions.Any(candidate => (uint)candidate.ActionId == 1 && candidate.ActionArgId == 96),
                "The human-only native1/96 animation profile must not be attached to a Lightbender.");
        }

        internal static void CompleteEarlierHistory(WildernessRuntimeTestHarness harness, params uint[] missionIds) =>
            CompleteEarlierHistory(harness, harness.Client, missionIds);

        private static void CompleteEarlierHistory(WildernessRuntimeTestHarness harness, Client client, params uint[] missionIds)
        {
            using var unit = harness.CreateChar();
            unit.ExecuteTransaction(() =>
            {
                foreach (var missionId in missionIds)
                    unit.CharacterMissions.Runtime.Archive(
                        new CharacterMissionEntry(client.Player.Id, missionId, (uint)MissionState.Completed)
                        {
                            AssignmentId = Guid.NewGuid().ToString("N"),
                            Generation = 1,
                            ContentRevision = WildernessMissionDataV1.Revision
                        }, harness.UtcNow);
            });
            harness.Manager.HydrateAndClearInvalid(client.Player, unit);
        }

        private static uint HeldQuantity(WildernessRuntimeTestHarness harness, uint template, Client client = null) =>
            (client ?? harness.Client).Player.Inventory.PersonalInventory
                .Where(id => id != 0)
                .Select(EntityManager.Instance.GetItem)
                .Where(item => item.ItemTemplateId == template)
                .Aggregate(0U, (total, item) => total + item.StackSize);

        private static void AssertRewardOffer(WildernessRuntimeTestHarness harness, Creature giver, uint missionId,
            uint credits, uint[] fixedItems, uint[] selectableItems)
        {
            harness.MoveTo(giver.Position);
            harness.Drain();
            new NpcManager(harness, harness.Manager).RequestNpcConverse(harness.Client,
                new RequestNPCConversePacket { EntityId = giver.EntityId });
            var conversation = harness.Drain().OfType<ConversePacket>().Single();
            var offers = conversation.ConvoDataDict[ConversationType.MissionDispense] as Dictionary<uint, MissionInfo>;
            Assert.IsNotNull(offers);
            Assert.IsTrue(offers.TryGetValue(missionId, out var offer), $"Native offer {missionId} is missing.");
            var reward = offer.MissionConstantData.RewardInfo;
            Assert.AreEqual(credits, reward.FixedReward.Credits[CurencyType.Credits]);
            CollectionAssert.AreEqual(fixedItems, reward.FixedReward.FixedItems.Select(item => item.ItemTemplateId).ToArray());
            CollectionAssert.AreEqual(selectableItems, reward.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsTrue(reward.FixedReward.FixedItems.Concat(reward.SelectableReward)
                .All(item => item.Quantity == 1 && item.ModuleIds.Count == 0),
                "The preview must advertise the same functional base items the server can actually grant.");
        }

        private static uint OwnedQuantity(WildernessRuntimeTestHarness harness, uint missionId, Client client = null) =>
            (client ?? harness.Client).Player.Inventory.PersonalInventory
                .Where(id => id != 0)
                .Select(EntityManager.Instance.GetItem)
                .Where(item => item.MissionOwnership?.MissionId == missionId)
                .Aggregate(0U, (total, item) => total + item.StackSize);

        private static DynamicObject Object(WildernessRuntimeTestHarness harness, uint missionId, string role, Client client = null)
        {
            var owner = (client ?? harness.Client).Player.Id;
            var obj = harness.Map.DynamicObjects.SingleOrDefault(candidate =>
                candidate.SceneMissionId == missionId && candidate.SceneOwnerCharacterId == owner &&
                candidate.SceneActorRole == role && candidate.IsInWorld && candidate.IsEnabled);
            Assert.IsNotNull(obj, $"No live assignment-owned {missionId}/{role} for character {owner}.");
            return obj;
        }

        private static void Use(WildernessRuntimeTestHarness harness, DynamicObject obj, uint argument, Client client = null)
        {
            client ??= harness.Client;
            var position = (uint)obj.EntityClassId == 7906
                ? WildernessElohPinholePlacementTests.HarvesterApproach(obj.SceneActorRole).Approach
                : obj.Position;
            client.SetWorldPosition(position, client.Player.Rotation);
            CellManager.Instance.UpdateVisibility(client);
            harness.Objects.RequestUseObjectPacket(client,
                new RequestUseObjectPacket { ActionId = ActionId.UseObject, ActionArgId = argument, EntityId = obj.EntityId });
            ActorActionManager.Instance.DoWork(harness.Map, obj.WindupTime + 1);
        }

        private static void Visit(WildernessRuntimeTestHarness harness, Vector3 center, float radius)
        {
            var outside = center + new Vector3(radius + 1, 0, 0);
            var inside = center + new Vector3(radius - 1, 0, 0);
            harness.MoveTo(outside);
            harness.Client.Movement = new Movement(outside, 1, 0, Vector2.Zero);
            harness.Client.Player.MoveBudget = 10;
            Assert.IsTrue(harness.Client.HandleMovement(new Movement(inside, 1, 0, Vector2.Zero)));
        }

        private static void Hit(WildernessRuntimeTestHarness harness, DynamicObject obj, int damage, Client client = null)
        {
            client ??= harness.Client;
            var (approach, hitPoint) = WildernessElohPinholePlacementTests.HarvesterApproach(obj.SceneActorRole);
            client.SetWorldPosition(approach, client.Player.Rotation);
            CellManager.Instance.UpdateVisibility(client);
            Assert.IsTrue(Vector3.Distance(approach + new Vector3(0, 1.6f, 0), hitPoint) < 3.1f);
            MissileManager.Instance.MissileLaunch(harness.Map,
                new ActionData(client.Player, ActionId.WeaponAttack, 133, obj.EntityId, 0), damage);
            MissileManager.Instance.DoWork(harness.Map, 1000);
        }

        private static void Kill(WildernessRuntimeTestHarness harness, Creature creature)
        {
            harness.MoveTo(creature.Position);
            creature.Attributes[Attributes.Health].Current = 0;
            harness.Creatures.HandleCreatureKill(harness.Map, creature, harness.Client.Player);
        }

        private static Client Reconnect(WildernessRuntimeTestHarness harness)
        {
            var previous = harness.Client;
            CellManager.Instance.RemoveFromWorld(previous);
            harness.Map.ClientList.Remove(previous);
            var client = harness.Context.CreateCompetingClient(harness.Manager);
            client.Player.State = CharacterState.Idle;
            client.Player.Race = previous.Player.Race;
            client.Player.Class = previous.Player.Class;
            client.Player.RuntimeMapChannel = harness.Map;
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
    }
}
