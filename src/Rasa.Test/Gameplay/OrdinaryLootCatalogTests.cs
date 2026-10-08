using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Loot;
using Rasa.Managers;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Repositories.World;
using Rasa.Structures;
using Rasa.Structures.World;

namespace Rasa.Test.Gameplay
{
    [TestClass]
    [DoNotParallelize]
    public class OrdinaryLootCatalogTests
    {
        private static OrdinaryLootCatalog Load(Content data) => OrdinaryLootCatalog.Load(data.World);

        private static OrdinaryLootProfile Resolve(OrdinaryLootCatalog catalog, uint map, uint creature) =>
            catalog.Resolve(map, creature);

        private static LootContentException Rejected(Content data, params string[] expected)
        {
            var error = Assert.ThrowsExactly<LootContentException>(() => Load(data));
            var diagnostics = error.Diagnostics;
            Assert.IsTrue(diagnostics.Count > 0);
            foreach (var text in expected)
                StringAssert.Contains(string.Join("\n", diagnostics), text);
            Assert.ThrowsExactly<NotSupportedException>(() => ((IList<string>)diagnostics).Clear());
            return error;
        }

        [TestMethod]
        public void LookupsDistinguishBoundEmptyRequiredMissingAndUnconfiguredLegacyContent()
        {
            var data = new Content();
            data.Profiles.Add(new LootProfileEntry { ProfileKey = "no_loot" });
            data.Bindings.Add(new CreatureLootBindingEntry { MapContextId = 1148, CreatureId = 501, ProfileKey = "no_loot" });
            var catalog = Load(data);

            Assert.AreEqual("regular", Resolve(catalog, 1220, 501).ProfileKey);
            Assert.AreEqual(0, Resolve(catalog, 1148, 501).Groups.Count);
            Assert.IsNull(Resolve(catalog, 1148, 503));
            var error = Assert.ThrowsExactly<GameplayRejectionException>(() => Resolve(catalog, 1220, 999));
            StringAssert.Contains(error.Message, "1220");
            StringAssert.Contains(error.Message, "999");
        }

        [TestMethod]
        public void ExplicitOptionalMapPolicyRetainsLegacyFallback()
        {
            var data = new Content();
            data.Policies[0].RequireProfiles = false;
            data.Bindings.Clear();
            Assert.IsNull(Resolve(Load(data), 1220, 501));
        }

        [TestMethod]
        public void CatalogDetachesAllMutableRowsAndSortsGroupsAndCandidatesWithoutKillTimeReads()
        {
            var data = new Content();
            data.Groups.Add(new LootGroupEntry
            {
                ProfileKey = "regular", GroupKey = "first", Kind = LootGroupKind.Equipment,
                ChanceBasisPoints = 10000, MinimumRolls = 1, MaximumRolls = 1, SortOrder = 0
            });
            data.Items.Add(new LootItemEntry
            {
                ProfileKey = "regular", GroupKey = "first", ItemTemplateId = 902, Weight = 1,
                MinimumQuantity = 2, MaximumQuantity = 2, SortOrder = 2
            });
            data.Items.Add(new LootItemEntry
            {
                ProfileKey = "regular", GroupKey = "first", ItemTemplateId = 901, Weight = 1,
                MinimumQuantity = 1, MaximumQuantity = 1, SortOrder = 1
            });
            var catalog = Load(data);
            var profile = Resolve(catalog, 1220, 501);
            var calls = data.Calls;
            data.Disposed = true;
            data.Profiles[0].ProfileKey = "changed";
            data.Groups[0].MaximumRolls = int.MaxValue;
            data.Items[0].ItemTemplateId = 999;
            data.Bindings[0].ProfileKey = "changed";
            data.Policies[0].RequireProfiles = false;
            data.Groups.Clear();
            data.Items.Clear();

            Assert.AreSame(profile, Resolve(catalog, 1220, 501));
            CollectionAssert.AreEqual(new[] { "first", "ammo" }, profile.Groups.Select(group => group.Key).ToArray());
            CollectionAssert.AreEqual(new[] { 901U, 902U }, profile.Groups[0].Candidates.Select(item => item.TemplateId).ToArray());
            CollectionAssert.AreEqual(new[] { (901U, 1U), (901U, 1U) }, profile.Roll((min, max) => min).ToArray());
            Assert.ThrowsExactly<NotSupportedException>(() => ((IList<OrdinaryLootGroup>)profile.Groups).Clear());
            Assert.ThrowsExactly<NotSupportedException>(() => ((IList<OrdinaryLootCandidate>)profile.Groups[0].Candidates).Clear());
            Assert.ThrowsExactly<GameplayRejectionException>(() => Resolve(catalog, 1220, 999));
            Assert.AreEqual(calls, data.Calls);
        }

        [TestMethod]
        [DynamicData(nameof(CorruptRows))]
        public void CorruptPersistedRowsProduceIdentifiableDiagnostics(string _, Action<Content> mutate, string expected)
        {
            var data = new Content();
            mutate(data);
            Rejected(data, expected);
        }

