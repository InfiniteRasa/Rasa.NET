using System.Collections.Generic;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// The Marshes set 7 (Stalker Woods, High Point Retreat) NPCs and pool, Valverde Marshes (map 1454): Retread Jeska (1863
    /// giver and receiver) and Retread Dudley (1864 and 1869) at High Point Retreat, Retread Duvall on the north road (1871;
    /// later 888 and 1867), and a Maw pool by 1871's flowers. The server had none of them.
    ///
    /// Ids (block 760300-760399, MarshesPaludosNpcsV1; checked 2026-11-02 against the World DB and the code of all worktrees:
    /// 760325-760399 free): 760325 Jeska, 760326 Dudley, 760327 Duvall (creature, npc_package, spawn pool, six appearance
    /// slots each); 760328 the Maw pool (spawnpool only, the Marshes Maw 531052).
    ///
    /// Sourced: client names 10321 "Retread Jeska", 10322 "Retread Dudley", 8551 "Retread Duvall" (creaturenamelanguage); Duvall's
    /// package 950 (objectiveconversation 1867,2 and 1904,1; free in the World DB npc_package); no objective conversation of
    /// 1863, 1864, 1869 or 1871 is theirs. TaRapedia NPC pages: Jeska -777, 270, -552 and Dudley
    /// -784, 243, -523 (Stalker Woods), Duvall -593, 216, -438. World DB: the Stalker Woods (High Point Retreat) waypoint 137
    /// (-784.3, 241.5, -547.9); Retread Vincent 510168 (class 3846, six appearance slots); the Marshes Maw 531052 (539's pools).
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3 or lower):
    ///  - The three copies of Retread Vincent's row and look with their names (Wilson's way, set 4); Jeska and Dudley server-only packages
    ///    (their ids), Duvall the client's 950.
    ///  - At TaRapedia's /loc with the navmesh height (probe of 2026-11-02): Jeska's 270 is no ground there (the camp floor is
    ///    241.2). All walked to from the waypoint.
    ///  - The Maw pool: two Maws (120 s), by 1871's flowers (the client "make sure a maw isn't around").
    /// </summary>
    public static class MarshesStalkerWoodsNpcsV1
    {
        public const uint MapContextId = MarshesMissionDataV1.MapContextId;
        public const uint Vincent = 510168;             // Retread Vincent, Retread City: the three's row and look

        public const uint Jeska = 760325, JeskaName = 10321, JeskaPackage = 760325;
        public static readonly (double X, double Y, double Z) JeskaAt = (-777, 241.18, -552);

        public const uint Dudley = 760326, DudleyName = 10322, DudleyPackage = 760326;
        public static readonly (double X, double Y, double Z) DudleyAt = (-784, 243.28, -523);

        public const uint Duvall = 760327, DuvallName = 8551, DuvallPackage = 950;   // the client's (objectiveconversation 1867,2 and 1904,1)
        public static readonly (double X, double Y, double Z) DuvallAt = (-593, 217.01, -438);

        public const uint MawPool = 760328, Maw = MarshesPaludosNpcsV1.Maw, Maws = 2, MawRespawnTenths = 1200, MawRadius = 6;
        public static readonly (double X, double Y, double Z) MawAt = (-756, 234.79, -520);

        public static IEnumerable<string> Up()
        {
            foreach (var (id, comment, name, package, at) in new[]
            {
                (Jeska, "Retread Jeska - Marshes, High Point Retreat (1863)", JeskaName, JeskaPackage, JeskaAt),
                (Dudley, "Retread Dudley - Marshes, High Point Retreat (1864, 1869)", DudleyName, DudleyPackage, DudleyAt),
                (Duvall, "Retread Duvall - Marshes, north road (1871)", DuvallName, DuvallPackage, DuvallAt)
            })
            {
                yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                             "action1, action2, action3, action4, action5, action6, action7, action8) " +
                             $"SELECT {id}, '{comment}', class_id, faction, level, max_hp, {name}, run_speed, walk_speed, action1, " +
                             $"action2, action3, action4, action5, action6, action7, action8 FROM creature WHERE id = {Vincent};";
                yield return "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                             $"SELECT {id}, slot_id, Class_id, color FROM creature_appearance WHERE id = {Vincent};";
                yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({id}, {package}, '{comment[..comment.IndexOf(" - ")]} - Marshes');";
                yield return Pool(id, at.X, at.Y, at.Z, MapContextId, id, 1, 1, 20, 0);
            }
            yield return Pool(MawPool, MawAt.X, MawAt.Y, MawAt.Z, MapContextId, Maw, Maws, Maws, MawRespawnTenths, MawRadius);
        }

        public static IEnumerable<string> Down()
        {
            yield return $"DELETE FROM spawnpool WHERE id IN ({Jeska}, {Dudley}, {Duvall}, {MawPool});";
            yield return $"DELETE FROM npc_package WHERE id IN ({Jeska}, {Dudley}, {Duvall});";
            yield return $"DELETE FROM creature_appearance WHERE id IN ({Jeska}, {Dudley}, {Duvall});";
            yield return $"DELETE FROM creature WHERE id IN ({Jeska}, {Dudley}, {Duvall});";
        }
    }
}
