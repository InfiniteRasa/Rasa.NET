using System.Collections.Generic;
using System.Globalization;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The Omega Missile on its pad in Omega Labs' silo (Omega_labs_missile): one world_usable row
    /// (PlacedUsables), row 10501.
    ///
    /// The missile is UsableTwoStateMissileOmegalab (29109, arch_hum_missile_omegalab_v01.geo), a
    /// two-state switch: USE_TS_STATE_0 is the missile standing ready (its state animation,
    /// arch_hum_missile_omegalab_idle_v01, with vfx_arch_hum_omegalabs_missile_pre_launch's gas
    /// and vents), the way to USE_TS_STATE_1 its launch (arch_hum_missile_omegalab_launch_v01,
    /// vfx_arch_hum_omegalabs_missile_launch) and USE_TS_STATE_1 the empty pad after it
    /// (..._post_launch) (usabledata animation and specialFX). No .map places it, so the live
    /// server did.
    ///
    /// It stands at the silo piece's own origin, (0, 180, 60): the centre of the pad
    /// (terra_arieki_cavern_abyss_missilesilo_v01), between the steam vents at x ±26 and the
    /// rotating warning lights at x ±42 to either side of z 60. Idle, and no use of it: the
    /// launch is The Final Countdown's, which the server does not have yet.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike: the row goes in where the class is loaded and
    /// no row has its id; Down takes it out.
    /// </summary>
    public static class OmegaLabsMissile
    {
        public const uint Id = 10501;
        public const uint MapContextId = 2203;     // Section 5: Omega Labs
        public const uint MissileClass = 29109;    // UsableTwoStateMissileOmegalab
        public const uint IdleState = 55;          // USE_TS_STATE_0

        public const double X = 0, Y = 180, Z = 60, Rotation = 0;

        public const string Comment = "Omega Labs: the Omega Missile on its pad in the silo, idle";

        public static IEnumerable<string> Up()
        {
            yield return "INSERT INTO world_usable (id, map_context_id, class_id, pos_x, pos_y, pos_z, rotation, state, usable, item_template_id, item_class_id, " +
                "item_count, reset_seconds, interval_min_seconds, interval_max_seconds, damage_min, damage_max, radius, action_arg, comment) " +
                $"SELECT {Id}, {MapContextId}, {MissileClass}, {N(X)}, {N(Y)}, {N(Z)}, {N(Rotation)}, {IdleState}, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, '{Comment}' " +
                $"FROM entityclass e WHERE e.id = {MissileClass} AND NOT EXISTS (SELECT 1 FROM world_usable x WHERE x.id = {Id});";
        }

        public static IEnumerable<string> Down()
        {
            yield return $"DELETE FROM world_usable WHERE id = {Id} AND class_id = {MissileClass};";
        }

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    }
}