        public static IEnumerable<object[]> CorruptRows()
        {
            yield return Case("profile key", d => d.Profiles[0].ProfileKey = "Bad", "Bad");
            yield return Case("long key", d => d.Groups[0].GroupKey = new string('a', 33), "regular");
            yield return Case("nonascii", d => d.Groups[0].GroupKey = "café", "café");
            yield return Case("null key", d => d.Profiles[0].ProfileKey = null, "profile");
            yield return Case("duplicate profile", d => d.Profiles.Add(d.Profiles[0]), "duplicate");
            yield return Case("duplicate group", d => d.Groups.Add(d.Groups[0]), "duplicate");
            yield return Case("duplicate item", d => d.Items.Add(d.Items[0]), "duplicate");
            yield return Case("duplicate binding", d => d.Bindings.Add(d.Bindings[0]), "501");
            yield return Case("duplicate policy", d => d.Policies.Add(d.Policies[0]), "1220");
            yield return Case("orphan group", d => d.Groups[0].ProfileKey = "absent", "absent");
            yield return Case("orphan item", d => d.Items[0].GroupKey = "absent", "absent");
            yield return Case("missing profile", d => d.Bindings[0].ProfileKey = "absent", "absent");
            yield return Case("missing map", d => d.Bindings[0].MapContextId = 777, "777");
            yield return Case("missing creature", d => d.Bindings[0].CreatureId = 777, "777");
            yield return Case("missing policy map", d => d.Policies[0].MapContextId = 777, "777");
            yield return Case("undefined kind", d => d.Groups[0].Kind = (LootGroupKind)99, "ammo");
            yield return Case("chance", d => d.Groups[0].ChanceBasisPoints = 10001, "ammo");
            yield return Case("rolls", d => d.Groups[0].MinimumRolls = -1, "ammo");
            yield return Case("roll ordering", d => d.Groups[0].MaximumRolls = 0, "ammo");
            yield return Case("roll cap", d => d.Groups[0].MaximumRolls = 33, "32");
            yield return Case("empty group", d => d.Items.Clear(), "ammo");
            yield return Case("weight", d => d.Items[0].Weight = 0, "901");
            yield return Case("signed quantity", d => d.Items[0].MinimumQuantity = -1, "901");
            yield return Case("quantity ordering", d => d.Items[0].MaximumQuantity = 0, "901");
            yield return Case("quantity rng", d => d.Items[0].MaximumQuantity = int.MaxValue, "901");
            yield return Case("missing template", d => d.Templates.Clear(), "901");
            yield return Case("missing link", d => d.Links.Clear(), "901");
            yield return Case("missing item class", d => d.Classes.Clear(), "2001");
            yield return Case("missing entity class", d => d.Entities.Clear(), "2001");
            yield return Case("zero native stack", d => d.Classes[0].StackSize = 0, "901");
            yield return Case("native stack overflow", d => d.Items[0].MaximumQuantity = 101, "901");
            yield return Case("hidden item", d => d.Classes[0].HidenInventoryFlag = 1, "901");
            yield return Case("mission item", d => d.Templates[0].QualityId = (byte)LootQuality.Mission, "Mission");
            yield return Case("junk outside junk group", d => d.Templates[0].QualityId = (byte)LootQuality.Junk, "Junk");
            yield return Case("nonjunk in junk group", d => d.Groups[0].Kind = LootGroupKind.Junk, "Junk");
            yield return Case("invalid quality", d => d.Templates[0].QualityId = 0, "901");
            yield return Case("junk rolls", d =>
            {
                d.Templates[0].QualityId = (byte)LootQuality.Junk;
                d.Groups[0].Kind = LootGroupKind.Junk;
                d.Groups[0].MaximumRolls = 2;
            }, "ammo");
            yield return Case("weight overflow", d =>
            {
                d.Items[0].Weight = int.MaxValue;
                d.Items.Add(new LootItemEntry
                {
                    ProfileKey = "regular", GroupKey = "ammo", ItemTemplateId = 902,
                    Weight = 1, MinimumQuantity = 1, MaximumQuantity = 1, SortOrder = 1
                });
            }, "weight");
            yield return Case("ambiguous native links", d => d.Links.Add(d.Links[0]), "901");
            yield return Case("null row", d => d.Groups.Add(null), "null");
        }

        private static object[] Case(string label, Action<Content> mutate, string expected) => new object[] { label, mutate, expected };

        [TestMethod]
        [DataRow("category255", "inventory category 255")]
        [DataRow("category6", "inventory category 6")]
        [DataRow("missingItem", "Item augmentation")]
        [DataRow("otherAugmentations", "Item augmentation")]
        [DataRow("weaponWithoutClass", "weapon class")]
        public void NativeInventoryContractRejectsUnusableCandidatesWithTemplateAndClassDiagnostics(
            string defect, string diagnostic)
        {
            var data = new Content();
            switch (defect)
            {
                case "category255": data.Templates[0].InventoryCategory = 255; break;
                case "category6": data.Templates[0].InventoryCategory = 6; break;
                case "missingItem": data.Entities[0].AugList = ""; break;
                case "otherAugmentations": data.Entities[0].AugList = "(16, 81)"; break;
                case "weaponWithoutClass":
                    data.Entities[0].AugList = "(6, 2, 4)";
                    data.WeaponTemplates.Add(new ItemTemplateWeaponEntry { Id = 901 });
                    break;
            }
            Rejected(data, "template 901", "class 2001", diagnostic);
        }

        [TestMethod]
        [DataRow("NativeMisc", "6", InventoryCategory.Misc)]
        [DataRow("Ammo_Test", "6", InventoryCategory.Consumable)]
        [DataRow("Consumable_Test", "6", InventoryCategory.Consumable)]
        [DataRow("Component_Test", "6", InventoryCategory.Crafting)]
        [DataRow("Metal1", "6", InventoryCategory.Crafting)]
        [DataRow("MisTest", "6", InventoryCategory.Mission)]
        [DataRow("Burrow_Test", "6", InventoryCategory.Mission)]
        [DataRow("NativeRecipe", "(6, 16)", InventoryCategory.Crafting)]
        [DataRow("NativeModule", "(6, 81)", InventoryCategory.Crafting)]
        [DataRow("NativeWeapon", "(6, 2, 4)", InventoryCategory.Equipment)]
        [DataRow("NativeArmor", "(6, 51, 4)", InventoryCategory.Equipment)]
        [DataRow("NativeEquipment", "(6, 4)", InventoryCategory.Equipment)]
        [DataRow("NativeCustomization", "(6, 19)", InventoryCategory.Consumable)]
        public void ZeroInventoryCategoryUsesLegacyNativeDefaultsInsteadOfBeingRejected(
            string name, string augmentations, InventoryCategory expected)
        {
            var data = new Content();
            data.Templates[0].InventoryCategory = 0;
            data.Entities[0].ClassName = name;
            data.Entities[0].AugList = augmentations;
            var parsed = EntityClassManager.ParseAugmentations(augmentations);
            var entity = new EntityClass(2001, name, 0, 0, parsed, false);
            Assert.AreEqual(expected, ItemManager.DefaultInventoryCategory(entity));
            Assert.AreEqual("regular", Resolve(Load(data), 1220, 501).ProfileKey);
            Assert.AreEqual((byte)0, data.Templates[0].InventoryCategory);
        }

