using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets.Inventory.Client;
    using Rasa.Packets.Mission.Server;
    using Rasa.Repositories.Char.CharacterMissionProgress;
    using Rasa.Repositories.UnitOfWork;
    using Rasa.Structures;

    [TestClass]
    [DoNotParallelize]
    public class BootcampGearingUpTests
    {
        private static readonly uint[] CrateTemplateIds = { 13066, 13096, 13156, 13186, 13713, 28 };

        [TestMethod]
        [DynamicData(nameof(ReconnectBoundaries))]
        public void GearingUpReconnectHydratesEachBoundaryWithoutDuplicatingGrants(
            string boundaryName)
        {
            var boundary = GetReconnectBoundary(boundaryName);
            using var harness = BootcampRuntimeTestHarness.Create();
            var actors = SeedActors(harness);
            harness.SeedMission(
                harness.Client.Player.Id,
                BootcampRuntimeTestHarness.MissionInitiation,
                (uint)MissionState.Completed,
                false);
            AdvanceToBoundary(harness, actors, boundary.Stage);
            var durableBeforeReconnect =
                harness.Context.ReadProgress(BootcampRuntimeTestHarness.MissionGearingUp)
                    .Missions[BootcampRuntimeTestHarness.MissionGearingUp];
            var rewardTotalsBeforeReconnect = harness.Context.ReadRewardTotals();
            var previousClient = harness.Client;
            var previousManager = harness.Manager;

            harness.ReconnectFresh();

            Assert.AreNotSame(previousClient, harness.Client);
            Assert.AreNotSame(previousManager, harness.Manager);
            var mission = harness.Client.Player.Missions[BootcampRuntimeTestHarness.MissionGearingUp];
            AssertMissionOrder(mission, boundary.ExpectedStates.ToArray());
            Assert.AreEqual(boundary.Completeable, mission.Completeable);
            AssertObjectiveProgressMatchesSnapshot(mission, durableBeforeReconnect, boundary.ExpectedStates);
            Assert.AreEqual(
                rewardTotalsBeforeReconnect.Experience,
                harness.Context.ReadRewardTotals().Experience);
            Assert.AreEqual(
                rewardTotalsBeforeReconnect.Credits,
                harness.Context.ReadRewardTotals().Credits);
            AssertTemplateCounts(
                harness.ReadOwnedTemplateCounts(CrateTemplateIds),
                boundary.ExpectsCrateGrant ? 1 : 0);
            AssertLightningGrantCounts(
                harness.ReadLightningGrantCounts(),
                boundary.ExpectsLightningGrant ? 1 : 0);
            boundary.AssertAvailability(harness);
        }

        [TestMethod]
        public void GearingUpPreservesObjectiveOrderAcrossReconnectsAndRejectsWrongNpcOrItem()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var mcAllister = harness.AddNpc(BootcampRuntimeTestHarness.MajorMcAllisterCreatureId);
            var delessio = harness.AddNpc(
                BootcampRuntimeTestHarness.CaptainDelessioCreatureId,
                BootcampRuntimeTestHarness.CaptainDelessioPackageId);
            var hartmann = harness.AddNpc(
                BootcampRuntimeTestHarness.CorporalHartmannCreatureId,
                BootcampRuntimeTestHarness.CorporalHartmannPackageId);

            harness.SeedMission(harness.Client.Player.Id, BootcampRuntimeTestHarness.MissionInitiation, (uint)MissionState.Completed, false);

            Assert.IsFalse(harness.Manager.AcceptOfferedMission(
                harness.Client,
                delessio.EntityId,
                BootcampRuntimeTestHarness.MissionGearingUp));
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(
                harness.Client,
                mcAllister.EntityId,
                BootcampRuntimeTestHarness.MissionGearingUp));
            var gained = harness.Context.Drain().OfType<MissionGainedPacket>().Single();
            CollectionAssert.AreEqual(
                new[]
                {
                    (4U, (uint)1, MissionObjectiveState.Incomplete)
                },
                gained.MissionInfo.ObjectivesList
                    .Select(objective => (objective.ObjectiveId, objective.Ordinal, objective.State))
                    .ToArray());
            AssertMissionOrder(
                harness.Client.Player.Missions[BootcampRuntimeTestHarness.MissionGearingUp],
                (4U, MissionObjectiveState.Incomplete),
                (1U, MissionObjectiveState.Inactive),
                (2U, MissionObjectiveState.Inactive),
                (5U, MissionObjectiveState.Inactive),
                (6U, MissionObjectiveState.Inactive),
                (3U, MissionObjectiveState.Inactive),
                (9U, MissionObjectiveState.Inactive),
                (8U, MissionObjectiveState.Inactive),
                (7U, MissionObjectiveState.Inactive));
            Assert.IsFalse(harness.Manager.AcceptOfferedMission(
                harness.Client,
                mcAllister.EntityId,
                BootcampRuntimeTestHarness.MissionGearingUp));
            Assert.AreEqual(
                0,
                harness.Context.Drain().OfType<MissionGainedPacket>().Count());

            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(
                harness.Client,
                hartmann.EntityId,
                BootcampRuntimeTestHarness.MissionGearingUp,
                4,
                1));
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(
                harness.Client,
                delessio.EntityId,
                BootcampRuntimeTestHarness.MissionGearingUp,
                4,
                1));
            Assert.IsNotNull(BootcampRuntimeTestHarness.FindScenarioObject(
                harness.BootcampMap,
                "bootcamp-equipment-crate"));

            harness.Reconnect();
            AssertMissionOrder(
                harness.Client.Player.Missions[BootcampRuntimeTestHarness.MissionGearingUp],
                (4U, MissionObjectiveState.Completed),
                (1U, MissionObjectiveState.Incomplete),
                (2U, MissionObjectiveState.Inactive),
                (5U, MissionObjectiveState.Inactive),
                (6U, MissionObjectiveState.Inactive),
                (3U, MissionObjectiveState.Inactive),
                (9U, MissionObjectiveState.Inactive),
                (8U, MissionObjectiveState.Inactive),
                (7U, MissionObjectiveState.Inactive));
            Assert.IsNotNull(BootcampRuntimeTestHarness.FindScenarioObject(
                harness.BootcampMap,
                "bootcamp-equipment-crate"));

            BootcampRuntimeTestHarness.LootEquipmentCrate(harness);
            var afterGrant = harness.Context.ReadRewardTotals();
            AssertItemTemplatesPresent(harness, 13066, 13096, 13156, 13186, 13713);

            Assert.IsFalse(harness.Manager.RecordProgress(
                harness.Client,
                MissionProgressEvent.Interaction(29877)));
            var afterDuplicate = harness.Context.ReadRewardTotals();
            Assert.AreEqual(afterGrant.ItemCount, afterDuplicate.ItemCount);

            var inventory = new InventoryManager(harness.Context, harness.Manager);
            PrepareEquipping(harness);
            var unrelated = CreateUnrelatedArmorItem(harness, inventory);
            Assert.IsFalse(harness.Manager.RecordProgress(
                harness.Client,
                MissionProgressEvent.ItemEquipped(
                    (uint)unrelated.ItemTemplate.Class,
                    unrelated.ItemTemplate.ItemTemplateId)));
            Assert.AreEqual(
                MissionObjectiveState.Incomplete,
                harness.Client.Player.Missions[BootcampRuntimeTestHarness.MissionGearingUp].Objectives[2].State);

            Assert.IsTrue(RecordTemplateEquipProgress(harness, 13066));
            Assert.AreEqual(
                MissionObjectiveState.Completed,
                harness.Client.Player.Missions[BootcampRuntimeTestHarness.MissionGearingUp].Objectives[2].State);

            harness.Reconnect();
            AssertMissionOrder(
                harness.Client.Player.Missions[BootcampRuntimeTestHarness.MissionGearingUp],
                (4U, MissionObjectiveState.Completed),
                (1U, MissionObjectiveState.Completed),
                (2U, MissionObjectiveState.Completed),
                (5U, MissionObjectiveState.Incomplete),
                (6U, MissionObjectiveState.Inactive),
                (3U, MissionObjectiveState.Inactive),
                (9U, MissionObjectiveState.Inactive),
                (8U, MissionObjectiveState.Inactive),
                (7U, MissionObjectiveState.Inactive));

            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(
                harness.Client,
                BootcampRuntimeTestHarness.FindNpcByPackage(
                    harness.BootcampMap,
                    BootcampRuntimeTestHarness.CaptainDelessioPackageId).EntityId,
                BootcampRuntimeTestHarness.MissionGearingUp,
                5,
                1));
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(
                harness.Client,
                BootcampRuntimeTestHarness.FindNpcByPackage(
                    harness.BootcampMap,
                    BootcampRuntimeTestHarness.CorporalHartmannPackageId).EntityId,
                BootcampRuntimeTestHarness.MissionGearingUp,
                6,
                1));

            harness.Reconnect();
            AssertMissionOrder(
                harness.Client.Player.Missions[BootcampRuntimeTestHarness.MissionGearingUp],
                (4U, MissionObjectiveState.Completed),
                (1U, MissionObjectiveState.Completed),
                (2U, MissionObjectiveState.Completed),
                (5U, MissionObjectiveState.Completed),
                (6U, MissionObjectiveState.Completed),
                (3U, MissionObjectiveState.Incomplete),
                (9U, MissionObjectiveState.Inactive),
                (8U, MissionObjectiveState.Inactive),
                (7U, MissionObjectiveState.Inactive));
        }

        [TestMethod]
        public void GearingUpCombatStagesGrantLightningOnceAndRequireTheCorrectPracticeTargetAction()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var mcAllister = harness.AddNpc(BootcampRuntimeTestHarness.MajorMcAllisterCreatureId);
            var delessio = harness.AddNpc(
                BootcampRuntimeTestHarness.CaptainDelessioCreatureId,
                BootcampRuntimeTestHarness.CaptainDelessioPackageId);
            var hartmann = harness.AddNpc(
                BootcampRuntimeTestHarness.CorporalHartmannCreatureId,
                BootcampRuntimeTestHarness.CorporalHartmannPackageId);
            var deSimone = harness.AddNpc(
                BootcampRuntimeTestHarness.CorporalDeSimoneCreatureId,
                BootcampRuntimeTestHarness.CorporalDeSimonePackageId);

            harness.SeedMission(harness.Client.Player.Id, BootcampRuntimeTestHarness.MissionInitiation, (uint)MissionState.Completed, false);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(
                harness.Client,
                mcAllister.EntityId,
                BootcampRuntimeTestHarness.MissionGearingUp));
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(
                harness.Client,
                delessio.EntityId,
                BootcampRuntimeTestHarness.MissionGearingUp,
                4,
                1));
            BootcampRuntimeTestHarness.LootEquipmentCrate(harness);
            var inventory = new InventoryManager(harness.Context, harness.Manager);
            PrepareEquipping(harness);
            Assert.IsTrue(RecordTemplateEquipProgress(harness, 13066));
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(
                harness.Client,
                delessio.EntityId,
                BootcampRuntimeTestHarness.MissionGearingUp,
                5,
                1));
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(
                harness.Client,
                hartmann.EntityId,
                BootcampRuntimeTestHarness.MissionGearingUp,
                6,
                1));

            Assert.IsTrue(harness.Manager.RecordProgress(
                harness.Client,
                MissionProgressEvent.ObjectHit(
                    PracticeTargetManager.EntityClassId,
                    (uint)ActionId.WeaponAttack)));

            Assert.AreEqual(
                MissionObjectiveState.Completed,
                harness.Client.Player.Missions[BootcampRuntimeTestHarness.MissionGearingUp].Objectives[3].State);

            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(
                harness.Client,
                hartmann.EntityId,
                BootcampRuntimeTestHarness.MissionGearingUp,
                9,
                1));
            using (var unit = harness.Context.CreateChar())
            {
                Assert.AreEqual(
                    1,
                    unit.CharacterSkills.GetCharacterSkills(harness.Client.Player.Id)
                        .Count(entry => entry.SkillId == (uint)SkillId.Lightning && entry.AbilityId == (int)ActionId.AaRecruitLightning && entry.SkillLevel == 1));
                Assert.AreEqual(
                    1,
                    unit.CharacterAbilityDrawers.GetCharacterAbilities(harness.Client.Player.Id)
                        .Count(entry => entry.AbilityId == (int)ActionId.AaRecruitLightning && entry.AbilityLevel == 1));
            }

            harness.Reconnect();
            using (var unit = harness.Context.CreateChar())
            {
                Assert.AreEqual(
                    1,
                    unit.CharacterSkills.GetCharacterSkills(harness.Client.Player.Id)
                        .Count(entry => entry.SkillId == (uint)SkillId.Lightning && entry.AbilityId == (int)ActionId.AaRecruitLightning && entry.SkillLevel == 1));
                Assert.AreEqual(
                    1,
                    unit.CharacterAbilityDrawers.GetCharacterAbilities(harness.Client.Player.Id)
                        .Count(entry => entry.AbilityId == (int)ActionId.AaRecruitLightning && entry.AbilityLevel == 1));
            }

            Assert.IsFalse(harness.Manager.RecordProgress(
                harness.Client,
                MissionProgressEvent.ObjectHit(
                    PracticeTargetManager.EntityClassId,
                    (uint)ActionId.WeaponAttack)));
            Assert.IsTrue(harness.Manager.RecordProgress(
                harness.Client,
                MissionProgressEvent.ObjectHit(
                    PracticeTargetManager.EntityClassId,
                    (uint)ActionId.AaRecruitLightning)));

            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(
                harness.Client,
                BootcampRuntimeTestHarness.FindNpcByPackage(
                    harness.BootcampMap,
                    BootcampRuntimeTestHarness.CorporalHartmannPackageId).EntityId,
                BootcampRuntimeTestHarness.MissionGearingUp,
                7,
                1));

            var beforeReward = harness.Context.ReadRewardTotals();
            var currentDeSimone = BootcampRuntimeTestHarness.FindNpcByPackage(
                harness.BootcampMap,
                BootcampRuntimeTestHarness.CorporalDeSimonePackageId);
            Assert.IsTrue(harness.Manager.CompleteOfferedMission(
                harness.Client,
                currentDeSimone.EntityId,
                BootcampRuntimeTestHarness.MissionGearingUp,
                selectionIndex: null,
                rating: null));
            var afterReward = harness.Context.ReadRewardTotals();
            Assert.AreEqual(beforeReward.Experience + 1250U, afterReward.Experience);
            Assert.AreEqual(beforeReward.Credits + 200, afterReward.Credits);

            Assert.IsFalse(harness.Manager.RewardOfferedMission(
                harness.Client,
                currentDeSimone.EntityId,
                BootcampRuntimeTestHarness.MissionGearingUp,
                selectionIndex: null,
                rating: null));
            var finalReward = harness.Context.ReadRewardTotals();
            Assert.AreEqual(afterReward.Experience, finalReward.Experience);
            Assert.AreEqual(afterReward.Credits, finalReward.Credits);
        }

        [TestMethod]
        public void CrateLoadoutIncludesCompatibleAmmoAndCanReloadThenFireTheGrantedRifle()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var mcAllister = harness.AddNpc(BootcampRuntimeTestHarness.MajorMcAllisterCreatureId);
            var delessio = harness.AddNpc(
                BootcampRuntimeTestHarness.CaptainDelessioCreatureId,
                BootcampRuntimeTestHarness.CaptainDelessioPackageId);

            harness.SeedMission(
                harness.Client.Player.Id,
                BootcampRuntimeTestHarness.MissionInitiation,
                (uint)MissionState.Completed,
                false);

            Assert.IsTrue(harness.Manager.AcceptOfferedMission(
                harness.Client,
                mcAllister.EntityId,
                BootcampRuntimeTestHarness.MissionGearingUp));
            harness.Drain();

            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(
                harness.Client,
                delessio.EntityId,
                BootcampRuntimeTestHarness.MissionGearingUp,
                4,
                1));
            BootcampRuntimeTestHarness.LootEquipmentCrate(harness);

            AssertItemTemplatesPresent(harness, 13066, 13096, 13156, 13186, 13713, 28);

            PrepareEquipping(harness);
            while (harness.Client.Player.Inventory.WeaponDrawer.Count < 5)
                harness.Client.Player.Inventory.WeaponDrawer.Add(0);
            while (harness.Client.Player.Inventory.EquippedInventory.Count < 17)
                harness.Client.Player.Inventory.EquippedInventory.Add(0);
            var rifleSlot = FindPersonalSlotByTemplate(harness.Client, 13713);
            var rifleEntityId = harness.Client.Player.Inventory.PersonalInventory[(int)rifleSlot];
            harness.Client.Player.Inventory.PersonalInventory[(int)rifleSlot] = 0;
            harness.Client.Player.Inventory.WeaponDrawer[0] = rifleEntityId;

            var manifestation = new ManifestationManager(harness.Context);
            var weapon = EntityManager.Instance.GetItem(harness.Client.Player.Inventory.WeaponDrawer[0]);
            Assert.IsNotNull(weapon);
            harness.Client.Player.ActiveWeapon = 0;
            harness.Client.Player.Inventory.EquippedInventory[13] = weapon.EntityId;
            manifestation.WeaponReady(harness.Client, true);
            Assert.AreEqual(0U, weapon.CurrentAmmo);
            Assert.IsFalse(manifestation.PlayerTryFireWeapon(harness.Client));
            var reload = harness.BootcampMap.PerformRecovery.Single(action => action.ActionId == ActionId.WeaponReload);
            manifestation.WeaponReload(reload);
            harness.BootcampMap.PerformRecovery.Remove(reload);
            manifestation.WeaponReady(harness.Client, true);

            Assert.IsTrue(manifestation.PlayerTryFireWeapon(harness.Client));
            Assert.AreEqual(19U, weapon.CurrentAmmo);
            using var unit = harness.Context.CreateChar();
            Assert.AreEqual(19U, unit.Items.GetItem(weapon.Id).AmmoCount);
        }

        private static void AssertMissionOrder(
            MissionLog mission,
            params (uint ObjectiveId, MissionObjectiveState State)[] expected) =>
            BootcampRuntimeTestHarness.AssertObjectiveStates(mission, expected);

        public static IEnumerable<object[]> ReconnectBoundaries()
        {
            foreach (var fixture in GetReconnectBoundaryFixtures())
                yield return new object[] { fixture.Name };
        }

        private static ReconnectBoundaryFixture GetReconnectBoundary(string name) =>
            GetReconnectBoundaryFixtures().Single(
                fixture => string.Equals(fixture.Name, name, StringComparison.Ordinal));

        private static IEnumerable<ReconnectBoundaryFixture> GetReconnectBoundaryFixtures()
        {
            yield return Fixture(
                "Acceptance",
                MissionBoundary.Accepted,
                completeable: false,
                expectsCrateGrant: false,
                expectsLightningGrant: false,
                availability: harness =>
                {
                    AssertNpcStatus(
                        harness,
                        BootcampRuntimeTestHarness.CaptainDelessioPackageId,
                        ConversationStatus.ObjectivComplete);
                    Assert.IsNotNull(BootcampRuntimeTestHarness.FindScenarioObject(
                        harness.BootcampMap,
                        "bootcamp-equipment-crate"));
                },
                (4U, MissionObjectiveState.Incomplete),
                (1U, MissionObjectiveState.Inactive),
                (2U, MissionObjectiveState.Inactive),
                (5U, MissionObjectiveState.Inactive),
                (6U, MissionObjectiveState.Inactive),
                (3U, MissionObjectiveState.Inactive),
                (9U, MissionObjectiveState.Inactive),
                (8U, MissionObjectiveState.Inactive),
                (7U, MissionObjectiveState.Inactive));

            yield return Fixture(
                "Delessio greeting",
                MissionBoundary.AfterDelessioGreeting,
                completeable: false,
                expectsCrateGrant: false,
                expectsLightningGrant: false,
                availability: harness =>
                {
                    AssertNpcUnavailable(harness, BootcampRuntimeTestHarness.CaptainDelessioPackageId);
                    Assert.IsNotNull(BootcampRuntimeTestHarness.FindScenarioObject(
                        harness.BootcampMap,
                        "bootcamp-equipment-crate"));
                },
                (4U, MissionObjectiveState.Completed),
                (1U, MissionObjectiveState.Incomplete),
                (2U, MissionObjectiveState.Inactive),
                (5U, MissionObjectiveState.Inactive),
                (6U, MissionObjectiveState.Inactive),
                (3U, MissionObjectiveState.Inactive),
                (9U, MissionObjectiveState.Inactive),
                (8U, MissionObjectiveState.Inactive),
                (7U, MissionObjectiveState.Inactive));

            yield return Fixture(
                "Crate grant",
                MissionBoundary.AfterCrateGrant,
                completeable: false,
                expectsCrateGrant: true,
                expectsLightningGrant: false,
                availability: harness =>
                {
                    AssertNpcUnavailable(harness, BootcampRuntimeTestHarness.CaptainDelessioPackageId);
                    Assert.IsNotNull(BootcampRuntimeTestHarness.FindScenarioObject(
                        harness.BootcampMap,
                        "bootcamp-equipment-crate"));
                },
                (4U, MissionObjectiveState.Completed),
                (1U, MissionObjectiveState.Completed),
                (2U, MissionObjectiveState.Incomplete),
                (5U, MissionObjectiveState.Inactive),
                (6U, MissionObjectiveState.Inactive),
                (3U, MissionObjectiveState.Inactive),
                (9U, MissionObjectiveState.Inactive),
                (8U, MissionObjectiveState.Inactive),
                (7U, MissionObjectiveState.Inactive));

            yield return Fixture(
                "Equip gear",
                MissionBoundary.AfterEquip,
                completeable: false,
                expectsCrateGrant: true,
                expectsLightningGrant: false,
                availability: harness =>
                    AssertNpcStatus(
                        harness,
                        BootcampRuntimeTestHarness.CaptainDelessioPackageId,
                        ConversationStatus.ObjectivComplete),
                (4U, MissionObjectiveState.Completed),
                (1U, MissionObjectiveState.Completed),
                (2U, MissionObjectiveState.Completed),
                (5U, MissionObjectiveState.Incomplete),
                (6U, MissionObjectiveState.Inactive),
                (3U, MissionObjectiveState.Inactive),
                (9U, MissionObjectiveState.Inactive),
                (8U, MissionObjectiveState.Inactive),
                (7U, MissionObjectiveState.Inactive));

            yield return Fixture(
                "Delessio follow-up",
                MissionBoundary.AfterDelessioFollowUp,
                completeable: false,
                expectsCrateGrant: true,
                expectsLightningGrant: false,
                availability: harness =>
                    AssertNpcStatus(
                        harness,
                        BootcampRuntimeTestHarness.CorporalHartmannPackageId,
                        ConversationStatus.ObjectivComplete),
                (4U, MissionObjectiveState.Completed),
                (1U, MissionObjectiveState.Completed),
                (2U, MissionObjectiveState.Completed),
                (5U, MissionObjectiveState.Completed),
                (6U, MissionObjectiveState.Incomplete),
                (3U, MissionObjectiveState.Inactive),
                (9U, MissionObjectiveState.Inactive),
                (8U, MissionObjectiveState.Inactive),
                (7U, MissionObjectiveState.Inactive));

            yield return Fixture(
                "Hartmann greeting",
                MissionBoundary.AfterHartmannGreeting,
                completeable: false,
                expectsCrateGrant: true,
                expectsLightningGrant: false,
                availability: harness =>
                {
                    AssertNpcUnavailable(harness, BootcampRuntimeTestHarness.CorporalHartmannPackageId);
                },
                (4U, MissionObjectiveState.Completed),
                (1U, MissionObjectiveState.Completed),
                (2U, MissionObjectiveState.Completed),
                (5U, MissionObjectiveState.Completed),
                (6U, MissionObjectiveState.Completed),
                (3U, MissionObjectiveState.Incomplete),
                (9U, MissionObjectiveState.Inactive),
                (8U, MissionObjectiveState.Inactive),
                (7U, MissionObjectiveState.Inactive));

            yield return Fixture(
                "Firearm dummy",
                MissionBoundary.AfterFirearmDummy,
                completeable: false,
                expectsCrateGrant: true,
                expectsLightningGrant: false,
                availability: harness =>
                {
                    AssertNpcStatus(
                        harness,
                        BootcampRuntimeTestHarness.CorporalHartmannPackageId,
                        ConversationStatus.ObjectivComplete);
                },
                (4U, MissionObjectiveState.Completed),
                (1U, MissionObjectiveState.Completed),
                (2U, MissionObjectiveState.Completed),
                (5U, MissionObjectiveState.Completed),
                (6U, MissionObjectiveState.Completed),
                (3U, MissionObjectiveState.Completed),
                (9U, MissionObjectiveState.Incomplete),
                (8U, MissionObjectiveState.Inactive),
                (7U, MissionObjectiveState.Inactive));

            yield return Fixture(
                "Lightning grant",
                MissionBoundary.AfterLightningGrant,
                completeable: false,
                expectsCrateGrant: true,
                expectsLightningGrant: true,
                availability: harness =>
                {
                    AssertNpcUnavailable(harness, BootcampRuntimeTestHarness.CorporalHartmannPackageId);
                },
                (4U, MissionObjectiveState.Completed),
                (1U, MissionObjectiveState.Completed),
                (2U, MissionObjectiveState.Completed),
                (5U, MissionObjectiveState.Completed),
                (6U, MissionObjectiveState.Completed),
                (3U, MissionObjectiveState.Completed),
                (9U, MissionObjectiveState.Completed),
                (8U, MissionObjectiveState.Incomplete),
                (7U, MissionObjectiveState.Inactive));

            yield return Fixture(
                "Lightning dummy",
                MissionBoundary.AfterLightningDummy,
                completeable: false,
                expectsCrateGrant: true,
                expectsLightningGrant: true,
                availability: harness =>
                    AssertNpcStatus(
                        harness,
                        BootcampRuntimeTestHarness.CorporalHartmannPackageId,
                        ConversationStatus.ObjectivComplete),
                (4U, MissionObjectiveState.Completed),
                (1U, MissionObjectiveState.Completed),
                (2U, MissionObjectiveState.Completed),
                (5U, MissionObjectiveState.Completed),
                (6U, MissionObjectiveState.Completed),
                (3U, MissionObjectiveState.Completed),
                (9U, MissionObjectiveState.Completed),
                (8U, MissionObjectiveState.Completed),
                (7U, MissionObjectiveState.Incomplete));

            yield return Fixture(
                "Pre-turn-in",
                MissionBoundary.BeforeTurnIn,
                completeable: true,
                expectsCrateGrant: true,
                expectsLightningGrant: true,
                availability: harness =>
                    AssertNpcStatus(
                        harness,
                        BootcampRuntimeTestHarness.CorporalDeSimonePackageId,
                        ConversationStatus.MissionComplete),
                (4U, MissionObjectiveState.Completed),
                (1U, MissionObjectiveState.Completed),
                (2U, MissionObjectiveState.Completed),
                (5U, MissionObjectiveState.Completed),
                (6U, MissionObjectiveState.Completed),
                (3U, MissionObjectiveState.Completed),
                (9U, MissionObjectiveState.Completed),
                (8U, MissionObjectiveState.Completed),
                (7U, MissionObjectiveState.Completed));
        }

        private static void PrepareEquipping(BootcampRuntimeTestHarness.Harness harness)
        {
            harness.Client.Player.AppearanceData ??= new Dictionary<EquipmentData, AppearanceData>();
            typeof(ManifestationManager)
                .GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)!
                .SetValue(null, new ManifestationManager(harness.Context));
        }

        private static Item CreateUnrelatedArmorItem(
            BootcampRuntimeTestHarness.Harness harness,
            InventoryManager inventory)
        {
            const uint templateId = 2800;
            const uint classId = 7000;
            harness.Context.AddRewardTemplate(templateId, classId);
            var classInfo = EntityClassManager.Instance.LoadedEntityClasses[(EntityClasses)classId];
            classInfo.EquipableClassInfo = new EquipableClassInfo((EquipmentData)0);
            var item = harness.Context.CreateInventoryItem(templateId, classId, 1);
            item.OwnerSlotId = 10;
            inventory.AddItemBySlot(
                harness.Client,
                InventoryType.Personal,
                item.EntityId,
                10,
                true,
                true);
            return item;
        }

        private static bool RecordTemplateEquipProgress(
            BootcampRuntimeTestHarness.Harness harness,
            uint templateId)
        {
            var slot = FindPersonalSlotByTemplate(harness.Client, templateId);
            var item = EntityManager.Instance.GetItem(harness.Client.Player.Inventory.PersonalInventory[(int)slot]);
            return item?.ItemTemplate != null &&
                harness.Manager.RecordProgress(
                harness.Client,
                MissionProgressEvent.ItemEquipped(
                    (uint)item.ItemTemplate.Class,
                    item.ItemTemplate.ItemTemplateId));
        }

        private static uint FindPersonalSlotByTemplate(Client client, uint templateId)
        {
            for (uint slot = 0; slot < client.Player.Inventory.PersonalInventory.Count; slot++)
            {
                var entityId = client.Player.Inventory.PersonalInventory[(int)slot];
                if (entityId == 0)
                    continue;
                var item = EntityManager.Instance.GetItem(entityId);
                if (item?.ItemTemplate?.ItemTemplateId == templateId)
                    return slot;
            }

            Assert.Fail($"Could not find personal inventory item template {templateId}.");
            return 0;
        }

        private static void AssertItemTemplatesPresent(
            BootcampRuntimeTestHarness.Harness harness,
            params uint[] expectedTemplateIds)
        {
            var actual = harness.Client.Player.Inventory.PersonalInventory
                .Where(entityId => entityId != 0)
                .Select(entityId => EntityManager.Instance.GetItem(entityId)?.ItemTemplate?.ItemTemplateId ?? 0)
                .Where(templateId => templateId != 0)
                .ToList();

            foreach (var templateId in expectedTemplateIds)
                CollectionAssert.Contains(actual, templateId);
        }

        private static ReconnectBoundaryFixture Fixture(
            string name,
            MissionBoundary stage,
            bool completeable,
            bool expectsCrateGrant,
            bool expectsLightningGrant,
            Action<BootcampRuntimeTestHarness.Harness> availability,
            params (uint ObjectiveId, MissionObjectiveState State)[] expectedStates) =>
            new(
                name,
                stage,
                completeable,
                expectsCrateGrant,
                expectsLightningGrant,
                availability,
                expectedStates);

        private static GearingUpActors SeedActors(BootcampRuntimeTestHarness.Harness harness) =>
            new(
                harness.AddNpc(BootcampRuntimeTestHarness.MajorMcAllisterCreatureId),
                harness.AddNpc(
                    BootcampRuntimeTestHarness.CaptainDelessioCreatureId,
                    BootcampRuntimeTestHarness.CaptainDelessioPackageId),
                harness.AddNpc(
                    BootcampRuntimeTestHarness.CorporalHartmannCreatureId,
                    BootcampRuntimeTestHarness.CorporalHartmannPackageId),
                harness.AddNpc(
                    BootcampRuntimeTestHarness.CorporalDeSimoneCreatureId,
                    BootcampRuntimeTestHarness.CorporalDeSimonePackageId));

        private static void AdvanceToBoundary(
            BootcampRuntimeTestHarness.Harness harness,
            GearingUpActors actors,
            MissionBoundary stage)
        {
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(
                harness.Client,
                actors.McAllister.EntityId,
                BootcampRuntimeTestHarness.MissionGearingUp));
            harness.Context.Drain();
            if (stage == MissionBoundary.Accepted)
                return;

            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(
                harness.Client,
                actors.Delessio.EntityId,
                BootcampRuntimeTestHarness.MissionGearingUp,
                4,
                1));
            if (stage == MissionBoundary.AfterDelessioGreeting)
                return;

            BootcampRuntimeTestHarness.LootEquipmentCrate(harness);
            if (stage == MissionBoundary.AfterCrateGrant)
                return;

            PrepareEquipping(harness);
            Assert.IsTrue(RecordTemplateEquipProgress(harness, 13066));
            if (stage == MissionBoundary.AfterEquip)
                return;

            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(
                harness.Client,
                actors.Delessio.EntityId,
                BootcampRuntimeTestHarness.MissionGearingUp,
                5,
                1));
            if (stage == MissionBoundary.AfterDelessioFollowUp)
                return;

            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(
                harness.Client,
                actors.Hartmann.EntityId,
                BootcampRuntimeTestHarness.MissionGearingUp,
                6,
                1));
            if (stage == MissionBoundary.AfterHartmannGreeting)
                return;

            Assert.IsTrue(harness.Manager.RecordProgress(
                harness.Client,
                MissionProgressEvent.ObjectHit(
                    PracticeTargetManager.EntityClassId,
                    (uint)ActionId.WeaponAttack)));
            if (stage == MissionBoundary.AfterFirearmDummy)
                return;

            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(
                harness.Client,
                actors.Hartmann.EntityId,
                BootcampRuntimeTestHarness.MissionGearingUp,
                9,
                1));
            if (stage == MissionBoundary.AfterLightningGrant)
                return;

            Assert.IsTrue(harness.Manager.RecordProgress(
                harness.Client,
                MissionProgressEvent.ObjectHit(
                    PracticeTargetManager.EntityClassId,
                    (uint)ActionId.AaRecruitLightning)));
            if (stage == MissionBoundary.AfterLightningDummy)
                return;

            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(
                harness.Client,
                actors.Hartmann.EntityId,
                BootcampRuntimeTestHarness.MissionGearingUp,
                7,
                1));
        }

        private static void AssertObjectiveProgressMatchesSnapshot(
            MissionLog mission,
            CharacterMissionProgress snapshot,
            IReadOnlyList<(uint ObjectiveId, MissionObjectiveState State)> expectedStates)
        {
            Assert.AreEqual(expectedStates.Count, snapshot.Objectives.Count);
            foreach (var expected in expectedStates)
            {
                var runtime = mission.Objectives[expected.ObjectiveId];
                var durable = snapshot.Objectives[expected.ObjectiveId];
                Assert.AreEqual((byte)expected.State, durable.State);
                Assert.AreEqual(expected.State, runtime.State);
                CollectionAssert.AreEquivalent(
                    durable.Counters.ToDictionary(pair => pair.Key, pair => pair.Value),
                    runtime.Counters.ToDictionary(pair => pair.Key, pair => pair.Value));
                CollectionAssert.AreEquivalent(
                    durable.ItemCounters.ToDictionary(pair => pair.Key, pair => pair.Value),
                    runtime.ItemCounters.ToDictionary(pair => pair.Key, pair => pair.Value));
            }
        }

        private static void AssertTemplateCounts(
            IReadOnlyDictionary<uint, int> counts,
            int expectedCountPerTemplate)
        {
            foreach (var templateId in CrateTemplateIds)
            {
                var actual = counts.TryGetValue(templateId, out var count) ? count : 0;
                Assert.AreEqual(expectedCountPerTemplate, actual, $"Unexpected count for template {templateId}.");
            }
        }

        private static void AssertLightningGrantCounts(
            (int SkillCount, int TrayCount) counts,
            int expectedCount)
        {
            Assert.AreEqual(expectedCount, counts.SkillCount);
            Assert.AreEqual(expectedCount, counts.TrayCount);
        }

        private static void AssertNpcStatus(
            BootcampRuntimeTestHarness.Harness harness,
            uint npcPackageId,
            ConversationStatus expectedStatus)
        {
            var npc = BootcampRuntimeTestHarness.FindNpcByPackage(harness.BootcampMap, npcPackageId);
            Assert.IsNotNull(npc);
            var conversation = harness.Manager.ClassifyNpcConversation(harness.Client.Player, npc);
            Assert.IsTrue(conversation.TryGetStatus(out var status, out var missionIds));
            Assert.AreEqual(expectedStatus, status);
            CollectionAssert.Contains(missionIds, BootcampRuntimeTestHarness.MissionGearingUp);
        }

        private static void AssertNpcUnavailable(
            BootcampRuntimeTestHarness.Harness harness,
            uint npcPackageId)
        {
            var npc = BootcampRuntimeTestHarness.FindNpcByPackage(harness.BootcampMap, npcPackageId);
            Assert.IsNotNull(npc);
            var conversation = harness.Manager.ClassifyNpcConversation(harness.Client.Player, npc);
            if (!conversation.TryGetStatus(out _, out var missionIds))
                return;
            CollectionAssert.DoesNotContain(missionIds, BootcampRuntimeTestHarness.MissionGearingUp);
        }

        private sealed record ReconnectBoundaryFixture(
            string Name,
            MissionBoundary Stage,
            bool Completeable,
            bool ExpectsCrateGrant,
            bool ExpectsLightningGrant,
            Action<BootcampRuntimeTestHarness.Harness> AssertAvailability,
            IReadOnlyList<(uint ObjectiveId, MissionObjectiveState State)> ExpectedStates)
        {
            public override string ToString() => Name;
        }

        private enum MissionBoundary
        {
            Accepted,
            AfterDelessioGreeting,
            AfterCrateGrant,
            AfterEquip,
            AfterDelessioFollowUp,
            AfterHartmannGreeting,
            AfterFirearmDummy,
            AfterLightningGrant,
            AfterLightningDummy,
            BeforeTurnIn
        }

        private sealed record GearingUpActors(
            Creature McAllister,
            Creature Delessio,
            Creature Hartmann,
            Creature DeSimone);
    }
}
