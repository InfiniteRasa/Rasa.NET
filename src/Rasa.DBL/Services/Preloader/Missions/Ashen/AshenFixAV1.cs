using System.Collections.Generic;
using System.Globalization;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Structures;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// Ligo Ashen Desert fix job A (real client 2026-11-09 14:47-15:38, to-fix row 1), a delta on migration 20271110000000 (set
    /// 1, AshenFithikInfestationV1), whose data class stays as it is.
    ///
    /// The play: at Fithik Infestation's (1274) marked pool 540067 the first four Fithik Guardians stood on the ground and died;
    /// the ones that came back stood about 6 m under the player, who stood on the ground over them at y 325.8, and could not be
    /// shot ("no line of sight") for 50 minutes.
    ///
    /// Why (probe 2026-11-09, the real navmesh and the client's collision): not the pool row's height. SpawnPoint sets each
    /// Fithik of a pool with a radius down at NavMeshQuery.RandomPointAround (Detour's findRandomPointAroundCircle: a random
    /// point in a polygon linked to the centre's, which can lie up to 10 m past the radius). Round 540067's centre the mesh
    /// has two floors: the ground at y 318.7-321 and a rock shelf over it at 324-326.6 (client floors at the centre: 326.4 and
    /// 320.6). The ground under the shelf (about x 232-240, z -660 to -636, the centre in it) is walkable and joined to the
    /// rest, so 45 of 300 spawn draws (15 %) and 18 of 130 Fithik over 40 kill-and-respawn rounds came down under the shelf,
    /// a client surface 3.5-7 m over them. The first spawn was lucky. The ones that come down there are not killed, so the
    /// pool, refilled only for what dies (TopUp), fills up with them; the strolls (inside the pool's 15 m) lead there too.
    ///
    /// Now: 540067 is a point pool (radius 0) at (261, 308.3, -642.2), on the open east slope of the Ridge 29 m from its
    /// upstream point: SpawnPoint sets the two to four Fithik of a full spawn on the point and the rings round it
    /// (Spacing.OnRings, within <see cref="PointSpread"/>) and a single one topped up on the point itself, their strolls stay within 10 m of where each came down (BehaviorManager.WanderZoneFallbackRadius),
    /// and every navmesh point within <see cref="ClearRadius"/> of the point is joined to the waypoints, has a client floor at
    /// its height and nothing over it (probe: 20 m clear; the nearest shelf or pocket further). Respawn time, count and creature
    /// as upstream. The marker (r30) moves onto the point.
    ///
    /// Reconstructed (ESTIMATE): the place. Upstream had the pool on the Ridge's top; the slope east of it is still the Ridge.
    /// </summary>
    public static class AshenFixAV1
    {
        public const uint MissionId = AshenFithikInfestationV1.MissionId;
        public const uint Pool = 540067;
        public const uint Fithik = AshenFithikInfestationV1.Fithik;

        /// <summary>
        /// The pool row upstream (World DB): its point (4 m under the ground) and radius. The point as the row holds it, the single
        /// floats 233.2f, 316.4f, -651.2f widened (as AshenMaunaBarrensNpcsV1.ExingBefore), so Down puts back the very values.
        /// </summary>
        public static readonly (double X, double Y, double Z) PoolBefore = (233.1999969482422, 316.3999938964844, -651.2000122070312);
        public const double RadiusBefore = 15;

        /// <summary>The pool now: a point on the client floor (308.26) of the slope, 0.36 m under the navmesh there (308.62).</summary>
        public static readonly (double X, double Y, double Z) PoolNow = (261, 308.3, -642.2);
        public const double RadiusNow = 0;

        /// <summary>1274's one marker (r30): set 1's place, and now on the pool's point, at the navmesh ground.</summary>
        public static readonly (uint Objective, uint Indicator) Marker = (AshenFithikInfestationV1.Kill, MissionIndicator.UnnamedFrom + 1);
        public static readonly (double X, double Y, double Z) MarkerBefore = AshenFithikInfestationV1.Pools[0].At;
        public static readonly (double X, double Y, double Z) MarkerNow = (261, 308.6, -642.2);

        /// <summary>How far from its point a point pool sets its creatures down: two rings of 1.1 spacings (Spacing.OnRings).</summary>
        public const double PointSpread = 6.6;

        /// <summary>How far round the point the ground is open and joined up: the rings and a stroll (10 m) from the outer one.</summary>
        public const double ClearRadius = 17;

        /// <summary>1274's evidence row (set 1 wrote 1-3).</summary>
        public const uint Evidence = 4;

        public const string Source = "repository:AshenFixAV1";

        public const string Note =
            "Fix job A (to-fix row 1): 540067's respawned Fithik stood under a rock shelf, 6 m under the player on it, and could " +
            "not be shot. Probe: its radius 15 (Detour random points, up to 10 m past it) took in the ground under the shelf " +
            "(client surface 3.5-7 m over it): 15 % of spawn draws; the ones there are never killed, so the pool fills with them. " +
            "Now a point pool (radius 0) at (261, 308.3, -642.2) on the open east slope, 29 m from upstream's point: a full spawn on " +
            "the point and its rings, a single top-up on the point; open ground joined to the waypoints 17 m round it; the marker on it. " +
            "ESTIMATE: the place.";

        public static IEnumerable<string> UpSql()
        {
            yield return PoolSql(PoolNow, RadiusNow, PoolBefore, RadiusBefore);
            yield return MarkerSql(MarkerNow, MarkerBefore);
        }

        public static IEnumerable<string> DownSql()
        {
            yield return MarkerSql(MarkerBefore, MarkerNow);
            yield return PoolSql(PoolBefore, RadiusBefore, PoolNow, RadiusNow);
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

        /// <summary>The pool from one place and radius to the other, only where it is the one (a second run changes nothing).</summary>
        private static string PoolSql((double X, double Y, double Z) to, double toRadius, (double X, double Y, double Z) from, double fromRadius) =>
            $"UPDATE spawnpool SET pos_x = {N(to.X)}, pos_y = {N(to.Y)}, pos_z = {N(to.Z)}, radius = {N(toRadius)} " +
            $"WHERE id = {Pool} AND creature_1_Id = {Fithik} AND {Near("pos_x", from.X)} AND {Near("pos_y", from.Y)} AND {Near("pos_z", from.Z)} " +
            $"AND {Near("radius", fromRadius)};";

        /// <summary>1274's marker from one place to the other, only where it is the one.</summary>
        private static string MarkerSql((double X, double Y, double Z) to, (double X, double Y, double Z) from) =>
            $"UPDATE mission_indicator SET pos_x = {N(to.X)}, pos_y = {N(to.Y)}, pos_z = {N(to.Z)} " +
            $"WHERE mission_id = {MissionId} AND content_revision = '{AshenMissionDataV1.Revision}' AND objective_id = {Marker.Objective} " +
            $"AND indicator_id = {Marker.Indicator} AND {Near("pos_x", from.X)} AND {Near("pos_y", from.Y)} AND {Near("pos_z", from.Z)};";

        /// <summary>A column at a value, as a single float holds it (the pool rows' columns): within 0.01.</summary>
        private static string Near(string column, double value) => $"ABS({column} - ({N(value)})) < 0.01";

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    }
}