        [TestMethod]
        [DataRow(InventoryCategory.Equipment)]
        [DataRow(InventoryCategory.Consumable)]
        [DataRow(InventoryCategory.Crafting)]
        [DataRow(InventoryCategory.Mission)]
        [DataRow(InventoryCategory.Misc)]
        public void ExplicitValidInventoryCategoriesRemainUsable(InventoryCategory category)
        {
            var data = new Content();
            data.Templates[0].InventoryCategory = (byte)category;
            Assert.AreEqual("regular", Resolve(Load(data), 1220, 501).ProfileKey);
        }

        [TestMethod]
        public void WeaponTemplateWithItsNativeClassMetadataRemainsUsable()
        {
            var data = new Content();
            data.Entities[0].AugList = "(6, 2, 4)";
            data.WeaponTemplates.Add(new ItemTemplateWeaponEntry { Id = 901 });
            data.WeaponClasses.Add(new WeaponClassEntry { Id = 2001 });
            Assert.AreEqual("regular", Resolve(Load(data), 1220, 501).ProfileKey);
        }

        [TestMethod]
        public void MultipleDefectsAccumulateRatherThanReportingOnlyTheFirst()
        {
            var data = new Content();
            data.Items[0].Weight = 0;
            data.Items[0].MinimumQuantity = -1;
            data.Bindings[0].CreatureId = 777;
            data.Policies.Add(new LootMapPolicyEntry { MapContextId = 888, RequireProfiles = true });
            var error = Rejected(data, "weight", "quantity", "777", "888");
            Assert.IsTrue(error.Diagnostics.Count >= 4);
        }

        [TestMethod]
        public void SortTiesUseOrdinalKeysAndTemplateIdsAsDeterministicTieBreakers()
        {
            var data = new Content();
            data.Groups.Add(new LootGroupEntry
            {
                ProfileKey = "regular", GroupKey = "aaa", Kind = LootGroupKind.Equipment,
                ChanceBasisPoints = 10000, MinimumRolls = 1, MaximumRolls = 1, SortOrder = 1
            });
            data.Items.Add(new LootItemEntry
            {
                ProfileKey = "regular", GroupKey = "aaa", ItemTemplateId = 902,
                Weight = 1, MinimumQuantity = 1, MaximumQuantity = 1
            });
            data.Items.Add(new LootItemEntry
            {
                ProfileKey = "regular", GroupKey = "aaa", ItemTemplateId = 901,
                Weight = 1, MinimumQuantity = 1, MaximumQuantity = 1
            });
            var profile = Resolve(Load(data), 1220, 501);
            CollectionAssert.AreEqual(new[] { "aaa", "ammo" }, profile.Groups.Select(group => group.Key).ToArray());
            CollectionAssert.AreEqual(new[] { 901U, 902U }, profile.Groups[0].Candidates.Select(item => item.TemplateId).ToArray());
        }

        [TestMethod]
        public void SignedSortOrdersPreserveThePersistenceContractAcrossTheFullIntegerRange()
        {
            var data = new Content();
            data.Groups[0].SortOrder = int.MinValue;
            data.Items[0].SortOrder = int.MinValue;
            data.Items.Add(new LootItemEntry
            {
                ProfileKey = "regular", GroupKey = "ammo", ItemTemplateId = 902,
                Weight = 1, MinimumQuantity = 1, MaximumQuantity = 1, SortOrder = int.MaxValue
            });
            var group = Resolve(Load(data), 1220, 501).Groups.Single();
            CollectionAssert.AreEqual(new[] { 901U, 902U }, group.Candidates.Select(item => item.TemplateId).ToArray());
        }

        [TestMethod]
        public void RewardingUnlocatedSceneWithMissingNativeCreatureCannotBeClassifiedAsNonLoot()
        {
            var data = new Content();
            var scene = new MissionSceneDefinition { Script = "data.sequence" };
            scene.Actors["missing"] = Actor("missing", 777, new ActorGameplayPolicy { RewardScenarioKills = true });
            data.Enabled.Add(new MissionContentDefinitionEntry { MissionId = 321, ContentRevision = "v1" });
            data.AddScene(321, "v1", scene);
            Rejected(data, "321", "missing", "777");
        }

        [TestMethod]
        public void CoverageUsesPopulatedSlotsAndKillClassificationRatherThanNamesOrGlobalHostiles()
        {
            var data = new Content();
            data.Creatures[0].Comment = "friendly vendor";
            data.Pools.Add(new SpawnPoolEntry
            {
                Id = 51, MapContextId = 1220, Creature1Id = 502, Creature1MaxCount = 1,
                Creature2Id = 506, Creature2MaxCount = 0, Creature3Id = 505, Creature3MaxCount = 1
            });
            data.Pools.Add(new SpawnPoolEntry { Id = 52, MapContextId = 1148, Creature1Id = 503, Creature1MaxCount = 1 });
            Assert.IsNotNull(Load(data));
            data.Bindings.Clear();
            Rejected(data, "1220", "501", "50");
        }

        [TestMethod]
        [DataRow(3U)]
        [DataRow(999U)]
        public void NeutralAndUnknownFactionsUseTheSameClassificationAsRuntime(uint faction)
        {
            var data = new Content();
            data.Creatures[0].Faction = faction;
            data.Bindings.Clear();
            Rejected(data, "1220", "501");
        }

        [TestMethod]
        public void ArrivalAndControlPointPopulationsAreCheckedOnTheirOwnMaps()
        {
            var data = new Content();
            data.Pools.Add(new SpawnPoolEntry
            {
                Id = 60, MapContextId = 1220, Mode = 1, Creature6Id = 506, Creature6MaxCount = 1
            });
            data.Arrivals.Add(new SpawnPoolArrivalEntry { Id = 70, PoolId = 60, Kind = 1 });
            data.Points.Add(new ControlPointEntry { Id = 80, MapContextId = 1220 });
            data.PointLinks.Add(new ControlPointLinkEntry
            {
                ControlPointId = 80, Kind = ControlPointLinkEntry.KindBanePool, ObjectId = 60
            });
            Rejected(data, "1220", "506", "60");
            data.Bindings.Add(new CreatureLootBindingEntry { MapContextId = 1220, CreatureId = 506, ProfileKey = "regular" });
            Assert.IsNotNull(Load(data));
        }

