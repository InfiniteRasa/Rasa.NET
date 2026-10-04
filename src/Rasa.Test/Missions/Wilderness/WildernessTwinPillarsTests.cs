using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Missions.Content.Wilderness;
using Rasa.Missions.Scenes;
using Rasa.Packets.Inventory.Client;
using Rasa.Packets.LootDispenser.Client;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.MapChannel.Server;
using Rasa.Packets.Mission.Server;
using Rasa.Packets.Protocol;
using Rasa.Services.Preloader.Missions.Wilderness;
using Rasa.Structures;
using Rasa.Structures.Char;
using Rasa.Structures.World;

namespace Rasa.Test.Missions.Wilderness
{
    [TestClass]
    [DoNotParallelize]
    public class WildernessTwinPillarsTests
    {
        [TestMethod]
        [DataRow("123")]
        [DataRow("132")]
        [DataRow("213")]
        [DataRow("231")]
        [DataRow("312")]
        [DataRow("321")]
        public void DeliverySceneWaitsForThreeDistinctCommittedDeliveriesAcrossRecovery(string order)
        {
            var registry = new SceneScriptRegistry();
            Assert.IsTrue(registry.TryResolve(WildernessSmugglerDeliveryScene.ScriptKey, 1, out var script));
            var bindings = new SceneBindings(WildernessMissionDataV1.Revision,
                new Dictionary<string, SceneActorDefinition>(), new Dictionary<string, SceneRoute>(),
                new Dictionary<uint, SceneSequence>
                {
                    [4] = new(characterIntents: new CharacterIntent[]
                    {
                        new ObjectiveIntent("complete-batch", 623, 5, MissionObjectiveState.Completed),
                        new ObjectiveIntent("reveal-moore", 623, 4, MissionObjectiveState.NotAssigned),
                        new ObjectiveIntent("activate-moore", 623, 4, MissionObjectiveState.Incomplete)
                    })
                });
            var run = new SceneRun("smuggling", WildernessMissionDataV1.Revision,
                WildernessSmugglerDeliveryScene.ScriptKey, 1, 1, 0, "{}", SceneStatus.Running, 1, 623);
            for (var count = 0; count < order.Length; count++)
            {
                var observation = new SceneObservation(SceneEventKind.Signal, 1, SequenceId: (uint)(order[count] - '0'));
                var result = script.Handle(new SceneContext(run, bindings, DateTime.UnixEpoch), observation);
                Assert.AreEqual(count == 2 ? 3 : 0, result.CharacterIntents.Count,
                    "The return stage must open after all three distinct receipts, never after an arbitrary last callback.");
                run = run with { Checkpoint = result.Checkpoint, Version = run.Version + 1 };
                Assert.IsTrue(new SceneScriptRegistry().TryResolve(WildernessSmugglerDeliveryScene.ScriptKey, 1, out script));
                var replay = script.Handle(new SceneContext(run, bindings, DateTime.UnixEpoch), observation);
                Assert.AreEqual(0, replay.CharacterIntents.Count);
                Assert.AreEqual(run.Checkpoint, replay.Checkpoint);
            }
        }

        [TestMethod]
        public void HubLoadsOnlyItsTenNativeMissionsAndPreservesTheirObjectiveIdentities()
        {
            using var harness = Create();
            var expected = new Dictionary<uint, uint[]>
            {
                [442] = new uint[] { 1, 2 },
                [444] = new uint[] { 1 },
                [623] = new uint[] { 1, 2, 3, 4, 5 },
                [791] = new uint[] { 1, 2, 3 },
                [570] = new uint[] { 1 },
                [574] = new uint[] { 2, 3 },
                [912] = new uint[] { 4 },
                [1634] = new uint[] { 4 },
                [1635] = new uint[] { 2 },
                [908] = new uint[] { 1 }
            };
            foreach (var pair in expected)
            {
                Assert.IsTrue(harness.Manager.TryGetOperationalMission(pair.Key, out var mission),
                    $"W5 mission {pair.Key} must be operational after its shared dependencies are integrated.");
                Assert.AreEqual(10000044U, mission.CategoryId);
                CollectionAssert.AreEqual(pair.Value, mission.Objectives.Keys.OrderBy(id => id).ToArray());
            }
            foreach (var excluded in new uint[] { 593, 923, 924, 751, 780, 767 })
                Assert.IsFalse(harness.Manager.TryGetOperationalMission(excluded, out _),
                    $"W5 must not enable excluded or instance-only mission {excluded}.");
        }

