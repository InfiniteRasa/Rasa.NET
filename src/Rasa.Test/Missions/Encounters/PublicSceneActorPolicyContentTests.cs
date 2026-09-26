using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions.Encounters
{
    using Rasa.Game.Missions.Content;
    using Rasa.Missions.Content;
    using Rasa.Missions.Definitions;
    using Rasa.Missions.Runtime;
    using Rasa.Missions.Scenes;
    using Rasa.Services.Preloader.Missions;
    using Rasa.Structures.World;
    using Rasa.Test.Missions.Content;

    [TestClass]
    [DoNotParallelize]
    public class PublicSceneActorPolicyContentTests
    {
        [TestMethod]
        public void AbsentPolicyPreservesHistoricalActorJsonWithEitherSerializer()
        {
            var actor = Actor();
            Assert.AreEqual(
                @"{""role"":""enemy"",""kind"":""Creature"",""templateId"":510210,""position"":{""x"":0,""y"":0,""z"":0},""orientation"":0,""initiallyInteractable"":true,""initialObjectState"":0,""missionId"":0,""spawnId"":0}",
                JsonSerializer.Serialize(actor, MissionContentCodec.Options));
            using var ordinaryJson = JsonDocument.Parse(JsonSerializer.Serialize(actor));
            Assert.IsFalse(ordinaryJson.RootElement.TryGetProperty("GameplayPolicy", out _));
            foreach (var scene in BootcampMissionDataV1.Scenes().Values.Append(BootcampMissionDataV1.Experience().Scene))
            {
                Assert.IsFalse(JsonSerializer.Serialize(scene).Contains("\"GameplayPolicy\"", StringComparison.Ordinal));
                Assert.IsFalse(JsonSerializer.Serialize(scene, MissionContentCodec.Options)
                    .Contains("\"gameplayPolicy\"", StringComparison.Ordinal));
            }
        }

        [TestMethod]
        public void SceneAndExperienceBindingsSnapshotCallerOwnedPolicyLists()
        {
            var tags = new List<string> { "hostile" };
            var drops = new List<LootDrop> { new(28, 100, 1, 1) };
            var policy = new ActorGameplayPolicy { Tags = tags, Loot = new AuthoredLootProfile(drops) };
            var scene = Scene(Actor(policy));
            var bindings = scene.Bindings("test");
            var catalog = new ActorPolicyCatalog();
            catalog.Add(1985, 510210, policy);
            tags[0] = "changed";
            drops.Clear();
            scene.Actors["enemy"] = Actor(new ActorGameplayPolicy { Invulnerable = true });

            var bound = bindings.Actors["enemy"].GameplayPolicy;
            CollectionAssert.AreEqual(new[] { "hostile" }, bound.Tags.ToArray());
            CollectionAssert.AreEqual(new[] { "hostile" }, catalog.Get(1985, 510210).Tags.ToArray());
            Assert.AreEqual(new LootDrop(28, 100, 1, 1), bound.Loot.Drops.Single());
            Assert.ThrowsExactly<NotSupportedException>(() => ((IList<string>)bound.Tags)[0] = "mutated");
        }

        [TestMethod]
        [DataRow(SceneActorKind.Object)]
        [DataRow(SceneActorKind.PracticeTarget)]
        [DataRow((SceneActorKind)99)]
        public void PolicyRequiresACreatureRole(SceneActorKind kind)
        {
            var scene = Scene(Actor(new ActorGameplayPolicy()) with { Kind = kind });
            Assert.ThrowsExactly<MissionRuleException>(() => Validate(scene));
        }

        [TestMethod]
        [DataRow(float.NaN)]
        [DataRow(float.PositiveInfinity)]
        [DataRow(float.NegativeInfinity)]
        [DataRow(-1f)]
        public void DefenseRadiusMustBeFiniteAndNonnegative(float radius)
        {
            var scene = Scene(Actor(new ActorGameplayPolicy { DefenseRadius = radius, DefenseTargetTag = "enemy" }));
            Assert.ThrowsExactly<MissionRuleException>(() => Validate(scene));
        }

        [TestMethod]
        [DynamicData(nameof(InvalidTags))]
        public void PolicyTagsMustBeNonemptyUniqueTokensAndDefenseMustNameATarget(ActorGameplayPolicy policy)
        {
            var error = Assert.ThrowsExactly<MissionRuleException>(() => Validate(Scene(Actor(policy))));
            StringAssert.Contains(error.Message, "enemy");
        }

        public static IEnumerable<object[]> InvalidTags()
        {
            yield return new object[] { new ActorGameplayPolicy { Tags = null } };
            yield return new object[] { new ActorGameplayPolicy { Tags = new string[] { null } } };
            yield return new object[] { new ActorGameplayPolicy { Tags = new[] { "" } } };
            yield return new object[] { new ActorGameplayPolicy { Tags = new[] { "two words" } } };
            yield return new object[] { new ActorGameplayPolicy { Tags = new[] { "enemy", "enemy" } } };
            yield return new object[] { new ActorGameplayPolicy { DefenseRadius = 10 } };
            yield return new object[] { new ActorGameplayPolicy { DefenseTargetTag = " " } };
        }

        [TestMethod]
        public void LootProfileRejectsNullDropsExplicitly()
        {
            Assert.ThrowsExactly<ArgumentException>(() => new AuthoredLootProfile(new LootDrop[] { null }));
        }

        [TestMethod]
        [DataRow(999999U, 1)]
        [DataRow(28U, int.MaxValue - 1)]
        public void ProductionCatalogRejectsMissingLootReferencesAndOversizedStacks(uint template, int maximum)
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            MissionContentTestSupport.ConfigureScenes(harness.WorldContext, scenes =>
                scenes[1994].Actors["enemy"] = Actor(new ActorGameplayPolicy
                {
                    RewardScenarioKills = true,
                    Loot = new AuthoredLootProfile(new[] { new LootDrop(template, 100, 1, maximum) })
                }));

            var error = Assert.ThrowsExactly<MissionRuleException>(() => harness.Manager.LoadMissions());

            StringAssert.Contains(error.Message, "enemy");
            StringAssert.Contains(error.Message, template.ToString());
        }

        [TestMethod]
        [DataRow("template")]
        [DataRow("link")]
        [DataRow("item-class")]
        [DataRow("entity-class")]
        public void ProductionCatalogValidatesEveryLootReference(string missing)
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            MissionContentTestSupport.ConfigureScenes(harness.WorldContext, scenes =>
                scenes[1994].Actors["enemy"] = Actor(new ActorGameplayPolicy
                {
                    RewardScenarioKills = true, Loot = new AuthoredLootProfile(new[] { new LootDrop(28, 100, 1, 1) })
                }));
            var world = harness.WorldContext;
            var link = world.Set<ItemTemplateItemClassEntry>().Single(entry => entry.ItemTemplateId == 28);
            switch (missing)
            {
                case "template": world.Remove(world.Set<ItemTemplateEntry>().Single(entry => entry.Id == 28)); break;
                case "link": world.Remove(link); break;
                case "item-class": world.Remove(world.Set<ItemClassEntry>().Single(entry => entry.Id == link.ItemClass)); break;
                case "entity-class": world.Remove(world.Set<EntityClassEntry>().Single(entry => entry.Id == link.ItemClass)); break;
            }
            world.SaveChanges();

            var error = Assert.ThrowsExactly<MissionRuleException>(() => harness.Manager.LoadMissions());

            StringAssert.Contains(error.Message, "enemy");
            StringAssert.Contains(error.Message, "28");
        }

        [TestMethod]
        [DataRow("policy")]
        [DataRow("absent")]
        [DataRow("kind")]
        [DataRow("template")]
        public void ProductionCatalogRejectsConflictingSharedActorDefinitions(string conflict)
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            MissionContentTestSupport.ConfigureScenes(harness.WorldContext, scenes =>
            {
                var shared = Actor(new ActorGameplayPolicy { Tags = new[] { "first", "second" } })
                    with { SharedKey = "policy-shared" };
                scenes[1994].Actors["enemy"] = shared;
                scenes[1995].Actors["enemy"] = conflict switch
                {
                    "policy" => shared with { GameplayPolicy = new ActorGameplayPolicy { Invulnerable = true } },
                    "absent" => shared with { GameplayPolicy = null },
                    "kind" => shared with { Kind = SceneActorKind.PublicSpawn, TemplateId = 510203 },
                    _ => shared with { TemplateId = 510216 }
                };
            });

            var error = Assert.ThrowsExactly<MissionRuleException>(() => harness.Manager.LoadMissions());

            StringAssert.Contains(error.Message, "policy-shared");
        }

        [TestMethod]
        public void ProductionCatalogAcceptsEquivalentSharedRolePoliciesAndRealLootReferences()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            MissionContentTestSupport.ConfigureScenes(harness.WorldContext, scenes =>
            {
                foreach (var id in new uint[] { 1994, 1995 })
                    scenes[id].Actors["enemy"] = Actor(new ActorGameplayPolicy
                    {
                        Tags = id == 1994 ? new[] { "first", "second" } : new[] { "second", "first" },
                        RewardScenarioKills = true, Loot = new AuthoredLootProfile(new[] { new LootDrop(28, 100, 1, 1) })
                    }) with { SharedKey = "policy-shared" };
            });

            Assert.IsFalse(harness.Manager.LoadMissions().BlocksReadiness);
        }

        private static SceneActorDefinition Actor(ActorGameplayPolicy policy = null) =>
            new("enemy", SceneActorKind.Creature, 510210, new ScenePosition(0, 0, 0), GameplayPolicy: policy);

        private static MissionSceneDefinition Scene(SceneActorDefinition actor) =>
            new() { Script = "data.sequence", Actors = new() { [actor.Role] = actor } };

        private static void Validate(MissionSceneDefinition scene) =>
            MissionSceneValidation.Validate(321, "test", scene, new[] { 1U });
    }
}
