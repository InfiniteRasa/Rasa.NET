using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Missions.Content;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Missions.Scenes;
using Rasa.Services.Preloader.Missions.Marshes;
using Rasa.Structures.World;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Marshes
{
    /// <summary>
    /// To-fix row 7 (548's block C marker 17 m from its terminal): a marker that does not hold its objective's target sends
    /// the player (and the tester bot, which uses only what stands inside a marker) to the wrong place. Why this test: for
    /// every Marshes objective that completes by a talk, an area or an object use, the target (the NPC or talking object, the
    /// area's centre, the object) lies inside at least one marker of that objective, on the same map. The markers of the
    /// objectives a target's objective completes (ObjectiveState) count too; a hidden objective's target may lie in any of
    /// the mission's markers. A talk is held if any of the package's NPCs stands inside (894's prisoner and survivor share a
    /// package).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class MarshesMarkerGuardTests
    {
        /// <summary>
        /// The exceptions, written out: 917 1, Ranger Alia walks a beat (a squad patrol, no pool and no scene actor): her
        /// marker covers the beat (MarshesMissionTests.TheMarkersStandAtTheirObjectivesTargets); 664 5, the waves.
        /// </summary>
        private static readonly (uint Mission, uint Objective, string Kind)[] Exceptions =
        {
            (MarshesDividedWeFallV1.MissionId, 1, "talk"),
            // 664 5: the waves' Thrax come from the camp's south and west and run at the player in the camp (r20 marker):
            // they are killed where they reach him, not where they stand up.
            (MarshesSurvivorsV2.MissionId, 5, "signal"),
            // 663 3 (stealth): a front guard's defeat sounds the alarm (to-fix 36), which fails 3; 3's markers are the
            // door and the lower entrance, where it is done, not the front guards, where it is lost.
            (MarshesInsertionPointV1.MissionId, 3, "signal")
        };

        [TestMethod]
        public void EveryTalkAreaAndUseTargetStandsInsideItsObjectivesMarker()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            var revision = MarshesMissionDataV1.Revision;
            var markers = world.MissionIndicatorEntries.AsNoTracking().Where(entry => entry.ContentRevision == revision).ToList();
            var triggers = world.MissionTriggerEntries.AsNoTracking().Where(entry => entry.ContentRevision == revision).ToList();
            var areas = world.MissionAreaEntries.AsNoTracking().Where(entry => entry.ContentRevision == revision).ToList();
            var packages = world.NpcPackageEntries.AsNoTracking().ToList().ToLookup(entry => entry.PackageId, entry => entry.Id);
            var pools = world.SpawnPoolEntries.AsNoTracking().ToList();
            var scenes = world.Set<MissionSceneBindingEntry>().AsNoTracking().Where(entry => entry.ContentRevision == revision).ToList()
                .ToDictionary(entry => entry.MissionId, entry => System.Text.Json.JsonSerializer.Deserialize<MissionSceneDefinition>(
                    entry.Bindings, MissionContentCodec.Options));
            uint SceneMap(MissionSceneDefinition scene) => scene.BoundMapContextId ?? scene.MapContextId;
            // A marker's map as the server finds it (the scene's IndicatorMaps, its map, the areas or the pools).
            var catalog = new MissionContentCatalog(harness, new Dictionary<uint, Rasa.Structures.Mission>(), null);
            catalog.Load();
            uint MarkerMap(MissionIndicatorEntry marker) => MissionMarkerProjection.ResolveMap(catalog, marker.MissionId, marker.ObjectiveId,
                new Rasa.Structures.MissionIndicator { IndicatorId = marker.IndicatorId });

            // The objectives an objective's completion completes (ObjectiveState), climbing.
            HashSet<uint> Allowed(uint mission, uint objective)
            {
                var allowed = new HashSet<uint> { objective };
                var open = new Stack<uint>(allowed);
                while (open.Count > 0)
                {
                    var current = open.Pop();
                    foreach (var up in triggers.Where(entry => entry.MissionId == mission &&
                                 entry.Kind == MissionTriggerKind.ObjectiveState && entry.RelatedObjectiveId == current))
                        if (allowed.Add(up.ObjectiveId))
                            open.Push(up.ObjectiveId);
                }
                return allowed;
            }

            bool Inside(uint mission, uint objective, IEnumerable<(Vector2 At, uint Map)> places)
            {
                var hidden = scenes.TryGetValue(mission, out var scene) && scene.HiddenObjectiveIds?.Contains(objective) == true;
                var allowed = Allowed(mission, objective);
                var own = markers.Where(marker => marker.MissionId == mission && (hidden || allowed.Contains(marker.ObjectiveId))).ToList();
                return places.Any(place => own.Any(marker => MarkerMap(marker) == place.Map &&
                    Vector2.Distance(place.At, new((float)marker.PosX, (float)marker.PosZ)) <= marker.Radius + 0.01));
            }

            IEnumerable<uint> Creatures(SpawnPoolEntry pool) => new[]
                { pool.Creature1Id, pool.Creature2Id, pool.Creature3Id, pool.Creature4Id, pool.Creature5Id, pool.Creature6Id }.Where(id => id != 0);

            var checkedTargets = 0;
            var misses = new List<(uint Mission, uint Objective, string Kind)>();
            foreach (var trigger in triggers)
            {
                var places = new List<(Vector2 At, uint Map)>();
                string kind;
                if (trigger.Kind == MissionTriggerKind.Conversation && trigger.NpcPackageId is { } package)
                {
                    kind = "talk";
                    var npcs = packages[package].ToHashSet();
                    places.AddRange(pools.Where(pool => Creatures(pool).Any(npcs.Contains))
                        .Select(pool => (new Vector2((float)pool.PosX, (float)pool.PosZ), pool.MapContextId)));
                    foreach (var scene in scenes.Values)
                        places.AddRange(scene.Actors.Values.Where(actor => actor.Position != null &&
                                (actor.Kind == SceneActorKind.Creature && npcs.Contains(actor.TemplateId) ||
                                 actor.Conversation is { } talk && talk.NpcPackageId == package && talk.MissionId == trigger.MissionId))
                            .Select(actor => (new Vector2(actor.Position.X, actor.Position.Z), SceneMap(scene))));
                }
                else if (trigger.Kind == MissionTriggerKind.AreaEntered && trigger.AreaId is { } areaId)
                {
                    kind = "area";
                    places.AddRange(areas.Where(area => area.MissionId == trigger.MissionId && area.AreaId == areaId)
                        .Select(area => (new Vector2((float)area.PosX, (float)area.PosZ), area.MapContextId)));
                }
                else
                    continue;
                checkedTargets++;
                if (!Inside(trigger.MissionId, trigger.ObjectiveId, places))
                    misses.Add((trigger.MissionId, trigger.ObjectiveId, kind));
            }
            foreach (var (mission, scene) in scenes)
                foreach (var actor in scene.Actors.Values.Where(actor => actor.UseAction is { ObjectiveId: > 0 } && actor.Position != null))
                {
                    checkedTargets++;
                    if (!Inside(mission, actor.UseAction.ObjectiveId, new[] { (new Vector2(actor.Position.X, actor.Position.Z), SceneMap(scene)) }))
                        misses.Add((mission, actor.UseAction.ObjectiveId, "use"));
                }

            // To-fix 894 (a): an objective counted by scene signals (a pump blown, a boss killed) has its markers over the actors
            // whose use, destruction or defeat sends the signal: the use target's own objective was checked above, but the
            // objective its signal counts for (894's 1, "round the pumps") was not.
            foreach (var (mission, scene) in scenes)
            {
                var sources = new List<(SceneActorDefinition Actor, uint Sequence)>();
                foreach (var actor in scene.Actors.Values.Where(actor => actor.Position != null))
                {
                    if (actor.UseAction is { } use)
                        sources.Add((actor, use.SequenceId));
                    if (actor.Destruction is { } destruction)
                        sources.Add((actor, destruction.SequenceId));
                    if (scene.DefeatSequences != null && scene.DefeatSequences.TryGetValue(actor.Role, out var defeat))
                        sources.Add((actor, defeat));
                }
                foreach (var (actor, sequence) in sources)
                {
                    if (!scene.Sequences.TryGetValue(sequence, out var run))
                        continue;
                    foreach (var signal in run.Signals.Where(signal => signal.MissionId == mission))
                        foreach (var objective in triggers.Where(entry => entry.MissionId == mission && entry.Kind == MissionTriggerKind.ProgressEvent &&
                                     entry.EventKind == (byte)MissionProgressEventKind.ScenarioEvent && entry.CounterId == signal.SequenceId &&
                                     entry.SubjectId == signal.EventId).Select(entry => entry.ObjectiveId).Distinct())
                        {
                            if (scene.HiddenObjectiveIds?.Contains(objective) == true)
                                continue;
                            checkedTargets++;
                            if (!Inside(mission, objective, new[] { (new Vector2(actor.Position.X, actor.Position.Z), SceneMap(scene)) }))
                                misses.Add((mission, objective, "signal"));
                        }
                }
            }

            Assert.IsGreaterThan(100, checkedTargets, "the targets are read");
            CollectionAssert.AreEquivalent(Exceptions, misses.Distinct().ToArray(),
                "outside every marker: " + string.Join(", ", misses.Distinct()));
        }
    }
}
