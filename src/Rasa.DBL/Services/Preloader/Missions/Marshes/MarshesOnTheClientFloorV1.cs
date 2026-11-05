using System.Collections.Generic;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// To-fix row 24 (2026-11-03, real client): Conscript Kaav at his row 21 place (462, 223.68, -592) stood where the client
    /// has no floor; a player next to him fell through the world. The row 21 check used the server navmesh, whose polygons
    /// (built with a 1.3 m edge error and a 2 m border) reach past the client's collision there: the client files have only
    /// the steep jamb walls of arch_bane_industrial_chunnel_jamb_coupler_tunnel_v01 at x 455 and more (MapGeometry of the
    /// client; the tester's scan: every point at x 455 or more falls). The client-floor guard test
    /// (MarshesClientFloorTests) found two more NPCs that stand where the client has no walkable floor within 0.8 m under
    /// them, both our own row 15 places (MarshesNpcsStandStillV1):
    ///  - Kaav to (443.5, 223.62, -589.5): floor 223.61, 1.8 m from the nearest wall (the row 21 pipe), 11.1 m from the
    ///    depot door's link 41 (radius 6), a clear line of sight from the door and the open sides; the tester stood at
    ///    (445, 223.6, -590). His 1028 marker with him.
    ///  - Elder Qunle (82, 63.17, -204) stood 8.2 m over the terrain (no floor of the village platform there): to
    ///    (74, 56.08, -196) on the ground, 33.8 m outside the hostile pool 540085.
    ///  - Shaman Rakla (67, 59.17, -254) stood under the terrain of the rock pile's slope: to (62, 59.18, -247), floor 59.16,
    ///    25.1 m outside pool 540085, 3 m from the nearest wall. Her 892 marker with her.
    /// All the places are ESTIMATES from the client files (MapGeometry, the same input as the navmesh builder).
    /// </summary>
    public static class MarshesOnTheClientFloorV1
    {
        public static readonly (uint Pool, (double X, double Y, double Z) Before, (double X, double Y, double Z) Now)[] Moves =
        {
            (MarshesRegulatorNpcsV1.Kaav, MarshesNpcsInTheOpenV1.KaavAt, (443.5, 223.62, -589.5)),
            (MarshesPreoDasNpcsV1.Qunle, MarshesNpcsStandStillV1.At(MarshesPreoDasNpcsV1.Qunle), (74, 56.08, -196)),
            (MarshesPreoDasVillageNpcsV1.Rakla, MarshesNpcsStandStillV1.At(MarshesPreoDasVillageNpcsV1.Rakla), (62, 59.18, -247))
        };

        public static (double X, double Y, double Z) KaavAt => At(MarshesRegulatorNpcsV1.Kaav);

        /// <summary>The markers on a moved NPC: Kaav's (1028) and Rakla's (892, MarshesSafeGroundV1.Markers).</summary>
        public static readonly (uint Mission, uint Objective, uint Indicator, uint Pool)[] Markers =
        {
            (MarshesRegulatorPartTwoV1.MissionId, MarshesRegulatorPartTwoV1.Tracker, 1000000001, MarshesRegulatorNpcsV1.Kaav),
            (MarshesPickingUpThePiecesV1.MissionId, 1, 1000000003, MarshesPreoDasVillageNpcsV1.Rakla)
        };

        public static (double X, double Y, double Z) At(uint pool) => System.Array.Find(Moves, move => move.Pool == pool).Now;

        private static (double X, double Y, double Z) Before(uint pool) => System.Array.Find(Moves, move => move.Pool == pool).Before;

        public static IEnumerable<string> Up()
        {
            foreach (var (pool, _, now) in Moves)
                yield return Place(pool, now);
            foreach (var marker in Markers)
                yield return Marker(marker, At(marker.Pool));
        }

        public static IEnumerable<string> Down()
        {
            foreach (var marker in Markers)
                yield return Marker(marker, Before(marker.Pool));
            foreach (var (pool, before, _) in Moves)
                yield return Place(pool, before);
        }

        private static string Place(uint pool, (double X, double Y, double Z) at) => System.FormattableString.Invariant(
            $"UPDATE spawnpool SET pos_x = {at.X}, pos_y = {at.Y}, pos_z = {at.Z} WHERE id = {pool};");

        private static string Marker((uint Mission, uint Objective, uint Indicator, uint Pool) marker, (double X, double Y, double Z) at) =>
            System.FormattableString.Invariant($"UPDATE mission_indicator SET pos_x = {at.X}, pos_y = {at.Y}, pos_z = {at.Z} WHERE mission_id = {marker.Mission} AND content_revision = '{MarshesMissionDataV1.Revision}' AND objective_id = {marker.Objective} AND indicator_id = {marker.Indicator};");
    }
}
