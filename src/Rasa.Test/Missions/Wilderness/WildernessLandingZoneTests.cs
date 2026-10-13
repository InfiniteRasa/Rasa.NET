using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Packets.Communicator.Server;
using Rasa.Packets.Inventory.Client;
using Rasa.Packets.LootDispenser.Client;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.MapChannel.Server;
using Rasa.Packets.Mission.Server;
using Rasa.Services.Preloader.Missions.Wilderness;
using Rasa.Structures;
using Rasa.Structures.Char;

namespace Rasa.Test.Missions.Wilderness
{
    [TestClass]
    [DoNotParallelize]
    public class WildernessLandingZoneTests
    {
        [TestMethod]
        public void DoctorOjyOffersSoldiersBloodThroughNativeConversationAndAssignment()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            harness.SpawnWorld(188);
            var ojy = harness.Npc(188);
            Assert.IsNotNull(ojy, "Dr. Ojy must spawn from the migrated public World pool.");
            Assert.AreEqual(110U, ojy.DbId);
            harness.MoveTo(ojy.Position);
            harness.Drain();
            var npcs = new NpcManager(harness, harness.Manager);

            npcs.RequestNpcConverse(harness.Client,
                new RequestNPCConversePacket { EntityId = ojy.EntityId });

            var conversation = harness.Drain().OfType<ConversePacket>().SingleOrDefault();
            Assert.IsNotNull(conversation, "The real Dr. Ojy must open a native conversation.");
            Assert.IsTrue(conversation.ConvoDataDict.TryGetValue(ConversationType.MissionDispense, out var offers),
                "Dr. Ojy must offer the Wilderness L.Z. Soldier's Blood mission.");
            Assert.IsTrue(((Dictionary<uint, MissionInfo>)offers).ContainsKey(776),
                "Ojy offers native mission 776, not Hansen's mission 777.");

            npcs.AssignNPCMission(harness.Client,
                new AssignNPCMissionPacket { NpcEntityId = ojy.EntityId, MissionId = 776 });

            Assert.IsTrue(harness.Client.Player.Missions.TryGetValue(776, out var mission));
            Assert.AreEqual(MissionState.Active, mission.State);
            Assert.AreEqual(MissionObjectiveState.Incomplete, mission.Objectives[2].State);
            Assert.IsFalse(mission.Completeable, "Accepting the mission must not supply the ten blood samples.");
            var gained = harness.Drain().OfType<MissionGainedPacket>().Single();
            Assert.AreEqual(776U, gained.MissionId);
            Assert.AreEqual(10000044U, gained.MissionInfo.MissionConstantData.CategoryId);
        }

