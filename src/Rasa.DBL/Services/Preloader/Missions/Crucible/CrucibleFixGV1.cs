using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// Ligo Crucible fix job G (to-fix row 61, upstream C11), a delta on migration 20271108000600 (set 5), which the live World
    /// DB has applied: its data class stays as it is.
    ///
    /// Shield Drone pool 770026 (Out Of Sight, 1208) at (535, 172.92, 80), radius 6, set its drones down at
    /// NavMeshQuery.RandomPointAround (Detour's findRandomPointAroundCircle: a random point anywhere in a navmesh polygon the
    /// walk from the centre reaches through portal edges within the radius). One reached polygon has lava at (542.0, 170.68,
    /// 126.4), 46 m off the centre (LavaDamage.InLava; probe 2026-11-07): one draw in a few hundred put a drone there, so the
    /// lava guard failed now and then. Now the pool is a point (radius 0): SpawnPoolManager sets the drones down on the rings
    /// round the point (Spacing.OnRings, within 6.6 m), with no lava there (the lava guard). No move: its marker stays on it.
    ///
    /// The lava guard (CrucibleGuardTests) no longer draws: it checks every navmesh point a radius pool can reach
    /// (CrucibleGuardTests.SpawnReach, the same walk as Detour's), so it finds such a polygon every run.
    /// </summary>
    public static class CrucibleFixGV1
    {
        /// <summary>The pool made a point and the radius it had.</summary>
        public static readonly (uint Pool, uint Before) Point = (CrucibleStaalSigalsNpcsV1.DronePools[2].Pool, CrucibleStaalSigalsNpcsV1.PoolRadius);

        /// <summary>1208's evidence row (its next free evidence id; fix job F used 4).</summary>
        public const uint Evidence = 5;

        public const string Source = "repository:CrucibleFixGV1";

        public const string Note =
            "Fix job G (to-fix row 61): Shield Drone pool 770026 at (535, 172.92, 80), radius 6, could set a drone down on lava at " +
            "(542.0, 170.68, 126.4), 46 m off its point (a navmesh random point in a polygon its circle reaches); a point now " +
            "(radius 0, the drones on rings within 6.6 m, no lava there). Not moved; its marker stays.";

        public static void Up(MigrationBuilder migration)
        {
            migration.Sql(Radius(Point.Pool, CrucibleFixFV1.PointRadius));
            CrucibleMissionDataV1.Existing(migration, CrucibleOutOfSightV1.MissionId)
                .Evidence(Evidence, MissionEvidenceSourceKind.Reconstruction, Source, Note, 0.3);
        }

        public static void Down(MigrationBuilder migration)
        {
            migration.Sql($"DELETE FROM mission_evidence WHERE mission_id = {CrucibleOutOfSightV1.MissionId} " +
                          $"AND content_revision = '{CrucibleMissionDataV1.Revision}' AND evidence_id = {Evidence};");
            migration.Sql(Radius(Point.Pool, Point.Before));
        }

        private static string Radius(uint pool, uint radius) => $"UPDATE spawnpool SET radius = {radius} WHERE id = {pool};";
    }
}
