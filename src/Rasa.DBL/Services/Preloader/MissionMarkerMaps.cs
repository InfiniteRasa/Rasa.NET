using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Rasa.Missions.Content;
using Rasa.Services.Preloader.Missions.Plateau;
using Rasa.Services.Preloader.Missions.Pools;
using Rasa.Structures;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader
{
    using D = DevilsDenContent;

    /// <summary>
    /// The map of each mission marker MissionMarkerProjection.ResolveMap puts on the wrong map
    /// (Mission_marker_maps), as MarkerMapScopes does for the markers it could put on none.
    ///
    /// These markers have no map of their own and the mission-wide default (scene map, area,
    /// spawn group, giver and receiver) is another map than the one they stand on, so the player
    /// sees them on a map where nothing is at that spot. The map of each is the one map with world
    /// objects at the marker's position (the world DB's spawn pools, logos and static objects):
    ///
    /// - Devil's Den (1394), given in Palisades: Hakari's Band (378), Luroron's Band (379) and Eloh
    ///   Artifacts (381) objective 1; The Devil's Strike (669) objectives 1 and 2; Missing Strike Team
    ///   (670) objective 1; Sever the Head (672) objectives 1, 3, 4 and 5 (the field commander, the
    ///   landing pad and the fuel depot).
    /// - One Way Trip (877), scene on Maligo Base (1830): objectives 9 and 13 on the Plateau (1497),
    ///   objective 10 in Ustor Yard (1502).
    /// - Weapons Trafficking (904), scene on the Lamna Armory (1465): objective 1, Amee Corman at the
    ///   Retread Camp on the Pools (1304).
    /// - Looking For Logos? (1308) objective 3: the Turpis Refinery door on the Pools (1304). The Far
    ///   Logos' shrine rule put it in the refinery (1429), so the door's marker was never seen; with
    ///   the door on the Pools the refinery had no marker, and a marker on the Far shrine (logos 64,
    ///   read from the table when the migration runs, radius MissionMarkerProjection.ShrineMarkerRadius)
    ///   is added for it, on 1429.
    /// - The Better Half (1975) objectives 3 and 4 and Ancient Mysteries (1978) objectives 2 and 3,
    ///   scenes in Velon Hollow (2136): on the Plateau (1497).
    /// - Turpis Transport (1305) objective 1, Corporal Hash by the refinery door on the Pools (1304), and
    ///   Deadly Silence (1974) objectives 1-4, the Order's leaders in Velon Hollow (2136): these have no
    ///   scene, so each gets one with only indicatorMaps (no script, as 1308's and 1978's data-only
    ///   scenes).
    ///
    /// Up adds an indicatorMaps property to a stored scene as its last property, only where it has
    /// none, inserts a scene only where the mission has none, and the refinery marker only where
    /// objective 3 exists and has no such marker. Down takes off exactly what Up added: the property
    /// where the scene ends with it, a scene only where it is still the one Up wrote, the marker only
    /// where it is still the one Up wrote. The JSON is joined with || on SQLite and CONCAT on MySQL,
    /// where || is OR.
    /// </summary>
    public static class MissionMarkerMaps
    {
        public const string Key = MarkerMapScopes.Key;

        private const uint Plateau = PlateauMissionDataV1.MapContextId, UstorYard = PlateauUstorYardV1.MapContextId;
        private const uint Pools = PoolsMissionDataV1.MapContextId, VelonHollow = PlateauVelonHollowBaneV1.MapContextId;
        private const string DenRevision = D.Revision, PlateauRevision = PlateauMissionDataV1.Revision, PoolsRevision = PoolsMissionDataV1.Revision;

        private static uint Unnamed(uint id) => MissionIndicator.UnnamedFrom + id;

        /// <summary>The marker added on the Far Logos' shrine, inside Turpis Refinery, for Looking For Logos? objective 3.</summary>
        public const uint RefineryMission = PoolsLookingForLogosV1.MissionId, RefineryObjective = PoolsLookingForLogosV1.Find;
        public static readonly uint RefineryIndicator = Unnamed(4);
        public const string RefineryComment = "Far Logos shrine in Turpis Refinery";

        /// <summary>The markers: (mission, its revision, objective, indicator, map).</summary>
        public static readonly (uint MissionId, string Revision, uint ObjectiveId, uint IndicatorId, uint MapContextId)[] Markers =
        {
            (D.HakarisBand, DenRevision, 1, Unnamed(1), D.Den),
            (D.LuroronsBand, DenRevision, 1, Unnamed(1), D.Den),
            (D.ElohArtifacts, DenRevision, 1, Unnamed(1), D.Den),
            (D.DevilsStrike, DenRevision, 1, Unnamed(1), D.Den),
            (D.DevilsStrike, DenRevision, 2, Unnamed(2), D.Den),
            (D.MissingStrikeTeam, DenRevision, 1, Unnamed(1), D.Den),
            (D.SeverTheHead, DenRevision, 1, D.FieldCommanderMarker, D.Den),
            (D.SeverTheHead, DenRevision, 3, Unnamed(3), D.Den),
            (D.SeverTheHead, DenRevision, 4, D.LandingPadMarker, D.Den),
            (D.SeverTheHead, DenRevision, 5, D.FuelDepotMarker, D.Den),
            (PlateauOneWayTripV1.MissionId, PlateauRevision, 9, Unnamed(1), Plateau),
            (PlateauOneWayTripV1.MissionId, PlateauRevision, 10, Unnamed(3), UstorYard),
            (PlateauOneWayTripV1.MissionId, PlateauRevision, 13, Unnamed(2), Plateau),
            (PlateauWeaponsTraffickingV1.MissionId, PlateauRevision, 1, Unnamed(1), Pools),
            (RefineryMission, PoolsRevision, RefineryObjective, Unnamed(2), Pools),
            (RefineryMission, PoolsRevision, RefineryObjective, RefineryIndicator, PoolsLookingForLogosV1.TurpisRefinery),
            (PlateauBetterHalfV1.MissionId, PlateauRevision, 3, Unnamed(3), Plateau),
            (PlateauBetterHalfV1.MissionId, PlateauRevision, 4, Unnamed(4), Plateau),
            (PlateauAncientMysteriesV1.MissionId, PlateauRevision, 2, Unnamed(1), Plateau),
            (PlateauAncientMysteriesV1.MissionId, PlateauRevision, 3, Unnamed(8), Plateau),
            (PlateauAncientMysteriesV1.MissionId, PlateauRevision, 3, Unnamed(9), Plateau),
            (PoolsTurpisTransportV1.MissionId, PoolsRevision, 1, Unnamed(1), Pools),
            (PlateauDeadlySilenceV1.MissionId, PlateauRevision, 1, Unnamed(1), VelonHollow),
            (PlateauDeadlySilenceV1.MissionId, PlateauRevision, 2, Unnamed(2), VelonHollow),
            (PlateauDeadlySilenceV1.MissionId, PlateauRevision, 3, Unnamed(3), VelonHollow),
            (PlateauDeadlySilenceV1.MissionId, PlateauRevision, 4, Unnamed(4), VelonHollow)
        };

        /// <summary>The missions with no scene: Up inserts one holding only indicatorMaps.</summary>
        public static readonly uint[] NewScenes = { PoolsTurpisTransportV1.MissionId, PlateauDeadlySilenceV1.MissionId };

        /// <summary>The scenes: (mission, revision).</summary>
        public static IEnumerable<(uint MissionId, string Revision)> Scenes() => Markers.Select(m => (m.MissionId, m.Revision)).Distinct();

        /// <summary>The stored scenes that get the property appended.</summary>
        public static IEnumerable<(uint MissionId, string Revision)> AppendedScenes() => Scenes().Where(s => !NewScenes.Contains(s.MissionId));

        /// <summary>The scenes Up inserts.</summary>
        public static IEnumerable<(uint MissionId, string Revision)> InsertedScenes() => Scenes().Where(s => NewScenes.Contains(s.MissionId));

        private static Dictionary<uint, Dictionary<uint, uint>> Maps(uint missionId) =>
            Markers.Where(m => m.MissionId == missionId)
                .GroupBy(m => m.ObjectiveId)
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key, g => g.ToDictionary(m => m.IndicatorId, m => m.MapContextId));

        /// <summary>A mission's indicatorMaps property as it is added: "indicatorMaps":{"objective":{"indicator":map}}.</summary>
        public static string Property(uint missionId) =>
            Key + JsonSerializer.Serialize(Maps(missionId).ToDictionary(o => o.Key.ToString(),
                o => o.Value.ToDictionary(i => i.Key.ToString(), i => i.Value)));

        /// <summary>The data-only scene inserted for a mission with none.</summary>
        public static string NewScene(uint missionId) =>
            JsonSerializer.Serialize(new MissionSceneDefinition { IndicatorMaps = Maps(missionId) }, MissionContentCodec.Options);

        public static IEnumerable<string> Up(bool mySql)
        {
            // The refinery marker first: a scene's indicatorMaps may name only markers the mission has.
            yield return "INSERT INTO mission_indicator (mission_id, content_revision, objective_id, indicator_id, requirement, " +
                "pos_x, pos_y, pos_z, radius, show_3d_effect, comment) " +
                $"SELECT {RefineryMission}, '{PoolsRevision}', {RefineryObjective}, {RefineryIndicator}, {(byte)MissionContentRequirement.Required}, " +
                $"l.pos_x, l.pos_y, l.pos_z, {MissionMarkerRadius}, 0, '{RefineryComment}' FROM logos l " +
                $"WHERE l.id = {PoolsLookingForLogosV1.FarLogos} " +
                $"AND EXISTS (SELECT 1 FROM mission_objective_definition o WHERE o.mission_id = {RefineryMission} " +
                $"AND o.content_revision = '{PoolsRevision}' AND o.objective_id = {RefineryObjective}) " +
                $"AND NOT EXISTS (SELECT 1 FROM mission_indicator i WHERE i.mission_id = {RefineryMission} " +
                $"AND i.content_revision = '{PoolsRevision}' AND i.objective_id = {RefineryObjective} AND i.indicator_id = {RefineryIndicator});";

            foreach (var (missionId, revision) in AppendedScenes())
            {
                var suffix = $",{Property(missionId)}}}";
                var head = "SUBSTR(bindings, 1, LENGTH(bindings) - 1)";
                var joined = mySql ? $"CONCAT({head}, '{suffix}')" : $"{head} || '{suffix}'";
                yield return $"UPDATE mission_scene_binding SET bindings = {joined} " +
                    $"WHERE mission_id = {missionId} AND content_revision = '{revision}' " +
                    $"AND bindings LIKE '%}}' AND bindings NOT LIKE '%{Key}%';";
            }

            foreach (var (missionId, revision) in InsertedScenes())
                yield return "INSERT INTO mission_scene_binding (mission_id, content_revision, script_key, state_version, bindings) " +
                    $"SELECT d.mission_id, d.content_revision, NULL, 1, '{NewScene(missionId)}' FROM mission_content_definition d " +
                    $"WHERE d.mission_id = {missionId} AND d.content_revision = '{revision}' " +
                    $"AND NOT EXISTS (SELECT 1 FROM mission_scene_binding b WHERE b.mission_id = {missionId} AND b.content_revision = '{revision}');";
        }

        public static IEnumerable<string> Down()
        {
            foreach (var (missionId, revision) in InsertedScenes())
                yield return $"DELETE FROM mission_scene_binding WHERE mission_id = {missionId} AND content_revision = '{revision}' " +
                    $"AND script_key IS NULL AND bindings = '{NewScene(missionId)}';";

            foreach (var (missionId, revision) in AppendedScenes())
            {
                var suffix = $",{Property(missionId)}}}";
                yield return $"UPDATE mission_scene_binding SET bindings = REPLACE(bindings, '{suffix}', '}}') " +
                    $"WHERE mission_id = {missionId} AND content_revision = '{revision}' AND bindings LIKE '%{suffix}';";
            }

            yield return $"DELETE FROM mission_indicator WHERE mission_id = {RefineryMission} AND content_revision = '{PoolsRevision}' " +
                $"AND objective_id = {RefineryObjective} AND indicator_id = {RefineryIndicator} AND comment = '{RefineryComment}';";
        }

        /// <summary>The refinery marker's radius, as a shrine marker's (MissionMarkerProjection.ShrineMarkerRadius, in Rasa.Game).</summary>
        public const int MissionMarkerRadius = 8;
    }
}
