using System.Collections.Generic;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// The Marshes set 2 (Falcon Hold) NPC and pools, Valverde Marshes (map 1454): Research Chemist Horsh, who gives Tastes
    /// Like Grubber (959) and Fixing the Food Chain (961), and two mission-only Swamp Grubber pools in the swamp west of
    /// Falcon Hold for 959. The server had none of them.
    ///
    /// Ids (block 760300-760399, MarshesPaludosNpcsV1; checked 2026-11-02 against the World DB and the code: 760309-760399
    /// free): 760309 Horsh (creature, creature_appearance, npc_package, spawn pool); 760310 and 760311 the two Grubber pools
    /// (spawn pools of the Marshes row 540014, no new creature).
    ///
    /// Sourced: client name 3142 "Research Chemist Horsh" (creaturenamelanguage; the mission logs spell him "Horsch").
    /// TaRapedia Research Chemist Horsh: Falcon Hold, /loc (-168, 219, 593). The client has no objective conversation at
    /// him (959 and 961 go by giver and receiver only).
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3 or lower):
    ///  - Horsh is a copy of Researcher Vetter's row and look (510165, the server's other Falcon Hold researcher: class
    ///    3846, level 10, 1000 hit points, his six appearance slots), with Horsh's name; a server-only package (his id), as
    ///    the triage's rule for new NPCs (PoolsSpyNpcsV1's Sheen). Height from the navmesh (probe of 2026-11-02).
    ///  - 959's pools: two of Swamp Grubber 540014 (the open world has only two, in the east; the client log says "western
    ///    marshes", Ten Ton Hammer "in the swamp outside the base") at (-250, 600) and (-290, 620), west of Falcon Hold;
    ///    three a pool, radius 6, 120 s respawn (as MarshesPaludosNpcsV1.GasPools).
    ///
    /// Plain SQL, valid on SQLite and MySQL (the copies are INSERT ... SELECT); down takes out only these rows.
    /// </summary>
    public static class MarshesFalconHoldNpcsV1
    {
        public const uint MapContextId = MarshesMissionDataV1.MapContextId;

        public const uint Horsh = 760309, HorshName = 3142, HorshPackage = 760309;
        public const uint Vetter = 510165;              // Researcher Vetter, Falcon Hold: Horsh's row and look
        public static readonly (double X, double Y, double Z) HorshAt = (-168, 219.45, 593);

        public const uint SwampGrubber = MarshesPaludosNpcsV1.SwampGrubber;
        public const uint PerPool = MarshesPaludosNpcsV1.PerPool, PoolRadius = MarshesPaludosNpcsV1.PoolRadius;
        public const uint PoolRespawnTenths = MarshesPaludosNpcsV1.PoolRespawnTenths;

        /// <summary>959's mission pools west of Falcon Hold (navmesh heights).</summary>
        public static readonly (uint Id, double X, double Y, double Z)[] SteakPools =
        {
            (760310, -250, 217.2, 600), (760311, -290, 214.42, 620)
        };

        public static IEnumerable<string> Up()
        {
            yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                         "action1, action2, action3, action4, action5, action6, action7, action8) " +
                         $"SELECT {Horsh}, 'Research Chemist Horsh - Falcon Hold (959, 961)', class_id, faction, level, max_hp, " +
                         $"{HorshName}, run_speed, walk_speed, action1, action2, action3, action4, action5, action6, action7, action8 " +
                         $"FROM creature WHERE id = {Vetter};";
            yield return "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                         $"SELECT {Horsh}, slot_id, Class_id, color FROM creature_appearance WHERE id = {Vetter};";
            yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({Horsh}, {HorshPackage}, 'Research Chemist Horsh - Falcon Hold');";
            yield return Pool(Horsh, HorshAt.X, HorshAt.Y, HorshAt.Z, MapContextId, Horsh, 1, 1, 20, 0);
            foreach (var (id, x, y, z) in SteakPools)
                yield return Pool(id, x, y, z, MapContextId, SwampGrubber, PerPool, PerPool, PoolRespawnTenths, PoolRadius);
        }

        public static IEnumerable<string> Down()
        {
            foreach (var (id, _, _, _) in SteakPools)
                yield return $"DELETE FROM spawnpool WHERE id = {id};";
            yield return $"DELETE FROM spawnpool WHERE id = {Horsh};";
            yield return $"DELETE FROM npc_package WHERE id = {Horsh};";
            yield return $"DELETE FROM creature_appearance WHERE id = {Horsh};";
            yield return $"DELETE FROM creature WHERE id = {Horsh};";
        }
    }
}
