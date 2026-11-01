using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Scenes;
using Rasa.Missions.Runtime;
using Rasa.Structures;
using Rasa.Structures.World;

namespace Rasa.Managers
{
    /// <summary>Map scope is server metadata: retail indicator tuples contain only position/radius/name/3D flag.</summary>
    internal static class MissionMarkerProjection
    {
        internal static void LoadDefaults(MissionContentCatalog catalog, IEnumerable<SpawnPoolEntry> pools)
        {
            var poolMaps = pools.ToDictionary(p => p.Id, p => p.MapContextId);
            var creatureMaps = pools.SelectMany(p => new[] { p.Creature1Id, p.Creature2Id, p.Creature3Id,
                    p.Creature4Id, p.Creature5Id, p.Creature6Id }.Where(id => id != 0).Select(id => (Id: id, Map: p.MapContextId)))
                .ToLookup(p => p.Id, p => p.Map);
            foreach (var mission in catalog.Missions.Values.Where(m => m.Objectives.Values.Any(o => o.Indicators.Count > 0)))
            {
                catalog.SceneBindings.TryGetValue(mission.MissionId, out var scene);
                if (scene?.MapContextId > 0) { catalog.IndicatorMapDefaults[mission.MissionId] = scene.MapContextId; continue; }
                var maps = new HashSet<uint>();
                if (catalog.Areas.TryGetValue(mission.MissionId, out var areas))
                    maps.UnionWith(areas.Values.Select(a => a.MapContextId));
                if (catalog.SpawnGroups.TryGetValue(mission.MissionId, out var groups))
                    maps.UnionWith(groups.Values.Select(g => g.MapContextId));
                maps.UnionWith(mission.Items.Values.Where(i => i.Drop != null).Select(i => i.Drop.MapContextId));
                maps.UnionWith(catalog.Experiences.Where(e => e.MissionTriggers.Any(t => t.MissionId == mission.MissionId))
                    .Select(e => e.MapContextId));
                if (scene != null)
                    foreach (var actor in scene.Actors.Values.Where(a => a.Kind == SceneActorKind.PublicSpawn))
                        if (poolMaps.TryGetValue(actor.TemplateId, out var map)) maps.Add(map);
                maps.Remove(0);
                // Simple NPC-only missions may have no scene/area. Use their placed contacts
                // only when no authored world scope is available and all contacts agree.
                if (maps.Count == 0)
                {
                    if (mission.MissionGiver.HasValue) maps.UnionWith(creatureMaps[mission.MissionGiver.Value]);
                    if (mission.MissionReciver.HasValue) maps.UnionWith(creatureMaps[mission.MissionReciver.Value]);
                    maps.Remove(0);
                }
                catalog.IndicatorMapDefaults[mission.MissionId] = maps.Count == 1 ? maps.Single() : 0;
            }
        }

        /// <summary>The Logos shrines (the logos table), for the objectives that send a player to one.</summary>
        internal static void LoadShrines(MissionContentCatalog catalog, IEnumerable<LogosEntry> shrines)
        {
            catalog.LogosShrines.Clear();
            foreach (var shrine in shrines ?? Enumerable.Empty<LogosEntry>())
                catalog.LogosShrines[shrine.Id] = shrine;
        }

        /// <summary>
        /// The placed shrines of the Logos an objective is completed by acquiring (its
        /// LogosAcquired subjects). A "find the Logos" mission's marker is on the shrine's map,
        /// whatever map its giver stands on: Logos: Feeling, Trap, Heal is given in the Wilderness
        /// and its shrines are in the Caves of Donn.
        /// </summary>
        internal static IReadOnlyList<LogosEntry> ShrinesFor(MissionContentCatalog catalog, uint missionId, uint objectiveId)
        {
            if (!catalog.Missions.TryGetValue(missionId, out var mission) ||
                !mission.Objectives.TryGetValue(objectiveId, out var objective))
                return Array.Empty<LogosEntry>();

            return objective.GetExecutableTransitionsOrLegacyDefault()
                .Where(transition => transition.ProgressRule?.Kind == MissionProgressEventKind.LogosAcquired)
                .SelectMany(transition => transition.ProgressRule.Subjects)
                .Distinct()
                .Select(logosId => catalog.LogosShrines.GetValueOrDefault(logosId))
                .Where(shrine => shrine != null)
                .ToList();
        }

        /// <summary>The one map all of an objective's Logos shrines are on, or 0.</summary>
        private static uint ShrineMap(MissionContentCatalog catalog, uint missionId, uint objectiveId)
        {
            var maps = ShrinesFor(catalog, missionId, objectiveId).Select(shrine => shrine.MapContextId).Distinct().ToList();
            return maps.Count == 1 ? maps[0] : 0;
        }

        /// <summary>Radius of a marker put on a shrine for an objective that has none authored, as the authored ones on shrines.</summary>
        internal const double ShrineMarkerRadius = 8;

        /// <summary>
        /// A marker on the shrine for a "find the Logos" objective with none authored - the
        /// Wilderness Logos dispatches (Logos: Area, Damage, Time, ...) and Pravus's had no
        /// mission_indicator rows, so nothing showed on the map or the radar. One Logos only:
        /// a list of them (Wilderness Targets of Opportunity) is left to its own text.
        /// </summary>
        private static MissionIndicator ShrineMarker(MissionContentCatalog catalog, uint missionId, uint objectiveId)
        {
            var shrines = ShrinesFor(catalog, missionId, objectiveId);
            if (shrines.Count != 1)
                return null;

            var shrine = shrines[0];
            return new MissionIndicator
            {
                MapContextId = shrine.MapContextId,
                Position = new Vector3((float)shrine.PosX, (float)shrine.PosY, (float)shrine.PosZ),
                Radius = ShrineMarkerRadius,
                IndicatorId = MissionIndicator.UnnamedFrom + objectiveId
            };
        }

