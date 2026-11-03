using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Rasa.Structures;

namespace Rasa.Services.Preloader
{
    using C = CraterLakeContent;
    using G = MagmaCavernsContent;
    using T = Missions.Timora.TimoraMinesMissionsV1;

    /// <summary>
    /// The map of each mission marker the server could not place (Marker_map_scopes).
    ///
    /// An objective's marker is shown only on its own map (MissionMarkerProjection): a marker whose
    /// map cannot be told - no scene map, area, spawn group or single map of its giver and receiver -
    /// is shown on none, so the player never saw these:
    ///
    /// - Crater Lake: The Dead Live (450) objective 4, the Wilderness' door to the facility (1220),
    ///   given at Ranja Gorge and taken in the facility; Reclaiming What's Ours (1056) objectives
    ///   3-6, the courtyard, panel, bomb caches and outer generator, and Bending the Rules (1065)
    ///   objective 3, the way in, all in the facility (1721). Reclaiming What's Ours' objectives 1
    ///   and 2 are Logos shrines', which the shrines place.
    /// - Timora Mines: Report to Field Ranger Kearney (340) objective 2, Kearney in the mines
    ///   (1348); Simpson gives it on Concordia Divide.
    /// - Magma Caverns: every marker of Executioner (1944) and Final Chance (1945), all in the
    ///   caverns (1977); both are radioed, with no giver or receiver.
    ///
    /// Each scene gets an indicatorMaps entry (MissionSceneDefinition.IndicatorMaps), added to its
    /// stored JSON as the last property, only where it has none; Down takes off exactly what Up
    /// added. The JSON is joined with || on SQLite and CONCAT on MySQL, where || is OR.
    /// </summary>
    public static class MarkerMapScopes
    {
        public const string Key = "\"indicatorMaps\":";

        private static uint Unnamed(uint objective) => MissionIndicator.UnnamedFrom + objective;

        /// <summary>The markers: (mission, its revision, objective, indicator, map).</summary>
        public static readonly (uint MissionId, string Revision, uint ObjectiveId, uint IndicatorId, uint MapContextId)[] Markers =
            new[]
            {
                (C.TheDeadLive, C.Revision, C.SpeakToVelns, Unnamed(C.SpeakToVelns), C.Wilderness),
                (C.ReclaimingWhatsOurs, C.Revision, C.KillOverseer, Unnamed(C.KillOverseer), C.CraterLake),
                (C.ReclaimingWhatsOurs, C.Revision, C.UsePanel, Unnamed(C.UsePanel), C.CraterLake),
                (C.ReclaimingWhatsOurs, C.Revision, C.GatherBombs, Unnamed(C.GatherBombs), C.CraterLake),
                (C.ReclaimingWhatsOurs, C.Revision, C.BlowGenerator, C.GeneratorMarker, C.CraterLake),
                (C.BendingTheRules, C.Revision, C.EscortOut, Unnamed(C.EscortOut), C.CraterLake),
                (T.ReportToKearney, T.Revision, 2U, T.Unnamed(2), TimoraMinesContent.Map)
            }
            .Concat(new[]
                {
                    G.ExMezen, G.ExDama, G.ExStopAssault, G.ExShutWormhole, G.ExWormhole1, G.ExFrontLine, G.ExDoor1, G.ExNelby,
                    G.ExWormhole2, G.ExDoor2, G.ExStronghold, G.ExSudevi, G.ExCamp, G.ExSalyard, G.ExLaptop, G.ExRelease, G.ExSubjects
                }
                .Select(objective => (G.Executioner, G.Revision, objective, Unnamed(objective), G.MagmaCaverns)))
            .Concat(new[]
                {
                    G.FcSalyard, G.FcSudevi, G.FcShutWormhole1, G.FcHoldCamp, G.FcNelby, G.FcShutWormhole2, G.FcHoldDoor,
                    G.FcRebelWormhole, G.FcInfiltrate, G.FcBeacon1, G.FcBeacon2, G.FcDama, G.FcMezen
                }
                .Select(objective => (G.FinalChance, G.Revision, objective, Unnamed(objective), G.MagmaCaverns)))
            .ToArray();

        /// <summary>The scenes changed: (mission, revision).</summary>
        public static IEnumerable<(uint MissionId, string Revision)> Scenes() => Markers.Select(m => (m.MissionId, m.Revision)).Distinct();

        /// <summary>A mission's indicatorMaps property as it is added: "indicatorMaps":{"objective":{"indicator":map}}.</summary>
        public static string Property(uint missionId) =>
            Key + JsonSerializer.Serialize(Markers.Where(m => m.MissionId == missionId)
                .GroupBy(m => m.ObjectiveId)
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key.ToString(), g => g.ToDictionary(m => m.IndicatorId.ToString(), m => m.MapContextId)));

        public static IEnumerable<string> Up(bool mySql)
        {
            foreach (var (missionId, revision) in Scenes())
            {
                var suffix = $",{Property(missionId)}}}";
                var head = "SUBSTR(bindings, 1, LENGTH(bindings) - 1)";
                var joined = mySql ? $"CONCAT({head}, '{suffix}')" : $"{head} || '{suffix}'";
                yield return $"UPDATE mission_scene_binding SET bindings = {joined} " +
                    $"WHERE mission_id = {missionId} AND content_revision = '{revision}' " +
                    $"AND bindings LIKE '%}}' AND bindings NOT LIKE '%{Key}%';";
            }
        }

        public static IEnumerable<string> Down()
        {
            foreach (var (missionId, revision) in Scenes())
            {
                var suffix = $",{Property(missionId)}}}";
                yield return $"UPDATE mission_scene_binding SET bindings = REPLACE(bindings, '{suffix}', '}}') " +
                    $"WHERE mission_id = {missionId} AND content_revision = '{revision}' AND bindings LIKE '%{suffix}';";
            }
        }
    }
}
