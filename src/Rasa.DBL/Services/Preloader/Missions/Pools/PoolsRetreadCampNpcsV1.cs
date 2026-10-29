using System.Collections.Generic;

namespace Rasa.Services.Preloader.Missions.Pools
{
    /// <summary>
    /// John McKormic, Jayson Venma, Austin Buckley and Retread Rickert at the Retread Camp, Valverde Pools (map 1304), for
    /// the Retread chain (560, 561, 562, 563). The server had Amee Corman (510195) at the camp, but not them.
    ///
    /// Sourced: client names 4367 "John McKormic", 4368 "Jayson Venma", 4369 "Austin Buckley", 4374 "Retread Rickert"
    /// (creaturenamelanguage); their packages 326, 327, 328, 333 (objectiveconversation 560,2 and 563,3-23), which are the
    /// name id less 4041, as the Hermit's 325 and Amee's 329 are. TaRapedia: McKormic, Venma and Buckley at the Retread
    /// Camp; TR MMO DB: Buckley a quest NPC at the Retread Camp. Ten Ton Hammer: the camp is "a handful of people standing
    /// around" the pyramid.
    ///
    /// Reconstructed (ESTIMATE, the user's choice of 2026-10-25): the four stand round Amee (-145, 862, 521), 3-7 m from her,
    /// on the navmesh's main region, 0.2 m above it; male; the officer uniform and level 40 of the Snake Pit's NPCs
    /// (PoolsSnakePitNpcsV1.Npc), as the server's other Retreads (Retread McCormick 510201, Vincent 510168) wear it.
    ///
    /// Ids: creatures and pools 760206-760209, free before. Plain SQL, valid on SQLite and MySQL; down takes out only
    /// these rows.
    /// </summary>
    public static class PoolsRetreadCampNpcsV1
    {
        public const uint McKormic = 760206, McKormicName = 4367, McKormicPackage = 326;
        public const uint Venma = 760207, VenmaName = 4368, VenmaPackage = 327;
        public const uint Buckley = 760208, BuckleyName = 4369, BuckleyPackage = 328;
        public const uint Rickert = 760209, RickertName = 4374, RickertPackage = 333;
        public const uint Amee = 510195, AmeePackage = 329;

        public static readonly (double X, double Y, double Z) AmeeAt = (-145, 862, 521);
        public static readonly (double X, double Y, double Z) McKormicAt = (-141.8, 862.6, 520.2);
        public static readonly (double X, double Y, double Z) VenmaAt = (-151, 863.0, 524);
        public static readonly (double X, double Y, double Z) BuckleyAt = (-142, 863.8, 515);
        public static readonly (double X, double Y, double Z) RickertAt = (-149, 864.0, 515);

        public static IEnumerable<string> Up()
        {
            foreach (var sql in PoolsSnakePitNpcsV1.Npc(McKormic, "John McKormic - Retread Camp", McKormicName, McKormicPackage, McKormicAt))
                yield return sql;
            foreach (var sql in PoolsSnakePitNpcsV1.Npc(Venma, "Jayson Venma - Retread Camp", VenmaName, VenmaPackage, VenmaAt))
                yield return sql;
            foreach (var sql in PoolsSnakePitNpcsV1.Npc(Buckley, "Austin Buckley - Retread Camp", BuckleyName, BuckleyPackage, BuckleyAt))
                yield return sql;
            foreach (var sql in PoolsSnakePitNpcsV1.Npc(Rickert, "Retread Rickert - Retread Camp", RickertName, RickertPackage, RickertAt))
                yield return sql;
        }

        public static IEnumerable<string> Down()
        {
            foreach (var id in new[] { Rickert, Buckley, Venma, McKormic })
            {
                yield return $"DELETE FROM spawnpool WHERE id = {id};";
                yield return $"DELETE FROM npc_package WHERE id = {id};";
                yield return $"DELETE FROM creature_appearance WHERE id = {id};";
                yield return $"DELETE FROM creature WHERE id = {id};";
            }
        }
    }
}
