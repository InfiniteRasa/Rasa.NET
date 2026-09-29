extern alias RasaGame;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Game.Missions.Persistence;
using Rasa.Managers;
using Rasa.Memory;
using Rasa.Missions.Scenes;
using Rasa.Packets.Inventory.Client;
using Rasa.Packets.LootDispenser.Client;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.MapChannel.Server;
using Rasa.Services.Preloader.Missions;
using Rasa.Services.Preloader.Missions.Wilderness;
using Rasa.Structures;
using Rasa.Structures.Char;
using Rasa.Structures.World;

namespace Rasa.Test.Missions.Wilderness
{
    [TestClass]
    [DoNotParallelize]
    public class WildernessRanjaGorgeTests
    {
        [TestMethod]
        [TestCategory("W6Independent")]
        public void PlagueInvestigationRecoversRealSamplesAndKeepsTheNativeCouncilOrder()
        {
            using var harness = CreateMigrated();
            harness.SpawnWorld(173, 178, 170, 175, 174);
            var eleanor = RequireNpc(harness, 173, 94);
            var samuel = RequireNpc(harness, 178, 95);
            var nula = RequireNpc(harness, 170, 93);
            var doyan = RequireNpc(harness, 175, 92);
            var todae = RequireNpc(harness, 174, 91);
            harness.MoveTo(eleanor.Position);
            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, eleanor.EntityId, 425));
            SeedCompleted(harness, 444);
            var sampleReward = Offer(harness, eleanor, 425);
            AssertSelectable(sampleReward, (12827, 13483), (12855, 13511));
            Assert.AreEqual(1350U, sampleReward.FixedReward.Credits[CurencyType.Credits]);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, eleanor.EntityId, 425));
            Assert.AreEqual(0U, Held(harness, 620));
            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, eleanor.EntityId, 679));
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, eleanor.EntityId, 425, 3, 1));

            harness.MoveTo(samuel.Position);
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, samuel.EntityId, 425, 1, 1));
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, samuel.EntityId, 425, 5, 1));
            var reminder = OpenConversation(harness, samuel);
            Assert.IsTrue(reminder.ConvoDataDict.ContainsKey(ConversationType.ObjectiveAmbient),
                "Samuel's native 425.2 reminder is read-only objective dialogue.");
            Assert.AreEqual(MissionObjectiveState.Incomplete,
                harness.Client.Player.Missions[425].Objectives[2].State);

            LootOne(harness, 169, 90, 620, () =>
                Assert.AreEqual(MissionObjectiveState.Incomplete,
                    harness.Client.Player.Missions[425].Objectives[2].State,
                    "Killing Graal is not recovery of the sample vials."));
            Assert.AreEqual(MissionObjectiveState.Incomplete,
                harness.Client.Player.Missions[425].Objectives[5].State);
            ExchangeSamples(harness, samuel);
            Reload(harness);
            Assert.AreEqual(1U, Held(harness, 622));
            Assert.AreEqual(MissionObjectiveState.Incomplete,
                harness.Client.Player.Missions[425].Objectives[3].State);
            harness.MoveTo(eleanor.Position);
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, eleanor.EntityId, 425, 3, 1));
            CompleteAndCheckReward(harness, eleanor, 425, 0, 7000, 1350);
            Assert.AreEqual(0U, Held(harness, 622));
            Assert.AreEqual(1U, Held(harness, 12827));

            var nativeReward = Offer(harness, eleanor, 679);
            Assert.AreEqual(0, nativeReward.SelectableReward.Count);
            Assert.AreEqual(0, nativeReward.FixedReward.FixedItems.Count);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, eleanor.EntityId, 679));
            harness.MoveTo(nula.Position);
            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, nula.EntityId, 451));
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, nula.EntityId, 679, 1, 1));
            CompleteAndCheckReward(harness, nula, 679, null, 4500, 900);
            var councilReward = Offer(harness, nula, 451);
            AssertSelectable(councilReward, (631, 7615), (97037, 25830));
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, nula.EntityId, 451));

            harness.MoveTo(todae.Position);
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, todae.EntityId, 451, 2, 1));
            harness.MoveTo(doyan.Position);
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, doyan.EntityId, 451, 1, 1));
            harness.MoveTo(todae.Position);
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, todae.EntityId, 451, 2, 1));
            Assert.IsTrue(harness.Client.Player.Missions[451].Completeable,
                "The native council header must not become an invented third conversation.");
            CompleteAndCheckReward(harness, todae, 451, 1, 10000, 1500);
            Assert.AreEqual(1U, Held(harness, 97037));
            Assert.IsFalse(harness.Manager.LoadedMissions.TryGetValue(446, out var casperReturn) &&
                casperReturn.IsOperational,
                "This independent hub must not install Casper's instance-dependent return quest.");
        }

        [TestMethod]
        [TestCategory("W6Independent")]
        [DataRow(CharacterStartingExperienceState.Completed, true)]
        [DataRow(CharacterStartingExperienceState.Skipped, true)]
        [DataRow(CharacterStartingExperienceState.Bootcamp, false)]
        public void CormanCollectionsRespectStartingExperienceAndTheirRealPredecessor(
            CharacterStartingExperienceState qualification, bool eligible)
        {
            using var harness = CreateMigrated();
            SetQualification(harness, harness.Client.Player.Id, qualification);
            harness.SpawnWorld(189);
            var soji = RequireNpc(harness, 189, 111);
            harness.MoveTo(soji.Position);
            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, soji.EntityId, 758));
            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, soji.EntityId, 787));
            SeedCompleted(harness, 771);
            Assert.AreEqual(eligible,
                harness.Manager.AcceptOfferedMission(harness.Client, soji.EntityId, 758));
            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, soji.EntityId, 787),
                "Accepting Fithikally Challenged is not completing it.");
        }

        [TestMethod]
        [TestCategory("W6Independent")]
        public void FithikAndXanxCollectionsCountLootAndConsumeTheirExactQuantitiesOnce()
        {
            using var harness = CreateMigrated(771);
            harness.SpawnWorld(189);
            var soji = RequireNpc(harness, 189, 111);
            AssertFixed(Offer(harness, soji, 758), (44920, 2), (45047, 4));
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, soji.EntityId, 758));
            for (uint count = 1; count <= 10; count++)
            {
                LootOne(harness, 144, 1, 2533);
                Assert.AreEqual(count, harness.Client.Player.Missions[758].Objectives[3].ItemCounters[11161]);
            }
            Reload(harness);
            Assert.AreEqual(10U, Held(harness, 2533));
            CompleteAndCheckReward(harness, soji, 758, null, 4000, 600);
            Assert.AreEqual(0U, Held(harness, 2533));
            Assert.AreEqual(2U, Held(harness, 44920));
            Assert.AreEqual(4U, Held(harness, 45047));

            AssertFixed(Offer(harness, soji, 787), (44919, 2), (44920, 6));
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, soji.EntityId, 787));
            for (uint count = 1; count <= 4; count++)
            {
                LootOne(harness, 160, 87, 2532);
                Assert.AreEqual(count, harness.Client.Player.Missions[787].Objectives[3].ItemCounters[11160]);
            }
            CompleteAndCheckReward(harness, soji, 787, null, 4000, 600);
            Assert.AreEqual(0U, Held(harness, 2532));
            Assert.AreEqual(2U, Held(harness, 44919));
            Assert.AreEqual(8U, Held(harness, 44920));
        }

        [TestMethod]
        [TestCategory("W6Rewards")]
        [DataRow(758U, 44920U, 4, 2U)]
        [DataRow(758U, 45047U, 6, 4U)]
        [DataRow(787U, 44919U, 3, 2U)]
        [DataRow(787U, 44920U, 4, 6U)]
        [DataRow(769U, 44921U, 5, 6U)]
        [DataRow(769U, 44920U, 4, 2U)]
        public void AwardedMedicalRewardsHealOnlyAfterCommittedNativeConsumptionAndRejectReplay(
            uint missionId, uint templateId, int actionLevel, uint expectedQuantity)
        {
            var collection = missionId switch
            {
                758U => (Prerequisite: 771U, Objective: 3U, Count: 10U, Spawn: 144U,
                    Creature: 1U, Template: 2533U, ItemClass: 11161U),
                787U => (Prerequisite: 758U, Objective: 3U, Count: 4U, Spawn: 160U,
                    Creature: 87U, Template: 2532U, ItemClass: 11160U),
                769U => (Prerequisite: 787U, Objective: 2U, Count: 3U, Spawn: 530120U,
                    Creature: 530120U, Template: 2531U, ItemClass: 11159U),
                _ => throw new ArgumentOutOfRangeException(nameof(missionId))
            };
            using var harness = CreateMigrated(collection.Prerequisite);
            harness.SpawnWorld(189);
            var soji = RequireNpc(harness, 189, 111);
            harness.MoveTo(soji.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, soji.EntityId, missionId));
            for (uint count = 1; count <= collection.Count; count++)
            {
                LootOne(harness, collection.Spawn, collection.Creature, collection.Template);
                Assert.AreEqual(count,
                    harness.Client.Player.Missions[missionId].Objectives[collection.Objective].ItemCounters[collection.ItemClass]);
            }
            CompleteAndCheckReward(harness, soji, missionId, null, 4000, 600);
            Assert.AreEqual(0U, Held(harness, collection.Template));
            Assert.AreEqual(expectedQuantity, Held(harness, templateId));

            PrepareEquipmentCharacter(harness, CharacterClass.Recruit, 0);
            var abilities = harness.LoadAbilities();
            var item = harness.Client.Player.Inventory.PersonalInventory.Where(id => id != 0)
                .Select(EntityManager.Instance.GetItem).Single(candidate => candidate.ItemTemplateId == templateId);
            var health = harness.Client.Player.Attributes[Attributes.Health];
            Assert.IsTrue(health.CurrentMax > 1);
            health.Current = 1;
            harness.Drain();
            abilities.RequestPerformAbility(harness.Client,
                NativeMedpackRequest(item.EntityId, actionLevel == 3 ? 4 : 3));
            Assert.IsFalse(harness.Map.PerformRecovery.Any(action => action.ActionId == (ActionId)419));
            Assert.IsTrue(harness.Drain().OfType<UserActionFailedPacket>().Any());
            Assert.AreEqual(expectedQuantity, Held(harness, templateId));
            Assert.AreEqual(1, health.Current);
            Assert.IsFalse(harness.Client.Player.ActiveEffects.Values.Any(effect => effect.TypeId == 280));

            var observedConsumptionSave = false;
            var registered = false;
            lock (RasaGame::Rasa.Game.Server.Clients)
                if (!RasaGame::Rasa.Game.Server.Clients.Contains(harness.Client))
                {
                    RasaGame::Rasa.Game.Server.Clients.Add(harness.Client);
                    registered = true;
                }
            harness.Context.BeforeSave = database =>
            {
                if (!database.ChangeTracker.Entries<ItemEntry>().Any(entry =>
                    entry.Entity.ItemId == item.Id &&
                    (entry.State == Microsoft.EntityFrameworkCore.EntityState.Deleted ||
                     entry.State == Microsoft.EntityFrameworkCore.EntityState.Modified &&
                     entry.Entity.StackSize == expectedQuantity - 1)))
                    return;
                observedConsumptionSave = true;
                Assert.AreEqual(expectedQuantity, item.StackSize,
                    "Uncommitted consumption must not be published to the live inventory.");
                Assert.AreEqual(1, health.Current);
                Assert.IsFalse(harness.Client.Player.ActiveEffects.Values.Any(effect => effect.TypeId == 280),
                    "Healing must not precede the item-consumption transaction.");
            };
            try
            {
                var request = NativeMedpackRequest(item.EntityId, actionLevel);
                abilities.RequestPerformAbility(harness.Client, request);
                var recovery = harness.Map.PerformRecovery.Single(action => action.ActionId == (ActionId)419);
                Assert.AreEqual(item.EntityId, recovery.ItemId);
                ActorActionManager.Instance.DoWork(harness.Map, 2500);
                harness.Context.BeforeSave = null;
                Assert.IsTrue(observedConsumptionSave);
                Assert.AreEqual(expectedQuantity - 1, item.StackSize);
                Assert.AreEqual(1, health.Current);
                var effect = harness.Client.Player.ActiveEffects.Values.Single(candidate => candidate.TypeId == 280);
                var effectId = effect.EffectId;

                Assert.IsTrue(harness.Drain().OfType<AbilityRecoveryPacket>().Any());
                abilities.RequestPerformAbility(harness.Client, request);
                Assert.IsFalse(harness.Map.PerformRecovery.Any(action => action.ActionId == (ActionId)419));
                Assert.IsTrue(harness.Drain().OfType<UserActionFailedPacket>().Any());
                Assert.AreEqual(expectedQuantity - 1, item.StackSize);
                Assert.AreEqual(effectId,
                    harness.Client.Player.ActiveEffects.Values.Single(candidate => candidate.TypeId == 280).EffectId);
                Assert.AreEqual(1, health.Current);
                using (var unit = harness.CreateChar())
                    Assert.AreEqual(expectedQuantity - 1, unit.Items.GetItem(item.Id).StackSize);

                effect.NextTickTick = Environment.TickCount64 - 1;
                GameEffectManager.Instance.DoWork(harness.Map, 1000);
                Assert.IsTrue(health.Current > 1 && health.Current <= health.CurrentMax,
                    "The actual native medpack effect must heal the injured player.");
                Assert.AreEqual(expectedQuantity - 1, Held(harness, templateId));
                Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, soji.EntityId, missionId, null));
                Assert.AreEqual(expectedQuantity - 1, Held(harness, templateId));
            }
            finally
            {
                harness.Context.BeforeSave = null;
                if (registered)
                    lock (RasaGame::Rasa.Game.Server.Clients)
                        RasaGame::Rasa.Game.Server.Clients.Remove(harness.Client);
            }
        }

        [TestMethod]
        [TestCategory("W6Independent")]
        public void FullRewardBagsPreserveSpleensAndAllowAnExactlyOnceRetry()
        {
            using var harness = CreateMigrated(771);
            harness.SpawnWorld(189);
            var soji = RequireNpc(harness, 189, 111);
            harness.MoveTo(soji.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, soji.EntityId, 758));
            for (var count = 0; count < 10; count++)
                LootOne(harness, 144, 1, 2533);
            var fillers = FillConsumableBag(harness);
            harness.MoveTo(soji.Position);
            var before = harness.Context.ReadRewardTotals();
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, soji.EntityId, 758, null));
            Assert.AreEqual(before, harness.Context.ReadRewardTotals());
            Assert.AreEqual(10U, Held(harness, 2533));
            Assert.AreEqual(0U, Held(harness, 44920));
            foreach (var filler in fillers.Take(6))
                InventoryManager.Instance.PersonalInventory_DestroyItem(harness.Client,
                    new PersonalInventory_DestroyItemPacket { EntityId = filler.EntityId, Quantity = 1 });
            Reload(harness);
            CompleteAndCheckReward(harness, soji, 758, null, 4000, 600);
            Assert.AreEqual(0U, Held(harness, 2533));
            Assert.AreEqual(2U, Held(harness, 44920));
            Assert.AreEqual(4U, Held(harness, 45047));
        }

        [TestMethod]
        [TestCategory("W6Independent")]
        public void RetainedRecoveredVialsResumeAfterSamuelWithoutAnotherKill()
        {
            using var harness = CreateMigrated(444);
            var samuel = RecoverSamples(harness);
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, 425));
            Reload(harness);
            Assert.AreEqual(1U, Held(harness, 620));
            var eleanor = RequireNpc(harness, 173, 94);
            harness.MoveTo(eleanor.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, eleanor.EntityId, 425));
            Assert.AreEqual(MissionObjectiveState.Incomplete,
                harness.Client.Player.Missions[425].Objectives[1].State);
            Assert.AreNotEqual(MissionObjectiveState.Completed,
                harness.Client.Player.Missions[425].Objectives[2].State);
            harness.MoveTo(samuel.Position);
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, samuel.EntityId, 425, 1, 1));
            Assert.AreEqual(MissionObjectiveState.Completed,
                harness.Client.Player.Missions[425].Objectives[2].State,
                "Activation must reconcile the legitimately retained sample, without another Graal kill or relog.");
            Assert.AreEqual(MissionObjectiveState.Incomplete,
                harness.Client.Player.Missions[425].Objectives[5].State);
            ExchangeSamples(harness, samuel);
        }

        [TestMethod]
        [TestCategory("W6Independent")]
        public void SamuelExchangeRollsBackWithItsItemsAndCanBeRetriedAfterReload()
        {
            using var harness = CreateMigrated(444);
            var samuel = RecoverSamples(harness);
            harness.MoveTo(samuel.Position);
            harness.Context.BeforeSave = _ => throw new DbUpdateException("W6 exchange persistence rejection.");
            try
            {
                Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, samuel.EntityId, 425, 5, 1));
            }
            finally
            {
                harness.Context.BeforeSave = null;
            }
            Reload(harness);
            Assert.AreEqual(1U, Held(harness, 620));
            Assert.AreEqual(0U, Held(harness, 622));
            Assert.AreEqual(MissionObjectiveState.Incomplete,
                harness.Client.Player.Missions[425].Objectives[5].State);
            ExchangeSamples(harness, samuel);
        }

        [TestMethod]
        [TestCategory("W6Independent")]
        public void EleanorRequiresThisAssignmentsCombinedVialNotAnUnboundLookalike()
        {
            using var harness = CreateMigrated(444);
            var samuel = RecoverSamples(harness);
            ExchangeSamples(harness, samuel);
            MissionItemPlanner removal = null;
            using (var unit = harness.CreateChar())
                unit.ExecuteTransaction(() =>
                {
                    var assignment = unit.CharacterMissions.GetByCharacterAndMission(harness.Client.Player.Id, 425);
                    removal = new MissionItemPlanner(harness.Client, unit, harness.Manager);
                    removal.Apply(new RemoveMissionItemsIntent("w6-lost-combined-samples", 425, "combined-samples"),
                        assignment.AssignmentId, assignment.Generation);
                });
            removal.Publish(harness.Client);
            AddUnboundNativeItem(harness, 622, 7576);
            Reload(harness);
            Assert.AreEqual(1U, Held(harness, 622));
            using (var unit = harness.CreateChar())
                Assert.IsFalse(unit.CharacterMissionItems.GetOwned(harness.Client.Player.Id)
                    .Any(item => item.MissionId == 425));
            var eleanor = RequireNpc(harness, 173, 94);
            var before = harness.Context.ReadRewardTotals();
            Assert.IsFalse(OpenConversation(harness, eleanor).ConvoDataDict.ContainsKey(ConversationType.ObjectiveComplete));
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, eleanor.EntityId, 425, 3, 1));
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, eleanor.EntityId, 425, 0));
            Assert.AreEqual(MissionObjectiveState.Incomplete,
                harness.Client.Player.Missions[425].Objectives[3].State);
            Assert.AreEqual(before, harness.Context.ReadRewardTotals());
        }

        [TestMethod]
        [TestCategory("W6Independent")]
        [DataRow(false)]
        [DataRow(true)]
        public void TerminalSampleCleanupDoesNotReissueTheCombinedVial(bool abandon)
        {
            using var harness = CreateMigrated(444);
            var samuel = RecoverSamples(harness);
            ExchangeSamples(harness, samuel);
            Assert.IsTrue(abandon
                ? harness.Manager.TryAbandon(harness.Client, 425)
                : harness.Manager.TryFailMission(harness.Client, 425));
            Reload(harness);
            Assert.AreEqual(0U, Held(harness, 622));
            using (var unit = harness.CreateChar())
                Assert.IsFalse(unit.CharacterMissionItems.GetOwned(harness.Client.Player.Id)
                    .Any(item => item.MissionId == 425));
            harness.MoveTo(samuel.Position);
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, samuel.EntityId, 425, 5, 1));
            Assert.AreEqual(0U, Held(harness, 622));
        }

        [TestMethod]
        [TestCategory("W6Independent")]
        public void MissingRouteClustersOrOutdoorPredatorsCannotProduceFallbackContent()
        {
            var route = new MigrationBuilder("Microsoft.EntityFrameworkCore.Sqlite");
            Assert.ThrowsExactly<ArgumentException>(() => WildernessRanjaGorgeV1.UpWalkingWounded(route, null));
            Assert.AreEqual(0, route.Operations.Count);
            var clusters = new MigrationBuilder("Microsoft.EntityFrameworkCore.Sqlite");
            Assert.ThrowsExactly<ArgumentException>(() =>
                WildernessRanjaGorgeV1.UpEggClusters(clusters, Array.Empty<SceneActorDefinition>()));
            Assert.AreEqual(0, clusters.Operations.Count);
            var predators = new MigrationBuilder("Microsoft.EntityFrameworkCore.Sqlite");
            Assert.ThrowsExactly<ArgumentException>(() =>
                WildernessRanjaGorgeV1.UpPredatory(predators, new uint[] { 520022 }));
            Assert.AreEqual(0, predators.Operations.Count);
        }

        [TestMethod]
        [TestCategory("W6Equipment")]
        [DataRow(12827U, 13483U, 2U, CharacterClass.Specialist, 30U)]
        [DataRow(12855U, 13511U, 3U, CharacterClass.Specialist, 30U)]
        [DataRow(11567U, 6495U, 2U, CharacterClass.Recruit, 19U)]
        [DataRow(11568U, 6498U, 16U, CharacterClass.Recruit, 19U)]
        public void SelectedArmorRewardsEquipProtectPersistAndUnequip(uint templateId, uint classId,
            uint slot, CharacterClass profession, uint skillId)
        {
            using var harness = CreateMigrated();
            PrepareEquipmentCharacter(harness, profession, skillId);
            var row = harness.World.Set<ItemTemplateArmorEntry>().SingleOrDefault(item => item.Id == templateId);
            Assert.IsNotNull(row, "The real World armor row must exist; a class name is not armor capacity.");
            Assert.IsTrue(row.ArmorValue > 0);
            var item = AddUnboundNativeItem(harness, templateId, classId);
            var sourceSlot = item.OwnerSlotId;
            Assert.AreEqual(classId, (uint)item.ItemTemplate.Class);
            Assert.IsTrue(item.ItemTemplate.ArmorValue > 0);
            Assert.AreEqual(slot, (uint)EntityClassManager.Instance.GetEquipableClassInfo(item).EquipmentSlotId);
            Assert.IsTrue(InventoryManager.Instance.ValidateItemEquip(harness.Client, item),
                "The fixture must meet actual level and skill requirements without altering item data.");
            var baselineArmor = harness.Client.Player.Attributes[Attributes.Armor].CurrentMax;
            InventoryManager.Instance.RequestEquipArmor(harness.Client, new RequestEquipArmorPacket
            {
                SrcInventory = InventoryType.Personal, SrcSlot = sourceSlot, DestSlot = slot
            });
            Assert.AreEqual(item.EntityId, harness.Client.Player.Inventory.EquippedInventory[(int)slot]);
            Assert.AreEqual(0UL, harness.Client.Player.Inventory.PersonalInventory[(int)sourceSlot]);
            AssertStoredItem(harness, item.Id, InventoryType.EquipedInventory, slot);
            var equippedArmor = harness.Client.Player.Attributes[Attributes.Armor].CurrentMax;
            Assert.IsTrue(equippedArmor > baselineArmor, "Equipping the reward must add real armor capacity.");

            ManifestationManager.Instance.UpdateStatsValues(harness.Client, true);
            var attacker = SpawnEquipmentTarget(harness);
            var armorBeforeHit = harness.Client.Player.Attributes[Attributes.Armor].Current;
            var healthBeforeHit = harness.Client.Player.Attributes[Attributes.Health].Current;
            Assert.IsTrue(armorBeforeHit > 10);
            MissileManager.Instance.MissileLaunch(harness.Map,
                new ActionData(attacker, ActionId.WeaponAttack, 133, 0)
                    { TargetId = harness.Client.Player.EntityId }, 10);
            Assert.AreEqual(1, harness.Map.QueuedMissiles.Count);
            MissileManager.Instance.DoWork(harness.Map, 1000);
            Assert.IsTrue(harness.Client.Player.Attributes[Attributes.Armor].Current < armorBeforeHit,
                "The normal incoming-hit path must consume the equipped armor pool.");
            Assert.AreEqual(healthBeforeHit, harness.Client.Player.Attributes[Attributes.Health].Current,
                "The equipped armor must protect health against this ordinary physical hit.");

            Reload(harness);
            ManifestationManager.Instance.UpdateStatsValues(harness.Client, false);
            var reloaded = EntityManager.Instance.GetItem(harness.Client.Player.Inventory.EquippedInventory[(int)slot]);
            Assert.IsNotNull(reloaded);
            Assert.AreEqual(item.Id, reloaded.Id);
            Assert.AreEqual(templateId, reloaded.ItemTemplateId);
            Assert.AreEqual(equippedArmor, harness.Client.Player.Attributes[Attributes.Armor].CurrentMax);
            AssertStoredItem(harness, item.Id, InventoryType.EquipedInventory, slot);

            InventoryManager.Instance.RequestEquipArmor(harness.Client, new RequestEquipArmorPacket
            {
                SrcInventory = InventoryType.Personal, SrcSlot = sourceSlot, DestSlot = slot
            });
            Assert.AreEqual(0UL, harness.Client.Player.Inventory.EquippedInventory[(int)slot]);
            Assert.AreEqual(reloaded.EntityId, harness.Client.Player.Inventory.PersonalInventory[(int)sourceSlot]);
            Assert.AreEqual(baselineArmor, harness.Client.Player.Attributes[Attributes.Armor].CurrentMax);
            AssertStoredItem(harness, item.Id, InventoryType.Personal, sourceSlot);
        }

        [TestMethod]
        [TestCategory("W6Equipment")]
        [DataRow(631U, 7615U, CharacterClass.Specialist, 14U)]
        [DataRow(97037U, 25830U, CharacterClass.Specialist, 0U)]
        [DataRow(3061U, 11893U, CharacterClass.Recruit, 1U)]
        [DataRow(3315U, 12147U, CharacterClass.Recruit, 1U)]
        public void SelectedWeaponsAndToolsHaveRealProfilesAndPersistInTheActiveDrawer(
            uint templateId, uint classId, CharacterClass profession, uint skillId)
        {
            using var harness = CreateMigrated();
            PrepareEquipmentCharacter(harness, profession, skillId);
            var row = harness.World.Set<ItemTemplateWeaponEntry>().SingleOrDefault(item => item.Id == templateId);
            Assert.IsNotNull(row);
            var item = AddUnboundNativeItem(harness, templateId, classId);
            Assert.IsNotNull(item.ItemTemplate.WeaponInfo);
            Assert.IsNotNull(EntityClassManager.Instance.GetWeaponClassInfo(item));
            Assert.AreEqual(13U, (uint)EntityClassManager.Instance.GetEquipableClassInfo(item).EquipmentSlotId);
            Assert.IsTrue(InventoryManager.Instance.ValidateItemEquip(harness.Client, item));
            var sourceSlot = item.OwnerSlotId;
            InventoryManager.Instance.RequestEquipWeapon(harness.Client, new RequestEquipWeaponPacket
            {
                SrcSlot = sourceSlot, InventoryType = InventoryType.Personal, DestSlot = 0
            });
            Assert.AreEqual(0UL, harness.Client.Player.Inventory.PersonalInventory[(int)sourceSlot]);
            Assert.AreEqual(item.EntityId, harness.Client.Player.Inventory.WeaponDrawer[0]);
            Assert.AreEqual(item.EntityId, harness.Client.Player.Inventory.EquippedInventory[13]);
            AssertStoredItem(harness, item.Id, InventoryType.WeaponDrawerInventory, 0);
            Reload(harness);
            var reloaded = EntityManager.Instance.GetItem(harness.Client.Player.Inventory.WeaponDrawer[0]);
            Assert.IsNotNull(reloaded);
            Assert.AreEqual(item.Id, reloaded.Id);
            Assert.AreEqual(templateId, reloaded.ItemTemplateId);
            Assert.IsNotNull(reloaded.ItemTemplate.WeaponInfo);
            Assert.AreEqual(reloaded.EntityId, harness.Client.Player.Inventory.EquippedInventory[13]);
            AssertStoredItem(harness, item.Id, InventoryType.WeaponDrawerInventory, 0);
            if (templateId is 3061 or 3315)
                VerifyPhysicalWeaponFire(harness, reloaded);
        }

        [TestMethod]
        [TestCategory("W6Integrated")]
        public void PredatoryFinishesWithOutdoorPredatorLootWithoutInstanceHistory()
        {
            using var harness = CreateMigrated(787);
            Assert.IsTrue(harness.Manager.LoadedMissions.TryGetValue(769, out var definition) &&
                definition.IsOperational, "Predatory requires the integrated outdoor Predator binding, not an instance bypass.");
            Assert.IsFalse(harness.Map.IsPrivateInstance);
            harness.SpawnWorld(189, 530120, 530121);
            var soji = RequireNpc(harness, 189, 111);
            using (var unit = harness.CreateChar())
            {
                var predecessor = unit.CharacterMissions.Runtime.History(harness.Client.Player.Id)
                    .Single(history => history.MissionId == 787);
                Assert.AreEqual((uint)MissionState.Completed, predecessor.Outcome);
                Assert.IsTrue(predecessor.Rewarded);
                Assert.IsNull(unit.CharacterMissions.GetByCharacterAndMission(harness.Client.Player.Id, 787),
                    "A completed prerequisite must not be an incomplete current assignment.");
            }
            AssertFixed(Offer(harness, soji, 769), (44921, 6), (44920, 2));
            Assert.AreEqual(MissionState.Completed, harness.Client.Player.MissionHistory[787],
                "Opening Soji must retain the authoritative prerequisite history.");
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, soji.EntityId, 769));
            var sources = definition.Items["predator-parts"].Drop.CreatureIds;
            CollectionAssert.AreEqual(new uint[] { 530120 }, sources.ToArray());
            var classes = harness.World.Set<CreatureEntry>().Where(creature => sources.Contains(creature.Id))
                .Select(creature => creature.ClassId).ToArray();
            Assert.AreEqual(sources.Count, classes.Length);
            Assert.IsTrue(classes.All(classId => classId == 3902),
                "The outdoor corpse sources must be native Predators, not renamed or unrelated creatures.");
            foreach (var spawnId in new uint[] { 530120, 530121 })
            {
                var predator = harness.Map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList)
                    .Distinct().SingleOrDefault(creature => creature.SpawnPool?.DbId == spawnId &&
                        creature.State != CharacterState.Dead);
                Assert.IsNotNull(predator, $"Actual outdoor Predator pool{spawnId} must be populated.");
                Assert.AreEqual(530120U, predator.DbId);
                var ground = harness.Map.NavMesh.GroundHeight(predator.Position);
                Assert.IsNotNull(ground);
                var huntingGround = new Vector3(predator.Position.X, ground.Value, predator.Position.Z);
                AssertConnected(harness, soji.Position, huntingGround,
                    $"Soji must have a connected outdoor route to Predator pool{spawnId}.");
                AssertConnected(harness, huntingGround, soji.Position,
                    $"Predator pool{spawnId} needs a connected return to Soji.");
            }
            for (uint count = 1; count <= 3; count++)
            {
                LootOne(harness, count == 2 ? 530121U : 530120U, 530120, 2531);
                Assert.AreEqual(count, harness.Client.Player.Missions[769].Objectives[2].ItemCounters[11159]);
            }
            CompleteAndCheckReward(harness, soji, 769, null, 4000, 600);
            Assert.AreEqual(0U, Held(harness, 2531));
            Assert.AreEqual(6U, Held(harness, 44921));
            Assert.AreEqual(2U, Held(harness, 44920));
        }

        [TestMethod]
        [TestCategory("W6Integrated")]
        public void RanjaCouncilHandoffsRequireConnectedRoutesToTheDaghdaContacts()
        {
            using var harness = CreateMigrated();
            harness.SpawnWorld(170, 175, 174);
            var nula = RequireNpc(harness, 170, 93);
            var doyan = RequireNpc(harness, 175, 92);
            var todae = RequireNpc(harness, 174, 91);
            AssertConnected(harness, nula.Position, doyan.Position,
                "Nula's handoff must reach the actual Daghda council surface, not the terrain above it.");
            AssertConnected(harness, doyan.Position, todae.Position,
                "The ordered Doyan/Todae conversations need a real connected walking route.");
        }

        [TestMethod]
        [TestCategory("W6Integrated")]
        public void SacsRequiresFourDistinctRealDestructionsAndNeverCountsRepeatedUse()
        {
            using var harness = CreateMigrated();
            VerifySacsDestruction(harness);
        }

        [TestMethod]
        [TestCategory("W6LocalEggs")]
        public void ProvisionalEggPosesSupportOnlyTheLocalDestructionAndCollectionContract()
        {
            using var harness = CreateMigrated();
            VerifySacsDestruction(harness);
        }

        [TestMethod]
        [TestCategory("W6Integrated")]
        [DataRow(false)]
        [DataRow(true)]
        public void RewardedSacsSceneEndsBeforeDisconnectAndCannotResurrectOnFreshReconnect(
            bool failRetirementAfterCommittedReward)
        {
            using var harness = CreateMigrated();
            harness.SpawnWorld(206);
            var sten = RequireNpc(harness, 206, 128);
            harness.MoveTo(sten.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, sten.EntityId, 860));
            var props = harness.Map.DynamicObjects.Where(prop => prop.SceneMissionId == 860 &&
                prop.SceneOwnerCharacterId == harness.Client.Player.Id).ToArray();
            Assert.AreEqual(4, props.Length);
            var runId = props[0].SceneRunId;
            var assignment = harness.Context.ReadMission(860);
            var assignmentId = assignment.AssignmentId;
            Assert.IsFalse(string.IsNullOrWhiteSpace(runId));
            Assert.IsTrue(props.All(prop => prop.SceneRunId == runId));
            foreach (var prop in props)
                HitCluster(harness, prop, 100);
            Assert.AreEqual(MissionObjectiveState.Completed,
                harness.Client.Player.Missions[860].Objectives[1].State);
            var beforeReward = harness.Context.ReadRewardTotals();
            harness.MoveTo(sten.Position);
            var rewarded = beforeReward;
            var failedRetirements = 0;
            if (failRetirementAfterCommittedReward)
                harness.Context.BeforeSave = database =>
                {
                    if (!database.ChangeTracker.Entries<MissionSceneEntry>().Any(entry =>
                        entry.State == Microsoft.EntityFrameworkCore.EntityState.Modified && entry.Entity.RunId == runId &&
                        entry.Entity.AssignmentId == assignmentId && entry.Entity.Status == "Ended"))
                        return;
                    using var committed = harness.CreateChar();
                    var saved = committed.CharacterMissions.GetByCharacterAndMission(harness.Client.Player.Id, 860);
                    if (saved?.AssignmentId != assignmentId || saved.MissionState != (uint)MissionState.Completed ||
                        !committed.CharacterMissions.Runtime.HasReceipt(
                            assignmentId, assignment.Generation, "mission-reward"))
                        return;
                    failedRetirements++;
                    throw new DbUpdateException("W6 exact-scene retirement failed after the reward committed.");
                };
            try
            {
                Assert.IsTrue(harness.Manager.CompleteOfferedMission(harness.Client, sten.EntityId, 860, 0));
                rewarded = harness.Context.ReadRewardTotals();
                Assert.AreEqual(beforeReward.Experience + 18000U, rewarded.Experience);
                Assert.AreEqual(beforeReward.Credits + 800, rewarded.Credits);
                Assert.AreEqual(1U, Held(harness, 3315));
                if (failRetirementAfterCommittedReward)
                {
                    Assert.IsTrue(failedRetirements > 0,
                        "The fault must hit a scene-only save after a separately visible reward receipt.");
                    using (var unit = harness.CreateChar())
                    {
                        Assert.IsTrue(unit.CharacterMissions.Runtime.HasReceipt(
                            assignmentId, assignment.Generation, "mission-reward"));
                        Assert.AreNotEqual("Ended", unit.CharacterMissions.Runtime.Scene(runId).Status);
                    }
                    var firstAttempts = failedRetirements;
                    harness.UtcNow = harness.UtcNow.AddSeconds(2);
                    harness.Manager.Scenes.Tick(harness.Map);
                    Assert.IsTrue(failedRetirements > firstAttempts,
                        "A nonfatal retirement failure must remain pending for the normal scene tick.");
                    Assert.AreEqual(rewarded, harness.Context.ReadRewardTotals());
                    Assert.AreEqual(1U, Held(harness, 3315));
                    Assert.IsFalse(harness.Map.DynamicObjects.Any(prop => prop.SceneMissionId == 860 &&
                        prop.SceneOwnerCharacterId == harness.Client.Player.Id),
                        "Pending retirement must not resurrect destroyed egg props.");
                }
                else
                    AssertRewardedSacsSceneRetired(harness, runId, assignmentId, props);

                harness.Client.State = RasaGame::Rasa.Data.ClientState.Disconnected;
                harness.Maps.CleanupDisconnected(harness.Client);
                if (failRetirementAfterCommittedReward)
                    using (var unit = harness.CreateChar())
                        Assert.AreNotEqual("Ended", unit.CharacterMissions.Runtime.Scene(runId).Status,
                            "The persistence fault remains armed through disconnect, until the reconnect retry.");
            }
            finally
            {
                harness.Context.BeforeSave = null;
            }
            var reconnected = harness.Context.CreateCompetingClient(harness.Manager);
            Assert.AreNotSame(harness.Client, reconnected);
            Assert.AreNotSame(harness.Client.Player, reconnected.Player);
            using (var unit = harness.CreateChar())
            {
                reconnected.Player.StartingExperienceCompleted =
                    MissionRequirementFactsAdapter.HasCompletedStartingExperience(unit, reconnected.Player.Id);
                MissionJournalAdapter.ApplyHistory(reconnected.Player,
                    unit.CharacterMissions.Runtime.History(reconnected.Player.Id));
            }
            InventoryManager.Instance.InitCharacterInventory(reconnected);
            harness.Manager.PublishInitialState(reconnected);
            harness.Manager.Scenes.Tick(harness.Map);
            harness.Manager.PublishInitialState(reconnected);

            AssertRewardedSacsSceneRetired(harness, runId, assignmentId, props);
            Assert.AreEqual(MissionState.Completed, reconnected.Player.MissionHistory[860]);
            var rifles = reconnected.Player.Inventory.PersonalInventory.Where(id => id != 0)
                .Select(id => EntityManager.Instance.GetItem(id))
                .Where(item => item.ItemTemplateId == 3315).ToArray();
            Assert.AreEqual(1, rifles.Length);
            Assert.AreEqual(1U, rifles[0].StackSize);
            reconnected.SetWorldPosition(sten.Position, reconnected.Player.Rotation);
            CellManager.Instance.UpdateVisibility(reconnected);
            Assert.IsFalse(harness.Manager.AcceptOfferedMission(reconnected, sten.EntityId, 860));
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(reconnected, sten.EntityId, 860, 0));
            Assert.AreEqual(rewarded, harness.Context.ReadRewardTotals());
            AssertRewardedSacsSceneRetired(harness, runId, assignmentId, props);
        }

        private static void VerifySacsDestruction(WildernessRuntimeTestHarness harness)
        {
            Assert.IsTrue(harness.Manager.LoadedMissions.TryGetValue(860, out var definition) &&
                definition.IsOperational, "Sacs And Violence requires four integrated native cave-object bindings.");
            harness.SpawnWorld(206, 202);
            var sten = RequireNpc(harness, 206, 128);
            var quincy = RequireNpc(harness, 202, 124);
            AssertSelectable(Offer(harness, sten, 860), (3315, 12147), (11568, 6498));
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, sten.EntityId, 860));
            var clusters = harness.Map.DynamicObjects.Where(obj => obj.SceneMissionId == 860 &&
                    obj.SceneOwnerCharacterId == harness.Client.Player.Id)
                .OrderBy(obj => obj.SceneActorRole).ToArray();
            Assert.AreEqual(4, clusters.Length);
            Assert.AreEqual(4, clusters.Select(obj => obj.EntityId).Distinct().Count());
            Assert.IsTrue(clusters.All(obj => (uint)obj.EntityClassId == 10180 && obj.IsInWorld));
            foreach (var cluster in clusters)
            {
                var height = harness.Map.NavMesh.GroundHeight(cluster.Position);
                Assert.IsNotNull(height);
                Assert.IsTrue(Math.Abs(height.Value - cluster.Position.Y) < 1,
                    $"Cluster {cluster.SceneActorRole} is not grounded at {cluster.Position}.");
                AssertConnected(harness, quincy.Position, cluster.Position,
                    $"Cluster {cluster.SceneActorRole} needs an actual connected path from the canyon infirmary.");
            }
            var first = clusters[0];
            harness.MoveTo(first.Position);
            var recoveries = harness.Map.PerformRecovery.Count;
            for (var attempt = 0; attempt < 3; attempt++)
                harness.Objects.RequestUseObjectPacket(harness.Client, new RequestUseObjectPacket
                {
                    EntityId = first.EntityId, ActionId = ActionId.UseObject, ActionArgId = 1
                });
            Assert.AreEqual(recoveries, harness.Map.PerformRecovery.Count);
            Assert.AreEqual(0U, harness.Client.Player.Missions[860].Objectives[1].Counters[0]);
            HitCluster(harness, first, 99);
            Assert.AreEqual(1U, first.CurrentHitPoints);
            Assert.AreEqual(0U, harness.Client.Player.Missions[860].Objectives[1].Counters[0]);
            HitCluster(harness, first, 1);
            Assert.AreEqual(1U, harness.Client.Player.Missions[860].Objectives[1].Counters[0]);
            Assert.IsFalse(PracticeTargetManager.TryGetTarget(harness.Map, first.EntityId, out _));
            MissileManager.Instance.MissileLaunch(harness.Map,
                new ActionData(harness.Client.Player, ActionId.WeaponAttack, 133, 0)
                    { TargetId = first.EntityId }, 100);
            MissileManager.Instance.DoWork(harness.Map, 1000);
            Assert.AreEqual(1U, harness.Client.Player.Missions[860].Objectives[1].Counters[0]);
            Reload(harness);
            harness.Manager.PublishInitialState(harness.Client);
            Assert.AreEqual(3, harness.Map.DynamicObjects.Count(obj => obj.SceneMissionId == 860 &&
                obj.SceneOwnerCharacterId == harness.Client.Player.Id && obj.IsEnabled));

            for (var index = 1; index < clusters.Length; index++)
            {
                Assert.AreEqual(MissionObjectiveState.Incomplete,
                    harness.Client.Player.Missions[860].Objectives[1].State);
                HitCluster(harness, clusters[index], 100);
                Assert.AreEqual((uint)index + 1, harness.Client.Player.Missions[860].Objectives[1].Counters[0]);
            }
            Assert.AreEqual(MissionObjectiveState.Completed,
                harness.Client.Player.Missions[860].Objectives[1].State,
                "The active native objective says four, not the conflicting six-count log/opening.");
            CompleteAndCheckReward(harness, sten, 860, 0, 18000, 800);
            Assert.AreEqual(1U, Held(harness, 3315));
        }

        private static void AssertRewardedSacsSceneRetired(WildernessRuntimeTestHarness harness,
            string runId, string assignmentId, IReadOnlyList<DynamicObject> props)
        {
            using (var unit = harness.CreateChar())
            {
                var scene = unit.CharacterMissions.Runtime.Scene(runId);
                Assert.IsNotNull(scene);
                Assert.AreEqual(860U, scene.MissionId);
                Assert.AreEqual(harness.Client.Player.Id, scene.OwnerCharacterId);
                Assert.AreEqual(assignmentId, scene.AssignmentId);
                Assert.AreEqual("Ended", scene.Status,
                    "Reward completion must retire this exact assignment scene before any disconnect cleanup.");
                var assignmentScenes = unit.CharacterMissions.Runtime.Scenes(harness.Client.Player.Id, 860)
                    .Where(candidate => candidate.AssignmentId == assignmentId).ToArray();
                Assert.AreEqual(1, assignmentScenes.Length);
                Assert.AreEqual(runId, assignmentScenes[0].RunId);
            }
            foreach (var prop in props)
            {
                Assert.IsFalse(prop.IsInWorld);
                Assert.IsFalse(harness.Map.DynamicObjects.Contains(prop));
            }
            Assert.IsFalse(harness.Map.DynamicObjects.Any(prop => prop.SceneMissionId == 860 &&
                prop.SceneOwnerCharacterId == harness.Client.Player.Id),
                "The rewarded assignment must not retain or recreate any egg-cluster world props.");
        }

        internal static WildernessRuntimeTestHarness CreateMigrated(params uint[] completed)
        {
            var harness = WildernessRuntimeTestHarness.Create();
            try
            {
                foreach (var id in new uint[] { 425, 679, 451, 697, 860, 758, 787, 769 })
                    Assert.IsTrue(harness.Manager.LoadedMissions.TryGetValue(id, out var mission) &&
                        mission.IsOperational, $"The W6 provider must install operational mission{id}.");
                SetQualification(harness, harness.Client.Player.Id, CharacterStartingExperienceState.Completed);
                SeedCompleted(harness, completed);
                return harness;
            }
            catch
            {
                harness.Dispose();
                throw;
            }
        }

        internal static void SetQualification(WildernessRuntimeTestHarness harness, uint characterId,
            CharacterStartingExperienceState state)
        {
            using var unit = harness.CreateChar();
            if (unit.CharacterStartingExperience.Get(characterId) == null)
                unit.CharacterStartingExperience.Add(new CharacterStartingExperienceEntry(
                    characterId, BootcampMissionDataV1.Revision, state));
            else
                unit.CharacterStartingExperience.SetState(characterId, state);
            var player = characterId == harness.Client.Player.Id
                ? harness.Client.Player
                : harness.Map.ClientList.Single(client => client.Player?.Id == characterId).Player;
            player.StartingExperienceCompleted =
                MissionRequirementFactsAdapter.HasCompletedStartingExperience(unit, characterId);
        }

        private static void SeedCompleted(WildernessRuntimeTestHarness harness, params uint[] completed)
        {
            using (var unit = harness.CreateChar())
                unit.ExecuteTransaction(() =>
                {
                    foreach (var mission in completed.Distinct())
                        unit.CharacterMissions.Runtime.Add(new CharacterMissionHistoryEntry
                        {
                            CharacterId = harness.Client.Player.Id,
                            MissionId = mission,
                            AssignmentId = Guid.NewGuid().ToString("N"),
                            AssignmentGeneration = 1,
                            ContentRevision = WildernessMissionDataV1.Revision,
                            CompletedAtUtc = harness.UtcNow,
                            RewardedAtUtc = harness.UtcNow,
                            Outcome = (uint)MissionState.Completed,
                            Rewarded = true
                        });
                });
            Reload(harness);
        }

        internal static void Reload(WildernessRuntimeTestHarness harness)
        {
            using (var unit = harness.CreateChar())
            {
                MissionJournalAdapter.ApplyHistory(harness.Client.Player,
                    unit.CharacterMissions.Runtime.History(harness.Client.Player.Id));
                harness.Manager.Hydrate(harness.Client.Player,
                    unit.CharacterMissions.Get(harness.Client.Player.Id),
                    unit.CharacterMissionProgress.Get(harness.Client.Player.Id));
            }
            InventoryManager.Instance.InitCharacterInventory(harness.Client);
            harness.Drain();
        }

        internal static Creature RequireNpc(WildernessRuntimeTestHarness harness, uint spawnId, uint creatureId)
        {
            var npc = harness.Npc(spawnId);
            Assert.IsNotNull(npc, $"Migrated creature {creatureId} / spawn {spawnId} must be a real public NPC.");
            Assert.AreEqual(creatureId, npc.DbId);
            return npc;
        }

        private static Creature RecoverSamples(WildernessRuntimeTestHarness harness)
        {
            harness.SpawnWorld(173, 178);
            var eleanor = RequireNpc(harness, 173, 94);
            var samuel = RequireNpc(harness, 178, 95);
            harness.MoveTo(eleanor.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, eleanor.EntityId, 425));
            harness.MoveTo(samuel.Position);
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, samuel.EntityId, 425, 1, 1));
            LootOne(harness, 169, 90, 620);
            return samuel;
        }

        private static void ExchangeSamples(WildernessRuntimeTestHarness harness, Creature samuel)
        {
            harness.MoveTo(samuel.Position);
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, samuel.EntityId, 425, 5, 1));
            Assert.AreEqual(0U, Held(harness, 620));
            Assert.AreEqual(1U, Held(harness, 622));
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, samuel.EntityId, 425, 5, 1));
            Assert.AreEqual(1U, Held(harness, 622));
            using var unit = harness.CreateChar();
            var ownership = unit.CharacterMissionItems.GetOwned(harness.Client.Player.Id)
                .Single(item => item.MissionId == 425);
            Assert.AreEqual("combined-samples", ownership.ItemKey);
            Assert.AreEqual(1U, ownership.Quantity);
        }

        internal static void LootOne(WildernessRuntimeTestHarness harness, uint spawnId, uint creatureId,
            uint template, Action beforePickup = null)
        {
            var pool = harness.Map.SpawnPools.Single(candidate => candidate.DbId == spawnId);
            harness.SpawnWorldAfter(pool.RespawnTime, spawnId);
            var creature = harness.Map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList)
                .Distinct().FirstOrDefault(candidate => candidate.DbId == creatureId &&
                    candidate.SpawnPool?.DbId == spawnId && candidate.State != CharacterState.Dead);
            Assert.IsNotNull(creature, $"Expected living creature{creatureId} in migrated pool{spawnId}.");
            harness.MoveTo(creature.Position);
            var held = Held(harness, template);
            creature.Attributes[Attributes.Health].Current = 0;
            harness.Creatures.HandleCreatureKill(harness.Map, creature, harness.Client.Player);
            Assert.IsTrue(harness.Map.LootDispensers.TryGetValue(creature.CorpseLootEntityId, out var loot));
            Assert.IsTrue(loot.LootItems.Any(item => item.ItemTemplateId == template));
            Assert.AreEqual(held, Held(harness, template), "A death event is not an inventory pickup.");
            beforePickup?.Invoke();
            LootDispenserManager.Instance.RequestLootAllFromCorpse(harness.Client,
                new RequestLootAllFromCorpsePacket { EntityId = loot.EntityId });
            Assert.AreEqual(held + 1, Held(harness, template));
        }

        internal static uint Held(WildernessRuntimeTestHarness harness, uint template)
        {
            using var unit = harness.CreateChar();
            return unit.CharacterInventories.GetItems(harness.Client.AccountEntry.Id)
                .Where(row => row.CharacterId == harness.Client.Player.Id)
                .Select(row => unit.Items.GetItem(row.ItemId))
                .Where(item => item.ItemTemplateId == template)
                .Aggregate(0U, (count, item) => count + item.StackSize);
        }

        private static ConversePacket OpenConversation(WildernessRuntimeTestHarness harness, Creature npc)
        {
            harness.MoveTo(npc.Position);
            harness.Drain();
            new NpcManager(harness, harness.Manager).RequestNpcConverse(harness.Client,
                new RequestNPCConversePacket { EntityId = npc.EntityId });
            return harness.Drain().OfType<ConversePacket>().Single();
        }

        internal static RewardInfo Offer(WildernessRuntimeTestHarness harness, Creature npc, uint missionId)
        {
            var packet = OpenConversation(harness, npc);
            Assert.IsTrue(packet.ConvoDataDict.TryGetValue(ConversationType.MissionDispense, out var value));
            var offers = value as Dictionary<uint, MissionInfo>;
            Assert.IsNotNull(offers);
            Assert.IsTrue(offers.ContainsKey(missionId), $"Native conversation did not offer mission{missionId}.");
            return offers[missionId].MissionConstantData.RewardInfo;
        }

        internal static void AssertSelectable(RewardInfo preview, params (uint Template, uint Class)[] expected)
        {
            CollectionAssert.AreEqual(expected.Select(item => item.Template).ToArray(),
                preview.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            CollectionAssert.AreEqual(expected.Select(item => item.Class).ToArray(),
                preview.SelectableReward.Select(item => (uint)item.Class).ToArray());
            Assert.IsTrue(preview.SelectableReward.All(item => item.Quantity == 1 && item.ModuleIds.Count == 0));
        }

        private static void AssertFixed(RewardInfo preview, params (uint Template, uint Quantity)[] expected)
        {
            Assert.AreEqual(0, preview.SelectableReward.Count);
            CollectionAssert.AreEquivalent(expected, preview.FixedReward.FixedItems
                .Select(item => (item.ItemTemplateId, item.Quantity)).ToArray());
        }

        internal static void CompleteAndCheckReward(WildernessRuntimeTestHarness harness, Creature receiver,
            uint missionId, int? choice, uint experience, uint credits)
        {
            harness.MoveTo(receiver.Position);
            var before = harness.Context.ReadRewardTotals();
            Assert.IsTrue(harness.Manager.CompleteOfferedMission(harness.Client, receiver.EntityId, missionId, choice));
            var after = harness.Context.ReadRewardTotals();
            Assert.AreEqual(before.Experience + experience, after.Experience);
            Assert.AreEqual(before.Credits + (int)credits, after.Credits);
            Assert.AreEqual(MissionState.Completed, harness.Client.Player.Missions[missionId].State);
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, receiver.EntityId, missionId, choice));
            Assert.AreEqual(after, harness.Context.ReadRewardTotals());
        }

        private static IReadOnlyList<Item> FillConsumableBag(WildernessRuntimeTestHarness harness)
        {
            var template = EntityClassManager.Instance.LoadedEntityClasses[(EntityClasses)21977].ItemTemplates[44921];
            Assert.AreEqual((InventoryCategory)2, template.InventoryCategory);
            var items = new List<Item>();
            for (uint slot = 50; slot < 100; slot++)
            {
                if (harness.Client.Player.Inventory.PersonalInventory[(int)slot] != 0)
                    continue;
                var item = ItemManager.StageItem(template, 1, "");
                item.OwnerId = harness.Client.Player.Id;
                item.OwnerSlotId = slot;
                using (var unit = harness.CreateChar())
                {
                    item.Id = unit.Items.CreateItem(item);
                    unit.CharacterInventories.AddInvItem(harness.Client.AccountEntry.Id, harness.Client.Player.Id,
                        (uint)InventoryType.Personal, slot, item.Id);
                }
                EntityManager.Instance.RegisterEntity(item.EntityId, EntityType.Item);
                EntityManager.Instance.RegisterItem(item.EntityId, item);
                harness.Client.Player.Inventory.PersonalInventory[(int)slot] = item.EntityId;
                items.Add(item);
            }
            Assert.IsTrue(items.Count >= 6);
            Assert.IsTrue(harness.Client.Player.Inventory.PersonalInventory.Skip(50).Take(50).All(id => id != 0));
            return items;
        }

        private static void HitCluster(WildernessRuntimeTestHarness harness, DynamicObject cluster, int damage)
        {
            harness.MoveTo(cluster.Position);
            var action = new ActionData(harness.Client.Player, ActionId.WeaponAttack, 133, 0)
            {
                TargetId = cluster.EntityId
            };
            MissileManager.Instance.MissileLaunch(harness.Map, action, damage);
            Assert.AreEqual(1, harness.Map.QueuedMissiles.Count);
            MissileManager.Instance.DoWork(harness.Map, 1000);
        }

        private static RequestPerformAbilityPacket NativeMedpackRequest(ulong itemId, int actionLevel)
        {
            using var stream = new MemoryStream();
            using (var writer = new PythonWriter(new BinaryWriter(stream, Encoding.UTF8, true)))
            {
                writer.WriteTuple(4);
                writer.WriteInt(419);
                writer.WriteInt(actionLevel);
                writer.WriteNoneStruct();
                writer.WriteULong(itemId);
            }
            stream.Position = 0;
            using var reader = new BinaryReader(stream);
            var request = new RequestPerformAbilityPacket();
            request.Read(reader);
            Assert.AreEqual(stream.Length, stream.Position);
            return request;
        }

        internal static void AssertConnected(WildernessRuntimeTestHarness harness, Vector3 from, Vector3 to,
            string message)
        {
            var path = harness.Map.NavMesh.FindPath(from, to, out var complete);
            Assert.IsNotNull(path, message);
            Assert.IsTrue(complete && path.Count > 0 && Vector3.Distance(path[^1], to) < 5, message);
        }

        private static Item AddUnboundNativeItem(WildernessRuntimeTestHarness harness, uint templateId, uint classId,
            uint quantity = 1)
        {
            var template = EntityClassManager.Instance.LoadedEntityClasses[(EntityClasses)classId].ItemTemplates[templateId];
            var firstSlot = ((int)template.InventoryCategory - 1) * 50;
            var slot = Enumerable.Range(firstSlot, 50)
                .First(index => harness.Client.Player.Inventory.PersonalInventory[index] == 0);
            var item = ItemManager.StageItem(template, quantity, "");
            item.OwnerId = harness.Client.Player.Id;
            item.OwnerSlotId = (uint)slot;
            using (var unit = harness.CreateChar())
            {
                item.Id = unit.Items.CreateItem(item);
                unit.CharacterInventories.AddInvItem(harness.Client.AccountEntry.Id, harness.Client.Player.Id,
                    (uint)InventoryType.Personal, (uint)slot, item.Id);
            }
            EntityManager.Instance.RegisterEntity(item.EntityId, EntityType.Item);
            EntityManager.Instance.RegisterItem(item.EntityId, item);
            harness.Client.Player.Inventory.PersonalInventory[slot] = item.EntityId;
            return item;
        }

        private static void PrepareEquipmentCharacter(WildernessRuntimeTestHarness harness,
            CharacterClass profession, uint skillId)
        {
            var progression = ManifestationManager.Instance;
            using (var unit = harness.CreateChar())
            {
                unit.Characters.UpdateCharacterLevel(harness.Client.Player.Id, 10);
                unit.Characters.UpdateCharacterClass(harness.Client.Player.Id, (uint)profession);
                unit.Characters.UpdateCharacterActiveWeapon(harness.Client.Player.Id, 0);
                if (skillId != 0)
                    unit.CharacterSkills.AddOrUpdate(harness.Client.Player.Id, skillId,
                        progression.SkillIdx2AbilityId[progression.GetSkillIndexById((int)skillId)], 1);
                unit.Complete();
            }
            harness.Client.Player.Level = 10;
            harness.Client.Player.Class = (uint)profession;
            harness.Client.Player.ActiveWeapon = 0;
            harness.Client.Player.Skills = harness.Maps.GetPlayerSkills(harness.Client.Player.Id);
            InventoryManager.Instance.InitCharacterInventory(harness.Client);
            progression.UpdateStatsValues(harness.Client, true);
            harness.Drain();
        }

        private static void AssertStoredItem(WildernessRuntimeTestHarness harness, uint itemId,
            InventoryType inventory, uint slot)
        {
            using var unit = harness.CreateChar();
            var row = unit.CharacterInventories.GetItems(harness.Client.AccountEntry.Id)
                .Single(item => item.CharacterId == harness.Client.Player.Id && item.ItemId == itemId);
            Assert.AreEqual((uint)inventory, row.InventoryType);
            Assert.AreEqual(slot, row.SlotId);
        }

        private static Creature SpawnEquipmentTarget(WildernessRuntimeTestHarness harness)
        {
            harness.SpawnWorld(46);
            var target = harness.Map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList)
                .Distinct().FirstOrDefault(creature => creature.DbId == 3 &&
                    creature.SpawnPool?.DbId == 46 && creature.State != CharacterState.Dead);
            Assert.IsNotNull(target, "Use a real migrated Thrax combat actor, not a test-only class.");
            harness.MoveTo(target.Position + new Vector3(0, 0, 2));
            return target;
        }

        private static void VerifyPhysicalWeaponFire(WildernessRuntimeTestHarness harness, Item weapon)
        {
            var profile = EntityClassManager.Instance.GetWeaponClassInfo(weapon);
            Assert.AreEqual((EntityClasses)3147, profile.AmmoClassId);
            AddUnboundNativeItem(harness, 28, 3147, 100);
            ManifestationManager.Instance.WeaponReady(harness.Client, true);
            ManifestationManager.Instance.RequestWeaponReload(harness.Client, false);
            Assert.IsTrue(harness.Map.PerformRecovery.Any(action => action.Actor == harness.Client.Player &&
                action.ActionId == ActionId.WeaponReload));
            ActorActionManager.Instance.DoWork(harness.Map, 10000);
            Assert.IsTrue(weapon.CurrentAmmo > 0);
            Assert.IsFalse(harness.Map.PerformRecovery.Any(action => action.Actor == harness.Client.Player &&
                action.ActionId == ActionId.WeaponReload));
            var target = SpawnEquipmentTarget(harness);
            harness.Client.Player.Target = target.EntityId;
            var health = target.Attributes[Attributes.Health].Current;
            var armor = target.Attributes[Attributes.Armor].Current;
            var ammunition = weapon.CurrentAmmo;
            MissileManager.Instance.RequestWeaponAttack(harness.Client, new RequestWeaponAttackPacket
            {
                ActionId = profile.WeaponAttackActionId, ActionArgId = checked((int)profile.WeaponAttackArgId),
                TargetId = (long)target.EntityId
            });
            var shot = harness.Map.QueuedMissiles.Single();
            Assert.AreSame(target, shot.TargetActor);
            Assert.IsTrue(shot.DamageA > 0);
            MissileManager.Instance.DoWork(harness.Map, 1000);
            Assert.IsTrue(target.Attributes[Attributes.Health].Current < health ||
                target.Attributes[Attributes.Armor].Current < armor,
                "The equipped reward must deal real damage through the native weapon request.");
            Assert.IsTrue(weapon.CurrentAmmo < ammunition);
            using var unit = harness.CreateChar();
            Assert.AreEqual(weapon.CurrentAmmo, unit.Items.GetItem(weapon.Id).AmmoCount);
        }
    }
}