        [TestMethod]
        [DataRow(630100U, -72.0, 216.892554, 120.0, 0.4092564, 216.79255359731442)]
        [DataRow(630101U, -116.0, 216.282193, 100.0, -1.308520799, 216.18219272144657)]
        [DataRow(630102U, -93.0, 209.812367, 48.0, -2.945533632, 209.71236743724728)]
        public void WorldBProvidesTheAdoptedMachinaPopulationAndNativeAttack(
            uint spawnId, double x, double y, double z, double heading, double nativeSupportY)
        {
            using var harness = Create();
            var creature = harness.World.Set<CreatureEntry>().Single(entry => entry.Id == 630100);
            Assert.AreEqual(6236U, creature.ClassId);
            var weapon = harness.World.Set<CreatureAppearanceEntry>()
                .Single(entry => entry.Id == 630100 && entry.SlotId == 13);
            Assert.AreEqual(6019U, weapon.ClassId);
            var attack = harness.World.Set<CreatureActionEntry>().Single(entry => entry.Id == creature.Action1);
            Assert.AreEqual(1U, attack.ActionId);
            Assert.AreEqual(154U, attack.ActionArgId);
            Assert.AreEqual(40.0, attack.RangeMax, 0.000001);
            Assert.IsTrue(attack.MinDamage > 0 && attack.MaxDamage >= attack.MinDamage);
            var spawn = harness.World.Set<SpawnPoolEntry>().Single(entry => entry.Id == spawnId);
            Assert.IsTrue(Spawns(spawn, 630100));
            Assert.AreEqual(1220U, spawn.MapContextId);
            Assert.AreEqual(x, spawn.PosX, 0.000001);
            Assert.AreEqual(y, spawn.PosY, 0.000001);
            Assert.AreEqual(z, spawn.PosZ, 0.000001);
            Assert.AreEqual(heading, spawn.Rotation, 0.000001);
            harness.SpawnWorld(spawnId);
            var actor = harness.Map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList)
                .Single(entry => entry.SpawnPool?.DbId == spawnId);
            Assert.AreEqual(630100U, actor.DbId);
            AssertQualifiedRuntimeGrounding(harness, actor, new Vector3((float)x, (float)y, (float)z), nativeSupportY);
        }

        [TestMethod]
        public void GeorgeUsesTheAdoptedHospitalInsteadOfTheElevatedMarker()
        {
            using var harness = Create();
            var spawn = harness.World.Set<SpawnPoolEntry>().Single(entry => entry.Id == 510005);
            Assert.AreEqual(-698.0, spawn.PosX, 0.000001);
            Assert.AreEqual(170.233, spawn.PosY, 0.000001);
            Assert.AreEqual(-345.0, spawn.PosZ, 0.000001);
            Assert.AreEqual(-2.35619449, spawn.Rotation, 0.000001);
            harness.SpawnWorld(510005);
            var george = RequireNpc(harness, 510005, 510005);
            AssertQualifiedRuntimeGrounding(harness, george,
                new Vector3(-698, 170.233f, -345), 170.13299765238838);
            harness.MoveTo(new Vector3(-696, 170.233f, -343));
            Assert.IsTrue(Vector3.Distance(harness.Client.Player.Position, george.Position) < 5);
        }

