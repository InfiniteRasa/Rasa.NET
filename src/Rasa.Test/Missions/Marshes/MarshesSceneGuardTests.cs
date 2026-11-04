using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Missions.Content;
using Rasa.Missions.Content.Marshes;
using Rasa.Services.Preloader.Missions.Marshes;
using Rasa.Structures.World;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Marshes
{
    /// <summary>
    /// The scene scripts' data rules that the scripts do not check at run time (code review of the Marshes batch). Why these
    /// tests: a scene that breaks one of them does not throw; it stops in the field, and only a playthrough of that mission
    /// would show it.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class MarshesSceneGuardTests
    {
        private static List<(uint MissionId, MissionSceneDefinition Scene)> Scenes(string script)
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var revision = MarshesMissionDataV1.Revision;
            return harness.World.Set<MissionSceneBindingEntry>().AsNoTracking()
                .Where(entry => entry.ContentRevision == revision).ToList()
                .Select(entry => (entry.MissionId, Scene: System.Text.Json.JsonSerializer.Deserialize<MissionSceneDefinition>(
                    entry.Bindings, MissionContentCodec.Options)))
                .Where(entry => entry.Scene.Script == script).ToList();
        }

        private static uint Name(MissionSceneDefinition scene, string key) => scene.Names.TryGetValue(key, out var value) ? value : 0;

        /// <summary>
        /// MarshesEscortScene: when a leg's whole group dies and the leg has no objective (leg.K.objective 0), the run goes
        /// "lost", and "lost" starts no later leg. So only an escort's last leg may have objective 0 (1033's mech, one leg).
        /// </summary>
        [TestMethod]
        public void OnlyAnEscortsLastLegMayHaveNoObjective()
        {
            var scenes = Scenes("marshes.escort");
            Assert.IsGreaterThanOrEqualTo(5, scenes.Count, "the escort scenes are read (497, 888, 889, 1023, 1033)");
            foreach (var (mission, scene) in scenes)
            {
                var legs = (int)Name(scene, MarshesEscortScene.Legs);
                Assert.IsGreaterThan(0, legs, $"{mission} legs");
                for (var leg = 1; leg < legs; leg++)
                    Assert.AreNotEqual(0u, Name(scene, MarshesEscortScene.Objective(leg)),
                        $"{mission} leg {leg} of {legs} has no objective: its group's death would stop the escort");
            }
        }

        /// <summary>
        /// MarshesDefenceScene: a wave's timer is dropped when a later wave was already sent, and the hold timer ends the
        /// defence. So the waves' delays (wave.K.after) must rise with K, and the hold must come after the last wave.
        /// </summary>
        [TestMethod]
        public void TheDefenceWavesComeInOrderAndBeforeTheHold()
        {
            var scenes = Scenes("marshes.defence");
            CollectionAssert.AreEquivalent(new[] { MarshesTrappedV1.MissionId, MarshesHeadEastV2.MissionId },
                scenes.Select(entry => entry.MissionId).ToArray(), "the defence scenes");
            foreach (var (mission, scene) in scenes)
            {
                var waves = (int)Name(scene, MarshesDefenceScene.Waves);
                Assert.IsGreaterThan(0, waves, $"{mission} waves");
                for (var wave = 2; wave <= waves; wave++)
                    Assert.IsGreaterThan(Name(scene, MarshesDefenceScene.After(wave - 1)), Name(scene, MarshesDefenceScene.After(wave)),
                        $"{mission} wave {wave} comes after wave {wave - 1}");
                Assert.IsGreaterThan(Name(scene, MarshesDefenceScene.After(waves)), Name(scene, MarshesDefenceScene.Hold),
                    $"{mission} the hold ends after the last wave");
            }
        }
    }
}
