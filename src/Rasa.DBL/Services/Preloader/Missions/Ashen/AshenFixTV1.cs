using System.Collections.Generic;
using System.Globalization;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// Ashen fix job T (to-fix row 60, real client 2026-11-14), a delta on 20271110002160 (fix S2), Bane Conscription Facility
    /// (1988): 1498's drop pools put a part of their creatures on the ramps round the biolab. The server spawns the full count (7
    /// for 531064, 5 for 531066), but Detour's FindRandomPointAroundCircle takes whole polygons that touch the circle
    /// (AshenSpawnReach): 531064's r 30 drew up to 40.8 m from its point, 9 % of its draws on the ramp at y 98-104; 531066's r 21.3
    /// 11 % on the west ramp at y 102-104 (the harness probe, 2026-11-14). The tester's scans found at most one creature per pool.
    ///  - Upstream pool 531064 (bio) radius 30 -> 12, upstream pool 531066 (parts) radius 21.3 -> 14: every seeded draw on the lab
    ///    floor (AshenFixTTests). Upstream rows, changed in place as fix L changed 531066's height (both pools are named in 1498's
    ///    scene as public-spawn actors, so they are mission placements); each guarded on its exact value; Down restores it.
    ///  - 1498's markers 1 and 2 round the new circles with set 19's PoolMarkerPadding (5 m): 35 -> 17 and 26.3 -> 19.
    /// Places, counts and respawn kept. ESTIMATE: retail's radii are not known.
    /// </summary>
    public static class AshenFixTV1
    {
        public const uint Blast = AshenBlastTheBiolabV1.MissionId;
        public const uint Facility = AshenConscriptionEntryNpcsV1.Facility;
        public const uint BioPool = AshenBlastTheBiolabV1.BioPool, PartsPool = AshenBlastTheBiolabV1.PartsPool;
        public const uint BioMarker = Rasa.Structures.MissionIndicator.UnnamedFrom + 1, PartsMarker = Rasa.Structures.MissionIndicator.UnnamedFrom + 2;

        public const double BioRadiusBefore = AshenBlastTheBiolabV1.BioPoolRadius, BioRadius = 12;
        public const double PartsRadiusBefore = AshenBlastTheBiolabV1.PartsPoolRadius, PartsRadius = 14;
        public const double Padding = AshenBlastTheBiolabV1.PoolMarkerPadding;

        public const uint Evidence = 7;
        public const string Source = "repository:AshenFixTV1";
        public const string Note =
            "Fix job T (to-fix row 60, real client 2026-11-14): 1498's drop pools drew creatures onto the ramps round the biolab " +
            "(whole-polygon draws); upstream pools 531064 r 30 -> 12 and 531066 r 21.3 -> 14, every draw on the lab floor; markers 1 " +
            "and 2 round them with 5 m (35 -> 17, 26.3 -> 19). ESTIMATE.";

        /// <summary>The changes: pool, its point (x, z), its marker, objective, radius before, radius now.</summary>
        public static readonly (uint Pool, (double X, double Z) At, uint Marker, uint Objective, double Before, double Now)[] Pools =
        {
            (BioPool, (AshenBlastTheBiolabV1.BioPoolAt.X, AshenBlastTheBiolabV1.BioPoolAt.Z), BioMarker, AshenBlastTheBiolabV1.BioMaterials, BioRadiusBefore, BioRadius),
            (PartsPool, (AshenBlastTheBiolabV1.PartsPoolAt.X, AshenBlastTheBiolabV1.PartsPoolAt.Z), PartsMarker, AshenBlastTheBiolabV1.MechanicalParts, PartsRadiusBefore, PartsRadius)
        };

        public static IEnumerable<string> UpSql()
        {
            foreach (var (pool, at, marker, objective, before, now) in Pools)
            {
                yield return PoolSql(pool, at, now, before);
                yield return MarkerSql(objective, marker, at, now + Padding, before + Padding);
            }
        }

        public static IEnumerable<string> DownSql()
        {
            for (var k = Pools.Length - 1; k >= 0; k--)
            {
                var (pool, at, marker, objective, before, now) = Pools[k];
                yield return MarkerSql(objective, marker, at, before + Padding, now + Padding);
                yield return PoolSql(pool, at, before, now);
            }
        }

        public static void Up(MigrationBuilder migration)
        {
            foreach (var sql in UpSql())
                migration.Sql(sql);
            AshenMissionDataV1.Existing(migration, Blast).Evidence(Evidence, MissionEvidenceSourceKind.Reconstruction, Source, Note, 0.3);
        }

        public static void Down(MigrationBuilder migration)
        {
            migration.Sql($"DELETE FROM mission_evidence WHERE mission_id = {Blast} AND content_revision = '{AshenMissionDataV1.Revision}' " +
                          $"AND evidence_id = {Evidence} AND source_uri = '{Source}';");
            foreach (var sql in DownSql())
                migration.Sql(sql);
        }

        private static string F(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string PoolSql(uint pool, (double X, double Z) at, double to, double from) =>
            $"UPDATE spawnpool SET radius = {F(to)} WHERE id = {pool} AND map_context_id = {Facility} " +
            $"AND ABS(pos_x - {F(at.X)}) < 0.001 AND ABS(pos_z - {F(at.Z)}) < 0.001 AND ABS(radius - {F(from)}) < 0.001;";

        private static string MarkerSql(uint objective, uint indicator, (double X, double Z) at, double to, double from) =>
            $"UPDATE mission_indicator SET radius = {F(to)} WHERE mission_id = {Blast} AND content_revision = '{AshenMissionDataV1.Revision}' " +
            $"AND objective_id = {objective} AND indicator_id = {indicator} AND ABS(pos_x - {F(at.X)}) < 0.001 AND ABS(pos_z - {F(at.Z)}) < 0.001 " +
            $"AND ABS(radius - {F(from)}) < 0.001;";
    }
}
