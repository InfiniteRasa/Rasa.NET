using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
using Rasa.Memory;
using Rasa.Packets;
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
    public class WildernessAliaBranchesTests
    {
        [TestMethod]
        public void ApirkaOffersConscientiousObjectorOnlyAfterFormingAlliances()
        {
            using var harness = CreateHarness();
            harness.SpawnWorld(219, 630010);
            var apirka = harness.Npc(219);
            Assert.IsNotNull(apirka, "Apirka must be an actual migrated public NPC.");
            Assert.AreEqual(43U, apirka.DbId);
            Assert.AreEqual(112U, apirka.Npc.NpcPackageId);
            harness.Client.Player.Level = 5;
            harness.MoveTo(apirka.Position);
            var npcs = new NpcManager(harness, harness.Manager);

            var before = Open(harness, npcs, apirka.EntityId);
            Assert.IsFalse(Offers(before, 1390),
                "Conscientious Objector must not bypass Forming Alliances.");

            SeedCompletedHistory(harness, 1407, 1069, 479);

            var after = Open(harness, npcs, apirka.EntityId);
            Assert.IsTrue(Offers(after, 1390),
                "Apirka must offer native mission 1390 after the actual opening predecessor history.");
            Assert.IsFalse(Offers(after, 1392), "The arrest follow-up requires the arrest outcome.");
            Assert.IsFalse(Offers(after, 1393), "The release follow-up requires the release outcome.");

            npcs.AssignNPCMission(harness.Client, new AssignNPCMissionPacket
            {
                NpcEntityId = apirka.EntityId,
                MissionId = 1390
            });

            Assert.AreEqual(1, harness.Drain().OfType<MissionGainedPacket>()
                .Count(packet => packet.MissionId == 1390));
            Assert.IsTrue(harness.Client.Player.Missions.TryGetValue(1390, out var mission));
            Assert.AreEqual(MissionState.Active, mission.State);
            Assert.AreEqual(MissionObjectiveState.Incomplete, mission.Objectives[1].State);
        }

        [TestMethod]
        [DataRow(1)]
        [DataRow(2)]
        public void NativeChoiceRequiresTheActualEscortAndUnlocksOnlyItsOutcome(int choice)
        {
            using var harness = CreateHarness();
            var npcs = new NpcManager(harness, harness.Manager);
            var milpas = StartMilpasEscort(harness, npcs, choice);
            var report = choice == 1 ? 11U : 10U;
            var followup = choice == 1 ? 1393U : 1392U;
            var excluded = choice == 1 ? 1392U : 1393U;
            var apirka = harness.Npc(219);
            harness.MoveTo(apirka.Position);
            Open(harness, npcs, apirka.EntityId);
            npcs.CompleteNPCObjective(harness.Client, new CompleteNPCObjectivePacket
            {
                EntityId = apirka.EntityId, MissionId = 1390, ObjectiveId = report, PlayerFlagId = 1
            });
            Assert.AreEqual(MissionObjectiveState.Inactive, harness.Client.Player.Missions[1390].Objectives[report].State,
                "The Apirka callback must not replace either escort.");
            FollowMilpas(harness, milpas, choice);
            CompleteObjective(harness, npcs, 219, 1390, report);
            Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[1390].Objectives[12].State);
            using (var unit = harness.CreateChar())
                Assert.AreEqual((uint)choice, unit.CharacterFlags.Get(harness.Client.Player.Id)[530002]);
            var before = Open(harness, npcs, apirka.EntityId);
            Assert.IsFalse(Offers(before, followup), "The first mission must be rewarded before its follow-up.");
            Assert.IsFalse(Offers(before, excluded));
            var experience = harness.Client.Player.Experience;
            var credits = harness.Client.Player.Credits[CurencyType.Credits];
            TurnIn(harness, npcs, 219, 1390, choice - 1);
            ReplayTurnIn(harness, npcs, 219, 1390, choice - 1);
            Assert.AreEqual(experience + 2000U, harness.Client.Player.Experience);
            Assert.AreEqual(credits + 400, harness.Client.Player.Credits[CurencyType.Credits]);
            Assert.AreEqual(1U, HeldQuantity(harness, choice == 1 ? 44918U : 44917U));
            var after = Open(harness, npcs, apirka.EntityId);
            Assert.IsTrue(Offers(after, followup));
            Assert.IsFalse(Offers(after, excluded));
            CollectionAssert.AreEqual(new uint[] { 20250, 35486 },
                Preview(after, followup).SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Accept(harness, npcs, 219, followup);
            Assert.AreEqual(116U, harness.Npc(100).Npc.NpcPackageId);
            CompleteObjective(harness, npcs, 100, followup, 1);
            experience = harness.Client.Player.Experience;
            credits = harness.Client.Player.Credits[CurencyType.Credits];
            TurnIn(harness, npcs, 100, followup, choice - 1);
            ReplayTurnIn(harness, npcs, 100, followup, choice - 1);
            Assert.AreEqual(experience + 8000U, harness.Client.Player.Experience);
            Assert.AreEqual(credits + 800, harness.Client.Player.Credits[CurencyType.Credits]);
            Assert.AreEqual(1U, HeldQuantity(harness, choice == 1 ? 20250U : 35486U));
            harness.MoveTo(apirka.Position);
            Open(harness, npcs, apirka.EntityId);
            npcs.AssignNPCMission(harness.Client,
                new AssignNPCMissionPacket { NpcEntityId = apirka.EntityId, MissionId = excluded });
            Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(excluded));
            VerifyArmorReward(harness, choice == 1 ? 20250U : 35486U,
                choice == 1 ? CharacterClass.Specialist : CharacterClass.Soldier, choice == 1 ? 30U : 21U);
        }

        [TestMethod]
        public void SuccessfulEscortReleasesMilpasBeforeTheFirstPlayersReward()
        {
            using var harness = CreateHarness();
            var npcs = new NpcManager(harness, harness.Manager);
            var milpas = StartMilpasEscort(harness, npcs, 1);
            var firstLease = harness.Manager.PublicActors.Handle(harness.Map, 630010);
            Assert.IsNotNull(firstLease);
            var next = harness.Context.CreateAdditionalClient(2, manager: harness.Manager);
            try
            {
                SeedHistoryFor(harness, next, 1407, 1069, 479);
                var apirka = harness.Npc(219);
                next.SetWorldPosition(apirka.Position, next.Player.Rotation);
                CellManager.Instance.UpdateVisibility(next);
                npcs.RequestNpcConverse(next, new RequestNPCConversePacket { EntityId = apirka.EntityId });
                npcs.AssignNPCMission(next, new AssignNPCMissionPacket { NpcEntityId = apirka.EntityId, MissionId = 1390 });
                Assert.IsFalse(next.Player.Missions.ContainsKey(1390), "Milpas must have only one public owner.");
                FollowMilpas(harness, milpas, 1);
                WaitForMilpasRelease(harness);
                npcs.RequestNpcConverse(next, new RequestNPCConversePacket { EntityId = apirka.EntityId });
                npcs.AssignNPCMission(next, new AssignNPCMissionPacket { NpcEntityId = apirka.EntityId, MissionId = 1390 });
                Assert.IsTrue(next.Player.Missions.ContainsKey(1390));
                var secondLease = harness.Manager.PublicActors.Handle(harness.Map, 630010);
                Assert.IsNotNull(secondLease);
                Assert.AreNotEqual(firstLease.RunId, secondLease.RunId);
                Assert.IsFalse(harness.Manager.PublicActors.TryResolve(harness.Map, firstLease, out _));
                Assert.AreEqual(1, harness.Map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList)
                    .Count(creature => creature.DbId == 630010 && creature.State != CharacterState.Dead));
                CompleteObjective(harness, npcs, 219, 1390, 11);
                TurnIn(harness, npcs, 219, 1390, 0);
                Assert.AreEqual(secondLease, harness.Manager.PublicActors.Handle(harness.Map, 630010));
            }
            finally
            {
                harness.Manager.Scenes.Detach(next, harness.Map);
                CellManager.Instance.RemoveFromWorld(next);
                harness.Map.ClientList.Remove(next);
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void ADeadEscortOrDepartingOwnerFailsAndReleasesThePublicActor(bool ownerDeparts)
        {
            using var harness = CreateHarness();
            var npcs = new NpcManager(harness, harness.Manager);
            var milpas = StartMilpasEscort(harness, npcs, 2);
            var assignment = harness.Client.Player.Missions[1390].AssignmentId;
            harness.MoveTo(milpas.Position + Vector3.UnitZ);
            harness.Tick(1000);
            if (ownerDeparts)
                harness.Manager.Scenes.Detach(harness.Client, harness.Map);
            else
            {
                milpas.Attributes[Attributes.Health].Current = 0;
                harness.Creatures.HandleCreatureKill(harness.Map, milpas, harness.Client.Player);
            }
            harness.Manager.Scenes.Tick(harness.Map);
            Assert.AreEqual(MissionState.Failed, harness.Client.Player.Missions[1390].State);
            Assert.IsFalse(harness.Client.Player.Missions[1390].Completeable);
            using (var unit = harness.CreateChar())
                Assert.IsFalse(unit.CharacterFlags.Get(harness.Client.Player.Id).ContainsKey(530002));
            WaitForMilpasRelease(harness);
            harness.Manager.PublishInitialState(harness.Client);
            Accept(harness, npcs, 219, 1390);
            Assert.AreNotEqual(assignment, harness.Client.Player.Missions[1390].AssignmentId);
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[1390].Objectives[1].State);
        }

        [TestMethod]
        public void FulgorsRealCorpseSuppliesTheShipmentOnlyAfterLootFits()
        {
            using var harness = CreateHarness();
            harness.SpawnWorld(210, 180, 580010, 580019);
            SeedCompletedHistory(harness, 1407, 1069, 479);
            var npcs = new NpcManager(harness, harness.Manager);
            var offer = Accept(harness, npcs, 210, 427);
            CollectionAssert.AreEqual(new uint[] { 20697 },
                Preview(offer, 427).FixedReward.FixedItems.Select(item => item.ItemTemplateId).ToArray());
            CompleteObjective(harness, npcs, 180, 427, 1);
            var thrax = KillSource(harness, 580019, 3, expectSingle: false);
            Assert.IsFalse(harness.Map.LootDispensers[thrax.CorpseLootEntityId].LootItems
                .Any(item => item.ItemTemplateId == 3786));
            FillCategory(harness, 11519, 50);
            var fulgor = KillSource(harness, 580010, 76);
            Assert.AreEqual(10857U, (uint)fulgor.EntityClass);
            Assert.AreEqual(10100U, fulgor.NameId);
            Assert.AreEqual(1, harness.Map.LootDispensers[fulgor.CorpseLootEntityId].LootItems
                .Count(item => item.ItemTemplateId == 3786));
            Assert.AreEqual(0U, harness.Client.Player.Missions[427].Objectives[6].ItemCounters[12714],
                "Killing Fulgor must not credit a shipment still on his corpse.");
            LootSource(harness, fulgor);
            Assert.AreEqual(0U, HeldQuantity(harness, 3786));
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[427].Objectives[6].State);
            FreeOneSlot(harness, 11519);
            LootSource(harness, fulgor);
            Assert.AreEqual(1U, HeldQuantity(harness, 3786));
            Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[427].Objectives[6].State);
            LootSource(harness, fulgor, expectMenu: false);
            Assert.AreEqual(1U, HeldQuantity(harness, 3786));
            CompleteObjective(harness, npcs, 210, 427, 7);
            var experience = harness.Client.Player.Experience;
            var credits = harness.Client.Player.Credits[CurencyType.Credits];
            TurnIn(harness, npcs, 210, 427);
            ReplayTurnIn(harness, npcs, 210, 427);
            Assert.AreEqual(0U, HeldQuantity(harness, 3786));
            Assert.AreEqual(1U, HeldQuantity(harness, 20697));
            Assert.AreEqual(experience + 4000U, harness.Client.Player.Experience);
            Assert.AreEqual(credits + 400, harness.Client.Player.Credits[CurencyType.Credits]);
            VerifyArmorReward(harness, 20697, CharacterClass.Specialist, 30);
        }

        [TestMethod]
        public void StaleFulgorLootCannotCreditANewAttemptAndRetainedShipmentDoesNotDeadlockRetry()
        {
            using var harness = CreateHarness();
            harness.SpawnWorld(210, 180, 580010);
            SeedCompletedHistory(harness, 1407, 1069, 479);
            var npcs = new NpcManager(harness, harness.Manager);
            Accept(harness, npcs, 210, 427);
            CompleteObjective(harness, npcs, 180, 427, 1);
            var oldAssignment = harness.Client.Player.Missions[427].AssignmentId;
            var oldCorpse = KillSource(harness, 580010, 76);
            npcs.AbandonMission(harness.Client, new AbandonMissionPacket { MissionId = 427 });
            Accept(harness, npcs, 210, 427);
            Assert.AreNotEqual(oldAssignment, harness.Client.Player.Missions[427].AssignmentId);
            CompleteObjective(harness, npcs, 180, 427, 1);
            LootSource(harness, oldCorpse);
            Assert.AreEqual(0U, HeldQuantity(harness, 3786));
            Assert.AreEqual(0U, harness.Client.Player.Missions[427].Objectives[6].ItemCounters[12714]);
            var freshCorpse = KillSource(harness, 580010, 76);
            Assert.AreNotEqual(oldCorpse.EntityId, freshCorpse.EntityId);
            LootSource(harness, freshCorpse);
            Assert.AreEqual(1U, HeldQuantity(harness, 3786));
            npcs.AbandonMission(harness.Client, new AbandonMissionPacket { MissionId = 427 });
            Assert.AreEqual(1U, HeldQuantity(harness, 3786));
            Accept(harness, npcs, 210, 427);
            CompleteObjective(harness, npcs, 180, 427, 1);
            harness.Manager.PublishInitialState(harness.Client);
            harness.Manager.PublishInitialState(harness.Client);
            Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[427].Objectives[6].State);
            Assert.AreEqual(1U, harness.Client.Player.Missions[427].Objectives[6].ItemCounters[12714]);
            CompleteObjective(harness, npcs, 210, 427, 7);
            TurnIn(harness, npcs, 210, 427);
            Assert.AreEqual(0U, HeldQuantity(harness, 3786));
        }

        private static Creature StartMilpasEscort(WildernessRuntimeTestHarness harness, NpcManager npcs, int choice)
        {
            harness.SpawnWorld(100, 192, 219, 630010);
            SeedCompletedHistory(harness, 1407, 1069, 479);
            var offer = Accept(harness, npcs, 219, 1390);
            CollectionAssert.AreEqual(new uint[] { 44918, 44917 },
                Preview(offer, 1390).SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsFalse(harness.Drain().OfType<MissionGainedPacket>().Single(packet => packet.MissionId == 1390)
                .MissionInfo.ObjectivesList.Any(objective => objective.ObjectiveId == 12));
            var quillas = harness.Npc(192);
            Assert.AreEqual(114U, quillas.DbId);
            Assert.AreEqual(1646U, quillas.Npc.NpcPackageId);
            Assert.IsTrue(Vector3.Distance(quillas.Position, new Vector3(783, 303.317277f, 130)) < 0.5f);
            harness.MoveTo(quillas.Position);
            Open(harness, npcs, quillas.EntityId);
            Choose(3);
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[1390].Objectives[1].State);
            Open(harness, npcs, quillas.EntityId);
            Choose(choice);
            Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[1390].Objectives[1].State);
            Choose(choice == 1 ? 2 : 1);
            Assert.AreEqual(MissionObjectiveState.Inactive,
                harness.Client.Player.Missions[1390].Objectives[choice == 1 ? 3U : 2U].State);
            CompleteObjective(harness, npcs, 192, 1390, choice == 1 ? 2U : 3U);
            var milpas = LiveCreature(harness, 630010, 630010);
            Assert.AreEqual(9519U, milpas.NameId);
            Assert.AreEqual(26833U, (uint)milpas.EntityClass);
            Assert.AreEqual(MissionObjectiveState.Incomplete,
                harness.Client.Player.Missions[1390].Objectives[choice == 1 ? 8U : 4U].State);
            return milpas;

            void Choose(int selected) => npcs.PerformNPCChoice(harness.Client, new PerformNPCChoicePacket
            {
                EntityId = quillas.EntityId, MissionId = 1390, ObjectiveId = 1, PlayerFlagId = 1, ChoiceIdx = selected
            });
        }

        private static void FollowMilpas(WildernessRuntimeTestHarness harness, Creature milpas, int choice)
        {
            var objective = choice == 1 ? 8U : 4U;
            var expected = choice == 1 ? new Vector3(899.45f, 276, 39.9f) : new Vector3(825, 301.5f, 503.8f);
            var start = milpas.Position;
            for (var tick = 0; tick < 1200 &&
                 harness.Client.Player.Missions[1390].Objectives[objective].State != MissionObjectiveState.Completed; tick++)
            {
                harness.MoveTo(milpas.Position + Vector3.UnitZ);
                harness.Tick(1000);
                if (choice == 1)
                    Assert.IsTrue(Vector2.Distance(new Vector2(milpas.Position.X, milpas.Position.Z),
                        new Vector2(888.721619f, 31.625519f)) > 8,
                        "The release escort must stay outside the actual Divide transfer trigger.");
                Assert.AreEqual(1220U, harness.Client.Player.MapContextId);
                Assert.IsNull(harness.Client.PendingTransfer);
            }
            Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[1390].Objectives[objective].State,
                $"Milpas stalled at {milpas.Position} while heading to {expected}.");
            Assert.IsTrue(Vector3.Distance(start, milpas.Position) > 50);
            Assert.IsTrue(Vector3.Distance(expected, milpas.Position) < 1.5f,
                $"Milpas ended at {milpas.Position}, not the grounded endpoint {expected}.");
            Assert.IsFalse(harness.Client.Player.Missions[1390].Completeable);
        }

        private static void WaitForMilpasRelease(WildernessRuntimeTestHarness harness)
        {
            for (var tick = 0; tick < 1200 && harness.Manager.PublicActors.Handle(harness.Map, 630010) != null; tick++)
            {
                harness.SpawnWorldAfter(1000, 630010);
                harness.Tick(1000);
            }
            Assert.IsNull(harness.Manager.PublicActors.Handle(harness.Map, 630010),
                "The completed or failed public encounter must release Milpas.");
        }

        private static Creature LiveCreature(
            WildernessRuntimeTestHarness harness, uint spawnId, uint creatureId, bool expectSingle = true)
        {
            var creatures = harness.Map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList)
                .Where(creature => creature.SpawnPool?.DbId == spawnId && creature.DbId == creatureId &&
                    creature.State != CharacterState.Dead);
            return expectSingle ? creatures.Single() : creatures.OrderBy(creature => creature.EntityId).First();
        }

        private static Creature KillSource(
            WildernessRuntimeTestHarness harness, uint spawnId, uint creatureId, bool expectSingle = true)
        {
            var pool = harness.Map.SpawnPools.Single(candidate => candidate.DbId == spawnId);
            harness.SpawnWorldAfter(pool.RespawnTime, spawnId);
            var creature = LiveCreature(harness, spawnId, creatureId, expectSingle);
            harness.MoveTo(creature.Position + new Vector3(3, 0, 0));
            creature.Attributes[Attributes.Health].Current = 0;
            harness.Creatures.HandleCreatureKill(harness.Map, creature, harness.Client.Player);
            Assert.AreNotEqual(0UL, creature.CorpseLootEntityId);
            return creature;
        }

        private static void LootSource(WildernessRuntimeTestHarness harness, Creature corpse, bool expectMenu = true)
        {
            harness.MoveTo(corpse.Position + new Vector3(3, 0, 0));
            harness.Drain();
            LootDispenserManager.Instance.RequestCorpseLooting(harness.Client,
                new RequestCorpseLootingPacket { EntityId = corpse.CorpseLootEntityId });
            if (expectMenu)
                Assert.IsTrue(harness.Drain().Any(packet => packet.Opcode == GameOpcode.LootCorpse),
                    "The actual native loot menu must open at a normal three-meter interaction distance.");
            LootDispenserManager.Instance.RequestLootAllFromCorpse(harness.Client,
                new RequestLootAllFromCorpsePacket { EntityId = corpse.CorpseLootEntityId });
        }

        [TestMethod]
        [DataRow(0, 4019U)]
        [DataRow(1, 26996U)]
        public void FathersDogtagsReachTheActualSavioursAndCannotPayTwice(int selection, uint reward)
        {
            using var harness = CreateHarness();
            harness.SpawnWorld(198, 194, 510004);
            SeedCompletedHistory(harness, 1407, 1069, 479, 428);
            var npcs = new NpcManager(harness, harness.Manager);
            var offer = Accept(harness, npcs, 198, 421);
            CollectionAssert.AreEqual(new uint[] { 4019, 26996 },
                Preview(offer, 421).SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.AreEqual(1U, HeldQuantity(harness, 613));

            var wrong = harness.Npc(510004);
            harness.MoveTo(wrong.Position);
            Open(harness, npcs, wrong.EntityId);
            npcs.CompleteNPCObjective(harness.Client, new CompleteNPCObjectivePacket
            {
                EntityId = wrong.EntityId, MissionId = 421, ObjectiveId = 3, PlayerFlagId = 1
            });
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[421].Objectives[3].State);
            Assert.AreEqual(1U, HeldQuantity(harness, 613));

            CompleteObjective(harness, npcs, 194, 421, 3);
            var before = harness.Client.Player.Credits[CurencyType.Credits];
            var experience = harness.Client.Player.Experience;
            TurnIn(harness, npcs, 194, 421, selection);
            Assert.AreEqual(0U, HeldQuantity(harness, 613));
            Assert.AreEqual(1U, HeldQuantity(harness, reward));
            Assert.AreEqual(before + 900, harness.Client.Player.Credits[CurencyType.Credits]);
            Assert.AreEqual(experience + 6000U, harness.Client.Player.Experience);
            ReplayTurnIn(harness, npcs, 194, 421, selection);
            Assert.AreEqual(1U, HeldQuantity(harness, reward));
            Assert.AreEqual(before + 900, harness.Client.Player.Credits[CurencyType.Credits]);
            if (reward == 26996)
                VerifyArmorReward(harness, reward, CharacterClass.Recruit, (uint)SkillId.MotorAssistArmor);
        }

        [TestMethod]
        public void RichardsReceivesBothPhysicalCratesAndRewardReplayRetainsOneGrant()
        {
            using var harness = CreateHarness();
            harness.SpawnWorld(100, 204);
            SeedCompletedHistory(harness, 1407, 1069);
            var npcs = new NpcManager(harness, harness.Manager);
            var offer = Accept(harness, npcs, 100, 422);
            CollectionAssert.AreEqual(new uint[] { 44918, 44918 },
                Preview(offer, 422).FixedReward.FixedItems.Select(item => item.ItemTemplateId).ToArray());
            Assert.AreEqual(1U, HeldQuantity(harness, 97));
            Assert.AreEqual(1U, HeldQuantity(harness, 697));

            var rogers = harness.Npc(100);
            Open(harness, npcs, rogers.EntityId);
            npcs.CompleteNPCObjective(harness.Client, new CompleteNPCObjectivePacket
            {
                EntityId = rogers.EntityId, MissionId = 422, ObjectiveId = 1, PlayerFlagId = 1
            });
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[422].Objectives[1].State);
            CompleteObjective(harness, npcs, 204, 422, 1);
            var experience = harness.Client.Player.Experience;
            var credits = harness.Client.Player.Credits[CurencyType.Credits];
            TurnIn(harness, npcs, 204, 422);
            Assert.AreEqual(0U, HeldQuantity(harness, 97));
            Assert.AreEqual(0U, HeldQuantity(harness, 697));
            Assert.AreEqual(2U, HeldQuantity(harness, 44918));
            Assert.AreEqual(experience + 5000U, harness.Client.Player.Experience);
            Assert.AreEqual(credits + 215, harness.Client.Player.Credits[CurencyType.Credits]);
            ReplayTurnIn(harness, npcs, 204, 422);
            Assert.AreEqual(2U, HeldQuantity(harness, 44918));
        }

        [TestMethod]
        public void TwoCrateAssignmentRejectsOneFreeSlotWithoutIssuingHalfTheDelivery()
        {
            using var harness = CreateHarness();
            harness.SpawnWorld(100);
            SeedCompletedHistory(harness, 1407, 1069);
            FillCategory(harness, 11519, 49);
            var rogers = harness.Npc(100);
            var npcs = new NpcManager(harness, harness.Manager);
            harness.MoveTo(rogers.Position);
            var conversation = Open(harness, npcs, rogers.EntityId);
            Assert.IsTrue(Offers(conversation, 422));
            var request = new AssignNPCMissionPacket { NpcEntityId = rogers.EntityId, MissionId = 422 };
            npcs.AssignNPCMission(harness.Client, request);
            Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(422));
            Assert.AreEqual(0U, HeldQuantity(harness, 97));
            Assert.AreEqual(0U, HeldQuantity(harness, 697));
            Assert.IsFalse(harness.Drain().OfType<MissionGainedPacket>().Any(packet => packet.MissionId == 422));
            using (var unit = harness.CreateChar())
                Assert.IsNull(unit.CharacterMissions.GetByCharacterAndMission(harness.Client.Player.Id, 422));

            FreeOneSlot(harness, 11519);
            npcs.AssignNPCMission(harness.Client, request);
            Assert.IsTrue(harness.Client.Player.Missions.ContainsKey(422));
            Assert.AreEqual(1U, HeldQuantity(harness, 97));
            Assert.AreEqual(1U, HeldQuantity(harness, 697));
            npcs.AssignNPCMission(harness.Client, request);
            Assert.AreEqual(1U, HeldQuantity(harness, 97));
            Assert.AreEqual(1U, HeldQuantity(harness, 697));
        }

        [TestMethod]
        public void FullRewardBagsPreserveDogtagsUntilTheRewardCanCommit()
        {
            using var harness = CreateHarness();
            harness.SpawnWorld(198, 194);
            SeedCompletedHistory(harness, 1407, 1069, 479, 428);
            var npcs = new NpcManager(harness, harness.Manager);
            Accept(harness, npcs, 198, 421);
            CompleteObjective(harness, npcs, 194, 421, 3);
            FillCategory(harness, 3869, 50);
            var receiver = harness.Npc(194);
            Open(harness, npcs, receiver.EntityId);
            var credits = harness.Client.Player.Credits[CurencyType.Credits];
            var request = new CompleteNPCMissionPacket { EntityId = receiver.EntityId, MissionId = 421, SelectionIdx = 0 };
            npcs.CompleteNPCMission(harness.Client, request);
            Assert.AreEqual(1U, HeldQuantity(harness, 613));
            Assert.AreEqual(0U, HeldQuantity(harness, 4019));
            Assert.AreEqual(credits, harness.Client.Player.Credits[CurencyType.Credits]);

            FreeOneSlot(harness, 3869);
            npcs.CompleteNPCMission(harness.Client, request);
            Assert.AreEqual(MissionState.Completed, harness.Client.Player.Missions[421].State);
            Assert.AreEqual(0U, HeldQuantity(harness, 613));
            Assert.AreEqual(1U, HeldQuantity(harness, 4019));
        }

        [TestMethod]
        public void AbandoningAnIssuedDeliveryRemovesOnlyItsOwnCrates()
        {
            using var harness = CreateHarness();
            harness.SpawnWorld(100);
            SeedCompletedHistory(harness, 1407, 1069);
            GrantItem(harness, 97, 1);
            var npcs = new NpcManager(harness, harness.Manager);
            Accept(harness, npcs, 100, 422);
            Assert.AreEqual(2U, HeldQuantity(harness, 97));
            npcs.AbandonMission(harness.Client, new AbandonMissionPacket { MissionId = 422 });
            Assert.AreEqual(1U, HeldQuantity(harness, 97));
            Assert.AreEqual(0U, HeldQuantity(harness, 697));
            Accept(harness, npcs, 100, 422);
            Assert.AreEqual(2U, HeldQuantity(harness, 97));
            Assert.AreEqual(1U, HeldQuantity(harness, 697));
        }

        [TestMethod]
        public void LangermanDispatchesToTheRealStandleyAtTwinPillars()
        {
            using var harness = CreateHarness();
            harness.SpawnWorld(211, 212);
            var npcs = new NpcManager(harness, harness.Manager);
            var standley = harness.Npc(212);
            Assert.IsNotNull(standley);
            Assert.AreEqual(134U, standley.DbId);
            Assert.AreEqual(2049U, standley.Npc.NpcPackageId);
            Accept(harness, npcs, 211, 1741);
            CompleteObjective(harness, npcs, 212, 1741, 1);
            TurnIn(harness, npcs, 212, 1741);
        }

        [TestMethod]
        [DataRow(false, 0, 3869U)]
        [DataRow(false, 1, 97328U)]
        [DataRow(true, 0, 3869U)]
        [DataRow(true, 1, 97328U)]
        public void IntactAndBreachedSupplyCratesDeliverThePhysicalItemOnce(
            bool damaged, int selection, uint reward)
        {
            using var harness = CreateHarness();
            harness.SpawnWorld(510004);
            SeedCompletedHistory(harness, 1407, 1069, 479);
            var npcs = new NpcManager(harness, harness.Manager);
            var offer = Accept(harness, npcs, 510004, 428);
            CollectionAssert.AreEqual(new uint[] { 3869, 97328 },
                Preview(offer, 428).SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            var crate = SupplyCrate(harness);
            harness.MoveTo(crate.Position);
            if (damaged)
            {
                HitSupply(harness, crate, 99);
                Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[428].Objectives[1].State);
                Assert.AreEqual(0U, HeldQuantity(harness, 686));
                HitSupply(harness, crate, 1);
                Assert.AreEqual(harness.UtcNow.AddSeconds(300), SupplyDeadline(harness));
                harness.UtcNow = harness.UtcNow.AddSeconds(299);
                harness.Manager.Scenes.Tick(harness.Map);
            }
            else
            {
                UseSupply(harness, crate);
                using var unit = harness.CreateChar();
                Assert.IsNull(unit.CharacterMissionDeadlines.Get(harness.Client.Player.Id, 428));
            }
            Assert.AreEqual(1U, HeldQuantity(harness, 686));
            Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[428].Objectives[1].State);
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[428].Objectives[2].State);
            CompleteObjective(harness, npcs, 510004, 428, 2);
            harness.Manager.Scenes.Tick(harness.Map);
            if (damaged)
                Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[428].Objectives[3].State);
            var experience = harness.Client.Player.Experience;
            var credits = harness.Client.Player.Credits[CurencyType.Credits];
            TurnIn(harness, npcs, 510004, 428, selection);
            ReplayTurnIn(harness, npcs, 510004, 428, selection);
            Assert.AreEqual(0U, HeldQuantity(harness, 686));
            Assert.AreEqual(1U, HeldQuantity(harness, reward));
            Assert.AreEqual(experience + 5000U, harness.Client.Player.Experience);
            Assert.AreEqual(credits + 750, harness.Client.Player.Credits[CurencyType.Credits]);
        }

        [TestMethod]
        public void AwardedCipherReloadsAndConsumesAmmoAtTheNormalDecodingBoundary()
        {
            using var harness = CreateHarness();
            harness.SpawnWorld(510004);
            SeedCompletedHistory(harness, 1407, 1069, 479);
            var npcs = new NpcManager(harness, harness.Manager);
            Accept(harness, npcs, 510004, 428);
            var crate = SupplyCrate(harness);
            harness.MoveTo(crate.Position);
            UseSupply(harness, crate);
            CompleteObjective(harness, npcs, 510004, 428, 2);
            TurnIn(harness, npcs, 510004, 428, 1);
            ReplayTurnIn(harness, npcs, 510004, 428, 1);
            var cipher = PersonalItem(harness, 97328);
            Assert.AreEqual(25828U, (uint)cipher.ItemTemplate.Class);
            Assert.IsNotNull(cipher.ItemTemplate.WeaponInfo);
            Assert.AreEqual(1U, cipher.ItemTemplate.WeaponInfo.AmmoPerShot);
            Assert.AreEqual(0U, cipher.CurrentAmmo);
            PrepareEquipmentEligibility(harness, CharacterClass.Specialist, (uint)SkillId.SpecialistTools);
            while (harness.Client.Player.Inventory.WeaponDrawer.Count < 5)
                harness.Client.Player.Inventory.WeaponDrawer.Add(0);
            harness.Client.Player.ActiveWeapon = 0;
            InventoryManager.Instance.RequestEquipWeapon(harness.Client, new RequestEquipWeaponPacket
            {
                SrcSlot = cipher.OwnerSlotId, InventoryType = InventoryType.Personal, DestSlot = 0
            });
            Assert.AreEqual(cipher.EntityId, harness.Client.Player.Inventory.WeaponDrawer[0]);
            GrantItem(harness, 56, 20);
            ManifestationManager.Instance.WeaponReady(harness.Client, true);
            ManifestationManager.Instance.RequestWeaponReload(harness.Client, false);
            Assert.IsTrue(harness.Map.PerformRecovery.Any(action =>
                action.Actor == harness.Client.Player && action.ActionId == ActionId.WeaponReload));
            ActorActionManager.Instance.DoWork(harness.Map, 10001);
            Assert.AreEqual(10U, cipher.CurrentAmmo);
            Assert.AreEqual(10U, HeldQuantity(harness, 56));
            using (var unit = harness.CreateChar())
                Assert.AreEqual(10U, unit.Items.GetItem(cipher.Id).AmmoCount);
            ManifestationManager.Instance.WeaponReady(harness.Client, true);
            var target = CipherFootlocker(harness);
            harness.MoveTo(target.Position + Vector3.UnitZ);
            var attempts = target.Lock.CipherAttemptsLeft;
            bool registeredHere;
            lock (Server.Clients)
            {
                registeredHere = !Server.Clients.Contains(harness.Client);
                if (registeredHere)
                    Server.Clients.Add(harness.Client);
            }
            try
            {
                ToolActionManager.Instance.RequestToolAction(harness.Client, CipherRequest(target.EntityId));
                Assert.AreEqual(9U, cipher.CurrentAmmo);
                Assert.IsTrue(harness.Map.PerformRecovery.Any(action =>
                    action.Actor == harness.Client.Player && action.ActionId == ActionId.ToolCipher));
                ActorActionManager.Instance.DoWork(harness.Map, 10001);
                Assert.AreEqual(attempts - 1, target.Lock.CipherAttemptsLeft,
                    "Either decode outcome must reach the normal lock-attempt boundary.");
                Assert.AreEqual(10U, HeldQuantity(harness, 56));
                using var unit = harness.CreateChar();
                Assert.AreEqual(9U, unit.Items.GetItem(cipher.Id).AmmoCount);
            }
            finally
            {
                if (registeredHere)
                    lock (Server.Clients)
                        Server.Clients.Remove(harness.Client);
            }
        }

        private static DynamicObject CipherFootlocker(WildernessRuntimeTestHarness harness)
        {
            using var unit = harness.CreateWorld();
            var entry = unit.Footlockers.GetFootlockers().Single(row => row.Id == 1 && row.MapContextId == 1220);
            var target = new DynamicObject
            {
                EntityClassId = (EntityClasses)entry.ClassId,
                Position = entry.Position,
                Rotation = entry.Rotation,
                MapContextId = entry.MapContextId,
                DynamicObjectType = DynamicObjectType.Lockbox,
                StateId = UseObjectState.CpointStateUnclaimed,
                Comment = entry.Comment,
                // The physical target is migrated; cipher eligibility is explicit fixture state.
                Lock = new UsableLock
                {
                    LockStateId = UseObjectState.CpointStateUnclaimed,
                    UnlockedStateId = UseObjectState.TdStateOpened,
                    CipherLevel = 1
                }
            };
            harness.Map.FootLockers.Add(entry.Id, target);
            harness.Objects.DynamicObjectWorker(harness.Map, 0);
            Assert.IsTrue(EntityManager.Instance.TryGetObject(target.EntityId, out var registered));
            Assert.AreSame(target, registered);
            return target;
        }

        private static RequestToolActionPacket CipherRequest(ulong target)
        {
            byte[] payload;
            using (var stream = new MemoryStream())
            {
                using var writer = new PythonWriter(new BinaryWriter(stream));
                writer.WriteTuple(3);
                writer.WriteInt((int)ActionId.ToolCipher);
                writer.WriteUInt(14);
                writer.WriteULong(target);
                payload = stream.ToArray();
            }
            using var reader = new PythonReader(new BinaryReader(new MemoryStream(payload)));
            var packet = new RequestToolActionPacket();
            packet.Read(reader);
            return packet;
        }

        [TestMethod]
        public void BreachedSuppliesExpireAtTheDurableDeadlineAfterSceneRecovery()
        {
            using var harness = CreateHarness();
            harness.SpawnWorld(510004);
            SeedCompletedHistory(harness, 1407, 1069, 479);
            var npcs = new NpcManager(harness, harness.Manager);
            Accept(harness, npcs, 510004, 428);
            var oldCrate = SupplyCrate(harness);
            var oldAssignment = harness.Client.Player.Missions[428].AssignmentId;
            var oldRun = oldCrate.SceneRunId;
            harness.MoveTo(oldCrate.Position);
            HitSupply(harness, oldCrate, 100);
            var due = SupplyDeadline(harness);
            harness.Manager.Scenes.Detach(harness.Client, harness.Map);
            harness.UtcNow = due;
            using (var unit = harness.CreateChar())
                harness.Manager.HydrateAndClearInvalid(harness.Client.Player, unit);
            harness.Manager.PublishInitialState(harness.Client);
            harness.Manager.Scenes.Tick(harness.Map);

            Assert.AreEqual(MissionState.Failed, harness.Client.Player.Missions[428].State);
            Assert.AreEqual(MissionObjectiveState.Failed, harness.Client.Player.Missions[428].Objectives[2].State);
            Assert.AreEqual(MissionObjectiveState.Failed, harness.Client.Player.Missions[428].Objectives[3].State);
            Assert.AreEqual(0U, HeldQuantity(harness, 686));
            Assert.IsFalse(harness.Client.Player.Missions[428].Completeable);
            Assert.AreEqual(0U, HeldQuantity(harness, 3869));
            Accept(harness, npcs, 510004, 428);
            var freshCrate = SupplyCrate(harness);
            Assert.AreNotEqual(oldAssignment, harness.Client.Player.Missions[428].AssignmentId);
            Assert.AreNotEqual(oldRun, freshCrate.SceneRunId);
            Assert.AreEqual(harness.Client.Player.Id, freshCrate.SceneOwnerCharacterId);
            using (var unit = harness.CreateChar())
            {
                var scene = unit.CharacterMissions.Runtime.Scene(freshCrate.SceneRunId);
                Assert.AreEqual(harness.Client.Player.Missions[428].AssignmentId, scene.AssignmentId);
                Assert.AreEqual(scene.Generation, freshCrate.SceneGeneration);
            }
            harness.MoveTo(freshCrate.Position);
            Assert.IsFalse(harness.Manager.Scenes.UseObject(harness.Client, oldCrate, 1),
                "A reused entity ID must not make the retired object instance authoritative.");
            Assert.AreEqual(0U, HeldQuantity(harness, 686));
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[428].Objectives[1].State);
            harness.MoveTo(freshCrate.Position);
            UseSupply(harness, freshCrate);
            Assert.AreEqual(1U, HeldQuantity(harness, 686));
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void FullBagsLeaveSupplyRecoveryRetryableWithoutPartialProgress(bool damaged)
        {
            using var harness = CreateHarness();
            harness.SpawnWorld(510004);
            SeedCompletedHistory(harness, 1407, 1069, 479);
            var npcs = new NpcManager(harness, harness.Manager);
            Accept(harness, npcs, 510004, 428);
            FillCategory(harness, 11519, 50);
            var crate = SupplyCrate(harness);
            harness.MoveTo(crate.Position);
            if (damaged)
                HitSupply(harness, crate, 100);
            else
                UseSupply(harness, crate);
            Assert.AreEqual(0U, HeldQuantity(harness, 686));
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[428].Objectives[1].State);
            using (var unit = harness.CreateChar())
                Assert.IsNull(unit.CharacterMissionDeadlines.Get(harness.Client.Player.Id, 428));
            if (damaged)
                Assert.AreEqual(100U, crate.CurrentHitPoints,
                    "Rejected lethal recovery must leave the physical source intact and retryable.");

            FreeOneSlot(harness, 11519);
            if (damaged)
                HitSupply(harness, crate, 100);
            else
                UseSupply(harness, crate);
            Assert.AreEqual(1U, HeldQuantity(harness, 686));
            Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[428].Objectives[1].State);
            npcs.AbandonMission(harness.Client, new AbandonMissionPacket { MissionId = 428 });
            Assert.AreEqual(0U, HeldQuantity(harness, 686));
        }

        [TestMethod]
        public void AnotherCharacterCannotTakeTheOwnersSupplyCrate()
        {
            using var harness = CreateHarness();
            harness.SpawnWorld(510004);
            SeedCompletedHistory(harness, 1407, 1069, 479);
            var npcs = new NpcManager(harness, harness.Manager);
            Accept(harness, npcs, 510004, 428);
            var crate = SupplyCrate(harness);
            var visitor = harness.Context.CreateAdditionalClient(2, manager: harness.Manager);
            try
            {
                visitor.SetWorldPosition(crate.Position, visitor.Player.Rotation);
                CellManager.Instance.UpdateVisibility(visitor);
                harness.Objects.RequestUseObjectPacket(visitor, new RequestUseObjectPacket
                {
                    EntityId = crate.EntityId, ActionId = ActionId.UseObject, ActionArgId = 1
                });
                ActorActionManager.Instance.DoWork(harness.Map, 10001);
                harness.Manager.Scenes.Tick(harness.Map);
                Assert.AreEqual(0U, HeldQuantity(harness, 686));
                Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[428].Objectives[1].State);
                using var unit = harness.CreateChar();
                Assert.IsFalse(unit.CharacterMissionItems.GetOwned(visitor.Player.Id)
                    .Any(item => item.MissionId == 428));
            }
            finally
            {
                CellManager.Instance.RemoveFromWorld(visitor);
                harness.Map.ClientList.Remove(visitor);
            }
        }

        private static DynamicObject SupplyCrate(WildernessRuntimeTestHarness harness) =>
            harness.Map.DynamicObjects.Single(obj => obj.SceneMissionId == 428 &&
                obj.SceneActorRole == "supplies" && obj.SceneOwnerCharacterId == harness.Client.Player.Id);

        private static void UseSupply(WildernessRuntimeTestHarness harness, DynamicObject crate)
        {
            harness.Objects.RequestUseObjectPacket(harness.Client, new RequestUseObjectPacket
            {
                EntityId = crate.EntityId, ActionId = ActionId.UseObject, ActionArgId = 1
            });
            ActorActionManager.Instance.DoWork(harness.Map, 10001);
            harness.Manager.Scenes.Tick(harness.Map);
        }

        private static void HitSupply(WildernessRuntimeTestHarness harness, DynamicObject crate, int damage)
        {
            Assert.IsTrue(PracticeTargetManager.TryGetTarget(harness.Map, crate.EntityId, out var target));
            Assert.AreSame(crate, target);
            var action = new ActionData(harness.Client.Player, ActionId.WeaponAttack, 133, 0) { TargetId = crate.EntityId };
            MissileManager.Instance.MissileLaunch(harness.Map, action, damage);
            Assert.AreEqual(1, harness.Map.QueuedMissiles.Count);
            MissileManager.Instance.DoWork(harness.Map, 1000);
            harness.Manager.Scenes.Tick(harness.Map);
        }

        private static DateTime SupplyDeadline(WildernessRuntimeTestHarness harness)
        {
            using var unit = harness.CreateChar();
            var deadline = unit.CharacterMissionDeadlines.Get(harness.Client.Player.Id, 428);
            Assert.IsNotNull(deadline);
            return deadline.DueAtUtc;
        }

        internal static WildernessRuntimeTestHarness CreateHarness() =>
            WildernessRuntimeTestHarness.Create();

        internal static ConversePacket Open(
            WildernessRuntimeTestHarness harness, NpcManager npcs, ulong entityId)
        {
            harness.Drain();
            npcs.RequestNpcConverse(harness.Client, new RequestNPCConversePacket { EntityId = entityId });
            var conversations = harness.Drain().OfType<ConversePacket>().ToArray();
            Assert.AreEqual(1, conversations.Length, "The real NPC must open a native conversation.");
            return conversations[0];
        }

        internal static bool Offers(ConversePacket conversation, uint missionId) =>
            conversation.ConvoDataDict.TryGetValue(ConversationType.MissionDispense, out var value) &&
            value is Dictionary<uint, MissionInfo> offers && offers.ContainsKey(missionId);

        internal static void SeedCompletedHistory(WildernessRuntimeTestHarness harness, params uint[] missionIds) =>
            SeedHistoryFor(harness, harness.Client, missionIds);

        private static void SeedHistoryFor(WildernessRuntimeTestHarness harness, Client client, params uint[] missionIds)
        {
            using var unit = harness.CreateChar();
            unit.ExecuteTransaction(() =>
            {
                foreach (var missionId in missionIds)
                    unit.CharacterMissions.Runtime.Archive(new CharacterMissionEntry(
                        client.Player.Id, missionId, (uint)MissionState.Completed)
                    {
                        ContentRevision = WildernessMissionDataV1.Revision
                    }, harness.UtcNow);
            });
            foreach (var missionId in missionIds)
            {
                client.Player.MissionHistory[missionId] = MissionState.Completed;
                client.Player.MissionSuccessHistory.Add(missionId);
            }
        }

        internal static ConversePacket Accept(WildernessRuntimeTestHarness harness, NpcManager npcs, uint spawnId, uint missionId)
        {
            var giver = harness.Npc(spawnId);
            Assert.IsNotNull(giver, $"Missing actual giver spawn {spawnId}.");
            harness.MoveTo(giver.Position);
            var conversation = Open(harness, npcs, giver.EntityId);
            Assert.IsTrue(Offers(conversation, missionId), $"Native offer {missionId} is absent.");
            npcs.AssignNPCMission(harness.Client,
                new AssignNPCMissionPacket { NpcEntityId = giver.EntityId, MissionId = missionId });
            Assert.IsTrue(harness.Client.Player.Missions.ContainsKey(missionId));
            Assert.AreEqual(MissionState.Active, harness.Client.Player.Missions[missionId].State);
            return conversation;
        }

        internal static RewardInfo Preview(ConversePacket conversation, uint missionId) =>
            ((Dictionary<uint, MissionInfo>)conversation.ConvoDataDict[ConversationType.MissionDispense])[missionId]
                .MissionConstantData.RewardInfo;

        internal static void CompleteObjective(
            WildernessRuntimeTestHarness harness, NpcManager npcs, uint spawnId, uint missionId, uint objectiveId)
        {
            var receiver = harness.Npc(spawnId);
            Assert.IsNotNull(receiver, $"Missing actual objective receiver spawn {spawnId}.");
            harness.MoveTo(receiver.Position);
            Open(harness, npcs, receiver.EntityId);
            npcs.CompleteNPCObjective(harness.Client, new CompleteNPCObjectivePacket
            {
                EntityId = receiver.EntityId, MissionId = missionId, ObjectiveId = objectiveId, PlayerFlagId = 1
            });
            Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[missionId].Objectives[objectiveId].State);
        }

        internal static void TurnIn(
            WildernessRuntimeTestHarness harness, NpcManager npcs, uint spawnId, uint missionId, int? selection = null)
        {
            var receiver = harness.Npc(spawnId);
            harness.MoveTo(receiver.Position);
            Open(harness, npcs, receiver.EntityId);
            npcs.CompleteNPCMission(harness.Client, new CompleteNPCMissionPacket
            {
                EntityId = receiver.EntityId, MissionId = missionId, SelectionIdx = selection
            });
            Assert.AreEqual(MissionState.Completed, harness.Client.Player.Missions[missionId].State);
        }

        internal static void ReplayTurnIn(
            WildernessRuntimeTestHarness harness, NpcManager npcs, uint spawnId, uint missionId, int? selection = null)
        {
            harness.Drain();
            var receiver = harness.Npc(spawnId);
            npcs.CompleteNPCMission(harness.Client, new CompleteNPCMissionPacket
            {
                EntityId = receiver.EntityId, MissionId = missionId, SelectionIdx = selection
            });
            npcs.RewardNPCMission(harness.Client, new RewardNPCMissionPacket
            {
                EntityId = receiver.EntityId, MissionId = missionId, SelectionIdx = selection
            });
            Assert.IsFalse(harness.Drain().OfType<MissionRewardedPacket>().Any(packet => packet.MissionId == missionId));
        }

        internal static uint HeldQuantity(WildernessRuntimeTestHarness harness, uint template)
        {
            using var unit = harness.CreateChar();
            return unit.CharacterInventories.GetItems(harness.Client.AccountEntry.Id)
                .Where(row => row.CharacterId == harness.Client.Player.Id && row.InventoryType == (uint)InventoryType.Personal)
                .Select(row => unit.Items.GetItem(row.ItemId))
                .Where(item => item.ItemTemplateId == template)
                .Aggregate(0U, (total, item) => total + item.StackSize);
        }

        internal static void VerifyArmorReward(
            WildernessRuntimeTestHarness harness, uint template, CharacterClass characterClass, uint skillId)
        {
            var armor = PersonalItem(harness, template);
            Assert.IsTrue(armor.ItemTemplate.ArmorValue > 0, $"Reward template {template} must provide real armor.");
            Assert.AreEqual(skillId, (uint)armor.ItemTemplate.EquipableInfo.SkillId);
            PrepareEquipmentEligibility(harness, characterClass, skillId);
            var slot = (uint)EntityClassManager.Instance.GetEquipableClassInfo(armor).EquipmentSlotId;
            Assert.AreEqual(0UL, harness.Client.Player.Inventory.EquippedInventory[(int)slot]);
            ManifestationManager.Instance.UpdateStatsValues(harness.Client, true);
            var before = harness.Client.Player.Attributes[Attributes.Armor].CurrentMax;
            InventoryManager.Instance.RequestEquipArmor(harness.Client, new RequestEquipArmorPacket
            {
                SrcInventory = InventoryType.Personal, SrcSlot = armor.OwnerSlotId, DestSlot = slot
            });
            Assert.AreEqual(armor.EntityId, harness.Client.Player.Inventory.EquippedInventory[(int)slot]);
            Assert.IsTrue(harness.Client.Player.Attributes[Attributes.Armor].CurrentMax > before,
                $"Equipping rewarded template {template} must increase actual armor capacity.");
            using var unit = harness.CreateChar();
            var equipped = unit.CharacterInventories.FindByItemId(armor.Id);
            Assert.AreEqual((uint)InventoryType.EquipedInventory, equipped.InventoryType);
            Assert.AreEqual(slot, equipped.SlotId);
        }

        private static Item PersonalItem(WildernessRuntimeTestHarness harness, uint template) =>
            harness.Client.Player.Inventory.PersonalInventory.Where(id => id != 0)
                .Select(EntityManager.Instance.GetItem).Single(item => item.ItemTemplateId == template);

        private static void PrepareEquipmentEligibility(
            WildernessRuntimeTestHarness harness, CharacterClass characterClass, uint skillId)
        {
            var level = Math.Max((byte)5, harness.Client.Player.Level);
            using var unit = harness.CreateChar();
            unit.ExecuteTransaction(() =>
            {
                unit.Characters.UpdateCharacterClass(harness.Client.Player.Id, (uint)characterClass);
                unit.Characters.UpdateCharacterProgression(harness.Client.Player.Id, harness.Client.Player.Experience, level);
                unit.CharacterSkills.AddOrUpdate(harness.Client.Player.Id, skillId, 0, 1);
            });
            harness.Client.Player.Class = (uint)characterClass;
            harness.Client.Player.Level = level;
            harness.Client.Player.Skills[(SkillId)skillId] = new SkillsData((SkillId)skillId, 0, 1);
        }

        internal static void GrantItem(WildernessRuntimeTestHarness harness, uint template, uint quantity)
        {
            using var grant = new InventoryManager.InventoryGrant();
            using (var unit = harness.CreateChar())
                unit.ExecuteTransaction(() => grant.PlanAndSave(harness.Client,
                    new[] { new InventoryManager.InventoryItemGrant(template, quantity) }, unit));
            grant.Publish(harness.Client);
        }

        internal static void FillCategory(WildernessRuntimeTestHarness harness, uint templateId, uint slots)
        {
            var template = ItemManager.Instance.GetItemTemplateById(templateId);
            var stack = EntityClassManager.Instance.GetClassInfo(template.Class).ItemClassInfo.StackSize;
            GrantItem(harness, templateId, checked(stack * slots));
        }

        internal static void FreeOneSlot(WildernessRuntimeTestHarness harness, uint templateId)
        {
            var item = harness.Client.Player.Inventory.PersonalInventory.Where(id => id != 0)
                .Select(EntityManager.Instance.GetItem).First(item => item.ItemTemplate.ItemTemplateId == templateId);
            InventoryManager.Instance.PersonalInventory_DestroyItem(harness.Client,
                new PersonalInventory_DestroyItemPacket { EntityId = item.EntityId, Quantity = item.StackSize });
        }
    }
}
