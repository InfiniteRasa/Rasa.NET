using System.Collections.Generic;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Archfiend and Old Scratch come back as soon as Horntail does (Three_devils_respawn). Herbal
    /// Remedy (695) wants all three of the Three Devils killed on the Lake Tinctuan landing pad;
    /// Horntail's pool (520065) respawns in 50 s, the other two's (580009, 580013) in 500 s, so a
    /// player who found any of them dead waited over eight minutes on each of those two. All three
    /// now come back in 50 s.
    ///
    /// spawnpool.respown_time is in tenths of a second. Guarded: a pool changes only while it holds
    /// its devil on the Wilderness at the time it had; Down puts back 5000 only where this put 500.
    /// Plain SQL, valid on SQLite and MySQL alike.
    /// </summary>
    public static class ThreeDevilsRespawn
    {
        /// <summary>Horntail's respawn, which the other two take: 50 s.</summary>
        public const uint Respawn = 500;

        /// <summary>What Archfiend's and Old Scratch's pools had: 500 s.</summary>
        public const uint Before = 5000;

        public static readonly (uint Pool, uint Creature, string Name)[] Pools =
        {
            (580009, 75, "Archfiend"),
            (580013, 80, "Old Scratch"),
        };

        public static IEnumerable<string> Up()
        {
            foreach (var (pool, creature, _) in Pools)
                yield return Set(pool, creature, Before, Respawn);
        }

        public static IEnumerable<string> Down()
        {
            foreach (var (pool, creature, _) in Pools)
                yield return Set(pool, creature, Respawn, Before);
        }

        private static string Set(uint pool, uint creature, uint from, uint to) =>
            $"UPDATE spawnpool SET respown_time = {to} WHERE id = {pool} AND creature_1_Id = {creature} " +
            $"AND map_context_id = 1220 AND respown_time = {from};";
    }
}
