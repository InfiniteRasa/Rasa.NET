using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions
{
    using Rasa.Data;
    using Rasa.Game.Missions.Content.Bootcamp;
    using Rasa.Managers;
    using Rasa.Memory;
    using Rasa.Packets;
    using Rasa.Packets.Game.Server;
    using Rasa.Packets.Inventory.Client;
    using Rasa.Packets.Inventory.Server;
    using Rasa.Packets.LootDispenser.Client;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Mission.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;
    using Rasa.Structures.World;
    using Rasa.Test.World;

    [TestClass]
    [DoNotParallelize]
    public class BootcampGearingUpInteractionTests
    {
        private static readonly Vector3 CratePosition = new(398, 122, 173);
        private static readonly Vector3[] PracticePositions =
        {
            new(386, 120, 184.7f),
            new(380, 120, 186),
            new(375, 120, 186)
        };

        [TestMethod]
        public void MissionPacketKeepsNavigationIndicatorsWithoutCreatingFloatingWorldEffects()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var actors = PrepareActors(harness);
            harness.Drain();

            Accept(harness, actors.McAllister);

            var gained = harness.Drain().OfType<MissionGainedPacket>().Single(packet => packet.MissionId == 1992);
            Assert.IsFalse(gained.MissionInfo.ObjectivesList.Any(objective => objective.ObjectiveId == 1),
                "The crate objective is not visible before Delessio's briefing.");
            CompleteObjective(harness, actors.Delessio, 4);
            var revealed = harness.Drain().OfType<ObjectiveRevealedPacket>()
                .Single(packet => packet.MissionId == 1992 && packet.ObjectiveId == 1);
            var indicators = revealed.MissionInfo.ObjectivesList.SelectMany(objective => objective.IndicatorList).ToArray();
            Assert.IsTrue(indicators.Length > 0, "Navigation indicators must not be removed.");
            Assert.IsTrue(indicators.Any(indicator => indicator.Position == CratePosition));
            Assert.IsFalse(indicators.Any(indicator => indicator.Show3DEffect),
                "The client's bShow3DEffect flag must be false to suppress the floating world stars.");
        }

        [TestMethod]
        public void PracticeDummiesExistAtMapEntryBeforeAcceptingGearingUp()
        {
            using var harness = BootcampRuntimeTestHarness.CreateFromPendingSelection();
            Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(1992));
            var targets = harness.BootcampMap.DynamicObjects
                .Where(target => (uint)target.EntityClassId == 29365)
                .ToArray();
            Assert.AreEqual(3, targets.Length, "Three real Practice Dummy props must already be in the map.");
            CollectionAssert.AreEquivalent(PracticePositions, targets.Select(target => target.Position).ToArray());
            Assert.IsTrue(targets.All(target => target.IsInWorld));
            harness.Drain();
            harness.MovePlayerTo(new Vector3(380, 120, 177));

            CellManager.Instance.UpdateVisibility(harness.Client);

            var introductions = harness.Drain().OfType<CreatePhysicalEntityPacket>()
                .Where(packet => (uint)packet.ClassId == 29365).ToArray();
            Assert.AreEqual(3, introductions.Length);
            foreach (var introduction in introductions)
            {
                Assert.IsFalse(introduction.EntityData.Any(packet => packet is CreatureInfoPacket),
                    "The actual practice model is a usable world object, not a creature.");
                Assert.IsTrue(introduction.EntityData.Any(packet => packet.Opcode == GameOpcode.DamageInfo));
                Assert.IsTrue(introduction.EntityData.Any(packet => packet is TargetCategoryPacket));
            }
        }

        [TestMethod]
        public void AcceptingFirearmTrainingDoesNotReplaceOrDuplicateThePracticeDummies()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var targets = harness.BootcampMap.DynamicObjects
                .Where(target => (uint)target.EntityClassId == 29365)
                .Select(target => target.EntityId).OrderBy(id => id).ToArray();
            Assert.AreEqual(3, targets.Length);
            var actors = PrepareActors(harness);
            Accept(harness, actors.McAllister);
            CompleteObjective(harness, actors.Delessio, 4);
            LootCrate(harness);
            EquipBoots(harness);
            CompleteObjective(harness, actors.Delessio, 5);

            CompleteObjective(harness, actors.Hartmann, 6);

            CollectionAssert.AreEqual(targets,
                harness.BootcampMap.DynamicObjects.Where(target => (uint)target.EntityClassId == 29365)
                    .Select(target => target.EntityId).OrderBy(id => id).ToArray());
            Assert.IsFalse(harness.BootcampMap.MapCellInfo.Cells.Values
                .SelectMany(cell => cell.CreatureList).Any(creature =>
                    creature.DbId == BootcampRuntimeTestHarness.PracticeDummyCreatureId ||
                    creature.DbId == BootcampRuntimeTestHarness.LightningDummyCreatureId));
        }


        [TestMethod]
        public void AssigningPlayerRefreshesTheWeaponDrawerAfterSelectingTheControlledActor()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var actors = PrepareActors(harness);
            Accept(harness, actors.McAllister);
            CompleteObjective(harness, actors.Delessio, 4);
            LootCrate(harness);
            var rifle = EquipAndReloadRifle(harness);
            harness.Drain();

            ManifestationManager.Instance.AssignPlayer(harness.Client);

            var packets = DrainMethods(harness).Select(method => method.Packet).ToList();
            var controlled = packets.FindIndex(packet => packet is SetControlledActorIdPacket);
            var drawer = packets.FindIndex(packet => packet is InventoryCreatePacket inventory &&
                inventory.InventoryType == InventoryType.WeaponDrawerInventory);
            var selected = packets.FindIndex(packet => packet is WeaponDrawerSlotPacket);
            Assert.IsTrue(controlled >= 0 && drawer > controlled && selected > drawer,
                "The weapon tray must receive its complete contents after the controlled actor initializes its UI, then its selection.");
            Assert.AreEqual(rifle.EntityId, ((InventoryCreatePacket)packets[drawer]).ListOfItems[0]);
            Assert.AreEqual(20U, rifle.CurrentAmmo);
        }

        [TestMethod]
        public void RifleDrawerReferencesAnIntroducedItemBeforeAndAfterReconnect()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var actors = PrepareActors(harness);
            Accept(harness, actors.McAllister);
            CompleteObjective(harness, actors.Delessio, 4);
            LootCrate(harness);
            var rifle = EquipAndReloadRifle(harness);
            AssertDrawerItemData(harness, rifle);

            harness.ReconnectFresh(drainPackets: false);

            rifle = EntityManager.Instance.GetItem(harness.Client.Player.Inventory.WeaponDrawer[0]);
            Assert.IsNotNull(rifle);
            AssertDrawerItemData(harness, rifle);
        }

        private static void AssertDrawerItemData(BootcampRuntimeTestHarness.Harness harness, Item rifle)
        {
            var knownItems = new HashSet<ulong>();
            var itemInfo = new HashSet<ulong>();
            var weaponInfo = new HashSet<ulong>();
            var sawRifleDrawer = false;
            foreach (var method in DrainMethods(harness))
            {
                switch (method.Packet)
                {
                    case CreatePhysicalEntityPacket created:
                        knownItems.Add(created.EntityId);
                        itemInfo.Remove(created.EntityId);
                        weaponInfo.Remove(created.EntityId);
                        if (created.EntityId == rifle.EntityId)
                            Assert.AreEqual((EntityClasses)27220, created.ClassId,
                                "The native rifle icon is selected by item class 27220.");
                        break;
                    case ItemInfoPacket info:
                        Assert.IsTrue(knownItems.Contains(method.EntityId));
                        itemInfo.Add(method.EntityId);
                        break;
                    case WeaponInfoPacket:
                        Assert.IsTrue(knownItems.Contains(method.EntityId));
                        weaponInfo.Add(method.EntityId);
                        break;
                    case DestroyPhysicalEntityPacket destroyed:
                        knownItems.Remove(destroyed.EntityId);
                        itemInfo.Remove(destroyed.EntityId);
                        weaponInfo.Remove(destroyed.EntityId);
                        break;
                    case InventoryAddItemPacket { Type: InventoryType.WeaponDrawerInventory } added
                        when added.EntityId == rifle.EntityId:
                        Assert.IsTrue(knownItems.Contains(added.EntityId) && itemInfo.Contains(added.EntityId) &&
                            weaponInfo.Contains(added.EntityId),
                            "The action bar must resolve the rifle and its metadata when the drawer changes.");
                        sawRifleDrawer = true;
                        break;
                    case InventoryCreatePacket { InventoryType: InventoryType.WeaponDrawerInventory } inventory
                        when inventory.ListOfItems.Contains(rifle.EntityId):
                        Assert.IsTrue(knownItems.Contains(rifle.EntityId) && itemInfo.Contains(rifle.EntityId) &&
                            weaponInfo.Contains(rifle.EntityId),
                            "The reconnected drawer must resolve the rifle's complete item entity.");
                        sawRifleDrawer = true;
                        break;
                }
            }
            Assert.IsTrue(sawRifleDrawer);
            Assert.IsTrue(knownItems.Contains(rifle.EntityId));
            Assert.AreEqual(13713U, rifle.ItemTemplateId);
        }

        [TestMethod]
        public void RifleHitAdvancesTrainingWithoutDestroyingOrReplacingTheTarget()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var actors = PrepareActors(harness);
            Accept(harness, actors.McAllister);
            CompleteObjective(harness, actors.Delessio, 4);
            LootCrate(harness);
            EquipBoots(harness);
            CompleteObjective(harness, actors.Delessio, 5);
            CompleteObjective(harness, actors.Hartmann, 6);
            var rifle = EquipAndReloadRifle(harness);
            var targets = harness.BootcampMap.DynamicObjects
                .Where(target => (uint)target.EntityClassId == 29365).ToArray();
            var target = targets.Single(candidate => candidate.Position == PracticePositions[0]);
            harness.Client.Player.Target = target.EntityId;
            harness.Drain();
            Assert.IsTrue(PracticeTargetManager.CanHit(harness.BootcampMap, harness.Client.Player, target),
                $"state={harness.Client.Player.State}, health={harness.Client.Player.Attributes[Attributes.Health].Current}, " +
                $"owner={harness.BootcampMap.OwnerCharacterId}, player={harness.Client.Player.Id}, target={target.IsInWorld}/{target.IsEnabled}");

            MissileManager.Instance.RequestWeaponAttack(harness.Client, new RequestWeaponAttackPacket
            {
                ActionId = ActionId.WeaponAttack,
                ActionArgId = 133,
                TargetId = (long)target.EntityId
            });
            var missile = harness.BootcampMap.QueuedMissiles.Single();
            Assert.AreSame(target, missile.TargetObject);
            Assert.IsTrue(missile.DamageA > 0);
            Assert.IsTrue(PracticeTargetManager.CanHit(harness.BootcampMap, harness.Client.Player, target));
            Assert.IsTrue(harness.Manager.LoadedMissions[1992].Objectives[3].ProgressRule.Matches(
                MissionProgressEvent.ObjectHit(29365, (uint)missile.ActionId)));
            MissileManager.Instance.DoWork(harness.BootcampMap, 1000);

            Assert.AreEqual(19U, rifle.CurrentAmmo);
            Assert.AreEqual(MissionObjectiveState.Completed,
                harness.Client.Player.Missions[1992].Objectives[3].State);
            Assert.AreEqual(UseObjectState.StateNull, target.StateId);
            Assert.IsTrue(target.IsInWorld && target.IsEnabled);
            CollectionAssert.AreEquivalent(targets,
                harness.BootcampMap.DynamicObjects.Where(candidate => (uint)candidate.EntityClassId == 29365).ToArray());
            AssertConversationUpdate(harness, actors.Hartmann);
        }

        [TestMethod]
        public void TrainingPreservesAlreadyLearnedRecruitSkillsAndRearrangedStartingAbilities()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var progression = new ManifestationManager(harness.Context);
            using (var unit = harness.Context.CreateChar())
            {
                foreach (var skill in new[]
                         {
                             SkillId.Lightning, SkillId.Sprint, SkillId.Firearms,
                             SkillId.HandToHand, SkillId.MotorAssistArmor
                         })
                    unit.CharacterSkills.AddOrUpdate(
                        harness.Client.Player.Id, (uint)skill,
                        progression.SkillIdx2AbilityId[progression.GetSkillIndexById((int)skill)], 1);
                unit.CharacterAbilityDrawers.AddOrUpdate(
                    harness.Client.Player.Id, 0, (int)ActionId.AaRecruitSprint, 1);
                unit.CharacterAbilityDrawers.AddOrUpdate(
                    harness.Client.Player.Id, 1, (int)ActionId.AaRecruitLightning, 1);
                unit.Complete();
            }
            harness.Client.Player.Skills = harness.Maps.GetPlayerSkills(harness.Client.Player.Id);
            harness.Client.Player.Abilities = harness.Maps.GetPlayerAbilities(harness.Client.Player.Id);
            var actors = PrepareActors(harness);
            Accept(harness, actors.McAllister);
            CompleteObjective(harness, actors.Delessio, 4);
            LootCrate(harness);
            EquipBoots(harness);
            Assert.AreEqual(-1, harness.Client.Player.Skills[SkillId.Firearms].AbilityId);
            Assert.AreEqual(-1, harness.Client.Player.Skills[SkillId.MotorAssistArmor].AbilityId);
            CompleteObjective(harness, actors.Delessio, 5);
            CompleteObjective(harness, actors.Hartmann, 6);
            EquipAndReloadRifle(harness);
            var target = harness.BootcampMap.DynamicObjects.First(candidate => (uint)candidate.EntityClassId == 29365);
            harness.Client.Player.Target = target.EntityId;
            MissileManager.Instance.RequestWeaponAttack(harness.Client,
                new RequestWeaponAttackPacket { ActionId = ActionId.WeaponAttack, ActionArgId = 133 });
            MissileManager.Instance.DoWork(harness.BootcampMap, 1000);
            CompleteObjective(harness, actors.Hartmann, 9);

            Assert.AreEqual((int)ActionId.AaRecruitSprint, harness.Client.Player.Abilities[0].AbilityId);
            Assert.AreEqual((int)ActionId.AaRecruitLightning, harness.Client.Player.Abilities[1].AbilityId);
            using var verify = harness.Context.CreateChar();
            var skills = verify.CharacterSkills.GetCharacterSkills(harness.Client.Player.Id);
            Assert.HasCount(5, skills);
            Assert.IsTrue(skills.All(skill => skill.SkillLevel == 1));
            Assert.AreEqual(-1, skills.Single(skill => skill.SkillId == (uint)SkillId.Firearms).AbilityId);
            Assert.AreEqual(-1, skills.Single(skill => skill.SkillId == (uint)SkillId.MotorAssistArmor).AbilityId);
            var drawer = verify.CharacterAbilityDrawers.GetCharacterAbilities(harness.Client.Player.Id);
            Assert.HasCount(2, drawer);
            Assert.AreEqual((int)ActionId.AaRecruitSprint, drawer.Single(slot => slot.AbilitySlot == 0).AbilityId);
            Assert.AreEqual((int)ActionId.AaRecruitLightning, drawer.Single(slot => slot.AbilitySlot == 1).AbilityId);
        }

        [TestMethod]
        public void RifleAndLightningUseTheSameStandingTargetsThroughTheFinalHandoff()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var actors = PrepareActors(harness, useWorldSpawns: true);
            var deSimone = BootcampRuntimeTestHarness.FindNpcByPackage(
                harness.BootcampMap, BootcampRuntimeTestHarness.CorporalDeSimonePackageId);
            Assert.IsNotNull(deSimone);
            Accept(harness, actors.McAllister);
            CompleteObjective(harness, actors.Delessio, 4);
            LootCrate(harness);
            EquipBoots(harness);
            CompleteObjective(harness, actors.Delessio, 5);
            CompleteObjective(harness, actors.Hartmann, 6);
            var rifle = EquipAndReloadRifle(harness);
            var targets = harness.BootcampMap.DynamicObjects
                .Where(target => (uint)target.EntityClassId == 29365).ToArray();
            harness.Client.Player.Target = targets[0].EntityId;
            MissileManager.Instance.RequestWeaponAttack(harness.Client,
                new RequestWeaponAttackPacket { ActionId = ActionId.WeaponAttack, ActionArgId = 133 });
            MissileManager.Instance.DoWork(harness.BootcampMap, 1000);
            Assert.AreEqual(MissionObjectiveState.Completed,
                harness.Client.Player.Missions[1992].Objectives[3].State);
            CompleteObjective(harness, actors.Hartmann, 9);
            harness.Client.Player.NextShotAt = 0;
            MissileManager.Instance.RequestWeaponAttack(harness.Client,
                new RequestWeaponAttackPacket { ActionId = ActionId.WeaponAttack, ActionArgId = 133 });
            MissileManager.Instance.DoWork(harness.BootcampMap, 1000);
            Assert.AreEqual(MissionObjectiveState.Incomplete,
                harness.Client.Player.Missions[1992].Objectives[8].State,
                "Another rifle hit must not complete the Lightning objective.");
            harness.MovePlayerTo(new Vector3(380, 120, 177));
            CellManager.Instance.UpdateVisibility(harness.Client);
            var beforeChi = harness.Client.Player.Attributes[Attributes.Chi].Current;
            harness.Drain();

            CastLightning(harness, targets[1]);

            Assert.AreEqual(beforeChi - 10, harness.Client.Player.Attributes[Attributes.Chi].Current);
            Assert.AreEqual(MissionObjectiveState.Completed,
                harness.Client.Player.Missions[1992].Objectives[8].State);
            AssertConversationUpdate(harness, actors.Hartmann);
            CollectionAssert.AreEquivalent(targets,
                harness.BootcampMap.DynamicObjects.Where(target => (uint)target.EntityClassId == 29365).ToArray());
            Assert.IsTrue(targets.All(target =>
                target.IsInWorld && target.IsEnabled && target.StateId == UseObjectState.StateNull));
            Assert.AreEqual(18U, rifle.CurrentAmmo);
            CompleteObjective(harness, actors.Hartmann, 7);
            Assert.IsTrue(harness.Manager.ClassifyNpcConversation(harness.Client.Player, deSimone)
                .TryGetStatus(out var status, out _));
            Assert.AreEqual(ConversationStatus.MissionComplete, status);
            Assert.IsTrue(harness.Manager.CompleteOfferedMission(
                harness.Client, deSimone.EntityId, 1992, null, null));
            Assert.AreEqual(MissionState.Completed, harness.Client.Player.Missions[1992].State);
            Assert.IsTrue(harness.Manager.OpenNpcConversation(harness.Client, deSimone.EntityId));
            new NpcManager(harness.Context, harness.Manager).AssignNPCMission(harness.Client,
                new AssignNPCMissionPacket { NpcEntityId = deSimone.EntityId, MissionId = 1994 });
            Assert.AreEqual(MissionState.Active, harness.Client.Player.Missions[1994].State);
            Assert.IsTrue(harness.Drain().OfType<MissionGainedPacket>().Any(packet => packet.MissionId == 1994));
        }

        [TestMethod]
        public void ObjectHitRulesRequireBothTheCorrectModelAndAction()
        {
            var rule = MissionProgressRule.CompleteOnObjectHit(29365, 194);
            Assert.IsTrue(rule.Matches(MissionProgressEvent.ObjectHit(29365, 194)));
            Assert.IsFalse(rule.Matches(MissionProgressEvent.ObjectHit(29365, 1)));
            Assert.IsFalse(rule.Matches(MissionProgressEvent.ObjectHit(29877, 194)));
            Assert.IsFalse(rule.Matches(MissionProgressEvent.AbilityHit(194, 29365)));
            Assert.IsFalse(rule.Matches(MissionProgressEvent.Interaction(29365)));
        }

        [TestMethod]
        public void PracticeTargetsRemainExactlyThreeAcrossPrivateInstanceReconnect()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var before = harness.BootcampMap.DynamicObjects
                .Where(target => (uint)target.EntityClassId == 29365).ToArray();
            Assert.AreEqual(3, before.Length);

            harness.ReconnectFresh();

            var after = harness.BootcampMap.DynamicObjects
                .Where(target => (uint)target.EntityClassId == 29365).ToArray();
            Assert.AreEqual(3, after.Length);
            CollectionAssert.AreEquivalent(PracticePositions, after.Select(target => target.Position).ToArray());
            Assert.IsTrue(after.All(target => target.IsInWorld && target.StateId == UseObjectState.StateNull));
            Assert.IsFalse(before.Any(target =>
                PracticeTargetManager.TryGetTarget(harness.BootcampMap, target.EntityId, out var current) &&
                ReferenceEquals(target, current)));
        }

        [TestMethod]
        public void PracticeTargetsRejectOtherInstancesAndDeadPlayers()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var target = harness.BootcampMap.DynamicObjects.First(candidate => (uint)candidate.EntityClassId == 29365);
            var otherMap = harness.Maps.GetOrCreatePrivateInstance(1985, 999);
            Assert.IsFalse(PracticeTargetManager.TryGetTarget(otherMap, target.EntityId, out _));
            Assert.IsFalse(PracticeTargetManager.CanHit(otherMap, harness.Client.Player, target));
            harness.Client.Player.Attributes[Attributes.Health].Current = 0;
            Assert.IsFalse(PracticeTargetManager.CanHit(harness.BootcampMap, harness.Client.Player, target));
            harness.Maps.ReleaseOwnedPrivateInstances(999);
        }

        [TestMethod]
        public void AcceptingGearingUpAdvertisesDelessioWithoutReintroducingTheNpc()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var actors = PrepareActors(harness);
            harness.Drain();

            Accept(harness, actors.McAllister);

            AssertConversationUpdate(harness, actors.Delessio);
        }

        [TestMethod]
        public void LootingAndEquippingAdvertisesDelessioWithoutReconnect()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var actors = PrepareActors(harness);
            Accept(harness, actors.McAllister);
            CompleteObjective(harness, actors.Delessio, 4);
            LootCrate(harness);
            harness.Drain();

            EquipBoots(harness);

            Assert.AreEqual(MissionObjectiveState.Incomplete,
                harness.Client.Player.Missions[1992].Objectives[5].State);
            AssertConversationUpdate(harness, actors.Delessio);
        }

        [TestMethod]
        public void DelessioHandoffAdvertisesHartmannWithoutReconnect()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var actors = PrepareActors(harness);
            Accept(harness, actors.McAllister);
            CompleteObjective(harness, actors.Delessio, 4);
            LootCrate(harness);
            EquipBoots(harness);
            harness.Drain();

            CompleteObjective(harness, actors.Delessio, 5);

            Assert.AreEqual(MissionObjectiveState.Incomplete,
                harness.Client.Player.Missions[1992].Objectives[6].State);
            AssertConversationUpdate(harness, actors.Hartmann);
        }

        private static (Creature McAllister, Creature Delessio, Creature Hartmann) PrepareActors(
            BootcampRuntimeTestHarness.Harness harness, bool useWorldSpawns = false)
        {
            harness.MovePlayerTo(CratePosition);
            CellManager.Instance.UpdateVisibility(harness.Client);
            if (useWorldSpawns)
                harness.SpawnWorldNpcs();
            var mcAllister = useWorldSpawns
                ? BootcampRuntimeTestHarness.FindCreature(harness.BootcampMap, BootcampRuntimeTestHarness.MajorMcAllisterCreatureId)
                : harness.AddNpc(BootcampRuntimeTestHarness.MajorMcAllisterCreatureId, position: CratePosition);
            var delessio = useWorldSpawns
                ? BootcampRuntimeTestHarness.FindCreature(harness.BootcampMap, BootcampRuntimeTestHarness.CaptainDelessioCreatureId)
                : harness.AddNpc(BootcampRuntimeTestHarness.CaptainDelessioCreatureId,
                    BootcampRuntimeTestHarness.CaptainDelessioPackageId, CratePosition);
            var hartmann = useWorldSpawns
                ? BootcampRuntimeTestHarness.FindCreature(harness.BootcampMap, BootcampRuntimeTestHarness.CorporalHartmannCreatureId)
                : harness.AddNpc(BootcampRuntimeTestHarness.CorporalHartmannCreatureId,
                    BootcampRuntimeTestHarness.CorporalHartmannPackageId, CratePosition);
            foreach (var npc in new[] { mcAllister, delessio, hartmann })
                CreatureManager.Instance.CreateCreatureOnClient(harness.Client, npc);
            harness.SeedMission(harness.Client.Player.Id,
                BootcampRuntimeTestHarness.MissionInitiation, (uint)MissionState.Completed, false);
            return (mcAllister, delessio, hartmann);
        }

        private static void Accept(BootcampRuntimeTestHarness.Harness harness, Creature npc)
        {
            harness.MovePlayerTo(npc);
            CellManager.Instance.UpdateVisibility(harness.Client);
            Assert.IsTrue(harness.Manager.OpenNpcConversation(harness.Client, npc.EntityId));
            new NpcManager(harness.Context, harness.Manager).AssignNPCMission(harness.Client,
                new AssignNPCMissionPacket { NpcEntityId = npc.EntityId, MissionId = 1992 });
            Assert.AreEqual(MissionState.Active, harness.Client.Player.Missions[1992].State);
        }

        private static void CompleteObjective(
            BootcampRuntimeTestHarness.Harness harness, Creature npc, uint objectiveId)
        {
            harness.MovePlayerTo(npc);
            CellManager.Instance.UpdateVisibility(harness.Client);
            Assert.IsTrue(harness.Manager.OpenNpcConversation(harness.Client, npc.EntityId));
            new NpcManager(harness.Context, harness.Manager).CompleteNPCObjective(harness.Client,
                new CompleteNPCObjectivePacket
                {
                    EntityId = npc.EntityId,
                    MissionId = 1992,
                    ObjectiveId = objectiveId,
                    PlayerFlagId = 1
                });
            Assert.AreEqual(MissionObjectiveState.Completed,
                harness.Client.Player.Missions[1992].Objectives[objectiveId].State);
        }

        private static void LootCrate(BootcampRuntimeTestHarness.Harness harness)
        {
            var crate = BootcampRuntimeTestHarness.FindScenarioObject(
                harness.BootcampMap, "bootcamp-equipment-crate");
            Assert.IsNotNull(crate);
            harness.UseObjectAndRecover(crate, actionArgId: DynamicObjectManager.FootlockerUseArgId);
            LootDispenserManager.Instance.RequestLootAllFromCorpse(harness.Client,
                new RequestLootAllFromCorpsePacket { EntityId = crate.LootDispenserEntityId });
            Assert.AreEqual(MissionObjectiveState.Completed,
                harness.Client.Player.Missions[1992].Objectives[1].State);
        }

        private static void EquipBoots(BootcampRuntimeTestHarness.Harness harness)
        {
            var boots = harness.Client.Player.Inventory.PersonalInventory
                .Where(id => id != 0)
                .Select(id => EntityManager.Instance.GetItem(id))
                .Single(item => item.ItemTemplateId == 13066);
            InventoryManager.Instance.RequestEquipArmor(harness.Client,
                new RequestEquipArmorPacket
                {
                    SrcInventory = InventoryType.Personal,
                    SrcSlot = boots.OwnerSlotId,
                    DestSlot = 2
                });
            Assert.AreEqual(boots.EntityId, harness.Client.Player.Inventory.EquippedInventory[2]);
            Assert.AreEqual(MissionObjectiveState.Completed,
                harness.Client.Player.Missions[1992].Objectives[2].State);
        }

        private static Item EquipAndReloadRifle(BootcampRuntimeTestHarness.Harness harness)
        {
            var rifle = harness.Client.Player.Inventory.PersonalInventory.Where(id => id != 0)
                .Select(id => EntityManager.Instance.GetItem(id))
                .Single(item => item.ItemTemplateId == 13713);
            while (harness.Client.Player.Inventory.WeaponDrawer.Count < 5)
                harness.Client.Player.Inventory.WeaponDrawer.Add(0);
            harness.Client.Player.ActiveWeapon = 0;
            InventoryManager.Instance.RequestEquipWeapon(harness.Client, new RequestEquipWeaponPacket
            {
                SrcSlot = rifle.OwnerSlotId,
                InventoryType = InventoryType.Personal,
                DestSlot = 0
            });
            Assert.AreEqual(rifle.EntityId, harness.Client.Player.Inventory.WeaponDrawer[0]);
            ManifestationManager.Instance.WeaponReady(harness.Client, true);
            ManifestationManager.Instance.RequestWeaponReload(harness.Client, false);
            harness.AdvanceRecovery(10000);
            ManifestationManager.Instance.WeaponReady(harness.Client, true);
            Assert.AreEqual(20U, rifle.CurrentAmmo);
            BootcampRuntimeTestHarness.PrepareDirectDamageClient(harness.Client);
            harness.Client.Player.Attributes[Attributes.Chi] =
                new ActorAttributes(Attributes.Chi, 100, 100, 100, 0, 0);
            return rifle;
        }

        private static void CastLightning(BootcampRuntimeTestHarness.Harness harness, DynamicObject target)
        {
            var manager = (AbilityManager)Activator.CreateInstance(typeof(AbilityManager),
                BindingFlags.Instance | BindingFlags.NonPublic, null,
                new object[] { harness.Context, harness.Manager }, null)!;
            var actions = (Dictionary<ActionId, ActionInfo>)typeof(AbilityManager)
                .GetField("_actions", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(manager)!;
            var info = BootcampRuntimeTestHarness.LightningInfo(40);
            info.Costs.Add(new ActionCost { Attribute = Attributes.Chi, Amount = 10 });
            actions[ActionId.AaRecruitLightning] = BootcampRuntimeTestHarness.LightningAction(info);
            using var stream = new MemoryStream();
            using (var binary = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true))
            using (var writer = new PythonWriter(binary))
                writer.WriteULong(target.EntityId);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            var request = new RequestPerformAbilityPacket
            {
                ActionId = ActionId.AaRecruitLightning,
                ActionArgId = 1,
                Target = ActionTarget.Read(reader)
            };
            lock (Rasa.Game.Server.Clients)
                Rasa.Game.Server.Clients.Add(harness.Client);
            try
            {
                manager.RequestPerformAbility(harness.Client, request);
                var pending = harness.BootcampMap.PerformRecovery
                    .Where(action => action.ActionId == ActionId.AaRecruitLightning).ToArray();
                Assert.AreEqual(1, pending.Length, "Lightning must accept the real Practice Dummy as its target.");
                manager.PerformRecovery(harness.BootcampMap, pending[0]);
                harness.BootcampMap.PerformRecovery.Remove(pending[0]);
            }
            finally
            {
                lock (Rasa.Game.Server.Clients)
                    Rasa.Game.Server.Clients.Remove(harness.Client);
            }
        }

        private static void AssertConversationUpdate(
            BootcampRuntimeTestHarness.Harness harness, Creature npc)
        {
            Assert.IsTrue(harness.Manager.ClassifyNpcConversation(harness.Client.Player, npc)
                .TryGetStatus(out var expected, out _));
            Assert.AreEqual(ConversationStatus.ObjectivComplete, expected);
            var updates = DrainMethods(harness)
                .Where(method => method.EntityId == npc.EntityId)
                .Select(method => method.Packet)
                .OfType<NPCConversationStatusPacket>()
                .ToArray();
            Assert.IsTrue(updates.Length > 0,
                $"NPC {npc.DbId} is ready on the server but the client received no conversation-status update.");
            Assert.AreEqual(expected, updates.Last().ConvoStatusId);
        }

        private static IReadOnlyList<CallMethodMessage> DrainMethods(
            BootcampRuntimeTestHarness.Harness harness) =>
            WorldTestContext.Drain(harness.Client)
                .Select(packet => packet.Message)
                .OfType<CallMethodMessage>()
                .ToArray();
    }
}