        [TestMethod]
        public void InvalidCoverageSourceReferencesFailClosedInsteadOfGuessingAMap()
        {
            var data = new Content();
            data.Points.Add(new ControlPointEntry { Id = 80, MapContextId = 1220 });
            data.PointLinks.Add(new ControlPointLinkEntry
            {
                ControlPointId = 80, Kind = ControlPointLinkEntry.KindBanePool, ObjectId = 999
            });
            Rejected(data, "80", "999");
        }

        [TestMethod]
        public void ExperienceSceneCoverageHonorsRewardEligibilityAndExplicitAuthoredEmptyLoot()
        {
            var data = new Content();
            var scene = new MissionSceneDefinition();
            scene.Actors["rewarding"] = Actor("rewarding", 506, new ActorGameplayPolicy { RewardScenarioKills = true });
            scene.Actors["nonrewarding"] = Actor("nonrewarding", 503, null);
            scene.Actors["friendly"] = Actor("friendly", 502, new ActorGameplayPolicy { RewardScenarioKills = true });
            scene.Actors["authored"] = Actor("authored", 503, new ActorGameplayPolicy
            {
                RewardScenarioKills = true, Loot = new AuthoredLootProfile(Array.Empty<LootDrop>())
            });
            data.AddExperience(scene);
            Rejected(data, "1220", "506", "rewarding");
            data.Bindings.Add(new CreatureLootBindingEntry { MapContextId = 1220, CreatureId = 506, ProfileKey = "regular" });
            Assert.IsNotNull(Load(data));
        }

        [TestMethod]
        [DataRow(1148U)]
        [DataRow(1220U)]
        public void MatchingSpawnMetadataDoesNotConstrainCreatedActorToItsGroupMap(uint groupMap)
        {
            var data = new Content();
            var scene = new MissionSceneDefinition { Script = "data.sequence" };
            scene.Actors["rewarding"] = Actor("rewarding", 506,
                new ActorGameplayPolicy { RewardScenarioKills = true }) with { MissionId = 321, GroupId = 7, SpawnId = 1 };
            data.Enabled.Add(new MissionContentDefinitionEntry { MissionId = 321, ContentRevision = "v1" });
            data.SpawnGroups.Add(new MissionSpawnGroupEntry
            {
                MissionId = 321, ContentRevision = "v1", SpawnGroupId = 7, MapContextId = groupMap,
                Enabled = true, SpawnPolicy = MissionSpawnGroupPolicy.ScenarioControlled
            });
            data.Spawns.Add(new MissionSpawnEntry
            {
                MissionId = 321, ContentRevision = "v1", SpawnGroupId = 7, SpawnId = 1, CreatureId = 506, Quantity = 1
            });
            data.Areas.Add(new MissionAreaEntry { MissionId = 321, ContentRevision = "v1", MapContextId = 1148 });
            data.AddScene(321, "v1", scene);
            Rejected(data, "321@v1", "506", "rewarding", "current run map", "not an enforced map constraint");
            data.Bindings.Add(new CreatureLootBindingEntry { MapContextId = 1220, CreatureId = 506, ProfileKey = "regular" });
            Rejected(data, "321@v1", "506", "rewarding", "current run map");
        }

        [TestMethod]
        public void RewardingMissionSceneWithoutAnExactMapLinkReportsConcreteAmbiguity()
        {
            var data = new Content();
            var scene = new MissionSceneDefinition { Script = "data.sequence" };
            scene.Actors["unlocated"] = Actor("unlocated", 506, new ActorGameplayPolicy { RewardScenarioKills = true });
            data.Enabled.Add(new MissionContentDefinitionEntry { MissionId = 321, ContentRevision = "v1" });
            data.AddScene(321, "v1", scene);
            Rejected(data, "321", "unlocated", "map");
        }

        [TestMethod]
        [DataRow(1220U, true)]
        [DataRow(1148U, false)]
        public void PublicEncounterMapsCreatedActorsThroughItsExactReservedSpawn(uint map, bool managed)
        {
            var data = new Content();
            data.Enabled.Add(new MissionContentDefinitionEntry { MissionId = 321, ContentRevision = "v1" });
            data.Pools.Add(new SpawnPoolEntry { Id = 51, MapContextId = map, Creature1Id = 502, Creature1MaxCount = 1 });
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                PublicEncounter = new PublicEncounterBinding(321, 51, "reserved", "data.sequence")
            };
            scene.Actors["reserved"] = new SceneActorDefinition("reserved", SceneActorKind.PublicSpawn, 51);
            scene.Actors["created"] = Actor("created", 506, new ActorGameplayPolicy { RewardScenarioKills = true });
            data.AddScene(321, "v1", scene);
            if (managed)
            {
                Rejected(data, "1220", "506", "created");
                data.Bindings.Add(new CreatureLootBindingEntry { MapContextId = 1220, CreatureId = 506, ProfileKey = "regular" });
            }
            Assert.IsNotNull(Load(data));
        }

