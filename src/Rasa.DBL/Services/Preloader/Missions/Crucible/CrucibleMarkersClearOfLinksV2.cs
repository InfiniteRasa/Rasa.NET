using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Structures;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// The Crucible markers and Flare Gasher pools moved clear of the map links (to-fix rows 23, 24 and 25), a delta on
    /// migrations 20271108000100 (Junkyard Dogs 1149) and 20271108000200 (Game Wardens 1147), which the live World DB has
    /// applied already: their data classes stay as they are.
    ///
    /// Row 23: upstream's Flaregasher pool 540139 (RegionSpawnpoolPreloader) stands on the Staal Junkyard door, map link 96
    /// (-492.13, 236.95, -149.06) r6 (MapLinkPreloader), and 1149's marker 1 (r25) holds that door. A player or bot that hunts
    /// in the marker walks into the link and the server moves it into instance 2138. The navmesh probe (2026-11-07): the
    /// bridge to the junkyard is a corridor about 8 m wide along x -492 from z -35 to z -227, with the link in it; the open
    /// ground before the bridge is the plateau north of it (y 248-250, z 30-65).
    /// Row 24: pools 540140 (-533.2, 236.9, -251.1) and 540141 (-432.8, 236.9, -56.4) and their markers are off the navmesh
    /// (540140 also behind the door, 540141 over a floor 29 m lower). The bot's "objective 1149/1 has no marker" came after
    /// row 23's move: on map 2138 the server sends no Crucible marker (MissionMarkerProjection.Apply shows a marker on its own
    /// map only).
    /// Row 25 (the new guard, CrucibleGuardTests): 1147's marker (-533.6, 249.7, 563.3) r55 holds the Chaukas Robotics
    /// Facility door, map link 98 (-528.6, 249.44, 581.6) r6. Upstream's Flaregasher pool 540144 stands on the Thunderhead
    /// border, map link 94 (-810.88, 292.13, 727.73) r8, where a player from the Thunderhead arrives.
    ///
    /// Now (ESTIMATE, probe 2026-11-07: navmesh ground, a client floor, a complete path from the Labbna waypoint):
    ///  - 540139, 540140, 540141 on the plateau before the bridge, 66-73 m apart, 190 m or more from link 96; 1149's three
    ///    markers (r25) on them.
    ///  - 540144 54 m south-west of link 94, 50 m from pool 540145.
    ///  - 1147: a marker (r20) on each Warden Bot pool (770009-770011) for each of the three objectives, 9 m or more clear of
    ///    link 98's circle.
    /// </summary>
    public static class CrucibleMarkersClearOfLinksV2
    {
        public const double GasherMarkerRadius = 25, WardenMarkerRadius = 20;

        /// <summary>The Flaregasher pools moved: pool, the place before (World DB, RegionSpawnpoolPreloader), the place now.</summary>
        public static readonly (uint Pool, (double X, double Y, double Z) Before, (double X, double Y, double Z) Now)[] Pools =
        {
            (540139, (-492.1000061035156, 236.89999389648438, -149.10000610351562), (-452, 249.56, 58)),
            (540140, (-533.2000122070312, 236.89999389648438, -251.10000610351562), (-518, 250.21, 64)),
            (540141, (-432.79998779296875, 236.89999389648438, -56.400001525878906), (-380, 248.05, 46)),
            (540144, (-810.9000244140625, 292.1000061035156, 727.7000122070312), (-841, 285.48, 674))
        };

        /// <summary>1149's markers (objective 1, indicators UnnamedFrom + 1..3): one on each pool by the junkyard bridge.</summary>
        public static IEnumerable<(uint Indicator, (double X, double Y, double Z) At)> GasherMarkers =>
            Pools.Take(3).Select((pool, index) => (MissionIndicator.UnnamedFrom + (uint)index + 1, pool.Now));

        /// <summary>
        /// 1147's markers: objective, indicator id, place. Objective o keeps its indicator UnnamedFrom + o (moved onto the first
        /// pool) and gets UnnamedFrom + 2o + 2 and + 2o + 3 on the other two.
        /// </summary>
        public static IEnumerable<(uint Objective, uint Indicator, (double X, double Y, double Z) At)> WardenMarkers =>
            new uint[] { 1, 2, 3 }.SelectMany(objective => CrucibleWardensAndLashersNpcsV1.WardenPools.Select((pool, index) =>
                (objective, MissionIndicator.UnnamedFrom + (index == 0 ? objective : 2 * objective + (uint)index + 1), pool.At)));

        public static void Up(MigrationBuilder migration)
        {
            foreach (var (pool, _, now) in Pools)
                migration.Sql(Place(pool, now));
            foreach (var (indicator, at) in GasherMarkers)
                migration.Sql(Marker(CrucibleJunkyardDogsV1.MissionId, CrucibleJunkyardDogsV1.Kill, indicator, at, GasherMarkerRadius));
            var wardens = CrucibleMissionDataV1.Existing(migration, CrucibleGameWardensV1.MissionId);
            foreach (var (objective, indicator, at) in WardenMarkers)
            {
                if (indicator == MissionIndicator.UnnamedFrom + objective)
                    migration.Sql(Marker(CrucibleGameWardensV1.MissionId, objective, indicator, at, WardenMarkerRadius));
                else
                    wardens.Indicator(objective, indicator - MissionIndicator.UnnamedFrom, at.X, at.Y, at.Z, WardenMarkerRadius);
            }

            CrucibleMissionDataV1.Existing(migration, CrucibleJunkyardDogsV1.MissionId).Evidence(4, MissionEvidenceSourceKind.Reconstruction,
                "repository:CrucibleMarkersClearOfLinksV2",
                "To-fix rows 23/24: Flaregasher pools 540139-540141 moved off the Staal Junkyard door (map link 96) and off the " +
                "navmesh gaps onto the plateau before the bridge (-452 58, -518 64, -380 46); the three markers r25 on them.", 0.3);
            wardens.Evidence(4, MissionEvidenceSourceKind.Reconstruction, "repository:CrucibleMarkersClearOfLinksV2",
                "To-fix row 25: the r55 marker held the Chaukas Robotics Facility door (map link 98); now a r20 marker on each " +
                "Warden Bot pool 770009-770011 for each objective.", 0.3);
        }

        public static void Down(MigrationBuilder migration)
        {
            foreach (var mission in new[] { CrucibleJunkyardDogsV1.MissionId, CrucibleGameWardensV1.MissionId })
                migration.Sql($"DELETE FROM mission_evidence WHERE mission_id = {mission} AND content_revision = '{CrucibleMissionDataV1.Revision}' AND evidence_id = 4;");
            foreach (var (objective, indicator, _) in WardenMarkers.Where(marker => marker.Indicator != MissionIndicator.UnnamedFrom + marker.Objective))
                migration.Sql($"DELETE FROM mission_indicator WHERE mission_id = {CrucibleGameWardensV1.MissionId} AND content_revision = " +
                              $"'{CrucibleMissionDataV1.Revision}' AND objective_id = {objective} AND indicator_id = {indicator};");
            foreach (var objective in new uint[] { 1, 2, 3 })
                migration.Sql(Marker(CrucibleGameWardensV1.MissionId, objective, MissionIndicator.UnnamedFrom + objective,
                    CrucibleGameWardensV1.BotsAt, CrucibleGameWardensV1.BotsRadius));
            var index = 0;
            foreach (var (_, at) in CrucibleJunkyardDogsV1.Pools)
                migration.Sql(Marker(CrucibleJunkyardDogsV1.MissionId, CrucibleJunkyardDogsV1.Kill, MissionIndicator.UnnamedFrom + (uint)++index,
                    at, CrucibleJunkyardDogsV1.MarkerRadius));
            foreach (var (pool, before, _) in Pools)
                migration.Sql(Place(pool, before));
        }

        private static string Place(uint pool, (double X, double Y, double Z) at) =>
            $"UPDATE spawnpool SET pos_x = {N(at.X)}, pos_y = {N(at.Y)}, pos_z = {N(at.Z)} WHERE id = {pool};";

        private static string Marker(uint mission, uint objective, uint indicator, (double X, double Y, double Z) at, double radius) =>
            $"UPDATE mission_indicator SET pos_x = {N(at.X)}, pos_y = {N(at.Y)}, pos_z = {N(at.Z)}, radius = {N(radius)} " +
            $"WHERE mission_id = {mission} AND content_revision = '{CrucibleMissionDataV1.Revision}' AND objective_id = {objective} " +
            $"AND indicator_id = {indicator};";

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    }
}
