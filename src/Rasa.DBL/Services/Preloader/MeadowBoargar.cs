using System.Collections.Generic;
using System.Globalization;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The Young Forest Boargar pack in the meadow west of Alia Das (spawnpool 580064, creature
    /// 44, 2-4, 15 m round) moves out of the scan of the light turret Alia_das_touchups put on the
    /// meadow's tripod (pool 640025, 660.93, 437.11) (Meadow_boargar).
    ///
    /// The pack stood at (646.91, 439.32), the fork's boargar ground: 14.2 m from the turret, so
    /// its circle reached the turret's mount and the two fought for as long as the server ran -
    /// ValidatePools: "its creatures can stand 0 m from the turret of pool 640025, inside its
    /// scan; they will fight for good."
    ///
    /// It goes 23 m west, to (624, 430), on the same meadow: its circle's edge is then 22.6 m from
    /// the turret, clear of the 18 m scan (SpawnPoolManager.TurretScan) and of the 20 m the
    /// Wilderness pools were laid out with, more than 20 m from the next pack's circle (580019's)
    /// and 40 m or more from every friendly NPC, hospital and waypoint. The whole circle is open
    /// navmesh ground above water (adv_foreas_concordia_wilderness), joined to where the pack
    /// was; the centre's height is the ground's there, 287.5006.
    ///
    /// Guarded: the pool moves only while it holds its boargar at the position it had. Plain SQL,
    /// valid on SQLite and MySQL alike.
    /// </summary>
    public static class MeadowBoargar
    {
        public const uint Pool = 580064;
        public const uint YoungForestBoargar = 44;
        public const uint TurretPool = 640025;

        /// <summary>As WildernessSpawnpoolPreloader wrote it: single-precision floats, which is what Down puts back, to the bit.</summary>
        public const float FromX = 646.9102f, FromY = 287.0703f, FromZ = 439.3203f;

        public const double ToX = 624.0, ToY = 287.5006, ToZ = 430.0;

        /// <summary>The statements, one each, as MigrationBuilder.Sql takes them.</summary>
        public static IEnumerable<string> Up()
        {
            yield return Move(FromX, FromY, FromZ, ToX, ToY, ToZ);
        }

        public static IEnumerable<string> Down()
        {
            yield return Move(ToX, ToY, ToZ, FromX, FromY, FromZ);
        }

        private static string Move(double fromX, double fromY, double fromZ, double toX, double toY, double toZ) =>
            $"UPDATE spawnpool SET pos_x = {N(toX)}, pos_y = {N(toY)}, pos_z = {N(toZ)} " +
            $"WHERE id = {Pool} AND creature_1_Id = {YoungForestBoargar} AND map_context_id = 1220 " +
            $"AND ABS(pos_x - {N(fromX)}) < 0.01 AND ABS(pos_y - {N(fromY)}) < 0.01 AND ABS(pos_z - {N(fromZ)}) < 0.01;";

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    }
}