        [TestMethod]
        [DataRow(776U, 2U, 2524U, 11150U, 10U, 3U, 580019U, 0U, 580019U)]
        [DataRow(795U, 3U, 2557U, 11317U, 4U, 630071U, 630071U, 776U, 630071U)]
        [DataRow(771U, 2U, 2527U, 11153U, 6U, 85U, 580033U, 795U, 580034U)]
        public void OjyCollectionRequiresEligibleCorpsePickupAndChargesTheExactItemOnce(
            uint missionId, uint objectiveId, uint templateId, uint classId, uint quantity,
            uint creatureId, uint spawnId, uint prerequisite, uint postQuotaSpawnId)
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            if (prerequisite != 0)
                CompleteHistory(harness, prerequisite);
            var ojy = Accept(harness, 188, missionId);
            var npcs = new NpcManager(harness, harness.Manager);
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, ojy.EntityId, missionId, null));
            var wrong = Kill(harness, creatureId == 3 ? 85U : 3U, creatureId == 3 ? 580033U : 580019U);
            Assert.IsFalse(wrong.LootItems.Any(item => item.ItemTemplateId == templateId));

            for (uint collected = 0; collected < quantity; collected++)
            {
                var loot = Kill(harness, creatureId, spawnId);
                Assert.AreEqual(1U, loot.LootItems.Single(item => item.ItemTemplateId == templateId).ItemQuantity);
                Assert.AreEqual(collected, harness.Client.Player.Missions[missionId].Objectives[objectiveId].ItemCounters[classId]);
                Take(harness, loot);
                Assert.AreEqual(collected + 1,
                    harness.Client.Player.Missions[missionId].Objectives[objectiveId].ItemCounters[classId]);
                Take(harness, loot);
                Assert.AreEqual(collected + 1, HeldQuantity(harness, templateId));
            }

            Assert.IsTrue(harness.Client.Player.Missions[missionId].Completeable);
            Assert.AreEqual(quantity, HeldQuantity(harness, templateId));
            Assert.IsFalse(Kill(harness, creatureId, postQuotaSpawnId).LootItems.Any(item => item.ItemTemplateId == templateId),
                "The real corpse path must stop issuing mission items once the turn-in quantity is held.");
            var before = harness.Context.ReadRewardTotals();
            harness.MoveTo(ojy.Position);
            npcs.RequestNpcConverse(harness.Client, new RequestNPCConversePacket { EntityId = ojy.EntityId });
            harness.Drain();
            npcs.CompleteNPCMission(harness.Client,
                new CompleteNPCMissionPacket { EntityId = ojy.EntityId, MissionId = missionId });
            Assert.AreEqual(MissionState.Completed, harness.Client.Player.Missions[missionId].State);
            Assert.AreEqual(0U, HeldQuantity(harness, templateId));
            Assert.AreEqual(before.Credits + 300, harness.Context.ReadRewardTotals().Credits);
            Assert.AreEqual(before.Experience + 4000, harness.Context.ReadRewardTotals().Experience);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count());
            npcs.CompleteNPCMission(harness.Client,
                new CompleteNPCMissionPacket { EntityId = ojy.EntityId, MissionId = missionId });
            Assert.AreEqual(before.Credits + 300, harness.Context.ReadRewardTotals().Credits);
        }

        [TestMethod]
        public void FuelCounterRequiresTenDistinctDestroyedNativeContainers()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var rodriguez = Accept(harness, 172, 665);
            var fuel = Enumerable.Range(1, 10).Select(id => harness.Map.DynamicObjects.Single(obj =>
                obj.SceneMissionId == 665 && obj.SceneActorRole == $"fuel-{id}")).ToArray();
            Assert.AreEqual(10, fuel.Select(obj => obj.EntityId).Distinct().Count());
            Assert.AreEqual(10, fuel.Select(obj => obj.Position).Distinct().Count());
            foreach (var container in fuel)
            {
                Assert.AreEqual(9260U, (uint)container.EntityClassId);
                Assert.IsNull(container.MissionUseAction);
                var ground = harness.Map.NavMesh.GroundHeight(container.Position);
                Assert.IsNotNull(ground);
                Assert.IsTrue(Math.Abs(ground.Value - container.Position.Y) < 0.5f);
            }
            DamageObject(harness, fuel[0], 99);
            Assert.AreEqual(1U, fuel[0].CurrentHitPoints);
            Assert.AreEqual(0U, harness.Client.Player.Missions[665].Objectives[1].Counters[0]);

            DamageObject(harness, fuel[0], 1);
            DamageObject(harness, fuel[0], 100);

            Assert.AreEqual(UseObjectState.StateDestroyed, fuel[0].StateId);
            Assert.IsFalse(fuel[0].IsEnabled);
            Assert.AreEqual(1U, harness.Client.Player.Missions[665].Objectives[1].Counters[0],
                "Further attacks on one destroyed barrel must not replace distinct targets.");
            for (var index = 1; index < 9; index++)
                DamageObject(harness, fuel[index], 100);
            Assert.AreEqual(9U, harness.Client.Player.Missions[665].Objectives[1].Counters[0]);
            Assert.IsFalse(harness.Client.Player.Missions[665].Completeable);
            Assert.IsFalse(harness.Manager.RecordProgress(harness.Client, MissionProgressEvent.Scenario(665, 2, 10)));
            Assert.IsFalse(harness.Manager.RecordProgress(harness.Client, MissionProgressEvent.Scenario(430, 1, 10)));

            DamageObject(harness, fuel[9], 100);

            Assert.AreEqual(10U, harness.Client.Player.Missions[665].Objectives[1].Counters[0]);
            Assert.IsTrue(harness.Client.Player.Missions[665].Completeable);
            Assert.IsTrue(fuel.All(obj => obj.CurrentHitPoints == 0 && !obj.IsEnabled));
            harness.MoveTo(rodriguez.Position);
            var npcs = new NpcManager(harness, harness.Manager);
            npcs.RequestNpcConverse(harness.Client, new RequestNPCConversePacket { EntityId = rodriguez.EntityId });
            npcs.CompleteNPCMission(harness.Client,
                new CompleteNPCMissionPacket { EntityId = rodriguez.EntityId, MissionId = 665, SelectionIdx = 0 });
            Assert.AreEqual(MissionState.Completed, harness.Client.Player.Missions[665].State);
            Assert.AreEqual(1U, HeldQuantity(harness, 13744));
            Assert.AreEqual(0U, HeldQuantity(harness, 28692));
        }

        // The bar over a fuel container is redrawn on USABLE_HITPOINT_CHANGE, which the client posts
        // from Recv_UpdateHitPoints alone: Recv_DamageInfo stores the figure and posts nothing.
        [TestMethod]
        public void DamagedFuelContainerTellsItsHitPointsWithUpdateHitPoints()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            Accept(harness, 172, 665);
            var fuel = harness.Map.DynamicObjects.Single(obj => obj.SceneMissionId == 665 && obj.SceneActorRole == "fuel-1");
            harness.MoveTo(fuel.Position + new Vector3(0, 0, 1));
            harness.Drain();

            DamageObject(harness, fuel, 40);

            var hit = harness.Drain();
            Assert.AreEqual(60, hit.OfType<UpdateHitPointsPacket>().Single().CurrentHitPoints);
            Assert.AreEqual(0, hit.OfType<DamageInfoPacket>().Count(), "DamageInfo would store the figure and leave the bar as it was.");

            DamageObject(harness, fuel, 60);

            var destroyed = harness.Drain().Where(packet =>
                packet is UpdateHitPointsPacket || packet is DamageInfoPacket || packet is ForceStatePacket || packet is UsePacket).ToArray();
            Assert.AreEqual(5, destroyed.Length);
            Assert.AreEqual(0, ((UpdateHitPointsPacket)destroyed[0]).CurrentHitPoints, "The update comes before DamageInfo stores the same figure.");
            var info = (DamageInfoPacket)destroyed[1];
            Assert.IsFalse(info.CanBeDamaged);
            Assert.AreEqual(100U, info.TotalHitPoints);
            Assert.AreEqual(0U, info.CurrentHitPoints);
            CollectionAssert.AreEqual(
                new[] { UseObjectState.IdesState50pHealth, UseObjectState.IdesState25pHealth, UseObjectState.StateDestroyed },
                destroyed.Skip(2).Cast<UsePacket>().Select(use => use.CurState).ToArray(),
                "From intact, down a health state at a time: only 25% to destroyed has the explosion.");
        }

        // A Bane barrel is an InertDestroyable: its client goes down through its health states and has
        // its explosion only on the way from 25% to destroyed (DestroyableStates).
        [TestMethod]
        public void AFuelContainerShowsItsDamageAStateAtATimeAndBlowsUpFromTheLast()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            Accept(harness, 172, 665);
            var fuel = harness.Map.DynamicObjects.Single(obj => obj.SceneMissionId == 665 && obj.SceneActorRole == "fuel-1");
            harness.MoveTo(fuel.Position + new Vector3(0, 0, 1));
            harness.Drain();

            DamageObject(harness, fuel, 49);
            Assert.IsEmpty(harness.Drain().OfType<UsePacket>().ToArray(), "51 of 100: intact");
            Assert.AreEqual(UseObjectState.IdesStateIntact, fuel.StateId);

            DamageObject(harness, fuel, 1);
            Assert.AreEqual(UseObjectState.IdesState50pHealth, harness.Drain().OfType<UsePacket>().Single().CurState, "half");
            Assert.AreEqual(UseObjectState.IdesState50pHealth, fuel.StateId);

            DamageObject(harness, fuel, 30);
            Assert.AreEqual(UseObjectState.IdesState25pHealth, harness.Drain().OfType<UsePacket>().Single().CurState, "20 of 100");
            Assert.AreEqual(UseObjectState.IdesState25pHealth, fuel.StateId);

            DamageObject(harness, fuel, 20);
            var sent = harness.Drain();
            var use = sent.OfType<UsePacket>().Single();
            Assert.AreEqual((harness.Client.Player.EntityId, UseObjectState.StateDestroyed), (use.PlayerEntityId, use.CurState));
            Assert.IsEmpty(sent.OfType<ForceStatePacket>().ToArray(), "Use, so the transition's explosion plays");
            Assert.AreEqual(UseObjectState.StateDestroyed, fuel.StateId);
        }

        // A chaingun is not fired as a missile (ConstantFire): its pulses have to reach an object too.
        [TestMethod]
        public void ConstantFireWeaponDamagesAndDestroysAFuelContainer()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            Accept(harness, 172, 665);
            var fuel = harness.Map.DynamicObjects.Single(obj => obj.SceneMissionId == 665 && obj.SceneActorRole == "fuel-1");
            var other = harness.Map.DynamicObjects.Single(obj => obj.SceneMissionId == 665 && obj.SceneActorRole == "fuel-2");
            harness.MoveTo(fuel.Position + new Vector3(0, 0, 1));
            var weapon = new Item
            {
                ItemTemplate = new ItemTemplate(new Rasa.Structures.World.ItemTemplateItemClassEntry { ItemTemplateId = 145, ItemClass = 6048 })
                {
                    WeaponInfo = new WeaponInfo(new Rasa.Structures.World.ItemTemplateWeaponEntry
                    {
                        Id = 145, AmmoPerShot = 1, Refire = 200, ReloadTime = 1500,
                        Windup = 0, Recovery = 1, Range = 80, ToolType = 15, AttackType = 2
                    })
                },
                ItemTemplateId = 145, StackSize = 1, Crafter = ""
            };
            var action = new ActionData(harness.Client.Player, ActionId.WeaponMachinegun, 4, 0) { TargetId = fuel.EntityId };
            harness.Client.Player.Target = fuel.EntityId;
            harness.Drain();

            try
            {
                ConstantFire.Pulse(harness.Map, harness.Client, weapon, action, 40, DamageType.Laser, 0);

                Assert.AreEqual(60U, fuel.CurrentHitPoints);
                Assert.AreEqual(100U, other.CurrentHitPoints, "Only the object aimed at.");
                var sent = harness.Drain();
                var entry = sent.OfType<ConstantFireTickPacket>().Single().Pulses.Single().Single();
                Assert.AreEqual(fuel.EntityId, entry.EntityId);
                Assert.AreEqual(40, entry.Amount);
                Assert.AreEqual(DamageType.Laser, entry.DamageType);
                Assert.AreEqual(60, sent.OfType<UpdateHitPointsPacket>().Single().CurrentHitPoints);

                ConstantFire.Pulse(harness.Map, harness.Client, weapon, action, 40, DamageType.Laser, 0);
                ConstantFire.Pulse(harness.Map, harness.Client, weapon, action, 40, DamageType.Laser, 0);

                Assert.AreEqual(0U, fuel.CurrentHitPoints);
                Assert.AreEqual(UseObjectState.StateDestroyed, fuel.StateId);
                Assert.AreEqual(1U, harness.Client.Player.Missions[665].Objectives[1].Counters[0]);

                // Destroyed, it takes no more: the pulse lists nothing.
                harness.Drain();
                ConstantFire.Pulse(harness.Map, harness.Client, weapon, action, 40, DamageType.Laser, 0);
                Assert.AreEqual(0, harness.Drain().OfType<ConstantFireTickPacket>().Single().Pulses.Single().Count);
                Assert.AreEqual(1U, harness.Client.Player.Missions[665].Objectives[1].Counters[0]);
            }
            finally
            {
                ConstantFire.Stop(harness.Client);
                harness.Client.Player.Target = 0;
            }
        }

        [TestMethod]
        public void FuelDestructionAndDistinctProgressSurviveRehydrationWithoutRespawningDestroyedTargets()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            Accept(harness, 172, 665);
            var first = harness.Map.DynamicObjects.Single(obj => obj.SceneMissionId == 665 && obj.SceneActorRole == "fuel-1");
            DamageObject(harness, first, 100);
            var assignment = harness.Client.Player.Missions[665].AssignmentId;
            var reconnected = harness.Context.CreateCompetingClient(harness.Manager);

            harness.Manager.PublishInitialState(reconnected);
            harness.Manager.PublishInitialState(reconnected);

            Assert.AreEqual(assignment, reconnected.Player.Missions[665].AssignmentId);
            Assert.AreEqual(1U, reconnected.Player.Missions[665].Objectives[1].Counters[0]);
            Assert.IsFalse(reconnected.Player.Missions[665].Completeable);
            var restored = harness.Map.DynamicObjects.Single(obj => obj.SceneMissionId == 665 && obj.SceneActorRole == "fuel-1");
            Assert.IsFalse(restored.IsEnabled);
            Assert.AreEqual(UseObjectState.StateDestroyed, restored.StateId);
        }

        [TestMethod]
        public void AllFourMortarsAreRequiredAndRespawningOneCannotReplaceTheOthers()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var wagner = Accept(harness, 181, 430);
            foreach (var id in new uint[] { 630076, 630077, 630078, 630079 })
                Assert.AreEqual(7482U, harness.World.CreatureEntries.Single(creature => creature.Id == id).ClassId);
            for (var repeat = 0; repeat < 4; repeat++)
                Kill(harness, 630076, 630076);
            Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[430].Objectives[3].State);
            foreach (var objective in new uint[] { 4, 5, 6 })
                Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[430].Objectives[objective].State);
            Assert.IsFalse(harness.Client.Player.Missions[430].Completeable);
            Kill(harness, 630079, 630079);
            Kill(harness, 630077, 630077);
            Assert.IsFalse(harness.Client.Player.Missions[430].Completeable);

            Kill(harness, 630078, 630078);

            Assert.IsTrue(harness.Client.Player.Missions[430].Completeable);
            harness.MoveTo(wagner.Position);
            var npcs = new NpcManager(harness, harness.Manager);
            npcs.RequestNpcConverse(harness.Client, new RequestNPCConversePacket { EntityId = wagner.EntityId });
            npcs.CompleteNPCMission(harness.Client,
                new CompleteNPCMissionPacket { EntityId = wagner.EntityId, MissionId = 430, SelectionIdx = 0 });
            Assert.AreEqual(MissionState.Completed, harness.Client.Player.Missions[430].State);
            Assert.AreEqual(1U, HeldQuantity(harness, 26940));
            Assert.AreEqual(0U, HeldQuantity(harness, 13739));
        }

        [TestMethod]
        public void MortarIndicatorsAreSentWithoutAnotherMissionsIndicatorName()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            harness.SpawnWorld(181);
            var wagner = harness.Npc(181);
            harness.MoveTo(wagner.Position);
            var npcs = new NpcManager(harness, harness.Manager);
            npcs.RequestNpcConverse(harness.Client, new RequestNPCConversePacket { EntityId = wagner.EntityId });
            harness.Drain();

            npcs.AssignNPCMission(harness.Client,
                new AssignNPCMissionPacket { NpcEntityId = wagner.EntityId, MissionId = 430 });

            var gained = harness.Drain().OfType<MissionGainedPacket>().Single(packet => packet.MissionId == 430);
            var objectives = gained.MissionInfo.ObjectivesList.ToDictionary(objective => objective.ObjectiveId);
            CollectionAssert.AreEquivalent(new uint[] { 3, 4, 5, 6 }, objectives.Keys.ToArray());
            foreach (var objective in objectives.Values)
            {
                // The rows were keyed 3..6 by objective, and sent as ids those are "Possible Food
                // Crate Location", "Warnet Valley", "Calla Ferns" and "AFS Hydro-electric Plant".
                // Without an id the client names each by its objective, "Destroy Mortar #1".."#4".
                var indicator = objective.IndicatorList.Single();
                Assert.AreEqual(MissionIndicator.UnnamedFrom + objective.ObjectiveId, indicator.IndicatorId);
                Assert.IsNull(indicator.ClientNameId, $"objective {objective.ObjectiveId}");
            }
        }

        [TestMethod]
        public void OjyUnlocksTheAuthoredBloodGlandAndDroneChainOnlyAfterEachRealTurnIn()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var ojy = Accept(harness, 188, 776);
            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, ojy.EntityId, 795));
            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, ojy.EntityId, 771));
            foreach (var (missionId, quantity, creatureId, spawnId, templateId) in new[]
            {
                (776U, 10, 3U, 580019U, 2524U),
                (795U, 4, 630071U, 630071U, 2557U),
                (771U, 6, 85U, 580033U, 2527U)
            })
            {
                if (missionId != 776)
                    Accept(harness, 188, missionId);
                for (var count = 0; count < quantity; count++)
                    Take(harness, Kill(harness, creatureId, spawnId));
                Assert.IsTrue(harness.Client.Player.Missions[missionId].Completeable);
                harness.MoveTo(ojy.Position);
                if (missionId == 776)
                    Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, ojy.EntityId, 795));
                if (missionId == 795)
                    Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, ojy.EntityId, 771));
                var npcs = new NpcManager(harness, harness.Manager);
                npcs.RequestNpcConverse(harness.Client, new RequestNPCConversePacket { EntityId = ojy.EntityId });
                npcs.CompleteNPCMission(harness.Client,
                    new CompleteNPCMissionPacket { EntityId = ojy.EntityId, MissionId = missionId });
                Assert.AreEqual(MissionState.Completed, harness.Client.Player.Missions[missionId].State);
                Assert.AreEqual(0U, HeldQuantity(harness, templateId));
            }
        }

        [TestMethod]
        public void FullMissionBagLeavesTheBloodOnTheCorpseAndRetryClaimsItOnce()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            Accept(harness, 188, 776);
            Grant(harness, 747, 50);
            var loot = Kill(harness, 3, 580019);
            var before = harness.Context.ReadRewardTotals();

            Take(harness, loot);

            Assert.AreEqual(before, harness.Context.ReadRewardTotals());
            Assert.AreEqual(0U, HeldQuantity(harness, 2524));
            Assert.AreEqual(0U, harness.Client.Player.Missions[776].Objectives[2].ItemCounters[11150]);
            Assert.IsTrue(loot.LootItems.All(item => !item.Taken));
            var filler = harness.Client.Player.Inventory.PersonalInventory.Where(id => id != 0)
                .Select(EntityManager.Instance.GetItem).First(item => item.ItemTemplate.ItemTemplateId == 747);
            InventoryManager.Instance.PersonalInventory_DestroyItem(harness.Client,
                new PersonalInventory_DestroyItemPacket { EntityId = filler.EntityId, Quantity = 1 });

            Take(harness, loot);
            Take(harness, loot);

            Assert.AreEqual(1U, HeldQuantity(harness, 2524));
            Assert.AreEqual(1U, harness.Client.Player.Missions[776].Objectives[2].ItemCounters[11150]);
            Assert.IsTrue(loot.FullyLooted);
            Assert.IsNull(loot.LootItems.Single(item => item.ItemTemplateId == 2524).Item.MissionOwnership);
        }

        [TestMethod]
        public void AbandonedBloodLootCannotBeClaimedByTheNextAttempt()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var ojy = Accept(harness, 188, 776);
            var oldAssignment = harness.Client.Player.Missions[776].AssignmentId;
            var loot = Kill(harness, 3, 580019);
            var npcs = new NpcManager(harness, harness.Manager);
            npcs.AbandonMission(harness.Client, new AbandonMissionPacket { MissionId = 776 });
            harness.MoveTo(ojy.Position);
            npcs.RequestNpcConverse(harness.Client, new RequestNPCConversePacket { EntityId = ojy.EntityId });
            npcs.AssignNPCMission(harness.Client,
                new AssignNPCMissionPacket { NpcEntityId = ojy.EntityId, MissionId = 776 });
            Assert.AreNotEqual(oldAssignment, harness.Client.Player.Missions[776].AssignmentId);
            harness.MoveTo(loot.Corpse.Position);

            Take(harness, loot);

            Assert.AreEqual(0U, HeldQuantity(harness, 2524));
            Assert.AreEqual(0U, harness.Client.Player.Missions[776].Objectives[2].ItemCounters[11150]);
            Assert.IsTrue(loot.LootItems.All(item => !item.Taken));
        }

        [TestMethod]
        public void BloodProgressSurvivesRehydrationWithoutCountingKillsAsSamples()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            Accept(harness, 188, 776);
            var assignment = harness.Client.Player.Missions[776].AssignmentId;
            Take(harness, Kill(harness, 3, 580019));
            Take(harness, Kill(harness, 3, 580019));
            Kill(harness, 3, 580019);

            var reconnected = harness.Context.CreateCompetingClient(harness.Manager);
            harness.Manager.PublishInitialState(reconnected);

            Assert.AreEqual(assignment, reconnected.Player.Missions[776].AssignmentId);
            Assert.AreEqual(2U, reconnected.Player.Missions[776].Objectives[2].ItemCounters[11150]);
            Assert.AreEqual(MissionObjectiveState.Incomplete, reconnected.Player.Missions[776].Objectives[2].State);
            Assert.AreEqual(2U, HeldQuantity(harness, 2524));
            Assert.IsFalse(reconnected.Player.Missions[776].Completeable);
        }

        [TestMethod]
        [DataRow(3U, false)]
        [DataRow(10U, false)]
        [DataRow(3U, true)]
        [DataRow(10U, true)]
        public void RetainedBloodCarriesIntoTheNextAttemptWithoutReplayingInventoryProgress(uint retained, bool fail)
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var ojy = Accept(harness, 188, 776);
            var oldAssignment = harness.Client.Player.Missions[776].AssignmentId;
            for (var count = 0U; count < retained; count++)
                Take(harness, Kill(harness, 3, 580019));
            var npcs = new NpcManager(harness, harness.Manager);
            // Failed, the mission is out of the client's log, with no Abandon to send for it:
            // the new assignment below takes the failed one's place as it is.
            if (fail)
                Assert.IsTrue(harness.Manager.TryFailMission(harness.Client, 776));
            else
                npcs.AbandonMission(harness.Client, new AbandonMissionPacket { MissionId = 776 });
            Assert.AreEqual(retained, HeldQuantity(harness, 2524));
            harness.MoveTo(ojy.Position);
            npcs.RequestNpcConverse(harness.Client, new RequestNPCConversePacket { EntityId = ojy.EntityId });

            npcs.AssignNPCMission(harness.Client,
                new AssignNPCMissionPacket { NpcEntityId = ojy.EntityId, MissionId = 776 });

            Assert.AreNotEqual(oldAssignment, harness.Client.Player.Missions[776].AssignmentId);
            Assert.AreEqual(retained, harness.Client.Player.Missions[776].Objectives[2].ItemCounters[11150]);
            harness.Manager.PublishInitialState(harness.Client);
            harness.Manager.PublishInitialState(harness.Client);
            Assert.AreEqual(retained, harness.Client.Player.Missions[776].Objectives[2].ItemCounters[11150],
                "Existing inventory must be reconciled as an absolute lower bound, not repeatedly added.");
            for (var count = retained; count < 10; count++)
                Take(harness, Kill(harness, 3, 580019));
            Assert.AreEqual(10U, HeldQuantity(harness, 2524));
            Assert.IsTrue(harness.Client.Player.Missions[776].Completeable);
            harness.MoveTo(ojy.Position);
            npcs.RequestNpcConverse(harness.Client, new RequestNPCConversePacket { EntityId = ojy.EntityId });
            npcs.CompleteNPCMission(harness.Client,
                new CompleteNPCMissionPacket { EntityId = ojy.EntityId, MissionId = 776 });
            Assert.AreEqual(MissionState.Completed, harness.Client.Player.Missions[776].State);
            Assert.AreEqual(0U, HeldQuantity(harness, 2524));
        }

        [TestMethod]
        public void BloodTemplateAliasDoesNotSubstituteForTheAuthoredCollectionOrCost()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            Grant(harness, 16572, 10);
            var ojy = Accept(harness, 188, 776);
            Assert.AreEqual(0U, harness.Client.Player.Missions[776].Objectives[2].ItemCounters[11150]);
            Assert.IsFalse(harness.Client.Player.Missions[776].Completeable);
            for (var count = 0; count < 10; count++)
                Take(harness, Kill(harness, 3, 580019));
            harness.MoveTo(ojy.Position);
            var npcs = new NpcManager(harness, harness.Manager);
            npcs.RequestNpcConverse(harness.Client, new RequestNPCConversePacket { EntityId = ojy.EntityId });

            npcs.CompleteNPCMission(harness.Client,
                new CompleteNPCMissionPacket { EntityId = ojy.EntityId, MissionId = 776 });

            Assert.AreEqual(MissionState.Completed, harness.Client.Player.Missions[776].State);
            Assert.AreEqual(0U, HeldQuantity(harness, 2524));
            Assert.AreEqual(10U, HeldQuantity(harness, 16572));
        }

        [TestMethod]
        [DataRow(776U, false)]
        [DataRow(776U, true)]
        [DataRow(771U, false)]
        [DataRow(771U, true)]
        public void ActiveAliasGrantOrRejectedNativeSaleDoesNotAdvanceOjyCollections(
            uint missionId, bool attemptSale)
        {
            var (alias, template, itemClass, quantity, creature, spawn, prerequisite) = missionId switch
            {
                776 => (16572U, 2524U, 11150U, 10U, 3U, 580019U, 0U),
                771 => (16575U, 2527U, 11153U, 6U, 85U, 580033U, 795U),
                _ => throw new ArgumentOutOfRangeException(nameof(missionId))
            };
            using var harness = WildernessRuntimeTestHarness.Create();
            if (prerequisite != 0)
                CompleteHistory(harness, prerequisite);
            if (attemptSale)
                Grant(harness, alias, quantity);
            Accept(harness, 188, missionId);
            Assert.AreEqual(0U, harness.Client.Player.Missions[missionId].Objectives[2].ItemCounters[itemClass]);

            if (attemptSale)
            {
                harness.SpawnWorld(120);
                var vendor = harness.Npc(120);
                Assert.IsNotNull(vendor);
                Assert.AreEqual(62U, vendor.DbId);
                Assert.IsNotNull(vendor.Npc.Vendor);
                Assert.AreEqual(12U, vendor.Npc.Vendor.VendorPackageId);
                harness.MoveTo(vendor.Position);
                var npcs = new NpcManager(harness, harness.Manager);
                npcs.RequestNpcConverse(harness.Client, new RequestNPCConversePacket { EntityId = vendor.EntityId });
                var conversation = harness.Drain().OfType<ConversePacket>().Single();
                Assert.IsTrue(conversation.ConvoDataDict.ContainsKey(ConversationType.Vending));
                var item = harness.Client.Player.Inventory.PersonalInventory.Where(id => id != 0)
                    .Select(EntityManager.Instance.GetItem).Single(item => item.ItemTemplate.ItemTemplateId == alias);
                Assert.AreEqual((int)LootQuality.Mission, item.ItemTemplate.QualityId);
                Assert.IsFalse(item.ItemTemplate.HasSellableFlag);
                Assert.IsTrue(item.ItemTemplate.NotTradable);
                var inventoryBefore = harness.Client.Player.Inventory.PersonalInventory.ToArray();
                var buybackBefore = harness.Client.Player.Inventory.BuybackItems.ToArray();
                var creditsBefore = harness.Client.Player.Credits[CurencyType.Credits];
                var totalsBefore = harness.Context.ReadRewardTotals();
                Assert.IsFalse(buybackBefore.Contains(item.EntityId));

                npcs.RequestVendorSale(harness.Client, new RequestVendorSalePacket
                {
                    VendorEntityId = vendor.EntityId, ItemEntityId = item.EntityId, Quantity = quantity
                });

                Assert.AreEqual(quantity, item.StackSize);
                Assert.AreEqual(quantity, HeldQuantity(harness, alias));
                Assert.AreSame(item, EntityManager.Instance.GetItem(item.EntityId));
                CollectionAssert.AreEqual(inventoryBefore, harness.Client.Player.Inventory.PersonalInventory.ToArray());
                CollectionAssert.AreEqual(buybackBefore, harness.Client.Player.Inventory.BuybackItems.ToArray());
                Assert.AreEqual(creditsBefore, harness.Client.Player.Credits[CurencyType.Credits]);
                Assert.AreEqual(totalsBefore, harness.Context.ReadRewardTotals());
                using var verify = harness.CreateChar();
                var owned = verify.CharacterInventories.FindByItemId(item.Id);
                Assert.IsNotNull(owned);
                Assert.AreEqual(harness.Client.Player.Id, owned.CharacterId);
                Assert.AreEqual((uint)InventoryType.Personal, owned.InventoryType);
                Assert.AreEqual(item.OwnerSlotId, owned.SlotId);
                Assert.AreEqual(quantity, verify.Items.GetItem(item.Id).StackSize);
            }
            else
            {
                var item = ItemManager.Instance.CreateFromTemplateId(alias, quantity);
                Assert.IsNotNull(item);
                Assert.IsNotNull(InventoryManager.Instance.GrantItemToInventory(harness.Client, item));
            }

            Assert.AreEqual(quantity, HeldQuantity(harness, alias));
            Assert.AreEqual(0U, HeldQuantity(harness, template));
            Assert.AreEqual(0U, harness.Client.Player.Missions[missionId].Objectives[2].ItemCounters[itemClass],
                "An alias grant or rejected sale must not advance the exact-template collection.");
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[missionId].Objectives[2].State);
            Assert.IsFalse(harness.Client.Player.Missions[missionId].Completeable);
            var packets = harness.Drain();
            if (attemptSale)
                Assert.AreEqual(1, packets.OfType<DisplayClientMessagePacket>()
                    .Count(packet => packet.MsgId == PlayerMessage.PmItemCanNotBeSold),
                    "The native no-sale guard must reject the quest alias, not an unrelated admission check.");
            Assert.IsFalse(packets.OfType<ObjectiveCompletedPacket>().Any(packet => packet.MissionId == missionId));

            Take(harness, Kill(harness, creature, spawn));

            Assert.AreEqual(quantity, HeldQuantity(harness, alias));
            Assert.AreEqual(1U, HeldQuantity(harness, template));
            Assert.AreEqual(1U, harness.Client.Player.Missions[missionId].Objectives[2].ItemCounters[itemClass],
                "The first real corpse claim must count only its exact bound template, not the held aliases.");
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[missionId].Objectives[2].State);
            Assert.IsFalse(harness.Client.Player.Missions[missionId].Completeable);
        }

        [TestMethod]
        public void SalterCannotIssueTheCatalyzerIntoAFullBagAndWagnerConsumesOnlyTheIssuedCopy()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            CompleteHistory(harness, 430);
            var wagner = Accept(harness, 181, 549);
            harness.SpawnWorld(193);
            var salter = harness.Npc(193);
            Assert.IsNotNull(salter);
            Assert.AreEqual(115U, salter.DbId);
            Assert.AreEqual(382U, salter.Npc.NpcPackageId);
            var npcs = new NpcManager(harness, harness.Manager);
            CompleteObjective(harness, npcs, wagner, 549);
            Assert.AreEqual(0U, HeldQuantity(harness, 747));
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[549].Objectives[1].State);
            Grant(harness, 747, 50);
            var before = harness.Context.ReadRewardTotals();

            CompleteObjective(harness, npcs, salter, 549);

            Assert.AreEqual(before, harness.Context.ReadRewardTotals());
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[549].Objectives[1].State);
            var filler = harness.Client.Player.Inventory.PersonalInventory.Where(id => id != 0)
                .Select(EntityManager.Instance.GetItem).First(item => item.ItemTemplate.ItemTemplateId == 747);
            InventoryManager.Instance.PersonalInventory_DestroyItem(harness.Client,
                new PersonalInventory_DestroyItemPacket { EntityId = filler.EntityId, Quantity = 1 });
            CompleteObjective(harness, npcs, salter, 549);
            Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[549].Objectives[1].State);
            var issued = harness.Client.Player.Inventory.PersonalInventory.Where(id => id != 0)
                .Select(EntityManager.Instance.GetItem).Single(item => item.MissionOwnership != null);
            Assert.AreEqual(747U, issued.ItemTemplate.ItemTemplateId);
            Assert.AreEqual(harness.Client.Player.Missions[549].AssignmentId, issued.MissionOwnership.AssignmentId);
            CompleteObjective(harness, npcs, salter, 549);
            Assert.AreEqual(50U, HeldQuantity(harness, 747));
            harness.MoveTo(wagner.Position);
            npcs.RequestNpcConverse(harness.Client, new RequestNPCConversePacket { EntityId = wagner.EntityId });
            harness.Drain();

            npcs.CompleteNPCMission(harness.Client,
                new CompleteNPCMissionPacket { EntityId = wagner.EntityId, MissionId = 549, SelectionIdx = 1 });

            Assert.AreEqual(MissionState.Completed, harness.Client.Player.Missions[549].State);
            Assert.AreEqual(49U, HeldQuantity(harness, 747), "Wagner must not consume an unrelated catalyzer.");
            Assert.AreEqual(1U, HeldQuantity(harness, 44918));
            Assert.AreEqual(0U, HeldQuantity(harness, 44917));
            npcs.CompleteNPCMission(harness.Client,
                new CompleteNPCMissionPacket { EntityId = wagner.EntityId, MissionId = 549, SelectionIdx = 1 });
            Assert.AreEqual(49U, HeldQuantity(harness, 747));
            Assert.AreEqual(1U, HeldQuantity(harness, 44918));
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void FailedOrAbandonedCatalyzerDeliveryCleansOnlyItsOwnItem(bool abandon)
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            CompleteHistory(harness, 430);
            Accept(harness, 181, 549);
            Grant(harness, 747, 1);
            harness.SpawnWorld(193);
            var npcs = new NpcManager(harness, harness.Manager);
            CompleteObjective(harness, npcs, harness.Npc(193), 549);
            Assert.AreEqual(2U, HeldQuantity(harness, 747));

            if (abandon)
                npcs.AbandonMission(harness.Client, new AbandonMissionPacket { MissionId = 549 });
            else
                Assert.IsTrue(harness.Manager.TryFailMission(harness.Client, 549));

            Assert.AreEqual(1U, HeldQuantity(harness, 747));
            Assert.IsFalse(harness.Client.Player.Inventory.PersonalInventory.Where(id => id != 0)
                .Select(EntityManager.Instance.GetItem).Any(item => item.MissionOwnership?.MissionId == 549));
        }

        [TestMethod]
        public void RandolphsDispatchCannotReplaceDuncansNativeReceipt()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            CompleteHistory(harness, 549);
            var randolph = Accept(harness, 208, 441);
            Assert.AreEqual(212U, randolph.Npc.NpcPackageId);
            var npcs = new NpcManager(harness, harness.Manager);

            CompleteObjective(harness, npcs, randolph, 441);

            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[441].Objectives[1].State);
            Assert.IsFalse(harness.Client.Player.Missions[441].Completeable);
            harness.SpawnWorld(203);
            var duncan = harness.Npc(203);
            Assert.IsNotNull(duncan);
            Assert.AreEqual(125U, duncan.DbId);
            Assert.AreEqual(218U, duncan.Npc.NpcPackageId);
            CompleteObjective(harness, npcs, duncan, 441);
            Assert.IsTrue(harness.Client.Player.Missions[441].Completeable);
            npcs.RequestNpcConverse(harness.Client, new RequestNPCConversePacket { EntityId = duncan.EntityId });
            npcs.CompleteNPCMission(harness.Client,
                new CompleteNPCMissionPacket { EntityId = duncan.EntityId, MissionId = 441, SelectionIdx = 1 });
            Assert.AreEqual(MissionState.Completed, harness.Client.Player.Missions[441].State);
            Assert.AreEqual(1U, HeldQuantity(harness, 44917));
            Assert.AreEqual(0U, HeldQuantity(harness, 44918));
        }

        [TestMethod]
        public void FullRewardBagPreservesDuncansTurnInUntilTheSelectedRewardFits()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            CompleteHistory(harness, 549);
            Accept(harness, 208, 441);
            harness.SpawnWorld(203);
            var duncan = harness.Npc(203);
            var npcs = new NpcManager(harness, harness.Manager);
            CompleteObjective(harness, npcs, duncan, 441);
            var fillerTemplate = ItemManager.Instance.GetItemTemplateById(44917);
            var selectedTemplate = ItemManager.Instance.GetItemTemplateById(44918);
            Assert.AreEqual(selectedTemplate.InventoryCategory, fillerTemplate.InventoryCategory);
            var stackSize = EntityClassManager.Instance.GetClassInfo(fillerTemplate.Class).ItemClassInfo.StackSize;
            Grant(harness, 44917, checked(50U * stackSize));
            var before = harness.Context.ReadRewardTotals();
            npcs.RequestNpcConverse(harness.Client, new RequestNPCConversePacket { EntityId = duncan.EntityId });
            harness.Drain();

            npcs.CompleteNPCMission(harness.Client,
                new CompleteNPCMissionPacket { EntityId = duncan.EntityId, MissionId = 441, SelectionIdx = 0 });

            Assert.AreEqual(before, harness.Context.ReadRewardTotals());
            Assert.AreEqual(MissionState.Active, harness.Client.Player.Missions[441].State);
            Assert.IsTrue(harness.Client.Player.Missions[441].Completeable);
            Assert.AreEqual(0, harness.Drain().OfType<MissionRewardedPacket>().Count());
            var filler = harness.Client.Player.Inventory.PersonalInventory.Where(id => id != 0)
                .Select(EntityManager.Instance.GetItem).First(item => item.ItemTemplate.ItemTemplateId == 44917);
            InventoryManager.Instance.PersonalInventory_DestroyItem(harness.Client,
                new PersonalInventory_DestroyItemPacket { EntityId = filler.EntityId, Quantity = stackSize });
            npcs.RequestNpcConverse(harness.Client, new RequestNPCConversePacket { EntityId = duncan.EntityId });
            npcs.CompleteNPCMission(harness.Client,
                new CompleteNPCMissionPacket { EntityId = duncan.EntityId, MissionId = 441, SelectionIdx = 0 });
            Assert.AreEqual(MissionState.Completed, harness.Client.Player.Missions[441].State);
            Assert.AreEqual(1U, HeldQuantity(harness, 44918));
            Assert.AreEqual(49U * stackSize, HeldQuantity(harness, 44917));
        }

        [TestMethod]
        [DataRow(549U, 181U, 430U, 44917U, 44918U)]
        [DataRow(441U, 208U, 549U, 44918U, 44917U)]
        [DataRow(665U, 172U, 0U, 13744U, 28692U)]
        [DataRow(430U, 181U, 0U, 26940U, 13739U)]
        public void NativeOffersAdvertiseTheSameItemChoicesThatAreGranted(
            uint missionId, uint spawnId, uint prerequisite, uint firstTemplate, uint secondTemplate)
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            if (prerequisite != 0)
                CompleteHistory(harness, prerequisite);
            harness.SpawnWorld(spawnId);
            var npc = harness.Npc(spawnId);
            Assert.IsNotNull(npc);
            harness.MoveTo(npc.Position);
            var npcs = new NpcManager(harness, harness.Manager);
            npcs.RequestNpcConverse(harness.Client, new RequestNPCConversePacket { EntityId = npc.EntityId });
            var conversation = harness.Drain().OfType<ConversePacket>().Single();
            var offers = (Dictionary<uint, MissionInfo>)conversation.ConvoDataDict[ConversationType.MissionDispense];
            var rewards = offers[missionId].MissionConstantData.RewardInfo.SelectableReward;
            CollectionAssert.AreEqual(new[] { firstTemplate, secondTemplate },
                rewards.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsTrue(rewards.All(item => item.Quantity == 1 && item.ModuleIds.Count == 0));
        }

        private static void CompleteObjective(WildernessRuntimeTestHarness harness, NpcManager npcs,
            Creature npc, uint missionId)
        {
            Assert.IsNotNull(npc);
            harness.MoveTo(npc.Position);
            npcs.RequestNpcConverse(harness.Client, new RequestNPCConversePacket { EntityId = npc.EntityId });
            npcs.CompleteNPCObjective(harness.Client, new CompleteNPCObjectivePacket
            {
                EntityId = npc.EntityId, MissionId = missionId, ObjectiveId = 1, PlayerFlagId = 1
            });
        }

        private static Creature Accept(WildernessRuntimeTestHarness harness, uint spawnId, uint missionId)
        {
            harness.SpawnWorld(spawnId);
            var npc = harness.Npc(spawnId);
            Assert.IsNotNull(npc, $"Migrated NPC spawn {spawnId} is unavailable.");
            harness.MoveTo(npc.Position);
            var npcs = new NpcManager(harness, harness.Manager);
            npcs.RequestNpcConverse(harness.Client, new RequestNPCConversePacket { EntityId = npc.EntityId });
            npcs.AssignNPCMission(harness.Client,
                new AssignNPCMissionPacket { NpcEntityId = npc.EntityId, MissionId = missionId });
            Assert.IsTrue(harness.Client.Player.Missions.TryGetValue(missionId, out var mission),
                $"Native NPC assignment did not accept mission {missionId}.");
            Assert.AreEqual(MissionState.Active, mission.State);
            harness.Drain();
            return npc;
        }

        private static LootDispenser Kill(WildernessRuntimeTestHarness harness, uint creatureId, uint spawnId)
        {
            var pool = harness.Map.SpawnPools.Single(pool => pool.DbId == spawnId);
            var living = harness.Map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList)
                .Where(creature => creature.SpawnPool?.DbId == spawnId && creature.State != CharacterState.Dead)
                .Distinct().ToArray();
            if (living.Length > 0 && living.All(creature => creature.DbId != creatureId))
            {
                // PR105's mixed squads respawn only after their remaining members die.
                foreach (var companion in living)
                {
                    companion.Attributes[Attributes.Health].Current = 0;
                    harness.Creatures.HandleCreatureKill(harness.Map, companion, harness.Client.Player);
                }
            }
            harness.SpawnWorldAfter(pool.RespawnTime, spawnId);
            var creature = harness.Map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList)
                .FirstOrDefault(creature => creature.DbId == creatureId && creature.SpawnPool?.DbId == spawnId &&
                    creature.State != CharacterState.Dead);
            Assert.IsNotNull(creature, $"Migrated creature {creatureId} from spawn {spawnId} is unavailable.");
            harness.MoveTo(creature.Position);
            creature.Attributes[Attributes.Health].Current = 0;
            harness.Creatures.HandleCreatureKill(harness.Map, creature, harness.Client.Player);
            Assert.IsTrue(harness.Map.LootDispensers.TryGetValue(creature.CorpseLootEntityId, out var loot));
            return loot;
        }

        private static void Take(WildernessRuntimeTestHarness harness, LootDispenser loot) =>
            LootDispenserManager.Instance.RequestLootAllFromCorpse(harness.Client,
                new RequestLootAllFromCorpsePacket { EntityId = loot.EntityId });

        private static void DamageObject(WildernessRuntimeTestHarness harness, DynamicObject obj, int damage)
        {
            harness.MoveTo(obj.Position + new Vector3(0, 0, 1));
            var action = new ActionData(harness.Client.Player, ActionId.WeaponAttack, 133, 0)
            {
                TargetId = obj.EntityId
            };
            MissileManager.Instance.MissileLaunch(harness.Map, action, damage);
            MissileManager.Instance.DoWork(harness.Map, 1000);
        }

        private static void Grant(WildernessRuntimeTestHarness harness, uint templateId, uint quantity)
        {
            using var grant = new InventoryManager.InventoryGrant();
            using (var unit = harness.CreateChar())
                unit.ExecuteTransaction(() => grant.PlanAndSave(harness.Client,
                    new[] { new InventoryManager.InventoryItemGrant(templateId, quantity) }, unit));
            grant.Publish(harness.Client);
        }

        private static uint HeldQuantity(WildernessRuntimeTestHarness harness, uint templateId)
        {
            using var unit = harness.CreateChar();
            return unit.CharacterInventories.GetItems(harness.Client.AccountEntry.Id)
                .Where(row => row.CharacterId == harness.Client.Player.Id)
                .Select(row => unit.Items.GetItem(row.ItemId))
                .Where(item => item.ItemTemplateId == templateId)
                .Aggregate(0U, (total, item) => total + item.StackSize);
        }

        private static void CompleteHistory(WildernessRuntimeTestHarness harness, uint missionId)
        {
            using var unit = harness.CreateChar();
            unit.ExecuteTransaction(() => unit.CharacterMissions.Runtime.Archive(
                new CharacterMissionEntry(harness.Client.Player.Id, missionId, (uint)MissionState.Completed)
                {
                    AssignmentId = Guid.NewGuid().ToString("N"),
                    Generation = 1,
                    ContentRevision = WildernessMissionDataV1.Revision
                }, harness.UtcNow));
            harness.Manager.HydrateAndClearInvalid(harness.Client.Player, unit);
            Assert.IsTrue(harness.Client.Player.MissionHistory.TryGetValue(missionId, out var outcome),
                $"Archived prerequisite {missionId} must be restored by the production history loader.");
            Assert.AreEqual(MissionState.Completed, outcome);
            Assert.IsTrue(harness.Client.Player.MissionSuccessHistory.Contains(missionId));
            Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(missionId),
                "A completed historical prerequisite must not require an invented current assignment.");
        }
    }
}
