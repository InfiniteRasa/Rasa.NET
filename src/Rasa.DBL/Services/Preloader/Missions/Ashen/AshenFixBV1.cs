using System.Collections.Generic;
using System.Globalization;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Structures;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// Ligo Ashen Desert fix job B (real client 2026-11-09, to-fix row 5), a delta on migration 20271110000100 (set 2,
    /// AshenMaunaBarrensNpcsV1, AshenChemicalOversightV1), whose data classes stay as they are.
    ///
    /// The play: Chemical Oversight (1244) was accepted, but no walk reached Thrax Overseer Exing at set 2's spot (-289, 332.98,
    /// -264) on the mesa's slope. Every navmesh route the tester followed stopped on a slope the client does not climb: west from
    /// the Mauna Barrens basin at (-323.2, 319.2, -243) (the leg end 6.6 m on and 9 m higher), east from the Sheitan Highway camp
    /// at (-226.1, 310.5, -285.1) (11.8 m on, 6 m higher), and from the Stonewall Ridge waypoint (758 m) on the same east slope at
    /// (-237.6, 309.4, -302.3).
    ///
    /// Why (probe 2026-11-09, the real navmesh and the client's floor): the last legs to set 2's spot climb the mesa's flanks,
    /// where the client floor rises more than 0.9 m a metre (from the basin at (-320, -248): 1.9 m; from the camp at (-214.9,
    /// -242.9): 4.6 m); the navmesh joins them (upstream N19). Of the navmesh spots on a 5 m grid within 45 m of (-275, -270)
    /// none on the mesa is reached on the client floor from both the basin and the camp; the 18 that are lie on the flat south
    /// of it (y 308.7-310.1, z -300 to -315).
    ///
    /// Now: Exing's pool (radius 0, as before) at (-260, 308.9, -310), on the flat at the mesa's south foot, 54 m from set 2's
    /// spot, still west of the Sheitan Highway and south of the Mauna Barrens (D10). The client floor is walked to it at 0.9 m
    /// steps at most with nothing of the placed meshes in the way at chest height from the Shadow's Edge Post waypoint (536 m),
    /// the Mauna Barrens basin (Atta pool 790051, 159 m) and both Sheitan Highway camp pools 790087 (115 m) and 790085 (84 m),
    /// the places the real client reached for 1242 and 1247; within 17 m of it every navmesh spot is one floor, joined to the
    /// waypoints, on a client floor under open sky. The marker (r15) moves onto it.
    ///
    /// Reconstructed (ESTIMATE): the place. Respawn, creature and level as upstream.
    /// </summary>
    public static class AshenFixBV1
    {
        public const uint MissionId = AshenChemicalOversightV1.MissionId;
        public const uint Pool = AshenMaunaBarrensNpcsV1.Exing;
        public const uint Exing = AshenMaunaBarrensNpcsV1.Exing;

        /// <summary>Set 2's spot (the row as set 2 wrote it).</summary>
        public static readonly (double X, double Y, double Z) PoolBefore = AshenMaunaBarrensNpcsV1.ExingAt;

        /// <summary>The spot now: the client floor (308.90), 0.36 m under the navmesh there (309.26).</summary>
        public static readonly (double X, double Y, double Z) PoolNow = (-260, 308.9, -310);

        /// <summary>1244's one marker (r15): set 2's place, and now on the pool's point, at the navmesh ground.</summary>
        public static readonly (uint Objective, uint Indicator) Marker = (AshenChemicalOversightV1.Kill, MissionIndicator.UnnamedFrom + 1);
        public static readonly (double X, double Y, double Z) MarkerBefore = AshenMaunaBarrensNpcsV1.ExingAt;
        public static readonly (double X, double Y, double Z) MarkerNow = (-260, 309.26, -310);

        /// <summary>How far round the point the ground is open and joined up (as fix A).</summary>
        public const double ClearRadius = AshenFixAV1.ClearRadius;

        /// <summary>1244's evidence row (set 2 wrote 1-3).</summary>
        public const uint Evidence = 4;

        public const string Source = "repository:AshenFixBV1";

        public const string Note =
            "Fix job B (to-fix row 5): no real-client walk reached Exing at set 2's spot (-289, 332.98, -264): every navmesh route " +
            "ends on a mesa flank the client floor does not climb (more than 0.9 m a metre; upstream N19). Now his pool at (-260, " +
            "308.9, -310) on the flat at the mesa's south foot, 54 m on, west of the Sheitan Highway and south of the Mauna Barrens; " +
            "walked on the client floor from Shadow's Edge Post, the Barrens basin and the highway camp; the marker on it. ESTIMATE: the place.";

        public static IEnumerable<string> UpSql()
        {
            yield return PoolSql(PoolNow, PoolBefore);
            yield return MarkerSql(MarkerNow, MarkerBefore);
        }

        public static IEnumerable<string> DownSql()
        {
            yield return MarkerSql(MarkerBefore, MarkerNow);
            yield return PoolSql(PoolBefore, PoolNow);
        }

        public static void Up(MigrationBuilder migration)
        {
            foreach (var sql in UpSql())
                migration.Sql(sql);
            AshenMissionDataV1.Existing(migration, MissionId)
                .Evidence(Evidence, MissionEvidenceSourceKind.Reconstruction, Source, Note, 0.3);
        }

        public static void Down(MigrationBuilder migration)
        {
            migration.Sql($"DELETE FROM mission_evidence WHERE mission_id = {MissionId} " +
                          $"AND content_revision = '{AshenMissionDataV1.Revision}' AND evidence_id = {Evidence};");
            foreach (var sql in DownSql())
                migration.Sql(sql);
        }

        /// <summary>The pool from one place to the other, only where it is the one (a second run changes nothing).</summary>
        private static string PoolSql((double X, double Y, double Z) to, (double X, double Y, double Z) from) =>
            $"UPDATE spawnpool SET pos_x = {N(to.X)}, pos_y = {N(to.Y)}, pos_z = {N(to.Z)} " +
            $"WHERE id = {Pool} AND creature_1_Id = {Exing} AND {Near("pos_x", from.X)} AND {Near("pos_y", from.Y)} AND {Near("pos_z", from.Z)};";

        /// <summary>1244's marker from one place to the other, only where it is the one.</summary>
        private static string MarkerSql((double X, double Y, double Z) to, (double X, double Y, double Z) from) =>
            $"UPDATE mission_indicator SET pos_x = {N(to.X)}, pos_y = {N(to.Y)}, pos_z = {N(to.Z)} " +
            $"WHERE mission_id = {MissionId} AND content_revision = '{AshenMissionDataV1.Revision}' AND objective_id = {Marker.Objective} " +
            $"AND indicator_id = {Marker.Indicator} AND {Near("pos_x", from.X)} AND {Near("pos_y", from.Y)} AND {Near("pos_z", from.Z)};";

        /// <summary>A column at a value, as a single float holds it: within 0.01.</summary>
        private static string Near(string column, double value) => $"ABS({column} - ({N(value)})) < 0.01";

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    }
}
