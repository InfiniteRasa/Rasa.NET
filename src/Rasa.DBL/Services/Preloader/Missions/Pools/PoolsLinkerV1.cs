using System.Collections.Generic;
using Rasa.Services.Preloader.Missions.Plateau;

namespace Rasa.Services.Preloader.Missions.Pools
{
    /// <summary>
    /// A Linker for Valverde Pools (map 1304), for Sabotage the Tower (937): "Eliminate the Linkers guarding the Eloh
    /// Relic site", counter 5060 "Linkers eliminated" (client missionobjective 937,2); TaRapedia: "Linkers eliminated 0 /
    /// 4". The server had no Linker on the Pools.
    ///
    /// Reconstructed (ESTIMATE): one creature from the server's "Linker - Ashen Desert" (531014: client class 9751
    /// Bane_Linker, name 414, level 40, 8409 hit points, armor 285, linker_chestblast, linker_hand_blast,
    /// linker_groundblast, linker_channel), at level 35 (user, 2026-10-25), its hit points, armor and damage 35/40 of
    /// it. It stands only in 937's scene (PoolsSabotageTheTowerV1): no spawn pool.
    ///
    /// Ids: creature 760205, actions 760034-760037, free before. Plain SQL, valid on SQLite and MySQL; down takes out only
    /// these rows.
    /// </summary>
    public static class PoolsLinkerV1
    {
        public const uint Linker = 760205;
        public const uint LinkerClass = 9751;           // Bane_Linker
        public const uint NameId = 414;
        public const uint Level = 35, HitPoints = 7358, Armor = 249;
        public const uint FirstAction = 760034, LastAction = 760037;

        public static IEnumerable<string> Up()
        {
            yield return PlateauCreatureSqlV1.Action(760034, "Linker - Pools linker_chestblast", 263, 1, 1, 40, 20000, 7333, 514, 770, 1);
            yield return PlateauCreatureSqlV1.Action(760035, "Linker - Pools linker_hand_blast", 412, 1, 1, 40, 5000, 566, 193, 256, 1);
            yield return PlateauCreatureSqlV1.Action(760036, "Linker - Pools linker_groundblast", 264, 1, 1, 40, 5000, 2633, 256, 385, 1);
            yield return PlateauCreatureSqlV1.Action(760037, "Linker - Pools linker_channel", 410, 1, 0, 40, 20000, 5666, 0, 0, 1);
            yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                "action1, action2, action3, action4, action5, action6, action7, action8) " +
                $"VALUES ({Linker}, 'Linker - Pools', {LinkerClass}, 0, {Level}, {HitPoints}, {NameId}, 9, 5, " +
                "760034, 760035, 760036, 760037, 0, 0, 0, 0);";
            yield return PlateauCreatureSqlV1.Stat(Linker, HitPoints, Armor);
        }

        public static IEnumerable<string> Down()
        {
            yield return $"DELETE FROM creature_stat WHERE id = {Linker};";
            yield return $"DELETE FROM creature WHERE id = {Linker};";
            yield return $"DELETE FROM creature_action WHERE id BETWEEN {FirstAction} AND {LastAction};";
        }
    }
}