        [TestMethod]
        public void ReservedEncounterMapTakesPrecedenceOverContradictorySpawnMetadata()
        {
            var data = new Content();
            data.Enabled.Add(new MissionContentDefinitionEntry { MissionId = 321, ContentRevision = "v1" });
            data.Pools.Add(new SpawnPoolEntry { Id = 51, MapContextId = 1220, Creature1Id = 502, Creature1MaxCount = 1 });
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                PublicEncounter = new PublicEncounterBinding(321, 51, "reserved", "data.sequence")
            };
            scene.Actors["reserved"] = new SceneActorDefinition("reserved", SceneActorKind.PublicSpawn, 51);
            scene.Actors["created"] = Actor("created", 506, new ActorGameplayPolicy { RewardScenarioKills = true })
                with { MissionId = 321, GroupId = 7, SpawnId = 1 };
            data.SpawnGroups.Add(new MissionSpawnGroupEntry
            {
                MissionId = 321, ContentRevision = "v1", SpawnGroupId = 7, MapContextId = 1148, Enabled = true
            });
            data.Spawns.Add(new MissionSpawnEntry
            {
                MissionId = 321, ContentRevision = "v1", SpawnGroupId = 7, SpawnId = 1, CreatureId = 506, Quantity = 1
            });
            data.AddScene(321, "v1", scene);
            var error = Rejected(data, "created", "map 1220", "creature 506", "binding");
            Assert.IsFalse(error.Message.Contains("disagree", StringComparison.Ordinal));
            data.Bindings.Add(new CreatureLootBindingEntry { MapContextId = 1220, CreatureId = 506, ProfileKey = "regular" });
            Assert.IsNotNull(Load(data));
        }

        [TestMethod]
        public void DisabledSceneRevisionsDoNotIntroduceCoverageFromAnotherMap()
        {
            var data = new Content();
            data.Enabled.Add(new MissionContentDefinitionEntry { MissionId = 321, ContentRevision = "v1" });
            var obsolete = new MissionSceneDefinition();
            obsolete.Actors["obsolete"] = Actor("obsolete", 503, new ActorGameplayPolicy { RewardScenarioKills = true });
            data.AddScene(321, "v0", obsolete);
            Assert.IsNotNull(Load(data));
        }

        [TestMethod]
        public void SceneWithoutARuntimeScriptDoesNotIntroduceInactiveActorCoverage()
        {
            var data = new Content();
            data.Enabled.Add(new MissionContentDefinitionEntry { MissionId = 321, ContentRevision = "v1" });
            var scene = new MissionSceneDefinition();
            scene.Actors["inactive"] = Actor("inactive", 506, new ActorGameplayPolicy { RewardScenarioKills = true });
            data.AddScene(321, "v1", scene);
            Assert.IsNotNull(Load(data));
        }

        [TestMethod]
        public void PublicSceneSpawnCannotBorrowAPopulationFromAnotherExplicitMap()
        {
            var data = new Content();
            var scene = new MissionSceneDefinition { Script = "data.sequence" };
            scene.Actors["other_map"] = new SceneActorDefinition("other_map", SceneActorKind.PublicSpawn, 50);
            data.AddExperience(scene);
            var experience = JsonSerializer.Deserialize<MissionExperienceDefinition>(data.Experiences[0].Bindings, MissionContentCodec.Options);
            data.Experiences[0].MapContextId = 1148;
            experience.MapContextId = 1148;
            data.Experiences[0].Bindings = JsonSerializer.Serialize(experience, MissionContentCodec.Options);
            Rejected(data, "other_map", "1148", "1220");
        }

        [TestMethod]
        public void ExperienceFallbackCanRewardCreatedActorsButCannotOverrideAnExplicitNonrewardingRole()
        {
            var data = new Content();
            var scene = new MissionSceneDefinition { Script = "data.sequence" };
            scene.Actors["inherited"] = Actor("inherited", 506, null);
            scene.Actors["suppressed"] = Actor("suppressed", 503, new ActorGameplayPolicy());
            data.AddExperience(scene);
            var experience = JsonSerializer.Deserialize<MissionExperienceDefinition>(data.Experiences[0].Bindings, MissionContentCodec.Options);
            experience.ActorPolicies[506] = new ActorGameplayPolicy { RewardScenarioKills = true };
            experience.ActorPolicies[503] = new ActorGameplayPolicy { RewardScenarioKills = true };
            data.Experiences[0].Bindings = JsonSerializer.Serialize(experience, MissionContentCodec.Options);
            Rejected(data, "506", "inherited");
            data.Bindings.Add(new CreatureLootBindingEntry { MapContextId = 1220, CreatureId = 506, ProfileKey = "regular" });
            Assert.IsNotNull(Load(data));
        }

        [TestMethod]
        public void PrivateExperienceAuthoredLootDoesNotExemptAnUnboundPublicPool()
        {
            var data = new Content();
            data.AddExperience(new MissionSceneDefinition { Script = "data.sequence" });
            var experience = JsonSerializer.Deserialize<MissionExperienceDefinition>(data.Experiences[0].Bindings, MissionContentCodec.Options);
            experience.ActorPolicies[501] = new ActorGameplayPolicy { Loot = new AuthoredLootProfile(Array.Empty<LootDrop>()) };
            data.Experiences[0].Bindings = JsonSerializer.Serialize(experience, MissionContentCodec.Options);
            data.Bindings.Clear();
            Rejected(data, "1220", "501", "50");
        }

        [TestMethod]
        public void UnlocatedSceneWithPotentialManagedExperienceRewardsReportsItsAmbiguity()
        {
            var data = new Content();
            data.AddExperience(new MissionSceneDefinition { Script = "data.sequence" });
            var experience = JsonSerializer.Deserialize<MissionExperienceDefinition>(data.Experiences[0].Bindings, MissionContentCodec.Options);
            experience.ActorPolicies[506] = new ActorGameplayPolicy { RewardScenarioKills = true };
            data.Experiences[0].Bindings = JsonSerializer.Serialize(experience, MissionContentCodec.Options);
            var scene = new MissionSceneDefinition { Script = "data.sequence" };
            scene.Actors["unlocated"] = Actor("unlocated", 506, null);
            data.Enabled.Add(new MissionContentDefinitionEntry { MissionId = 321, ContentRevision = "v1" });
            data.AddScene(321, "v1", scene);
            Rejected(data, "321", "unlocated", "map");
        }

        [TestMethod]
        [DataRow("database")]
        [DataRow("unsupported")]
        [DataRow("null")]
        public void MissingSchemaUnsupportedUnitAndNullRepositoryNeverBecomeASuccessfulEmptyCatalog(string mode)
        {
            var data = new Content { RepositoryFailure = mode };
            Rejected(data, "ordinary");
        }

        [TestMethod]
        public void EmptyInstalledSchemaIsValidButCannotCoverAManagedMap()
        {
            var data = new Content();
            data.Profiles.Clear(); data.Groups.Clear(); data.Items.Clear(); data.Bindings.Clear(); data.Policies.Clear();
            Assert.IsNull(Resolve(Load(data), 1220, 501));
            data.Policies.Add(new LootMapPolicyEntry { MapContextId = 1220, RequireProfiles = true });
            Rejected(data, "501", "1220");
        }

        [TestMethod]
        [DataRow("pools")]
        [DataRow("arrivals")]
        [DataRow("control-points")]
        [DataRow("missions")]
        public void MissingCoverageRepositoriesGiveContentDiagnosticsInsteadOfNullDereferences(string repository)
        {
            var data = new Content { MissingCoverageRepository = repository };
            Rejected(data, "coverage");
        }

        [TestMethod]
        public void NativeMigratedWorldLoadsAndManagedPoolCoverageCanUseDeliberateNoLootBindings()
        {
            using var harness = Rasa.Test.Missions.Wilderness.WildernessRuntimeTestHarness.Create(
                targetWorldMigration: Rasa.Test.Database.OrdinaryLootMigrationTests.SchemaMigration);
            using (var world = harness.CreateWorld())
                Assert.IsNull(Resolve(OrdinaryLootCatalog.Load(world), 1220, 1));

            var sources = harness.World.SpawnPoolEntries.AsEnumerable()
                .Where(pool => pool.MapContextId == 1220)
                .SelectMany(pool => new[]
                {
                    (pool.Creature1Id, pool.Creature1MaxCount), (pool.Creature2Id, pool.Creature2MaxCount),
                    (pool.Creature3Id, pool.Creature3MaxCount), (pool.Creature4Id, pool.Creature4MaxCount),
                    (pool.Creature5Id, pool.Creature5MaxCount), (pool.Creature6Id, pool.Creature6MaxCount)
                })
                .Where(slot => slot.Item1 != 0 && slot.Item2 > 0).Select(slot => slot.Item1).ToHashSet();
            var eligible = harness.World.CreatureEntries.AsEnumerable()
                .Where(creature => sources.Contains(creature.Id) &&
                    TargetCategories.PlayerMayAttack(TargetCategories.FromFaction(creature.Faction)))
                .Select(creature => creature.Id).ToArray();
            Assert.IsTrue(eligible.Length > 0);
            harness.World.LootProfileEntries.Add(new LootProfileEntry { ProfileKey = "no_loot" });
            harness.World.LootMapPolicyEntries.Add(new LootMapPolicyEntry { MapContextId = 1220, RequireProfiles = true });
            harness.World.CreatureLootBindingEntries.AddRange(eligible.Select(creature => new CreatureLootBindingEntry
            {
                MapContextId = 1220, CreatureId = creature, ProfileKey = "no_loot"
            }));
            harness.World.SaveChanges();
            using (var world = harness.CreateWorld())
            {
                var catalog = OrdinaryLootCatalog.Load(world);
                Assert.AreEqual(0, Resolve(catalog, 1220, eligible[0]).Groups.Count);
            }
            uint nativeTemplate;
            using (var world = harness.CreateWorld())
            {
                var links = world.Equipment.GetItemTemplateClasses().ToDictionary(row => row.ItemTemplateId, row => row.ItemClass);
                var classes = world.Equipment.GetItemClasses().ToDictionary(row => row.Id);
                var entities = world.EntityClasses.Get().Select(row => row.Id).ToHashSet();
                nativeTemplate = world.Equipment.GetItemTemplates().OrderBy(row => row.Id).First(row =>
                    row.Id > 0 && row.QualityId == (byte)LootQuality.Normal &&
                    links.TryGetValue(row.Id, out var itemClass) && entities.Contains(itemClass) &&
                    classes.TryGetValue(itemClass, out var native) && native.StackSize > 0 &&
                    native.HidenInventoryFlag == 0).Id;
            }
            var runtimeTemplate = InventoryManager.InventoryPlan.RequireTemplate(nativeTemplate);
            var runtimeClass = EntityClassManager.Instance.GetClassInfo(runtimeTemplate.Class);
            Assert.IsTrue(runtimeClass.Augmentations.Contains(AugmentationType.Item));
            var staged = ItemManager.StageItem(runtimeTemplate, 1, "");
            try
            {
                Assert.AreSame(runtimeTemplate, staged.ItemTemplate);
                Assert.AreEqual(1U, staged.StackSize);
                Assert.AreEqual(runtimeClass.ItemClassInfo.MaxHitPoints, staged.CurrentHitPoints);
                ItemManager.Instance.SendItemDataToClient(harness.Client, staged, false);
                Assert.IsTrue(harness.Drain().Count > 0);
            }
            finally
            {
                EntityManager.Instance.FreeEntity(staged.EntityId);
            }
            harness.World.LootProfileEntries.Add(new LootProfileEntry { ProfileKey = "verified_native" });
            harness.World.LootGroupEntries.Add(new LootGroupEntry
            {
                ProfileKey = "verified_native", GroupKey = "drop", Kind = LootGroupKind.Equipment,
                ChanceBasisPoints = 10000, MinimumRolls = 1, MaximumRolls = 1
            });
            harness.World.LootItemEntries.Add(new LootItemEntry
            {
                ProfileKey = "verified_native", GroupKey = "drop", ItemTemplateId = nativeTemplate,
                Weight = 1, MinimumQuantity = 1, MaximumQuantity = 1
            });
            var missing = harness.World.CreatureLootBindingEntries.Single(binding => binding.CreatureId == eligible[0]);
            missing.ProfileKey = "verified_native";
            harness.World.SaveChanges();
            using (var world = harness.CreateWorld())
                Assert.AreEqual(nativeTemplate, Resolve(OrdinaryLootCatalog.Load(world), 1220, eligible[0])
                    .Groups.Single().Candidates.Single().TemplateId);
            harness.World.CreatureLootBindingEntries.Remove(missing);
            harness.World.SaveChanges();
            using (var world = harness.CreateWorld())
            {
                var error = Assert.ThrowsExactly<LootContentException>(() => OrdinaryLootCatalog.Load(world));
                StringAssert.Contains(error.Message, eligible[0].ToString());
                StringAssert.Contains(error.Message, "1220");
            }
        }

        [TestMethod]
        public void ReadinessDoesNotCatchUnrelatedProgrammingErrors()
        {
            var data = new Content { UnexpectedFailure = true };
            Assert.ThrowsExactly<InvalidOperationException>(() => Server.LoadOrdinaryLootAndCheckReadiness(data.World));
        }

        [TestMethod]
        public void ProfileCapsApplyAcrossGroupsAndValidJunkQuantitiesAreNotLimitedToOne()
        {
            var data = new Content();
            data.Templates[0].QualityId = (byte)LootQuality.Junk;
            data.Groups[0].Kind = LootGroupKind.Junk;
            data.Items[0].MinimumQuantity = 3;
            data.Items[0].MaximumQuantity = 3;
            var catalog = Load(data);
            CollectionAssert.AreEqual(new[] { (901U, 3U) }, Resolve(catalog, 1220, 501).Roll((min, max) => min).ToArray());
            data.Groups.Add(new LootGroupEntry
            {
                ProfileKey = "regular", GroupKey = "other", Kind = LootGroupKind.Junk,
                ChanceBasisPoints = 10000, MinimumRolls = 1, MaximumRolls = 1
            });
            data.Items.Add(new LootItemEntry
            {
                ProfileKey = "regular", GroupKey = "other", ItemTemplateId = 901,
                Weight = 1, MinimumQuantity = 1, MaximumQuantity = 1
            });
            Rejected(data, "multiple Junk");
            data.Groups[0].Kind = LootGroupKind.Equipment;
            data.Groups[1].Kind = LootGroupKind.Equipment;
            data.Templates[0].QualityId = (byte)LootQuality.Normal;
            data.Groups[0].MaximumRolls = 16;
            data.Groups[1].MaximumRolls = 17;
            Rejected(data, "32");
        }

        [TestMethod]
        public void UninitializedProductionAccessThrowsAndFailedReadinessDoesNotPublishOrOpenNetwork()
        {
            var type = typeof(OrdinaryLootCatalog);
            var active = type.GetField("_current", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(active);
            var previous = active.GetValue(null);
            var config = Logger.Config;
            var output = Console.Out;
            using var log = new StringWriter();
            try
            {
                active.SetValue(null, null);
                Assert.ThrowsExactly<InvalidOperationException>(() => _ = OrdinaryLootCatalog.Current);
                var data = new Content();
                data.Bindings.Clear();
                data.Items[0].Weight = 0;
                var expectedDiagnostics = Assert.ThrowsExactly<LootContentException>(() => Load(data)).Diagnostics;
                var calls = new List<string>();
                Logger.UpdateConfig(new Logger.LoggerConfig { IsDebugMode = true, LogToFile = false });
                Logger.Flush();
                Console.SetOut(log);
                Action stage = () => calls.Add("network-or-world");
                var lifecycle = new ServerStartupLifecycle(
                    () => { calls.Add("missions"); return true; },
                    () => { calls.Add("loot"); return Server.LoadOrdinaryLootAndCheckReadiness(data.World); },
                    stage, stage, stage, stage, stage, stage, stage, stage, stage);
                Assert.IsFalse(lifecycle.Start());
                Assert.IsNull(active.GetValue(null));
                Logger.Flush();
                CollectionAssert.AreEqual(new[] { "missions", "loot" }, calls.ToArray());
                foreach (var diagnostic in expectedDiagnostics)
                    StringAssert.Contains(log.ToString(), diagnostic);
                Assert.IsFalse(log.ToString().Contains("Server ready!", StringComparison.Ordinal));

                data.Bindings.Add(new CreatureLootBindingEntry { MapContextId = 1220, CreatureId = 501, ProfileKey = "regular" });
                data.Items[0].Weight = 1;
                Assert.IsTrue(Server.LoadOrdinaryLootAndCheckReadiness(data.World));
                var published = active.GetValue(null);
                Assert.IsNotNull(published);
                data.Items[0].Weight = 0;
                Assert.IsFalse(Server.LoadOrdinaryLootAndCheckReadiness(data.World));
                Assert.AreSame(published, active.GetValue(null));
            }
            finally
            {
                Logger.Flush();
                Console.SetOut(output);
                if (config != null) Logger.UpdateConfig(config);
                else typeof(Logger).GetProperty(nameof(Logger.Config)).SetValue(null, null);
                active.SetValue(null, previous);
            }
        }

        private static SceneActorDefinition Actor(string role, uint creature, ActorGameplayPolicy policy) =>
            new(role, SceneActorKind.Creature, creature, new ScenePosition(0, 0, 0), GameplayPolicy: policy);

        public sealed class Content : IOrdinaryLootRepository
        {
            internal List<LootProfileEntry> Profiles { get; } = new() { new() { ProfileKey = "regular" } };
            internal List<LootGroupEntry> Groups { get; } = new()
            {
                new() { ProfileKey = "regular", GroupKey = "ammo", Kind = LootGroupKind.Ammunition,
                    ChanceBasisPoints = 10000, MinimumRolls = 1, MaximumRolls = 1, SortOrder = 1 }
            };
            internal List<LootItemEntry> Items { get; } = new()
            {
                new() { ProfileKey = "regular", GroupKey = "ammo", ItemTemplateId = 901, Weight = 1,
                    MinimumQuantity = 1, MaximumQuantity = 1 }
            };
            internal List<CreatureLootBindingEntry> Bindings { get; } = new()
            {
                new() { MapContextId = 1220, CreatureId = 501, ProfileKey = "regular" }
            };
            internal List<LootMapPolicyEntry> Policies { get; } = new() { new() { MapContextId = 1220, RequireProfiles = true } };
            internal List<ItemTemplateEntry> Templates { get; } = new()
            {
                new() { Id = 901, QualityId = (byte)LootQuality.Normal },
                new() { Id = 902, QualityId = (byte)LootQuality.Normal }
            };
            internal List<ItemTemplateItemClassEntry> Links { get; } = new()
            {
                new() { ItemTemplateId = 901, ItemClass = 2001 }, new() { ItemTemplateId = 902, ItemClass = 2002 }
            };
            internal List<ItemClassEntry> Classes { get; } = new()
            {
                new() { Id = 2001, StackSize = 100 }, new() { Id = 2002, StackSize = 100 }
            };
            internal List<EntityClassEntry> Entities { get; } = new()
            {
                new() { Id = 2001, ClassName = "Ammo_Test", AugList = "6" },
                new() { Id = 2002, ClassName = "NativeMisc", AugList = "6" }
            };
            internal List<ItemTemplateWeaponEntry> WeaponTemplates { get; } = new();
            internal List<WeaponClassEntry> WeaponClasses { get; } = new();
            internal List<MapInfoEntry> Maps { get; } = new() { new() { Id = 1220 }, new() { Id = 1148 } };
            internal List<CreatureEntry> Creatures { get; } = new()
            {
                new() { Id = 501, Faction = 0 }, new() { Id = 502, Faction = 1 },
                new() { Id = 503, Faction = 0 }, new() { Id = 505, Faction = 4 }, new() { Id = 506, Faction = 0 }
            };
            internal List<SpawnPoolEntry> Pools { get; } = new()
            {
                new() { Id = 50, MapContextId = 1220, Creature1Id = 501, Creature1MaxCount = 1 }
            };
            internal List<SpawnPoolArrivalEntry> Arrivals { get; } = new();
            internal List<ControlPointEntry> Points { get; } = new();
            internal List<ControlPointLinkEntry> PointLinks { get; } = new();
            internal List<MissionContentDefinitionEntry> Enabled { get; } = new();
            internal List<MissionSceneBindingEntry> Scenes { get; } = new();
            internal List<MissionExperienceBindingEntry> Experiences { get; } = new();
            internal List<MissionSpawnGroupEntry> SpawnGroups { get; } = new();
            internal List<MissionSpawnEntry> Spawns { get; } = new();
            internal List<MissionAreaEntry> Areas { get; } = new();
            internal string RepositoryFailure { get; set; }
            internal string MissingCoverageRepository { get; set; }
            internal bool UnexpectedFailure { get; set; }
            internal bool Disposed { get; set; }
            internal int Calls { get; private set; }
            internal IWorldUnitOfWork World { get; }

            public Content()
            {
                var equipment = Proxy<IEquipmentRepository>(new()
                {
                    ["GetItemTemplates"] = () => Templates, ["GetItemTemplateClasses"] = () => Links,
                    ["GetItemClasses"] = () => Classes, ["GetWeaponItems"] = () => WeaponTemplates,
                    ["GetWeaponClasses"] = () => WeaponClasses
                });
                var entities = Proxy<IEntityClassRepository>(new() { ["Get"] = () => Entities });
                var creatures = Proxy<ICreatureRepository>(new() { ["Get"] = () => Creatures });
                var maps = Proxy<IMapInfoRepository>(new() { ["Get"] = () => Maps });
                var pools = Proxy<ISpawnpoolRepository>(new() { ["Get"] = () => Pools });
                var arrivals = Proxy<ISpawnPoolArrivalRepository>(new() { ["GetArrivals"] = () => Arrivals });
                var points = Proxy<IControlPointRepository>(new()
                {
                    ["GetControlPoints"] = () => Points, ["GetLinks"] = () => PointLinks
                });
                var missions = Proxy<IMigratedMissionContentRepository>(new()
                {
                    ["GetEnabledDefinitions"] = () => Enabled, ["GetSceneBindings"] = () => Scenes,
                    ["GetExperiences"] = () => Experiences, ["GetSpawnGroups"] = () => SpawnGroups,
                    ["GetSpawns"] = () => Spawns, ["GetAreas"] = () => Areas
                });
                World = Proxy<IWorldUnitOfWork>(new()
                {
                    ["get_OrdinaryLoot"] = () => RepositoryFailure == "null" ? null :
                        RepositoryFailure == "unsupported" ? throw new NotSupportedException("ordinary loot is unsupported") : this,
                    ["get_Equipment"] = () => equipment, ["get_EntityClasses"] = () => entities,
                    ["get_Creatures"] = () => creatures, ["get_MapInfos"] = () => maps,
                    ["get_Spawnpools"] = () => MissingCoverageRepository == "pools" ? null : pools,
                    ["get_SpawnPoolArrivals"] = () => MissingCoverageRepository == "arrivals" ? null : arrivals,
                    ["get_ControlPoints"] = () => MissingCoverageRepository == "control-points" ? null : points,
                    ["get_MissionContent"] = () => MissingCoverageRepository == "missions" ? null : missions,
                    ["Dispose"] = () => { Disposed = true; return null; }
                });
            }

            public OrdinaryLootContentSnapshot Get()
            {
                Calls++;
                if (UnexpectedFailure) throw new InvalidOperationException("Programming failure.");
                if (RepositoryFailure == "database") throw new SqliteException("no such table: loot_profile", 1);
                return new(Profiles, Groups, Items, Bindings, Policies);
            }

            internal void AddExperience(MissionSceneDefinition scene) => Experiences.Add(new MissionExperienceBindingEntry
            {
                ExperienceKey = "test", Enabled = true, MapContextId = 1220,
                Bindings = JsonSerializer.Serialize(new MissionExperienceDefinition
                {
                    Key = "test", Revision = "v1", MapContextId = 1220, PrivatePerCharacter = true, Scene = scene
                }, MissionContentCodec.Options)
            });

            internal void AddScene(uint mission, string revision, MissionSceneDefinition scene) => Scenes.Add(new()
            {
                MissionId = mission, ContentRevision = revision, ScriptKey = scene.Script,
                StateVersion = scene.StateVersion, Bindings = JsonSerializer.Serialize(scene, MissionContentCodec.Options)
            });

            private T Proxy<T>(Dictionary<string, Func<object>> handlers) where T : class
            {
                var proxy = DispatchProxy.Create<T, StrictProxy>();
                ((StrictProxy)(object)proxy).Call = method =>
                {
                    if (Disposed) throw new ObjectDisposedException(nameof(Content));
                    Calls++;
                    return handlers.TryGetValue(method.Name, out var handler) ? handler() :
                        throw new NotSupportedException("Unexpected repository access: " + method.Name);
                };
                return proxy;
            }
        }

        public class StrictProxy : DispatchProxy
        {
            internal Func<MethodInfo, object> Call { get; set; }
            protected override object Invoke(MethodInfo targetMethod, object[] args) => Call(targetMethod);
        }
    }
}
