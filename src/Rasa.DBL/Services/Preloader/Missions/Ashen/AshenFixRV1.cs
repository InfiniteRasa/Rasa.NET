using System.Collections.Generic;
using System.Globalization;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// Ashen fix job R (real client 2026-11-13), a delta on 20271110002120 (fix P, live): 1537's objective 6 ("Sweep the Room")
    /// could not be reached. Set 18 put the Bane's pool 790658 on the Queen's Chamber's west ledge (147, 194.11, -590); the only
    /// way up from the chamber's floor is a terrain slope at (243..238, -667..-668) of 48-50 degrees over 2 m (single triangles
    /// of 56-58), where the real client stopped at (240.1, 169.6, -664.1) and (237.8, 169.9, -663.3). The server navmesh does not
    /// join it either: the ledge is an island of 10457 m2, the chamber's floor and the bottom are one region of 31662 m2.
    /// The pool moves onto the chamber's floor on the way the tester walked today, 66 m from the bottom: (225, 158.75, -502),
    /// navmesh ground, a 7 m disc of 0-38 degree floor, 7.2 m from a wall. Its radius, creatures and respawn are kept; 1537's
    /// marker 6 moves with it (its radius kept); objective 6 counts the pool by id. A rule break (docs/ashen-playtest.md): the
    /// new place is 60 m+ outside every hostile pool's circle, but under 100 m from upstream warcry pools 531043's (the chamber's
    /// Atta, 70.9 m) and 531044's (an island 29 m above, 89.8 m); no place on the chamber floor keeps 100 m from 531043 but the
    /// bottom itself (Vandeweghe's place).
    /// Each UPDATE guarded on set 18's exact values; Down puts them back.
    /// </summary>
    public static class AshenFixRV1
    {
        public const uint Queen = AshenGodSaveTheQueenV1.MissionId;
        public const uint Outpost = AshenAvernusHiveNpcsV1.Outpost;
        public const uint BanePool = AshenAvernusHiveNpcsV1.BanePool;
        public const uint SweepObjective = AshenGodSaveTheQueenV1.SweepTheRoom, SweepMarker = Rasa.Structures.MissionIndicator.UnnamedFrom + 6;

        /// <summary>The Bane's pool and 1537's marker 6: set 18's place on the west ledge, fix R's on the chamber's floor.</summary>
        public static readonly (double X, double Y, double Z) BaneBefore = AshenAvernusHiveNpcsV1.BaneAt, BaneAt = (225, 158.75, -502);

        /// <summary>Marker 6's radius (set 18's: the pool's 5 m and 4 m), kept.</summary>
        public const double SweepMarkerRadius = AshenAvernusHiveNpcsV1.BaneRadius + AshenGodSaveTheQueenV1.PoolMarkerPadding;

        public const uint Evidence = 5;
        public const string Source = "repository:AshenFixRV1";
        public const string Note =
            "Fix job R (real client 2026-11-13): the Bane's pool 790658 moved from the Queen's Chamber's west ledge (147, 194.11, -590), " +
            "a navmesh island the real client could not climb to (a 48-50 degree terrain slope at (240, -668)), onto the chamber's floor " +
            "(225, 158.75, -502) on the way from the bottom; 1537's marker 6 with it. No source places the Bane. ESTIMATE.";

        public static IEnumerable<string> UpSql()
        {
            yield return PoolSql(BaneAt, BaneBefore);
            yield return MarkerSql(BaneAt, BaneBefore);
        }

        public static IEnumerable<string> DownSql()
        {
            yield return MarkerSql(BaneBefore, BaneAt);
            yield return PoolSql(BaneBefore, BaneAt);
        }

        public static void Up(MigrationBuilder migration)
        {
            foreach (var sql in UpSql())
                migration.Sql(sql);
            AshenMissionDataV1.Existing(migration, Queen).Evidence(Evidence, MissionEvidenceSourceKind.Reconstruction, Source, Note, 0.3);
        }

        public static void Down(MigrationBuilder migration)
        {
            migration.Sql($"DELETE FROM mission_evidence WHERE mission_id = {Queen} AND content_revision = '{AshenMissionDataV1.Revision}' " +
                          $"AND evidence_id = {Evidence} AND source_uri = '{Source}';");
            foreach (var sql in DownSql())
                migration.Sql(sql);
        }

        private static string F(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string Near((double X, double Y, double Z) at) =>
            $"ABS(pos_x - {F(at.X)}) < 0.001 AND ABS(pos_y - {F(at.Y)}) < 0.001 AND ABS(pos_z - {F(at.Z)}) < 0.001";

        private static string Set((double X, double Y, double Z) to) => $"pos_x = {F(to.X)}, pos_y = {F(to.Y)}, pos_z = {F(to.Z)}";

        private static string PoolSql((double X, double Y, double Z) to, (double X, double Y, double Z) from) =>
            $"UPDATE spawnpool SET {Set(to)} WHERE id = {BanePool} AND map_context_id = {Outpost} AND ABS(radius - {AshenAvernusHiveNpcsV1.BaneRadius}) < 0.001 " +
            $"AND creature_1_Id = {AshenAvernusHiveNpcsV1.Caretaker} AND {Near(from)};";

        private static string MarkerSql((double X, double Y, double Z) to, (double X, double Y, double Z) from) =>
            $"UPDATE mission_indicator SET {Set(to)} WHERE mission_id = {Queen} AND content_revision = '{AshenMissionDataV1.Revision}' " +
            $"AND objective_id = {SweepObjective} AND indicator_id = {SweepMarker} AND ABS(radius - {F(SweepMarkerRadius)}) < 0.001 AND {Near(from)};";
    }
}