        internal static void Validate(Mission mission, MissionSceneDefinition scene)
        {
            if (scene.IndicatorMaps == null) return;
            foreach (var objective in scene.IndicatorMaps)
            {
                if (!mission.Objectives.TryGetValue(objective.Key, out var definition) || objective.Value == null)
                    throw new MissionRuleException($"Mission {mission.MissionId}: indicator maps name an unknown objective.");
                foreach (var entry in objective.Value)
                    if (entry.Value == 0 || !definition.Indicators.Any(i => i.IndicatorId == entry.Key))
                        throw new MissionRuleException($"Mission {mission.MissionId}: indicator maps require an existing indicator and a nonzero map id.");
            }
        }

        internal static uint ResolveMap(MissionContentCatalog catalog, uint missionId, uint objectiveId, MissionIndicator indicator)
        {
            if (indicator.MapContextId != 0) return indicator.MapContextId;
            if (catalog.SceneBindings.TryGetValue(missionId, out var scene) && scene.IndicatorMaps != null &&
                scene.IndicatorMaps.TryGetValue(objectiveId, out var overrides) &&
                overrides.TryGetValue(indicator.IndicatorId, out var authoredMap)) return authoredMap;

            // A marker for a Logos is where its shrine is.
            var shrineMap = ShrineMap(catalog, missionId, objectiveId);
            if (shrineMap != 0) return shrineMap;

            // Existing database rows predate map metadata. These cross-map quests cannot use
            // a single mission-wide default. Match their authored markers, never coordinates.
            // Sources: TorcastraMissionsV1, DevilsDenContent, MinosCavernsMissions,
            // PravusResearchContent and OjasaHiveContent in Services/Preloader; the Plateau and
            // Pools quests' NPC markers off the giver's map, each on the NPC it marks (the
            // missions' preloaders, Services/Preloader/Missions/Plateau and Pools).
            var legacyMap = (missionId, objectiveId, indicator.IndicatorId) switch
            {
                (356, 4 or 5 or 8 or 9 or 10 or 11, var id) when id == MissionIndicator.UnnamedFrom + objectiveId => 1349U,
                (397, 2, 24) => 1349U, // Prison cell switch
                (399, >= 1 and <= 5, 10000001) => 1349U, // Five prison cell blocks
                (380, 1, 1000000001) => 1394U, // Caritas in the Den
                (671, 3, 1000000003) or (671, 8, 1000000008) => 1394U,
                (671, 7, 1000000007) => 1244U, // Return to Cyrida in Palisades
                (382 or 384, 1, 33) => 1148U, // Minos cave mouth on the Divide
                (593, 1, 54) => 1220U, // Pravus entrance in Wilderness
                (1491, 1, 278) => 1761U, // Hive entrance on Incline
                (1491, 2, 1000000002) => 1865U, // John inside the hive
                (935, 1, 1000000001) => 1497U, // Trinity Bridge conduits, Plateau
                (935, 1, 1000000002) => 2029U, // Veryin in the Temporal Chamber
                (1068, 1, 1000000001) => 1304U, // "Snake" Washington, Snake Pit (Pools)
                (1329, 1, 1000000001) => 1694U, // Kaethi Rose, Retread Caves
                (1541, 1, 1000000001) => 1304U, // Amee Corman, Retread Camp (Pools)
                (1751, 1, 1000000001) => 1454U, // Retread Vincent, Retread City (Marshes)
                (1982, 1, 1000000001) => 2136U, // Elder Hathen, Velon Hollow
                (2016, 1, 1000000001) => 1497U, // General Bailey, Fort Defiance (Plateau)
                (2017, 1, 1000000001) => 1304U, // the Hermit, Fractured Butte (Pools)
                (2017, 2, 1000000002) => 1454U, // Retread McCormick, Retread City (Marshes)
                (2017, 6, 1000000004) => 1220U, // Archaeologist Topper, Twin Pillars (Wilderness)
                _ => 0U
            };
            return legacyMap != 0 ? legacyMap : catalog.IndicatorMapDefaults.GetValueOrDefault(missionId);
        }

        internal static MissionInfo Apply(Manifestation player, uint missionId, MissionInfo info, MissionContentCatalog catalog)
        {
            var currentMap = player?.MapContextId ?? 0;
            foreach (var objective in info.ObjectivesList)
            {
                if (objective.IndicatorList.Count == 0 && ShrineMarker(catalog, missionId, objective.ObjectiveId) is { } shrineMarker)
                    objective.IndicatorList.Add(shrineMarker);

                var visible = new List<MissionIndicator>();
                foreach (var indicator in objective.IndicatorList)
                {
                    var map = ResolveMap(catalog, missionId, objective.ObjectiveId, indicator);
                    // Missing/ambiguous scope is not a marker that should appear on every map.
                    if (map == 0 || map != currentMap) continue;
                    visible.Add(new MissionIndicator { MapContextId = map, Position = indicator.Position,
                        Radius = indicator.Radius, IndicatorId = indicator.IndicatorId, Show3DEffect = indicator.Show3DEffect });
                }
                objective.IndicatorList.Clear();
                objective.IndicatorList.AddRange(visible);
            }
            return info;
        }
    }
}