        [TestMethod]
        public void CormanChainRequiresDuncanRealBloodAnalysisAndOwnedResultsDelivery()
        {
            using var harness = Create();
            harness.SpawnWorld(218, 203, 173);
            var victor = RequireNpc(harness, 218, 140);
            var duncan = RequireNpc(harness, 203, 125);
            var eleanor = RequireNpc(harness, 173, 94);
            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, victor.EntityId, 444),
                "Unity Among Men cannot be admitted before Quarantine is rewarded.");
            Accept(harness, victor, 442);
            Assert.AreEqual(MissionObjectiveState.Inactive, harness.Client.Player.Missions[442].Objectives[2].State);
            Assert.AreEqual(0L, Held(harness, 749));
            var analyzer = harness.Map.DynamicObjects.SingleOrDefault(obj =>
                obj.SceneMissionId == 442 && obj.SceneActorRole == "blood-analyzer");
            Assert.IsNotNull(analyzer);
            Assert.AreEqual(7123U, (uint)analyzer.EntityClassId);
            Assert.IsTrue(Vector3.Distance(analyzer.Position, new Vector3(-124.8f, 222.04809f, -479.2f)) < 0.001f);
            Assert.AreEqual(Math.PI, analyzer.Rotation, 0.000001);
            Assert.IsNotNull(analyzer.MissionConversation);
            Assert.AreEqual(1486U, analyzer.MissionConversation.NpcPackageId);

            Assert.IsNull(OpenAnalyzer(harness, analyzer));

            Assert.AreEqual(MissionObjectiveState.Inactive, harness.Client.Player.Missions[442].Objectives[2].State);
            Assert.AreEqual(0L, Held(harness, 749));
            Talk(harness, duncan, 442, 1);
            Assert.AreEqual(1L, Held(harness, 749));
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[442].Objectives[2].State);
            var sample = Inventory(harness).Single(item => item.ItemTemplate.ItemTemplateId == 749);
            Assert.AreEqual(harness.Client.Player.Missions[442].AssignmentId, sample.MissionOwnership.AssignmentId);

            var analysis = OpenAnalyzer(harness, analyzer);
            Assert.IsNotNull(analysis);
            Assert.IsTrue(analysis.ConvoDataDict.ContainsKey(ConversationType.ObjectiveComplete));
            Assert.AreEqual(1L, Held(harness, 749));
            new NpcManager(harness, harness.Manager).CompleteNPCObjective(harness.Client,
                new CompleteNPCObjectivePacket
                {
                    EntityId = analyzer.EntityId,
                    MissionId = 442,
                    ObjectiveId = 2,
                    PlayerFlagId = 1
                });

            Assert.AreEqual(0L, Held(harness, 749));
            Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[442].Objectives[2].State);
            Assert.IsNull(OpenAnalyzer(harness, analyzer));
            Assert.AreEqual(0L, Held(harness, 749));
            Reward(harness, victor, 442, 0, 3315, 3500, 700);
            Accept(harness, victor, 444);
            Assert.AreEqual(1L, Held(harness, 750));
            var results = Inventory(harness).Single(item => item.ItemTemplate.ItemTemplateId == 750);
            Assert.AreEqual(harness.Client.Player.Missions[444].AssignmentId, results.MissionOwnership.AssignmentId);

            Talk(harness, eleanor, 444, 1);

            Assert.AreEqual(0L, Held(harness, 750));
            Reward(harness, eleanor, 444, 1, 20697, 6000, 1200);
            Reload(harness);
            Assert.AreEqual(MissionState.Completed, harness.Client.Player.Missions[442].State);
            Assert.AreEqual(MissionState.Completed, harness.Client.Player.Missions[444].State);
            Assert.AreEqual(0L, Held(harness, 749));
            Assert.AreEqual(0L, Held(harness, 750));
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void QuarantineFailureOrAbandonmentCleansOnlyItsIssuedBlood(bool abandon)
        {
            using var harness = Create();
            harness.SpawnWorld(218, 203);
            var victor = RequireNpc(harness, 218, 140);
            var duncan = RequireNpc(harness, 203, 125);
            Accept(harness, victor, 442);
            Talk(harness, duncan, 442, 1);
            var assignment = harness.Client.Player.Missions[442].AssignmentId;
            Assert.AreEqual(1L, Held(harness, 749));

            Assert.IsTrue(abandon
                ? harness.Manager.TryAbandon(harness.Client, 442)
                : harness.Manager.TryFailMission(harness.Client, 442));

            Assert.AreEqual(0L, Held(harness, 749));
            using var unit = harness.CreateChar();
            Assert.IsFalse(unit.CharacterMissionItems.GetOwned(harness.Client.Player.Id)
                .Any(item => item.AssignmentId == assignment));
        }

        [TestMethod]
        public void DuncanDoesNotAdvanceQuarantineWhenTheSampleCannotFit()
        {
            using var harness = Create();
            FillQuestInventory(harness);
            harness.SpawnWorld(218, 203);
            Accept(harness, RequireNpc(harness, 218, 140), 442);
            var duncan = RequireNpc(harness, 203, 125);
            harness.MoveTo(duncan.Position);
            OpenNativeNpcConversation(harness, duncan);
            var before = harness.Context.ReadRewardTotals();
            harness.Drain();

            Assert.IsFalse(harness.Manager.TryCompleteNpcObjective(harness.Client, duncan.EntityId, 442, 1, 1));

            Assert.AreEqual(before, harness.Context.ReadRewardTotals());
            Assert.AreEqual(0L, Held(harness, 749));
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[442].Objectives[1].State);
            Assert.AreEqual(MissionObjectiveState.Inactive, harness.Client.Player.Missions[442].Objectives[2].State);
            Assert.IsFalse(harness.Drain().OfType<ObjectiveCompletedPacket>().Any());
            var stack = Inventory(harness).First(item => item.ItemTemplate.ItemTemplateId == 753);
            InventoryManager.Instance.PersonalInventory_DestroyItem(harness.Client,
                new PersonalInventory_DestroyItemPacket { EntityId = stack.EntityId, Quantity = stack.StackSize });

            Talk(harness, duncan, 442, 1);

            Assert.AreEqual(1L, Held(harness, 749));
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[442].Objectives[2].State);
        }

        [TestMethod]
        [DataRow(912U, 4U, 38U, 4000U, 1500)]
        [DataRow(1634U, 4U, 49U, 4500U, 800)]
        [DataRow(1635U, 2U, 53U, 4500U, 900)]
        [DataRow(908U, 1U, 9U, 4000U, 600)]
        public void OutdoorLogosUsesItsActualShrineAndRewardsOnlyOnce(
            uint missionId, uint objectiveId, uint logosId, uint experience, int credits)
        {
            using var harness = Create();
            harness.SpawnWorld(212);
            var standley = RequireNpc(harness, 212, 134);
            if (missionId == 908)
            {
                Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, standley.EntityId, missionId));
                SeedCompletedHistory(harness, 1069);
            }
            Accept(harness, standley, missionId);
            Assert.AreEqual(MissionObjectiveState.Incomplete,
                harness.Client.Player.Missions[missionId].Objectives[objectiveId].State);
            Assert.IsFalse(harness.Client.Player.Logos.Contains(logosId));
            if (logosId == 53)
            {
                Assert.IsFalse(harness.Client.Player.Logos.Contains(56U));
                Assert.IsFalse(harness.Client.Player.Logos.Contains(23U));
                AcquireLogos(harness, 56);
                AcquireLogos(harness, 23);
            }

            AcquireLogos(harness, logosId);

            Assert.AreEqual(MissionObjectiveState.Completed,
                harness.Client.Player.Missions[missionId].Objectives[objectiveId].State);
            Reward(harness, standley, missionId, null, null, experience, credits);
            Reload(harness);
            Assert.IsTrue(harness.Client.Player.Logos.Contains(logosId));
            Assert.AreEqual(MissionState.Completed, harness.Client.Player.Missions[missionId].State);
            Assert.AreEqual(0L, Inventory(harness).Sum(item => (long)item.StackSize),
                "A Logos reward must not manufacture inventory copies of shrine knowledge.");
        }

        [TestMethod]
        [DataRow(912U, 4U, 38U)]
        [DataRow(1634U, 4U, 49U)]
        [DataRow(1635U, 2U, 53U)]
        [DataRow(908U, 1U, 9U)]
        public void AlreadyLearnedOutdoorLogosIsRecognizedAtAcceptance(
            uint missionId, uint objectiveId, uint logosId)
        {
            using var harness = Create();
            if (missionId == 908)
                SeedCompletedHistory(harness, 1069);
            if (logosId == 53)
            {
                AcquireLogos(harness, 56);
                AcquireLogos(harness, 23);
            }
            AcquireLogos(harness, logosId);
            harness.SpawnWorld(212);

            Accept(harness, RequireNpc(harness, 212, 134), missionId);

            Assert.AreEqual(MissionObjectiveState.Completed,
                harness.Client.Player.Missions[missionId].Objectives[objectiveId].State);
            Assert.IsTrue(harness.Client.Player.Missions[missionId].Completeable);
            using var unit = harness.CreateChar();
            Assert.AreEqual(1, unit.CharacterLogoses.GetLogos(harness.Client.Player.Id).Count(id => id == logosId));
        }

        [TestMethod]
        public void MachinationsCollectsActualForeanCorpsesAndFinishesOutsidePravus()
        {
            using var harness = Create();
            harness.SpawnWorld(207, 183, 197);
            var taylor = RequireNpc(harness, 207, 129);
            var baruhi = RequireNpc(harness, 183, 106);
            var parsons = RequireNpc(harness, 197, 119);
            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, baruhi.EntityId, 574));
            Accept(harness, taylor, 570);
            Talk(harness, baruhi, 570, 1);
            Reward(harness, baruhi, 570, 1, 20846, 8000, 1200);
            Accept(harness, baruhi, 574);
            CollectMachinaRemains(harness);
            Assert.AreEqual(10L, Held(harness, 753));
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[574].Objectives[3].State);
            Talk(harness, parsons, 574, 3);
            Reward(harness, parsons, 574, 0, 20548, 8000, 1200);
            Assert.AreEqual(0L, Held(harness, 753));
            Assert.AreEqual(1220U, harness.Client.Player.MapContextId);
            Assert.IsFalse(harness.Manager.TryGetOperationalMission(593, out _));
        }

        private static void CollectMachinaRemains(WildernessRuntimeTestHarness harness)
        {
            var drop = harness.Manager.LoadedMissions[574].Items["machina-remains"].Drop;
            var creatureId = drop.CreatureIds.Single();
            Assert.AreEqual(630100U, creatureId);
            var source = harness.World.Set<CreatureEntry>().Single(creature => creature.Id == creatureId);
            Assert.AreEqual(6236U, source.ClassId);
            for (uint count = 0; count < 10; count++)
            {
                var spawnId = 630100U + count % 3;
                harness.SpawnWorldAfter(60000, spawnId);
                var creature = harness.Map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList)
                    .FirstOrDefault(actor => actor.DbId == creatureId &&
                        actor.SpawnPool?.DbId == spawnId && actor.State != CharacterState.Dead);
                Assert.IsNotNull(creature);
                harness.MoveTo(creature.Position);
                Assert.AreEqual(Race.Human, harness.Client.Player.Race);
                Assert.IsTrue(harness.Client.Player.Attributes[Attributes.Health].Current > 0);
                var lethalDamage = checked(creature.Attributes[Attributes.Health].Current +
                    Math.Max(0, creature.Attributes[Attributes.Armor].Current));
                Assert.IsTrue(lethalDamage > 0);
                Assert.AreEqual(lethalDamage,
                    ActorManager.Instance.Damage(harness.Map, creature, lethalDamage, harness.Client.Player));
                Assert.AreEqual(CharacterState.Dead, creature.State);
                Assert.AreEqual(0, creature.Attributes[Attributes.Health].Current,
                    "The real damage path must produce a zero-HP corpse before a loot claim is valid.");
                Assert.AreEqual(Race.Human, harness.Client.Player.Race);
                Assert.IsTrue(harness.Client.Player.Attributes[Attributes.Health].Current > 0,
                    "Kill XP and level-up must leave a living player eligible to claim the corpse.");
                Assert.AreEqual(count, harness.Client.Player.Missions[574].Objectives[2].ItemCounters[7991],
                    "A kill alone must not substitute for taking the actual remains.");
                Assert.AreNotEqual(0UL, creature.CorpseLootEntityId);
                var loot = harness.Map.LootDispensers[creature.CorpseLootEntityId];
                Assert.AreEqual(harness.Client.Player.EntityId, loot.Owner);
                var remains = loot.LootItems.Single(item => item.ItemTemplateId == 753);
                Assert.AreEqual(1U, remains.ItemQuantity);
                Assert.IsFalse(remains.Taken);
                Assert.AreEqual((long)count, Held(harness, 753));
                LootDispenserManager.Instance.RequestLootAllFromCorpse(harness.Client,
                    new RequestLootAllFromCorpsePacket { EntityId = loot.EntityId });
                Assert.IsTrue(remains.Taken, "Native pickup must claim the actual template753 remains.");
                Assert.AreEqual((long)count + 1, Held(harness, 753));
                using (var unit = harness.CreateChar())
                {
                    // Fully merged loot rows are retired; verify the surviving inventory stacks.
                    var slots = unit.CharacterInventories.GetItems(harness.Client.AccountEntry.Id)
                        .Where(entry => entry.CharacterId == harness.Client.Player.Id &&
                            entry.InventoryType == (uint)InventoryType.Personal).ToArray();
                    var saved = unit.Items.GetItems(slots.Select(entry => entry.ItemId).ToArray())
                        .Where(entry => entry.ItemTemplateId == 753).ToArray();
                    var live = Inventory(harness).Where(entry => entry.ItemTemplate.ItemTemplateId == 753).ToArray();
                    Assert.AreEqual((long)count + 1, saved.Sum(entry => (long)entry.StackSize),
                        "Every claimed remains unit must persist in this character's personal inventory.");
                    CollectionAssert.AreEquivalent(saved.Select(entry => entry.ItemId).ToArray(),
                        live.Select(entry => entry.Id).ToArray(),
                        "Runtime and durable remains identities must agree after native stack merging.");
                    foreach (var stack in saved)
                    {
                        Assert.AreEqual(1, slots.Count(entry => entry.ItemId == stack.ItemId));
                        Assert.IsTrue(stack.StackSize > 0);
                        Assert.AreEqual(stack.StackSize, live.Single(entry => entry.Id == stack.ItemId).StackSize);
                    }
                }
                Assert.AreEqual(count + 1, harness.Client.Player.Missions[574].Objectives[2].ItemCounters[7991]);
            }
        }

        [TestMethod]
        public void RetainedMachinaRemainsAreRecognizedWithoutReplayingHistoricalKills()
        {
            using var harness = Create();
            SeedCompletedHistory(harness, 570);
            using (var grant = new InventoryManager.InventoryGrant())
            {
                using (var unit = harness.CreateChar())
                    unit.ExecuteTransaction(() => grant.PlanAndSave(harness.Client,
                        new[] { new InventoryManager.InventoryItemGrant(753, 10) }, unit));
                grant.Publish(harness.Client);
            }
            harness.SpawnWorld(183);

            Accept(harness, RequireNpc(harness, 183, 106), 574);

            Assert.AreEqual(10L, Held(harness, 753));
            Assert.AreEqual(10U, harness.Client.Player.Missions[574].Objectives[2].ItemCounters[7991]);
            Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[574].Objectives[2].State);
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[574].Objectives[3].State);
        }

        [TestMethod]
        [DataRow(444U, 0, 20399U, 13664U, 3U, 30U, CharacterClass.Specialist)]
        [DataRow(444U, 1, 20697U, 13756U, 16U, 30U, CharacterClass.Specialist)]
        [DataRow(570U, 1, 20846U, 13802U, 15U, 30U, CharacterClass.Specialist)]
        [DataRow(574U, 0, 20548U, 13710U, 1U, 30U, CharacterClass.Specialist)]
        [DataRow(574U, 1, 36083U, 18596U, 15U, 21U, CharacterClass.Soldier)]
        public void RewardedArmorEquipsProtectsReloadsAndUnequips(
            uint missionId, int selection, uint templateId, uint classId, uint slot,
            uint skillId, CharacterClass characterClass)
        {
            using var harness = Create();
            EarnArmorReward(harness, missionId, selection, templateId);
            PrepareArmorEligibility(harness, characterClass, skillId);
            var item = Inventory(harness).Single(entry => entry.ItemTemplate.ItemTemplateId == templateId);
            var itemId = item.Id;
            Assert.AreEqual(classId, (uint)item.ItemTemplate.Class);
            Assert.IsTrue(item.ItemTemplate.ArmorValue > 0,
                "A real reward template must load positive capacity from shared World armor data.");
            Assert.AreEqual<int>(harness.World.Set<ItemTemplateArmorEntry>().Single(entry => entry.Id == templateId).ArmorValue,
                item.ItemTemplate.ArmorValue);
            Assert.IsNotNull(item.ItemTemplate.EquipableInfo);
            Assert.AreEqual(skillId, (uint)item.ItemTemplate.EquipableInfo.SkillId);
            Assert.IsTrue(harness.Client.Player.Skills[(SkillId)skillId].SkillLevel >= item.ItemTemplate.EquipableInfo.SkillLevel);
            Assert.AreEqual(slot, (uint)EntityClassManager.Instance.GetEquipableClassInfo(item).EquipmentSlotId);
            Assert.IsTrue(InventoryManager.Instance.ValidateItemEquip(harness.Client, item),
                "Exercise a genuinely eligible character, not an equipment rejection path.");
            var baseline = harness.Client.Player.Attributes[Attributes.Armor].CurrentMax;
            Assert.AreEqual(0, baseline);
            var sourceSlot = (uint)harness.Client.Player.Inventory.PersonalInventory.IndexOf(item.EntityId);

            InventoryManager.Instance.RequestEquipArmor(harness.Client, new RequestEquipArmorPacket
            {
                SrcInventory = InventoryType.Personal, SrcSlot = sourceSlot, DestSlot = slot
            });

            Assert.AreEqual(item.EntityId, harness.Client.Player.Inventory.EquippedInventory[(int)slot]);
            var capacity = harness.Client.Player.Attributes[Attributes.Armor].CurrentMax;
            Assert.IsTrue(capacity > baseline, "Equipping the actual reward must increase server armor capacity.");
            AssertEquippedPersistence(harness, itemId, slot);
            for (var tick = 0; tick < 100 && harness.Client.Player.Attributes[Attributes.Armor].Current < 10; tick++)
                ActorManager.Instance.Regenerate(harness.Map);
            var armorBeforeHit = harness.Client.Player.Attributes[Attributes.Armor].Current;
            Assert.IsTrue(armorBeforeHit >= 5, "Real armor regeneration must fill the equipped capacity.");
            harness.SpawnWorldAfter(60000, 630100);
            var attacker = harness.Map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList)
                .First(actor => actor.DbId == 630100 && actor.State != CharacterState.Dead);
            harness.MoveTo(attacker.Position + new Vector3(2, 0, 0));
            var healthBeforeHit = harness.Client.Player.Attributes[Attributes.Health].Current;

            Assert.AreEqual(5, ActorManager.Instance.Damage(harness.Map, harness.Client.Player, 5, attacker));

            Assert.IsTrue(harness.Client.Player.Attributes[Attributes.Armor].Current < armorBeforeHit);
            Assert.AreEqual(healthBeforeHit, harness.Client.Player.Attributes[Attributes.Health].Current,
                "The equipped reward must protect health from a real server damage operation.");
            Reload(harness);
            ManifestationManager.Instance.UpdateStatsValues(harness.Client, false);
            var reloaded = EntityManager.Instance.GetItem(harness.Client.Player.Inventory.EquippedInventory[(int)slot]);
            Assert.IsNotNull(reloaded);
            Assert.AreEqual(itemId, reloaded.Id);
            Assert.AreEqual(templateId, reloaded.ItemTemplate.ItemTemplateId);
            Assert.AreEqual(capacity, harness.Client.Player.Attributes[Attributes.Armor].CurrentMax);
            AssertEquippedPersistence(harness, itemId, slot);
            var emptySlot = harness.Client.Player.Inventory.PersonalInventory.Take(50).ToList().FindIndex(id => id == 0);
            Assert.IsTrue(emptySlot >= 0);

            InventoryManager.Instance.RequestEquipArmor(harness.Client, new RequestEquipArmorPacket
            {
                SrcInventory = InventoryType.Personal, SrcSlot = (uint)emptySlot, DestSlot = slot
            });

            Assert.AreEqual(0UL, harness.Client.Player.Inventory.EquippedInventory[(int)slot]);
            Assert.AreEqual(baseline, harness.Client.Player.Attributes[Attributes.Armor].CurrentMax);
            var unarmoredHealth = harness.Client.Player.Attributes[Attributes.Health].Current;
            Assert.IsTrue(unarmoredHealth > 5);
            Assert.AreEqual(5, ActorManager.Instance.Damage(harness.Map, harness.Client.Player, 5, attacker));
            Assert.IsTrue(harness.Client.Player.Attributes[Attributes.Health].Current < unarmoredHealth,
                "After unequipping, the same damage must reach health instead of phantom armor.");
            Reload(harness);
            ManifestationManager.Instance.UpdateStatsValues(harness.Client, false);
            Assert.AreEqual(baseline, harness.Client.Player.Attributes[Attributes.Armor].CurrentMax);
            Assert.AreEqual(0UL, harness.Client.Player.Inventory.EquippedInventory[(int)slot]);
            Assert.IsTrue(Inventory(harness).Any(entry => entry.Id == itemId));
        }

        private static void EarnArmorReward(WildernessRuntimeTestHarness harness, uint missionId, int selection, uint templateId)
        {
            switch (missionId)
            {
                case 444:
                    SeedCompletedHistory(harness, 442);
                    harness.SpawnWorld(218, 173);
                    Accept(harness, RequireNpc(harness, 218, 140), 444);
                    var eleanor = RequireNpc(harness, 173, 94);
                    Talk(harness, eleanor, 444, 1);
                    Reward(harness, eleanor, 444, selection, templateId, 6000, 1200);
                    break;
                case 570:
                    harness.SpawnWorld(207, 183);
                    Accept(harness, RequireNpc(harness, 207, 129), 570);
                    var baruhi = RequireNpc(harness, 183, 106);
                    Talk(harness, baruhi, 570, 1);
                    Reward(harness, baruhi, 570, selection, templateId, 8000, 1200);
                    break;
                case 574:
                    SeedCompletedHistory(harness, 570);
                    harness.SpawnWorld(183, 197);
                    Accept(harness, RequireNpc(harness, 183, 106), 574);
                    CollectMachinaRemains(harness);
                    var parsons = RequireNpc(harness, 197, 119);
                    Talk(harness, parsons, 574, 3);
                    Reward(harness, parsons, 574, selection, templateId, 8000, 1200);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(missionId));
            }
        }

        private static void PrepareArmorEligibility(WildernessRuntimeTestHarness harness,
            CharacterClass characterClass, uint skillId)
        {
            using (var unit = harness.CreateChar())
                unit.ExecuteTransaction(() =>
                {
                    unit.Characters.UpdateCharacterLevel(harness.Client.Player.Id, 8);
                    unit.Characters.UpdateCharacterClass(harness.Client.Player.Id, (uint)characterClass);
                    unit.CharacterSkills.AddOrUpdate(harness.Client.Player.Id, skillId, -1, 1);
                });
            harness.Client.Player.Level = 8;
            harness.Client.Player.Class = (uint)characterClass;
            harness.Client.Player.Skills[(SkillId)skillId] = new SkillsData((SkillId)skillId, -1, 1);
            InventoryManager.Instance.InitCharacterInventory(harness.Client);
            ManifestationManager.Instance.UpdateStatsValues(harness.Client, true);
        }

        private static void AssertEquippedPersistence(WildernessRuntimeTestHarness harness, uint itemId, uint slot)
        {
            using var unit = harness.CreateChar();
            var row = unit.CharacterInventories.GetItems(harness.Client.AccountEntry.Id).Single(entry => entry.ItemId == itemId);
            Assert.AreEqual(harness.Client.Player.Id, row.CharacterId);
            Assert.AreEqual((uint)InventoryType.EquipedInventory, row.InventoryType);
            Assert.AreEqual(slot, row.SlotId);
        }

        internal static WildernessRuntimeTestHarness Create()
        {
            var harness = WildernessRuntimeTestHarness.Create();
            try
            {
                using var unit = harness.CreateChar();
                unit.ExecuteTransaction(() => unit.CharacterStartingExperience.Add(
                    new CharacterStartingExperienceEntry(harness.Client.Player.Id, WildernessMissionDataV1.Revision,
                        CharacterStartingExperienceState.Skipped)));
                harness.Client.Player.StartingExperienceCompleted = true;
                harness.Drain();
                return harness;
            }
            catch
            {
                harness.Dispose();
                throw;
            }
        }

        internal static Creature RequireNpc(WildernessRuntimeTestHarness harness, uint spawnId, uint creatureId)
        {
            var npc = harness.Npc(spawnId);
            Assert.IsNotNull(npc, $"Shared World must provide actual NPC {creatureId} at spawn {spawnId}.");
            Assert.AreEqual(creatureId, npc.DbId);
            return npc;
        }

        internal static void Accept(WildernessRuntimeTestHarness harness, Creature npc, uint missionId)
        {
            OpenNativeNpcConversation(harness, npc);
            Assert.IsTrue(harness.Manager.TryAcceptNpcMission(harness.Client, npc.EntityId, missionId),
                $"Native NPC {npc.DbId} must offer and accept mission {missionId}.");
            harness.Drain();
        }

        internal static ConversePacket OpenNativeNpcConversation(WildernessRuntimeTestHarness harness, Creature npc)
        {
            harness.MoveTo(npc.DbId == 510005 ? new Vector3(-696, 170.233f, -343) : npc.Position);
            lock (harness.Client.SyncRoot)
            {
                // Opening must not hide earlier reward/failure packets or discard the native reply.
                var pending = new List<ProtocolPacket>();
                while (harness.Client.DequeueOutgoingPacket() is ProtocolPacket queued)
                    pending.Add(queued);
                try
                {
                    new NpcManager(harness, harness.Manager).RequestNpcConverse(harness.Client,
                        new RequestNPCConversePacket
                        {
                            EntityId = npc.EntityId, ActionId = ActionId.Converse, ActionArgId = 1
                        });
                    ConversePacket conversation = null;
                    while (harness.Client.DequeueOutgoingPacket() is ProtocolPacket emitted)
                    {
                        pending.Add(emitted);
                        if (emitted.Message is CallMethodMessage { Packet: ConversePacket opened })
                            conversation = opened;
                    }
                    Assert.IsNotNull(conversation, $"Actual NPC {npc.DbId} did not emit a native Converse packet.");
                    return conversation;
                }
                finally
                {
                    foreach (var preserved in pending)
                        harness.Client.SendMessage(preserved.Message, preserved.Compress, preserved.Channel);
                }
            }
        }

        internal static void Talk(WildernessRuntimeTestHarness harness, Creature npc, uint missionId, uint objectiveId)
        {
            OpenNativeNpcConversation(harness, npc);
            Assert.IsTrue(harness.Manager.TryCompleteNpcObjective(harness.Client, npc.EntityId, missionId, objectiveId, 1));
            harness.Drain();
        }

        internal static void Reward(WildernessRuntimeTestHarness harness, Creature npc, uint missionId,
            int? selection, uint? expectedTemplate, uint experience, int credits)
        {
            var conversation = OpenNativeNpcConversation(harness, npc);
            var previews = (Dictionary<uint, RewardInfo>)conversation.ConvoDataDict[ConversationType.MissionComplete];
            var preview = previews[missionId];
            if (expectedTemplate.HasValue)
            {
                var item = selection.HasValue
                    ? preview.SelectableReward[selection.Value]
                    : preview.FixedReward.FixedItems.Single();
                Assert.AreEqual(expectedTemplate.Value, item.ItemTemplateId);
                Assert.AreEqual(1U, item.Quantity);
            }
            var before = harness.Context.ReadRewardTotals();
            var held = expectedTemplate.HasValue ? Held(harness, expectedTemplate.Value) : 0;

            Assert.IsTrue(harness.Manager.TryCompleteNpcMission(harness.Client, npc.EntityId, missionId, selection, null));

            var after = harness.Context.ReadRewardTotals();
            Assert.AreEqual(before.Experience + experience, after.Experience);
            Assert.AreEqual(before.Credits + credits, after.Credits);
            if (expectedTemplate.HasValue)
                Assert.AreEqual(held + 1, Held(harness, expectedTemplate.Value));
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count());
            Assert.IsFalse(harness.Manager.TryCompleteNpcMission(harness.Client, npc.EntityId, missionId, selection, null));
            Assert.AreEqual(after, harness.Context.ReadRewardTotals());
        }

        internal static void Reload(WildernessRuntimeTestHarness harness)
        {
            using var unit = harness.CreateChar();
            harness.Manager.Hydrate(harness.Client.Player, unit.CharacterMissions.Get(harness.Client.Player.Id),
                unit.CharacterMissionProgress.Get(harness.Client.Player.Id));
            InventoryManager.Instance.InitCharacterInventory(harness.Client);
            harness.Manager.PublishInitialState(harness.Client);
            harness.Drain();
        }

        internal static Item[] Inventory(WildernessRuntimeTestHarness harness) =>
            harness.Client.Player.Inventory.PersonalInventory.Where(id => id != 0)
                .Select(EntityManager.Instance.GetItem).ToArray();

        internal static long Held(WildernessRuntimeTestHarness harness, uint template) =>
            Inventory(harness).Where(item => item.ItemTemplate.ItemTemplateId == template)
                .Sum(item => (long)item.StackSize);

        internal static void FillQuestInventory(WildernessRuntimeTestHarness harness)
        {
            using var grant = new InventoryManager.InventoryGrant();
            using (var unit = harness.CreateChar())
                unit.ExecuteTransaction(() => grant.PlanAndSave(harness.Client,
                    new[] { new InventoryManager.InventoryItemGrant(753, 500) }, unit));
            grant.Publish(harness.Client);
            Assert.AreEqual(50, Inventory(harness).Count(item => item.ItemTemplate.ItemTemplateId == 753));
        }

        private static void Use(WildernessRuntimeTestHarness harness, DynamicObject obj, uint argument)
        {
            harness.MoveTo(obj.Position);
            harness.Objects.RequestUseObjectPacket(harness.Client, new RequestUseObjectPacket
            {
                EntityId = obj.EntityId,
                ActionId = ActionId.UseObject,
                ActionArgId = argument
            });
            ActorActionManager.Instance.DoWork(harness.Map, 10000);
            harness.Drain();
        }

        private static ConversePacket OpenAnalyzer(WildernessRuntimeTestHarness harness, DynamicObject analyzer)
        {
            harness.MoveTo(new Vector3(-124.8f, 220.91783f, -477.2f));
            Assert.IsTrue(analyzer.Position.Y - harness.Client.Player.Position.Y > 1);
            Assert.IsTrue(Vector3.Distance(analyzer.Position, harness.Client.Player.Position) < 5);
            harness.Drain();
            new NpcManager(harness, harness.Manager).RequestNpcConverse(harness.Client,
                new RequestNPCConversePacket { EntityId = analyzer.EntityId, ActionId = ActionId.Converse, ActionArgId = 1 });
            return harness.Drain().OfType<ConversePacket>().LastOrDefault();
        }

        private static void AcquireLogos(WildernessRuntimeTestHarness harness, uint logosId)
        {
            var shrine = harness.Map.DynamicObjects.OfType<Logos>().Single(logos => logos.Id == logosId);
            Use(harness, shrine, DynamicObjectManager.LogosUseArgId);
            using var unit = harness.CreateChar();
            Assert.IsTrue(unit.CharacterLogoses.GetLogos(harness.Client.Player.Id).Contains(logosId));
        }

        private static void SeedCompletedHistory(WildernessRuntimeTestHarness harness, uint missionId)
        {
            using var unit = harness.CreateChar();
            unit.ExecuteTransaction(() => unit.CharacterMissions.Runtime.Archive(
                new CharacterMissionEntry(harness.Client.Player.Id, missionId, (uint)MissionState.Completed)
                {
                    AssignmentId = Guid.NewGuid().ToString("N"),
                    Generation = 1,
                    ContentRevision = WildernessMissionDataV1.Revision
                }, harness.UtcNow));
            harness.Client.Player.MissionHistory[missionId] = MissionState.Completed;
            harness.Client.Player.MissionSuccessHistory.Add(missionId);
        }

        private static bool Spawns(SpawnPoolEntry pool, uint creatureId) =>
            pool.Creature1Id == creatureId && pool.Creature1MinCount > 0 ||
            pool.Creature2Id == creatureId && pool.Creature2MinCount > 0 ||
            pool.Creature3Id == creatureId && pool.Creature3MinCount > 0 ||
            pool.Creature4Id == creatureId && pool.Creature4MinCount > 0 ||
            pool.Creature5Id == creatureId && pool.Creature5MinCount > 0 ||
            pool.Creature6Id == creatureId && pool.Creature6MinCount > 0;

        private static void AssertQualifiedRuntimeGrounding(WildernessRuntimeTestHarness harness,
            Creature actor, Vector3 authored, double nativeSupportY)
        {
            var ground = harness.Map.NavMesh.GroundHeight(authored);
            Assert.IsTrue(ground.HasValue, "The authored walking root must have a real nav surface.");
            Assert.IsFalse(harness.Map.NavMesh.IsUnderground(authored));
            Assert.IsFalse(harness.Map.NavMesh.IsUnderground(actor.Position));
            Assert.IsTrue(Math.Abs(ground.Value - authored.Y) < 0.5,
                "Runtime projection must stay within the approved authored-root tolerance.");
            Assert.IsTrue(Math.Abs(ground.Value - nativeSupportY) < 0.5,
                "The nav surface must agree with the adopted native terrain/hospital floor, not another layer.");
            Assert.AreEqual(authored.X, actor.Position.X, 0.001f);
            Assert.AreEqual(authored.Z, actor.Position.Z, 0.001f);
            Assert.AreEqual(ground.Value, actor.Position.Y, 0.001f);
            Assert.IsTrue(Vector3.Distance(actor.Position, authored) < 0.5f);
        }
    }
}
