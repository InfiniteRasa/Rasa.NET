using System.Collections.Generic;
using System.Globalization;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Herbal Remedy (695) sends the player to kill "The Three Devils" - Old Scratch, Horntail and
    /// Archfiend - who guard the Bane landing pad on the shore of Lake Tinctuan, inside its
    /// objective's 35 m indicator at (615, 284.26, 737). The fork had all three on Horntail's point
    /// there. Rebuild_wilderness_spawns kept Horntail (520065) and sent the other two to lead camps
    /// in Lower Eloh Creek - Archfiend (580009) the camp at (290, 269.4), Old Scratch (580013) the
    /// outpost at (343.4, 164) - where they walked into walls they could not be fought from, and
    /// nowhere near the mission's ground (Three_devils).
    ///
    /// They go back beside Horntail, either side of him on the pad, 11.7 m off and 20 m apart:
    /// open, connected navmesh ground (adv_foreas_concordia_wilderness), at its height.
    ///
    /// Guarded: a pool moves only while it holds its devil at the position it had. Plain SQL,
    /// valid on SQLite and MySQL alike.
    /// </summary>
    public static class ThreeDevils
    {
        /// <summary>
        /// From is the pool's position as WildernessSpawnpoolPreloader wrote it: single-precision
        /// floats, which is what Down puts back, to the bit.
        /// </summary>
        public sealed record Devil(uint Pool, uint Creature, string Name, float FromX, float FromY, float FromZ, double ToX, double ToY, double ToZ);

        /// <summary>Horntail's pool, which stays where it is.</summary>
        public const uint HorntailPool = 520065;

        public static readonly Devil[] Devils =
        {
            new(580009, 75, "Archfiend", 290.0000f, 234.3709f, 269.4000f, 605.0, 284.8088, 731.0),
            new(580013, 80, "Old Scratch", 343.4000f, 233.5164f, 164.0000f, 625.0, 285.0003, 731.0),
        };

        /// <summary>The statements, one each, as MigrationBuilder.Sql takes them.</summary>
        public static IEnumerable<string> Up()
        {
            foreach (var devil in Devils)
                yield return Move(devil, devil.FromX, devil.FromY, devil.FromZ, devil.ToX, devil.ToY, devil.ToZ);
        }

        public static IEnumerable<string> Down()
        {
            foreach (var devil in Devils)
                yield return Move(devil, devil.ToX, devil.ToY, devil.ToZ, devil.FromX, devil.FromY, devil.FromZ);
        }

        private static string Move(Devil devil, double fromX, double fromY, double fromZ, double toX, double toY, double toZ) =>
            $"UPDATE spawnpool SET pos_x = {N(toX)}, pos_y = {N(toY)}, pos_z = {N(toZ)} " +
            $"WHERE id = {devil.Pool} AND creature_1_Id = {devil.Creature} AND map_context_id = 1220 " +
            $"AND ABS(pos_x - {N(fromX)}) < 0.01 AND ABS(pos_y - {N(fromY)}) < 0.01 AND ABS(pos_z - {N(fromZ)}) < 0.01;";

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    }
}
