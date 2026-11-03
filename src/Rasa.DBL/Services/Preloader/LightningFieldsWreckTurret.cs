using System.Collections.Generic;
using System.Globalization;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Lightning Fields' turret spot on the platform south of the point (Lightning_fields_wreck_turret).
    ///
    /// Add_control_point_turrets put an AFS turret (551245) and a Bane turret (551246) on the
    /// turret platform at (-184, 436.3, 388) in Torden Plains. The map already stands a wreck there:
    /// an ArchHumBaseTurretDestroyed, the burnt-out standard gun on its own round base, 2.31 m over
    /// the platform with its own facing. The client always draws it, so whichever gun the point's
    /// holder had came up inside it. The platform is scenery for a battle already lost; the two
    /// pools and their control_point_link rows go, and Lightning Fields keeps its other five spots.
    /// No other turret pool on any map stands on a map's turret wreck.
    ///
    /// Guarded: a pool goes only while it is the one the seed made (its map, creature, mode and
    /// position), and is recorded; Down puts back only what Up took away.
    /// </summary>
    public static class LightningFieldsWreckTurret
    {
        public const uint Point = 23;
        public const uint Map = 1764;
        public const uint AfsPool = 551245, BanePool = 551246;
        public const uint AfsTurret = 550011, BaneTurret = 551007;
        public const uint AfsTurretClass = 4064, BaneTurretClass = 7482;
        public const double X = -184.0, Y = 438.86, Z = 388.0, Rotation = 0.36;
        public const uint Respawn = 3000;

        /// <summary>The pools: id, creature, its class, the pool's mode and its control_point_link kind.</summary>
        public static readonly (uint Pool, uint Creature, uint Class, int Mode, int Kind)[] Pools =
        {
            (AfsPool, AfsTurret, AfsTurretClass, 0, 2),
            (BanePool, BaneTurret, BaneTurretClass, 1, 1)
        };

        public static IEnumerable<string> Up()
        {
            yield return "CREATE TABLE IF NOT EXISTS lightning_fields_wreck_repair (pool_id INTEGER NOT NULL PRIMARY KEY);";

            foreach (var p in Pools)
            {
                yield return "INSERT INTO lightning_fields_wreck_repair (pool_id) SELECT id FROM spawnpool " +
                    $"WHERE id={p.Pool} AND map_context_id={Map} AND creature_1_Id={p.Creature} AND mode={p.Mode} " +
                    $"AND ABS(pos_x-({F(X)}))<0.001 AND ABS(pos_y-({F(Y)}))<0.001 AND ABS(pos_z-({F(Z)}))<0.001 " +
                    $"AND EXISTS (SELECT 1 FROM creature c WHERE c.id=spawnpool.creature_1_Id AND c.class_id={p.Class}) " +
                    "AND NOT EXISTS (SELECT 1 FROM lightning_fields_wreck_repair r WHERE r.pool_id=spawnpool.id);";
                yield return $"DELETE FROM control_point_link WHERE control_point_id={Point} AND kind={p.Kind} AND object_id={p.Pool} " +
                    $"AND EXISTS (SELECT 1 FROM lightning_fields_wreck_repair r WHERE r.pool_id={p.Pool});";
                yield return $"DELETE FROM spawnpool WHERE id={p.Pool} " +
                    $"AND EXISTS (SELECT 1 FROM lightning_fields_wreck_repair r WHERE r.pool_id={p.Pool});";
            }
        }

        public static IEnumerable<string> Down()
        {
            foreach (var p in Pools)
            {
                yield return "INSERT INTO spawnpool (id, mode, anim_type, respown_time, pos_x, pos_y, pos_z, rotation, map_context_id, " +
                    "creature_1_Id, creature_1_min_count, creature_1_max_count, creature_2_Id, creature_2_min_count, creature_2_max_count, " +
                    "creature_3_Id, creature_3_min_count, creature_3_max_count, creature_4_Id, creature_4_min_count, creature_4_max_count, " +
                    "creature_5_Id, creature_5_min_count, creature_5_max_count, creature_6_Id, creature_6_min_count, creature_6_max_count, radius) " +
                    $"SELECT {p.Pool}, {p.Mode}, 0, {Respawn}, {F(X)}, {F(Y)}, {F(Z)}, {F(Rotation)}, {Map}, {p.Creature}, 1, 1, " +
                    "0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0.0 " +
                    $"FROM lightning_fields_wreck_repair r WHERE r.pool_id={p.Pool} " +
                    $"AND NOT EXISTS (SELECT 1 FROM spawnpool s WHERE s.id={p.Pool});";
                yield return "INSERT INTO control_point_link (control_point_id, kind, object_id) " +
                    $"SELECT {Point}, {p.Kind}, {p.Pool} FROM lightning_fields_wreck_repair r WHERE r.pool_id={p.Pool} " +
                    $"AND NOT EXISTS (SELECT 1 FROM control_point_link l WHERE l.kind={p.Kind} AND l.object_id={p.Pool});";
            }

            yield return "DROP TABLE lightning_fields_wreck_repair;";
        }

        private static string F(double value) => value.ToString("0.0##", CultureInfo.InvariantCulture);
    }
}
