using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
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
    public class WildernessDaghdasUrnTests
    {
        [TestMethod]
        [DataRow(0, 12887U)]
        [DataRow(1, 164U)]
        public void AriochsCodexRequiresActualCorpseLootAndJuvaksSelectedRewardPaysOnce(int selection, uint rewardTemplate)
        {
            using var harness = CreateIntegratedHub();
            harness.SpawnWorld(179, 580011);
            var juvak = harness.Npc(179);
            Assert.IsNotNull(juvak);
            Assert.AreEqual(102U, juvak.DbId);
            AssertRewardOffer(harness, juvak, 696, 1500, 12887, 164);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, juvak.EntityId, 696));
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, juvak.EntityId, 696, 2, 1));

            var arioch = LiveCreature(harness, 77, 580011);
            Kill(harness, arioch);
            Assert.AreEqual(0U, Held(harness, 2328));
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[696].Objectives[1].State,
                "A kill is not a Codex pickup.");
            var corpse = harness.Map.LootDispensers[arioch.CorpseLootEntityId];
            Assert.AreEqual(1, corpse.LootItems.Count(item => item.ItemTemplateId == 2328));
            LootDispenserManager.Instance.RequestLootAllFromCorpse(harness.Client,
                new RequestLootAllFromCorpsePacket { EntityId = corpse.EntityId });
            Assert.AreEqual(1U, Held(harness, 2328));
            Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[696].Objectives[1].State);
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, juvak.EntityId, 696, 2, 1));
            Assert.IsTrue(harness.Manager.CompleteOfferedMission(harness.Client, juvak.EntityId, 696, selection));
            Assert.AreEqual(0U, Held(harness, 2328));
            Assert.AreEqual(1U, Held(harness, rewardTemplate));
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, juvak.EntityId, 696, selection));
            Assert.AreEqual(1U, Held(harness, rewardTemplate));
            if (selection == 0)
                VerifyArmorReward(harness, 12887, 1, 188, CharacterClass.Specialist, 30);
            else
                VerifyWeaponReward(harness, 164, 1);
        }

        [TestMethod]
        [TestCategory("WildernessG2")]
        public void ChildhoodsEndRequiresThreeEncounterXanxAndPreservesTheNativeContactOrder()
        {
            using var harness = CreateIntegratedHub();
            harness.SpawnWorld(174, 175, 176, 177, 580053);
            var todae = harness.Npc(174);
            var anjuhi = harness.Npc(176);
            var tirna = harness.Npc(177);
            var doyan = harness.Npc(175);
            Assert.IsNotNull(todae);
            Assert.IsNotNull(anjuhi);
            Assert.IsNotNull(tirna);
            Assert.IsNotNull(doyan);
            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, todae.EntityId, 682));
            SeedHistory(harness, 451);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, todae.EntityId, 682));
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, tirna.EntityId, 682, 4, 1));
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, anjuhi.EntityId, 682, 2, 1));
            harness.Tick();
            Kill(harness, LiveCreature(harness, 87, 580053));
            Assert.AreEqual(0U, harness.Client.Player.Missions[682].Objectives[3].Counters[0],
                "An ordinary public Xanx cannot count as one of Anjuhi's attackers.");
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, anjuhi.EntityId, 682, 5, 1));
            var attackers = Creatures(harness).Where(creature => creature.DbId == 87 &&
                creature.SpawnPool?.ScenarioMissionId == 682 &&
                creature.SpawnPool.ScenarioOwnerCharacterId == harness.Client.Player.Id &&
                creature.State != CharacterState.Dead).ToArray();
            Assert.AreEqual(3, attackers.Length);
            Kill(harness, attackers[0]);
            Kill(harness, attackers[1]);
            Assert.AreEqual(2U, harness.Client.Player.Missions[682].Objectives[3].Counters[0]);
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, anjuhi.EntityId, 682, 5, 1));
            Kill(harness, attackers[2]);
            Assert.AreEqual(3U, harness.Client.Player.Missions[682].Objectives[3].Counters[0]);
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[682].Objectives[5].State);

            for (var tick = 0; tick < 160 && harness.Manager.PublicActors.Handle(harness.Map, 176) != null; tick++)
            {
                harness.SpawnWorldAfter(250, 176);
                harness.Tick();
            }
            Assert.IsNull(harness.Manager.PublicActors.Handle(harness.Map, 176),
                "The completed attack must release the public ranger rather than retain a permanent reservation.");
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, doyan.EntityId, 682, 6, 1));
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, harness.Npc(176).EntityId, 682, 5, 1));
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, tirna.EntityId, 682, 4, 1));
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, doyan.EntityId, 682, 6, 1));
            Assert.IsTrue(harness.Manager.CompleteOfferedMission(harness.Client, doyan.EntityId, 682, 1));
            Assert.AreEqual(1U, Held(harness, 166));
            VerifyWeaponReward(harness, 166, 3);
        }

        [TestMethod]
        [TestCategory("WildernessG2")]
        [DataRow(false)]
        [DataRow(true)]
        public void ActualAnjuhiDeathFailsTheCurrentStageAndCannotFailARetry(bool attackStarted)
        {
            using var harness = CreateIntegratedHub();
            harness.SpawnWorld(174, 176, 580053);
            SeedHistory(harness, 451);
            var todae = harness.Npc(174);
            var anjuhi = harness.Npc(176);
            Assert.IsNotNull(todae);
            Assert.IsNotNull(anjuhi);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, todae.EntityId, 682));
            if (attackStarted)
            {
                Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, anjuhi.EntityId, 682, 2, 1));
                harness.Tick();
            }
            var assignment = harness.Client.Player.Missions[682].AssignmentId;
            var lease = harness.Manager.PublicActors.Handle(harness.Map, 176);
            Assert.IsNotNull(lease);
            var killer = LiveCreature(harness, 87, 580053);
            anjuhi.Attributes[Attributes.Health].Current = 0;
            harness.Creatures.HandleCreatureKill(harness.Map, anjuhi, killer);
            harness.Tick();
            Assert.AreEqual(MissionState.Failed, harness.Client.Player.Missions[682].State);
            Assert.AreEqual(MissionObjectiveState.Failed,
                harness.Client.Player.Missions[682].Objectives[attackStarted ? 3U : 2U].State);
            for (var tick = 0; tick < 160 && harness.Manager.PublicActors.Handle(harness.Map, 176) != null; tick++)
            {
                harness.SpawnWorldAfter(250, 176);
                harness.Tick();
            }
            Assert.IsNull(harness.Manager.PublicActors.Handle(harness.Map, 176));
            new NpcManager(harness, harness.Manager).AbandonMission(harness.Client,
                new AbandonMissionPacket { MissionId = 682 });
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, todae.EntityId, 682));
            Assert.AreNotEqual(assignment, harness.Client.Player.Missions[682].AssignmentId);
            Assert.IsFalse(harness.Manager.Scenes.Submit(lease.RunId,
                new Rasa.Missions.Scenes.SceneObservation(Rasa.Missions.Scenes.SceneEventKind.ActorDied,
                    lease.Generation, Role: "anjuhi", SourceEntityId: anjuhi.EntityId)));
            Assert.AreEqual(MissionState.Active, harness.Client.Player.Missions[682].State);
        }

        [TestMethod]
        [TestCategory("WildernessG2")]
        public void HerbalRemedyRequiresFivePhysicalHerbsAndAllThreeDevilsWithoutStealingOpportunityCredit()
        {
            using var harness = CreateIntegratedHub();
            harness.SpawnWorld(175, 191, 196, 580009, 580013, 520065);
            SeedHistory(harness, 682);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, harness.Npc(196).EntityId, 1449));
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, harness.Npc(175).EntityId, 695));
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, harness.Npc(191).EntityId, 695, 4, 1));
            harness.Tick();
            Assert.AreEqual(0, harness.Client.Player.Missions[695].Objectives[1].Counters.Count,
                "Native herb objective1 has no authored counter label; completion must not invent visible counter0.");
            var scratch = LiveCreature(harness, 80, 580013);
            Kill(harness, scratch);
            Kill(harness, LiveCreature(harness, 75, 580009));
            Assert.AreEqual(2U, harness.Client.Player.Missions[695].Objectives[5].Counters[0]);
            Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[1449].Objectives[24].State);
            Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[1449].Objectives[25].State);
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[1449].Objectives[23].State);

            var pool = harness.Map.SpawnPools.Single(entry => entry.DbId == 580013);
            harness.SpawnWorldAfter(pool.RespawnTime, 580013);
            var repeatedScratch = LiveCreature(harness, 80, 580013);
            Assert.AreNotEqual(scratch.EntityId, repeatedScratch.EntityId);
            Kill(harness, repeatedScratch);
            Assert.AreEqual(2U, harness.Client.Player.Missions[695].Objectives[5].Counters[0],
                "A second real Old Scratch death cannot replace Horntail.");
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[695].Objectives[7].State);
            for (var index = 1; index <= 4; index++)
                Use(harness, Object(harness, 695, $"tinctu-{index}"));
            Assert.AreEqual(4U, Owned(harness, 695, 2326));
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[695].Objectives[1].State);
            Kill(harness, LiveCreature(harness, 79, 520065));
            Assert.AreEqual(3U, harness.Client.Player.Missions[695].Objectives[5].Counters[0]);
            Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[1449].Objectives[23].State);
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, harness.Npc(191).EntityId, 695, 1),
                "Three Devils and four herbs do not fulfill Herbal Remedy.");
            Use(harness, Object(harness, 695, "tinctu-5"));
            Assert.AreEqual(5U, Owned(harness, 695, 2326));
            Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[695].Objectives[1].State);
            Assert.IsTrue(harness.Manager.CompleteOfferedMission(harness.Client, harness.Npc(191).EntityId, 695, 1));
            Assert.AreEqual(0U, Owned(harness, 695, 2326));
            Assert.AreEqual(1U, Held(harness, 12943));
            Assert.AreEqual(MissionState.Active, harness.Client.Player.Missions[1449].State,
                "Independent Targets of Opportunity work, including its real instance goals, remains unfinished.");
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, harness.Npc(191).EntityId, 695, 1));
            VerifyArmorReward(harness, 12943, 15, 281, CharacterClass.Specialist, 30);
        }

        [TestMethod]
        [TestCategory("WildernessG2")]
        public void AHerbCannotBeHarvestedTwiceOrByAnotherCharacterAndOldCallbacksCannotIssueAfterAbandon()
        {
            using var harness = CreateIntegratedHub();
            harness.SpawnWorld(175, 191);
            SeedHistory(harness, 682);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, harness.Npc(175).EntityId, 695));
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, harness.Npc(191).EntityId, 695, 4, 1));
            harness.Tick();
            var herb = Object(harness, 695, "tinctu-1");
            var stale = Object(harness, 695, "tinctu-2");
            Assert.AreEqual(10512U, (uint)herb.EntityClassId);
            Assert.AreEqual(3U, herb.MissionUseAction.ActionArgId);
            Assert.AreEqual(81U, (uint)herb.StateId);
            Assert.IsTrue(Vector3.Distance(new Vector3(600, 285.1309f, 728), herb.Position) < 0.001f);
            var second = harness.Context.CreateAdditionalClient(2, manager: harness.Manager);
            try
            {
                Use(harness, herb, second);
                Assert.AreEqual(0U, Owned(harness, 695, 2326));
                Use(harness, herb);
                Use(harness, herb);
                Assert.AreEqual(1U, Owned(harness, 695, 2326));
                Assert.AreEqual(82U, (uint)herb.StateId);
                Assert.IsFalse(herb.IsEnabled, "Native inactive state82 must stop further targeting.");
                var oldAssignment = harness.Client.Player.Missions[695].AssignmentId;
                new NpcManager(harness, harness.Manager).AbandonMission(harness.Client,
                    new AbandonMissionPacket { MissionId = 695 });
                Assert.AreEqual(0U, Owned(harness, 695, 2326));
                Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, harness.Npc(175).EntityId, 695));
                Assert.AreNotEqual(oldAssignment, harness.Client.Player.Missions[695].AssignmentId);
                Use(harness, stale);
                Assert.AreEqual(0U, Owned(harness, 695, 2326));
            }
            finally
            {
                CellManager.Instance.RemoveFromWorld(second);
                harness.Map.ClientList.Remove(second);
            }
        }

        [TestMethod]
        public void ElixirVitaeIssuesOneEssenceRequiresEleanorAndConsumesItWithThePromisedReward()
        {
            using var harness = CreateIntegratedHub();
            harness.SpawnWorld(191, 173);
            var gadfly = harness.Npc(191);
            var eleanor = harness.Npc(173);
            Assert.IsNotNull(gadfly);
            Assert.IsNotNull(eleanor);
            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, gadfly.EntityId, 698));
            SeedHistory(harness, 695);
            AssertRewardOffer(harness, gadfly, 698, 550, 12831, 12943);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, gadfly.EntityId, 698));
            Assert.AreEqual(1U, Owned(harness, 698, 2355));
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, gadfly.EntityId, 698, 1, 1));
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, eleanor.EntityId, 698, 1, 1));
            Assert.IsTrue(harness.Manager.CompleteOfferedMission(harness.Client, eleanor.EntityId, 698, 0));
            Assert.AreEqual(0U, Owned(harness, 698, 2355));
            Assert.AreEqual(1U, Held(harness, 12831));
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, eleanor.EntityId, 698, 0));
            VerifyArmorReward(harness, 12831, 2, 141, CharacterClass.Specialist, 30);
        }

        internal static WildernessRuntimeTestHarness CreateIntegratedHub() =>
            WildernessRuntimeTestHarness.Create();

        internal static void SeedHistory(WildernessRuntimeTestHarness harness, params uint[] missions)
        {
            using var unit = harness.CreateChar();
            unit.ExecuteTransaction(() =>
            {
                foreach (var mission in missions)
                    unit.CharacterMissions.Runtime.Archive(new CharacterMissionEntry(
                        harness.Client.Player.Id, mission, (uint)MissionState.Completed)
                    {
                        ContentRevision = WildernessMissionDataV1.Revision
                    }, harness.UtcNow);
            });
            harness.Manager.HydrateAndClearInvalid(harness.Client.Player, unit);
        }

        internal static IEnumerable<Creature> Creatures(WildernessRuntimeTestHarness harness) =>
            harness.Map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList).Distinct();

        internal static Creature LiveCreature(WildernessRuntimeTestHarness harness, uint creatureId, uint spawnId)
        {
            var creature = Creatures(harness).FirstOrDefault(candidate => candidate.DbId == creatureId &&
                candidate.SpawnPool?.DbId == spawnId && candidate.State != CharacterState.Dead);
            Assert.IsNotNull(creature, $"Existing live creature{creatureId}/spawn{spawnId} is required.");
            return creature;
        }

        internal static void Kill(WildernessRuntimeTestHarness harness, Creature creature)
        {
            harness.MoveTo(creature.Position);
            creature.Attributes[Attributes.Health].Current = 0;
            harness.Creatures.HandleCreatureKill(harness.Map, creature, harness.Client.Player);
            harness.Tick();
        }

        internal static uint Held(WildernessRuntimeTestHarness harness, uint template) =>
            harness.Client.Player.Inventory.PersonalInventory.Where(id => id != 0)
                .Select(EntityManager.Instance.GetItem).Where(item => item.ItemTemplateId == template)
                .Aggregate(0U, (total, item) => total + item.StackSize);

        internal static uint Owned(WildernessRuntimeTestHarness harness, uint mission, uint template) =>
            harness.Client.Player.Inventory.PersonalInventory.Where(id => id != 0)
                .Select(EntityManager.Instance.GetItem).Where(item =>
                    item.ItemTemplateId == template && item.MissionOwnership?.MissionId == mission)
                .Aggregate(0U, (total, item) => total + item.StackSize);

        internal static void VerifyArmorReward(WildernessRuntimeTestHarness harness, uint template, uint slot,
            int expectedArmorValue, CharacterClass characterClass, uint skillId)
        {
            MissileManager.Instance.DoWork(harness.Map, 10000);
            WaitForCombatIdle(harness);
            var armor = PersonalItem(harness, template);
            Assert.AreEqual(expectedArmorValue, armor.ItemTemplate.ArmorValue,
                "The granted item must load PR105's class-sourced World armor row.");
            Assert.AreEqual(slot, (uint)EntityClassManager.Instance.GetEquipableClassInfo(armor).EquipmentSlotId);
            Assert.AreEqual(skillId, (uint)armor.ItemTemplate.EquipableInfo.SkillId);
            PrepareEquipmentEligibility(harness, characterClass, skillId);
            ManifestationManager.Instance.UpdateStatsValues(harness.Client, true);
            var before = harness.Client.Player.Attributes[Attributes.Armor].CurrentMax;
            var personalSlot = armor.OwnerSlotId;
            Assert.AreEqual(0UL, harness.Client.Player.Inventory.EquippedInventory[(int)slot]);
            var request = new RequestEquipArmorPacket
            {
                SrcInventory = InventoryType.Personal, SrcSlot = personalSlot, DestSlot = slot
            };
            InventoryManager.Instance.RequestEquipArmor(harness.Client, request);
            Assert.AreEqual(armor.EntityId, harness.Client.Player.Inventory.EquippedInventory[(int)slot]);
            var equippedMaximum = harness.Client.Player.Attributes[Attributes.Armor].CurrentMax;
            Assert.IsTrue(equippedMaximum > before, "Equipping the earned reward must increase real armor capacity.");
            Assert.IsTrue(harness.Client.Player.Attributes[Attributes.Armor].RefreshAmount > 0);
            using (var unit = harness.CreateChar())
            {
                var equipped = unit.CharacterInventories.FindByItemId(armor.Id);
                Assert.AreEqual((uint)InventoryType.EquipedInventory, equipped.InventoryType);
                Assert.AreEqual(slot, equipped.SlotId);
            }

            for (var second = 0; second < 60; second++)
                ActorManager.Instance.Regenerate(harness.Map);
            var armorBeforeHit = harness.Client.Player.Attributes[Attributes.Armor].Current;
            Assert.IsTrue(armorBeforeHit > 10);
            var healthBeforeHit = harness.Client.Player.Attributes[Attributes.Health].Current;
            harness.SpawnWorld(580019);
            var attacker = LiveCreature(harness, 3, 580019);
            harness.MoveTo(attacker.Position + new Vector3(0, 0, 2));
            var hitStartedAt = Environment.TickCount64;
            MissileManager.Instance.MissileLaunch(harness.Map,
                new ActionData(attacker, ActionId.WeaponAttack, 133, harness.Client.Player.EntityId, 0), 10);
            MissileManager.Instance.DoWork(harness.Map, 2000);
            var damagedArmor = harness.Client.Player.Attributes[Attributes.Armor].Current;
            Assert.IsTrue(damagedArmor < armorBeforeHit, "The ordinary combat path must consume armor protection.");
            Assert.AreEqual(healthBeforeHit, harness.Client.Player.Attributes[Attributes.Health].Current,
                "A protected, non-bypassing hit must not be charged to health instead of the equipped armor.");
            Assert.IsTrue(harness.Client.Player.InCombat);
            Assert.IsTrue(harness.Client.Player.CombatExpiresAt >= hitStartedAt + CombatRegen.CombatTimeoutMs,
                "Real damage must start the normal combat-idle timeout.");
            Assert.AreEqual(0, harness.Client.Player.Attributes[Attributes.Armor].RefreshAmount,
                "PR105 body armor does not regenerate in combat.");
            for (var second = 0; second < 10; second++)
                ActorManager.Instance.Regenerate(harness.Map);
            Assert.AreEqual(damagedArmor, harness.Client.Player.Attributes[Attributes.Armor].Current,
                "Regeneration ticks must leave damaged armor unchanged during combat.");
            ManifestationManager.Instance.CombatWorker(harness.Map);
            Assert.IsTrue(harness.Client.Player.InCombat, "An idle check must not bypass the damage timeout.");

            WaitForCombatIdle(harness);
            Assert.AreEqual(harness.Client.Player.ArmorRegenRate,
                harness.Client.Player.Attributes[Attributes.Armor].RefreshAmount);
            Assert.IsTrue(harness.Client.Player.Attributes[Attributes.Armor].RefreshAmount > 0);
            for (var second = 0; second < 10; second++)
                ActorManager.Instance.Regenerate(harness.Map);
            Assert.IsTrue(harness.Client.Player.Attributes[Attributes.Armor].Current > damagedArmor,
                "After the real combat-idle transition, the equipped reward must recover through actor ticks.");

            InventoryManager.Instance.RequestEquipArmor(harness.Client, request);
            Assert.AreEqual(0UL, harness.Client.Player.Inventory.EquippedInventory[(int)slot]);
            Assert.AreEqual(before, harness.Client.Player.Attributes[Attributes.Armor].CurrentMax);
            InventoryManager.Instance.RequestEquipArmor(harness.Client, request);
            Assert.AreEqual(equippedMaximum, harness.Client.Player.Attributes[Attributes.Armor].CurrentMax,
                "Re-equipping the same item must not stack its contribution twice.");
            InventoryManager.Instance.InitCharacterInventory(harness.Client);
            ManifestationManager.Instance.UpdateStatsValues(harness.Client, true);
            var restored = EntityManager.Instance.GetItem(harness.Client.Player.Inventory.EquippedInventory[(int)slot]);
            Assert.AreEqual(armor.Id, restored.Id);
            Assert.AreEqual(template, restored.ItemTemplateId);
            Assert.AreEqual(equippedMaximum, harness.Client.Player.Attributes[Attributes.Armor].CurrentMax);
        }

        internal static void VerifyWeaponReward(WildernessRuntimeTestHarness harness, uint template,
            uint expectedAmmoPerShot)
        {
            MissileManager.Instance.DoWork(harness.Map, 10000);
            InventoryManager.Instance.InitCharacterInventory(harness.Client);
            Assert.AreEqual(5, harness.Client.Player.Inventory.WeaponDrawer.Count,
                "Native inventory initialization must create the five weapon-drawer slots before equip.");
            var weapon = PersonalItem(harness, template);
            Assert.IsNotNull(weapon.ItemTemplate.WeaponInfo);
            var ammoPerShot = weapon.ItemTemplate.WeaponInfo.AmmoPerShot;
            Assert.AreEqual(expectedAmmoPerShot, ammoPerShot,
                "The native reward pistol spends one round; the native reward shotgun spends three.");
            var weaponClass = EntityClassManager.Instance.GetWeaponClassInfo(weapon);
            Assert.IsNotNull(weaponClass);
            Assert.AreEqual(13U, (uint)EntityClassManager.Instance.GetEquipableClassInfo(weapon).EquipmentSlotId);
            Assert.AreEqual(3147U, (uint)weaponClass.AmmoClassId);
            PrepareEquipmentEligibility(harness, CharacterClass.Recruit, 1);
            InventoryManager.Instance.RequestEquipWeapon(harness.Client, new RequestEquipWeaponPacket
            {
                InventoryType = InventoryType.Personal, SrcSlot = weapon.OwnerSlotId, DestSlot = 0
            });
            Assert.AreEqual(weapon.EntityId, harness.Client.Player.Inventory.WeaponDrawer[0]);
            ManifestationManager.Instance.RequestArmWeapon(harness.Client, 0);
            ManifestationManager.Instance.RequestWeaponDraw(harness.Client);
            Assert.IsTrue(harness.Client.Player.WeaponReady);
            Assert.AreEqual(weapon.EntityId, harness.Client.Player.Inventory.EquippedInventory[13]);
            using (var grant = new InventoryManager.InventoryGrant())
            {
                using (var unit = harness.CreateChar())
                    unit.ExecuteTransaction(() => grant.PlanAndSave(harness.Client,
                        new[] { new InventoryManager.InventoryItemGrant(28, 60) }, unit));
                grant.Publish(harness.Client);
            }
            ManifestationManager.Instance.RequestWeaponReload(harness.Client, true);
            ActorActionManager.Instance.DoWork(harness.Map, 10000);
            MissileManager.Instance.DoWork(harness.Map, 10000);
            Assert.IsTrue(weapon.CurrentAmmo > 0);
            harness.SpawnWorld(580019);
            var target = LiveCreature(harness, 3, 580019);
            harness.MoveTo(target.Position + new Vector3(0, 0, 2));
            harness.Client.Player.Target = target.EntityId;
            var before = target.Attributes[Attributes.Health].Current + target.Attributes[Attributes.Armor].Current;
            var clip = weapon.CurrentAmmo;
            Assert.IsTrue(clip >= ammoPerShot);
            var ammoAfterShot = clip - ammoPerShot;
            var reserveBeforeShot = Held(harness, 28);
            Assert.IsTrue(ManifestationManager.Instance.PlayerTryFireWeapon(harness.Client),
                "The awarded gun must fire through the normal weapon handler.");
            MissileManager.Instance.DoWork(harness.Map, 2000);
            Assert.IsTrue(target.Attributes[Attributes.Health].Current + target.Attributes[Attributes.Armor].Current < before);
            Assert.AreEqual(ammoAfterShot, weapon.CurrentAmmo);
            Assert.AreEqual(reserveBeforeShot, Held(harness, 28), "Firing consumes the clip, not reserve stacks.");
            using (var shot = harness.CreateChar())
                Assert.AreEqual(ammoAfterShot, shot.Items.GetItem(weapon.Id).AmmoCount);
            var reserve = Held(harness, 28);
            ManifestationManager.Instance.RequestWeaponReload(harness.Client, true);
            ActorActionManager.Instance.DoWork(harness.Map, 10000);
            Assert.IsTrue(weapon.CurrentAmmo > ammoAfterShot);
            var reloaded = weapon.CurrentAmmo - ammoAfterShot;
            Assert.IsTrue(reloaded <= reserve);
            Assert.AreEqual(reserve - reloaded, Held(harness, 28));
            using var verify = harness.CreateChar();
            Assert.AreEqual(weapon.CurrentAmmo, verify.Items.GetItem(weapon.Id).AmmoCount);
            Assert.AreEqual((uint)InventoryType.WeaponDrawerInventory,
                verify.CharacterInventories.FindByItemId(weapon.Id).InventoryType);
        }

        private static void WaitForCombatIdle(WildernessRuntimeTestHarness harness)
        {
            var deadline = harness.Client.Player.CombatExpiresAt;
            Assert.IsTrue(SpinWait.SpinUntil(() =>
            {
                ManifestationManager.Instance.CombatWorker(harness.Map);
                return !harness.Client.Player.InCombat;
            }, TimeSpan.FromMilliseconds(CombatRegen.CombatTimeoutMs + 1000)),
                "The normal idle worker must end combat after its actual damage timeout.");
            Assert.IsTrue(Environment.TickCount64 >= deadline,
                "The fixture must not force an early combat exit to restore armor regeneration.");
        }

        private static Item PersonalItem(WildernessRuntimeTestHarness harness, uint template) =>
            harness.Client.Player.Inventory.PersonalInventory.Where(id => id != 0)
                .Select(EntityManager.Instance.GetItem).Single(item => item.ItemTemplateId == template);

        private static void PrepareEquipmentEligibility(
            WildernessRuntimeTestHarness harness, CharacterClass characterClass, uint skillId)
        {
            var level = Math.Max((byte)20, harness.Client.Player.Level);
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

        internal static DynamicObject Object(WildernessRuntimeTestHarness harness, uint mission, string role)
        {
            var obj = harness.Map.DynamicObjects.SingleOrDefault(candidate =>
                candidate.SceneMissionId == mission && candidate.SceneActorRole == role &&
                candidate.SceneOwnerCharacterId == harness.Client.Player.Id && candidate.IsInWorld && candidate.IsEnabled);
            Assert.IsNotNull(obj, $"A real usable {mission}/{role} is required.");
            Assert.IsNotNull(obj.MissionUseAction);
            return obj;
        }

        internal static void Use(WildernessRuntimeTestHarness harness, DynamicObject obj, Client client = null)
        {
            client ??= harness.Client;
            client.SetWorldPosition(obj.Position, client.Player.Rotation);
            CellManager.Instance.UpdateVisibility(client);
            harness.Objects.RequestUseObjectPacket(client, new RequestUseObjectPacket
            {
                EntityId = obj.EntityId, ActionId = ActionId.UseObject, ActionArgId = obj.MissionUseAction.ActionArgId
            });
            ActorActionManager.Instance.DoWork(harness.Map, obj.WindupTime + 1);
            harness.Tick();
        }

        internal static void AssertRewardOffer(WildernessRuntimeTestHarness harness, Creature npc, uint mission,
            uint credits, params uint[] selectable)
        {
            harness.MoveTo(npc.Position);
            harness.Drain();
            new NpcManager(harness, harness.Manager).RequestNpcConverse(harness.Client,
                new RequestNPCConversePacket { EntityId = npc.EntityId, ActionId = ActionId.Converse, ActionArgId = 1 });
            var conversation = harness.Drain().OfType<ConversePacket>().Single();
            Assert.IsTrue(conversation.ConvoDataDict.TryGetValue(ConversationType.MissionDispense, out var value));
            Assert.IsInstanceOfType<Dictionary<uint, MissionInfo>>(value);
            var offers = (Dictionary<uint, MissionInfo>)value;
            Assert.IsTrue(offers.TryGetValue(mission, out var info), $"Native mission{mission} must be offered.");
            Assert.AreEqual(credits, info.MissionConstantData.RewardInfo.FixedReward.Credits[CurencyType.Credits]);
            CollectionAssert.AreEqual(selectable, info.MissionConstantData.RewardInfo.SelectableReward
                .Select(item => item.ItemTemplateId).ToArray());
            Assert.IsTrue(info.MissionConstantData.RewardInfo.SelectableReward.All(item => item.Quantity == 1 &&
                item.ModuleIds.Count == 0));
        }
    }
}
