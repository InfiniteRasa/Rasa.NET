using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Structures;
using Rasa.Test.Missions;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Gameplay
{
    [TestClass, DoNotParallelize]
    public class ScenarioCreatureStatsTests
    {
        [TestMethod]
        public void SceneCreaturesUseTheSameAuthoredHealthArmorAndAttributesAsPublicCreatures()
        {
            using var h = WildernessRuntimeTestHarness.Create();
            var pool = new SpawnPool { MapContextId = 1220, RuntimeMapChannel = h.Map, Position = h.Client.Player.Position };
            var ordinary = h.Creatures.CreateCreature(630001, pool);
            var scene = h.Creatures.CreateScenarioCreature(pool, 630001, pool.Position, 0);
            Assert.IsNotNull(ordinary); Assert.IsNotNull(scene);
            CellManager.Instance.AddToWorld(h.Map, ordinary);
            CellManager.Instance.AddToWorld(h.Map, scene);
            foreach (var attribute in new[] { Attributes.Body, Attributes.Mind, Attributes.Spirit, Attributes.Health, Attributes.Armor })
                Assert.AreEqual(ordinary.Attributes[attribute].CurrentMax, scene.Attributes[attribute].CurrentMax, attribute.ToString());
            Assert.AreEqual(4000, scene.Attributes[Attributes.Health].CurrentMax);
            Assert.AreEqual(300, scene.Attributes[Attributes.Armor].CurrentMax);
        }
        [TestMethod]
        public void SyntheticSceneTemplatesKeepTheFallbackWithoutAccessingWorldStorage()
        {
            using var context = MissionTestContext.WithDefinitions(321);
            var manager = new CreatureManager(context, new ManifestationManager(context), context.Manager);
            manager.LoadedCreatures[999999] = new Creature
            {
                DbId = 999999, EntityClass = EntityClasses.HumanBaseMale, Level = 6,
                MaxHitPoints = 321, AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };
            var entityClass = EntityClassManager.Instance.LoadedEntityClasses[EntityClasses.HumanBaseMale];
            var added = !entityClass.Augmentations.Contains(AugmentationType.Creature);
            if (added) entityClass.Augmentations.Add(AugmentationType.Creature);
            try
            {
                var pool = new SpawnPool { MapContextId = context.Map.MapInfo.MapContextId, RuntimeMapChannel = context.Map };
                var creature = manager.CreateScenarioCreature(pool, 999999, pool.Position, 0);
                Assert.IsNotNull(creature);
                CellManager.Instance.AddToWorld(context.Map, creature);
                Assert.AreEqual(321, creature.Attributes[Attributes.Health].CurrentMax);
                Assert.AreEqual(100, creature.Attributes[Attributes.Armor].CurrentMax);
                Assert.AreEqual(18, creature.Attributes[Attributes.Body].CurrentMax);
                // MissionTestContext.CreateWorld throws on any access; reaching here proves no spawn-time query.
            }
            finally { if (added) entityClass.Augmentations.Remove(AugmentationType.Creature); }
        }
    }
}
