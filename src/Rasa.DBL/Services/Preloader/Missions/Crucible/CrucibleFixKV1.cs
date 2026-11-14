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
    /// Ligo Crucible fix job K (real client 2026-11-08 10:12-10:49, to-fix row 78; NOTES-crucible-triage mechanics item "Fix K"),
    /// a delta on migrations 20271108000600 (set 5, CrucibleStaalSigalsNpcsV1), 20271108001000 (fix job F) and 20271108001200
    /// (fix job G), whose data classes stay as they are.
    ///
    /// The play (visible, not immune, no GM): 347 hits of 54 Physical (about 0.6 a second) on a Shield Drone of Out Of Sight
    /// (1208; creature 770022, level 35, 5473 hit points, armour 248) and it never died. Upstream's ShieldDrone.Heal gives
    /// HEAL_AMOUNT 500 (CR_SHIELD_DRONE_HEAL 245/1, action_property 1301/1302) every 5 s to the drone itself and to every Bane of
    /// its side within 60 m (RADIUS_AROUND_SOURCE, ShieldDrone.Radius), and the other drones are Bane of its side: set 5's pools
    /// of three drones gave each drone up to 300 a second, and pools 770025 (490, 65) and 770026 (535, 80), 47 m apart, six
    /// drones that heal each other. Probe (scratch test, armour first, drones fighting): 323 a second kills three pooled drones
    /// in 47 s, a lone one in 22 s; 54 a second kills none, a lone one neither (its own heal is 100 a second).
    ///
    /// Sources: upstream's World DB has at most one drone in a pool on every other map (map 1220: two), mostly with other Bane;
    /// TaRapedia (1206): Bane "usually protected by a Shield Drone"; the strategy guide (ShieldDrone.cs): "use melee attacks,
    /// close-range weaponry, or Lightning". Upstream's drone code (the heal of other drones too) stays: an upstream matter.
    ///
    /// Now: one drone in each of the three pools (min 1, max 1); 770025 a point (radius 0) as the other two are since fix jobs F
    /// and G, so its drone stands at the point or on the first ring (Spacing.OnRings) and a spawn draw does not carry it off
    /// (CrucibleGuardTests.SpawnReach); 770026 and its 1208 marker (the third) moved to <see cref="Move"/>, more than the heal
    /// reach and both pools' rings from 770024 and 770025 (CrucibleGuardTests: no two drone pools within reach). The respawn
    /// stays 120 s (respown_time 1200 is tenths of a second, SpawnPoolManager.SpawnPoolInit): a pool of one comes back 120 s
    /// after its drone dies.
    ///
    /// Not changed: the drone's strike row 770022 keeps damage_type 0. 0 is "not stated": CreatureAttacks.DamageTypeOf then
    /// takes the action's DAMAGE_TYPE (396/1 has none: action_property 2398-2400 are DAMAGE_AMOUNT_MIN/MAX and
    /// DAMAGE_SCALE_TYPE) and then CreatureWeaponDamage (no 396 pair), which gives Physical (1), what upstream's drone rows state.
    /// The same blow either way.
    ///
    /// The new place (probe 2026-11-08, a scratch test on the guards' own checks, removed): the nearest navmesh point to set 5's
    /// on a 4 m grid that is 74 m or more (60 m and both pools' 6.6 m rings, and a little) from 770024 and 770025, has a
    /// walkable client floor and none just over it, no lava the server burns on within 20 m, its marker circle (r20) and the
    /// pool's chase 444 m outside every map link's circle, 187 m from the nearest friendly NPC, and is walked to on the navmesh
    /// from the Prometheus Outpost waypoint (421 m), 770025 (78 m) and 770024 (167 m) with every client step under 1.5 m and
    /// every 12 m grade under 0.9 (CrucibleGuardTests.ClientClimb) and 10 m or more outside every link. The nearest other
    /// hostile pool is 770025 itself.
    ///
    /// Reconstructed (ESTIMATE): the place (none of the sources has one; the nearest place to set 5's that keeps every guard);
    /// one drone a pool (upstream's, not the client's: no source counts the drones of 1208).
    /// </summary>
    public static class CrucibleFixKV1
    {
        public const uint ShieldDrone = CrucibleStaalSigalsNpcsV1.ShieldDrone;

        /// <summary>The drones a pool held (set 5) and holds now.</summary>
        public const uint DronesBefore = CrucibleStaalSigalsNpcsV1.DronesPerPool, DronesPerPool = 1;

        /// <summary>The three drone pools.</summary>
        public static readonly uint[] Pools = CrucibleStaalSigalsNpcsV1.DronePools.Select(pool => pool.Pool).ToArray();

        /// <summary>The pool made a point and the radius it had (set 5's).</summary>
        public static readonly (uint Pool, uint Before) Point = (CrucibleStaalSigalsNpcsV1.DronePools[1].Pool, CrucibleStaalSigalsNpcsV1.PoolRadius);

        /// <summary>The pool moved: the place before (set 5's, a point since fix job G) and now.</summary>
        public static readonly (uint Pool, (double X, double Y, double Z) Before, (double X, double Y, double Z) Now) Move =
            (CrucibleStaalSigalsNpcsV1.DronePools[2].Pool, CrucibleStaalSigalsNpcsV1.DronePools[2].At, (568, 172.88, 64));

        /// <summary>1208's marker on the moved pool (CrucibleOutOfSightV1: the pools' markers 1-3 in order).</summary>
        public static readonly (uint Mission, uint Objective, uint Indicator) Marker =
            (CrucibleOutOfSightV1.MissionId, CrucibleOutOfSightV1.Processors, MissionIndicator.UnnamedFrom + 3);

        /// <summary>The heal's reach (CR_SHIELD_DRONE_HEAL RADIUS_AROUND_SOURCE, upstream ShieldDrone.Radius).</summary>
        public const double HealReach = 60;

        /// <summary>
        /// How far a point pool's creature stands from its point: Spacing.OnRings sets them on two rings of 1.1 spacings (3 m)
        /// a ring, the outer 6.6 m out.
        /// </summary>
        public const double PointSpread = 6.6;

        /// <summary>Where a drone pool stands after this migration: the moved one's new place, else null.</summary>
        public static (double X, double Y, double Z)? Now(uint pool) => pool == Move.Pool ? Move.Now : null;

        /// <summary>1208's evidence row (set 5 wrote 1-3, fix job F 4, fix job G 5).</summary>
        public const uint Evidence = 6;

        public const string Source = "repository:CrucibleFixKV1";

        public const string Note =
            "Fix job K (to-fix row 78): a Shield Drone (770022, 5473 hit points) took 347 hits of 54 Physical and never died: " +
            "upstream's drone heal (500 every 5 s to itself and every Bane of its side within 60 m) let the three drones of a pool, " +
            "and the six of pools 770025 and 770026 (47 m apart), heal each other. Upstream has at most one drone a pool. Now one " +
            "drone in each of 770024-770026, 770025 a point, 770026 and its marker moved to (568, 172.88, 64), 37 m east-south-east, 78 m from 770025 and 161 m (a 167 m walk) from 770024, more than 60 m and the rings " +
            "from the other two (ESTIMATE: the place). Respawn 120 s. A lone drone still heals itself 100 a second.";

        public static IEnumerable<string> UpSql()
        {
            foreach (var pool in Pools)
                yield return Count(pool, DronesPerPool);
            yield return Radius(Point.Pool, CrucibleFixFV1.PointRadius);
            yield return Place(Move.Pool, Move.Now);
            yield return MarkerAt(Move.Now);
        }

        public static IEnumerable<string> DownSql()
        {
            yield return MarkerAt(Move.Before);
            yield return Place(Move.Pool, Move.Before);
            yield return Radius(Point.Pool, Point.Before);
            foreach (var pool in Pools)
                yield return Count(pool, DronesBefore);
        }

        public static void Up(MigrationBuilder migration)
        {
            foreach (var sql in UpSql())
                migration.Sql(sql);
            CrucibleMissionDataV1.Existing(migration, CrucibleOutOfSightV1.MissionId)
                .Evidence(Evidence, MissionEvidenceSourceKind.Reconstruction, Source, Note, 0.3);
        }

        public static void Down(MigrationBuilder migration)
        {
            migration.Sql($"DELETE FROM mission_evidence WHERE mission_id = {CrucibleOutOfSightV1.MissionId} " +
                          $"AND content_revision = '{CrucibleMissionDataV1.Revision}' AND evidence_id = {Evidence};");
            foreach (var sql in DownSql())
                migration.Sql(sql);
        }

        private static string Count(uint pool, uint count) =>
            $"UPDATE spawnpool SET creature_1_min_count = {count}, creature_1_max_count = {count} WHERE id = {pool} AND creature_1_Id = {ShieldDrone};";

        private static string Radius(uint pool, uint radius) => $"UPDATE spawnpool SET radius = {radius} WHERE id = {pool};";

        private static string Place(uint pool, (double X, double Y, double Z) at) =>
            $"UPDATE spawnpool SET pos_x = {N(at.X)}, pos_y = {N(at.Y)}, pos_z = {N(at.Z)} WHERE id = {pool};";

        private static string MarkerAt((double X, double Y, double Z) at) =>
            $"UPDATE mission_indicator SET pos_x = {N(at.X)}, pos_y = {N(at.Y)}, pos_z = {N(at.Z)} " +
            $"WHERE mission_id = {Marker.Mission} AND content_revision = '{CrucibleMissionDataV1.Revision}' AND objective_id = {Marker.Objective} " +
            $"AND indicator_id = {Marker.Indicator};";

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    }
}
