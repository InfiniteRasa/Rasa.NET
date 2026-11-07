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
    /// Munit and Vargo moved to places a player walks to and can see, and Yeg's marker kept off a floor hole (to-fix rows 32,
    /// 34 and 35), a delta on migrations 20271108000000 (set 1: Munit) and 20271108000100 (set 2: Vargo, Yeg), which the live
    /// World DB has applied already: their data classes stay as they are.
    ///
    /// Row 32: Munit 770002 stood at the Top Khana Quarter region point (-215, 196.5, 205). The only way up to him is the
    /// Top Khana room on the y 196.7 level, and its floor has a hole near (-206.8..-210, 196.7, 260..262): the real client fell
    /// into an off-mesh pit (-208, 159.7, 262.2). The navmesh path Vitto -> Munit and pad 909 -> Munit cross it.
    /// Row 34: Merchant Vargo 510033 (upstream's place, -192.5, 180.5, 264.2) stood inside the Staal Centrum crafting station
    /// (kraftwerk 22, class 9595, prop_hum_craftstation_v01.geo, at -192.51, 180.5, 264.15, about 2.4 x 4 x 2 m): the client
    /// answered every use with "You cannot see your target from here".
    /// Row 35: a second hole on the y 196.7 level near (-176..-161, 196.7, 357..345) (pit at -163.4, 159.6, 345.3). Yeg's r8
    /// marker (1146 objective 4) reached into the tester's avoid circle round it (-168, 350, r7).
    /// Both holes are in the panopticon room mesh (arch_brann_corridor_room_01_panopticon_v01.geo): our client floor reader
    /// finds a floor there, the real client has none (NOTES-upstream-bugs N10).
    ///
    /// Now (ESTIMATE, probe 2026-11-07: navmesh ground, a client floor, seen from 8 places 2 m round, a complete navmesh path
    /// from the lower pad 909 that does not enter either hole's circle above y 190):
    ///  - Munit on the lower corridor under his quarter (-218, 180.68, 228), 23 m from the region point, 30 m from Roscoe;
    ///    his markers (The Wire 1145 objective 3, Earning Respect 1170 objectives 1 and 2) on him.
    ///  - Vargo 1.5 m east and 1.1 m north of the crafting station's box (-190.5, 180.79, 267.2), 25 m from Roscoe; his
    ///    markers (1146 objectives 3 and 6) on him.
    ///  - Yeg's marker r5 (it was r8), 2.6 m clear of the hole's circle.
    /// </summary>
    public static class CrucibleStaalWalksV1
    {
        public const uint Munit = CrucibleOutpostIntrepidNpcsV1.Munit, Vargo = CrucibleGrupaChiefsNpcsV1.Vargo;
        public const double YegMarkerBefore = 8, YegMarkerNow = 5;

        /// <summary>The pools moved: pool, the place before (sets 1 and 2), the place now.</summary>
        public static readonly (uint Pool, (double X, double Y, double Z) Before, (double X, double Y, double Z) Now)[] Moves =
        {
            (Munit, CrucibleOutpostIntrepidNpcsV1.MunitAt, (-218, 180.68, 228)),
            (Vargo, CrucibleGrupaChiefsNpcsV1.VargoAt, (-190.5, 180.79, 267.2))
        };

        /// <summary>The markers that go with them: mission, objective, indicator, pool.</summary>
        public static readonly (uint Mission, uint Objective, uint Indicator, uint Pool)[] Markers =
        {
            (CrucibleWireV1.MissionId, 3, MissionIndicator.UnnamedFrom + 4, Munit),
            (CrucibleEarningRespectV1.MissionId, 1, MissionIndicator.UnnamedFrom + 1, Munit),
            (CrucibleEarningRespectV1.MissionId, 2, MissionIndicator.UnnamedFrom + 2, Munit),
            (CrucibleProtectionRacketV1.MissionId, 3, MissionIndicator.UnnamedFrom + 3, Vargo),
            (CrucibleProtectionRacketV1.MissionId, 6, MissionIndicator.UnnamedFrom + 6, Vargo)
        };

        /// <summary>Yeg's marker: 1146 objective 4.</summary>
        public static readonly (uint Mission, uint Objective, uint Indicator) YegMarker =
            (CrucibleProtectionRacketV1.MissionId, 4, MissionIndicator.UnnamedFrom + 4);

        /// <summary>
        /// The two holes the real client fell through (to-fix rows 32 and 35), as the tester's avoid circles
        /// (RASABOT_NAV_AVOID): centre, radius; both on the y 196.7 level.
        /// </summary>
        public static readonly ((double X, double Y, double Z) At, double Radius)[] Holes =
        {
            ((-213, 196.7, 254), 8), ((-168, 196.8, 350), 7)
        };

        /// <summary>Where a pool stands after this migration: a moved one's new place, else its set's.</summary>
        public static (double X, double Y, double Z)? Now(uint pool) =>
            Moves.Any(move => move.Pool == pool) ? Moves.Single(move => move.Pool == pool).Now : null;

        public static void Up(MigrationBuilder migration)
        {
            foreach (var (pool, _, now) in Moves)
                migration.Sql(Place(pool, now));
            foreach (var (mission, objective, indicator, pool) in Markers)
                migration.Sql(Marker(mission, objective, indicator, Moves.Single(move => move.Pool == pool).Now));
            migration.Sql(Radius(YegMarker.Mission, YegMarker.Objective, YegMarker.Indicator, YegMarkerNow));

            CrucibleMissionDataV1.Existing(migration, CrucibleWireV1.MissionId).Evidence(4, MissionEvidenceSourceKind.Reconstruction,
                "repository:CrucibleStaalWalksV1",
                "To-fix row 32: the only way up to Munit's room crosses a floor hole; Munit moved to the lower corridor under the " +
                "Top Khana Quarter (-218, 180.68, 228), walked to from the lower office teleporter; his marker on him.", 0.3);
            CrucibleMissionDataV1.Existing(migration, CrucibleEarningRespectV1.MissionId).Evidence(4, MissionEvidenceSourceKind.Reconstruction,
                "repository:CrucibleStaalWalksV1",
                "To-fix row 32: Munit moved to the lower corridor under the Top Khana Quarter (-218, 180.68, 228); objectives 1 and " +
                "2's markers on him.", 0.3);
            CrucibleMissionDataV1.Existing(migration, CrucibleProtectionRacketV1.MissionId).Evidence(4, MissionEvidenceSourceKind.Reconstruction,
                "repository:CrucibleStaalWalksV1",
                "To-fix rows 34/35: Vargo stood inside the Staal Centrum crafting station (kraftwerk 22) and could not be seen; " +
                "moved out of it (-190.5, 180.79, 267.2), his markers on him. Yeg's marker r5, clear of the floor hole near " +
                "(-168, 196.8, 350).", 0.3);
        }

        public static void Down(MigrationBuilder migration)
        {
            foreach (var mission in new[] { CrucibleWireV1.MissionId, CrucibleEarningRespectV1.MissionId, CrucibleProtectionRacketV1.MissionId })
                migration.Sql($"DELETE FROM mission_evidence WHERE mission_id = {mission} AND content_revision = '{CrucibleMissionDataV1.Revision}' AND evidence_id = 4;");
            migration.Sql(Radius(YegMarker.Mission, YegMarker.Objective, YegMarker.Indicator, YegMarkerBefore));
            foreach (var (mission, objective, indicator, pool) in Markers)
                migration.Sql(Marker(mission, objective, indicator, Moves.Single(move => move.Pool == pool).Before));
            foreach (var (pool, before, _) in Moves)
                migration.Sql(Place(pool, before));
        }

        private static string Place(uint pool, (double X, double Y, double Z) at) =>
            $"UPDATE spawnpool SET pos_x = {N(at.X)}, pos_y = {N(at.Y)}, pos_z = {N(at.Z)} WHERE id = {pool};";

        private static string Marker(uint mission, uint objective, uint indicator, (double X, double Y, double Z) at) =>
            $"UPDATE mission_indicator SET pos_x = {N(at.X)}, pos_y = {N(at.Y)}, pos_z = {N(at.Z)} " + Where(mission, objective, indicator);

        private static string Radius(uint mission, uint objective, uint indicator, double radius) =>
            $"UPDATE mission_indicator SET radius = {N(radius)} " + Where(mission, objective, indicator);

        private static string Where(uint mission, uint objective, uint indicator) =>
            $"WHERE mission_id = {mission} AND content_revision = '{CrucibleMissionDataV1.Revision}' AND objective_id = {objective} " +
            $"AND indicator_id = {indicator};";

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    }
}
