using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// The Ashen Desert set 7 creatures, Ligo Ashen Desert (map 1734), the Boneyard, for Scavenger Vakiel Latan's Robotic Salvage
    /// Recovery (1264): the Nitroglazer and the Hominis Machina Mk XI, new at level 40, each in two pools of three in the
    /// Boneyard, where the World DB has neither (its Nitroglazers are level 20-28, its Hominis Machina level 8-14, on other maps).
    /// Weapon Salvage Recovery (1266) and Heavy Salvage Recovery (1265) use creatures of earlier sets only (the Ashen Thrax
    /// Soldier 531008 and Ravager 531009, set 3's Desert Predator 790080, set 4's Desert Stalker 790121).
    ///
    /// Ids (the Ashen block 790000-790999, AshenLeadInNpcsV1; set 7 790240-790279):
    ///  - 790240 the Nitroglazer (creature, creature_stat; its weapon creature_action 790240 and its mortar 790242);
    ///  - 790241 the Hominis Machina Mk XI (creature, creature_stat, creature_appearance; its weapon creature_action 790241);
    ///  - 790243, 790244 the Nitroglazer pools; 790245, 790246 the Hominis Machina pools.
    ///
    /// Sourced:
    ///  - Names (client creaturenamelanguage, 1.0): 434 "Nitroglazer", 7716 "Hominis Machina Mk XI". Classes (World DB): 7234
    ///    (the Nitroglazer rows 567033, 710015), 3868 (the Hominis Machina rows 8, 566013, 660012).
    ///  - Places: "Nitroglazers and Hominus Machina Mark XI ... spawn throughout The Boneyard" (TaRapedia Robotic Salvage
    ///    Recovery, 0.5); the client's offer "We have collected much of the robotic salvage from Boneyard, so you must kill them
    ///    for parts" (1.0). World DB region The Boneyard (579, 283, -291) r200.
    ///  - Rank: TaRapedia's "Mark XI"; the client's rank names run Mk I to XVI (7706-7721), and the Ashen Desert Stalker of the
    ///    same band is "Class XI" (9321, set 4).
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Level 40 (the zone band's floor, as set 4's Stalker), 8409 hit points and armour 285: the World DB's Ashen level-40
    ///    hostile rows (Linker 531014, Atta Soldier 531015, Caretaker 531011). Run 9, walk 5 (the World DB rows').
    ///  - The Nitroglazer's attacks: the EWC row 710015's two (weapon 1/232, 1-30 m, 1500 ms; mortar 289/1, 5-40 m, 12000 ms;
    ///    physical) at the Ashen Thrax Soldier's level-39 weapon damage 247-370, the mortar at the EWC row's ratio to its
    ///    weapon (1.44 / 1.5): 356-555.
    ///  - The Hominis Machina's: the Torcastra row 660012's two (weapon 1/97, 1-20 m, 800 ms; its machina_revive 62059, the
    ///    upstream row kept) at the Ashen Thrax Ravager's 800 ms weapon damage 247-370. Its look: 660012's (class 7508 in slots
    ///    18 and 19).
    ///  - Two pools of three of each, radius 0 (rings, as sets 2 and 4), 120 s respawn, in the Boneyard: the Nitroglazers in
    ///    its east (700, -250) and (690, -310), the Hominis Machina in its west (440, -320) and (440, -360), by the Hominis
    ///    Machina corpse parts of the client map (505, 283, -321). Each pool's point the navmesh ground; round it (17 m) one
    ///    navmesh floor, joined to the Ashoka Settlement waypoint, on a client floor under open sky; walked to on the client
    ///    floor from the Ashoka Settlement waypoint (0.9 m a metre, knee, chest and head height clear); 60 m or more from the
    ///    other hostile pools and 90 m or more (across) from the Ashen NPCs (Latan and Ito on Ashoka's lower level included;
    ///    probe of 2026-11-09).
    ///
    /// Plain SQL, valid on SQLite and MySQL; Down takes out only these rows. No upstream row changes.
    /// </summary>
    public static class AshenScavengerNpcsV1
    {
        public const uint MapContextId = AshenMissionDataV1.MapContextId;
        public const uint Level = 40, HitPoints = 8409, Armor = 285;
        public const uint PerPool = 3, RespawnTenths = 1200, PoolRadius = 0;

        /// <summary>The Nitroglazer: id, class, name; its weapon and mortar.</summary>
        public const uint Nitroglazer = 790240, NitroglazerClass = 7234, NitroglazerName = 434;
        public const uint NitroWeapon = 790240, NitroMortar = 790242;

        /// <summary>The Hominis Machina Mk XI: id, class, name, the row whose look it takes; its weapon; the upstream revive.</summary>
        public const uint Machina = 790241, MachinaClass = 3868, MachinaName = 7716, MachinaLook = 660012;
        public const uint MachinaWeapon = 790241, MachinaRevive = 62059;

        /// <summary>The Nitroglazer pools: pool, point.</summary>
        public static readonly (uint Pool, (double X, double Y, double Z) At)[] NitroglazerPools =
        {
            (790243, (700, 282.2, -250)),
            (790244, (690, 281.05, -310))
        };

        /// <summary>The Hominis Machina pools: pool, point.</summary>
        public static readonly (uint Pool, (double X, double Y, double Z) At)[] MachinaPools =
        {
            (790245, (440, 287.89, -320)),
            (790246, (440, 285.32, -360))
        };

        public const uint NextFreeId = 790247;

        public static IEnumerable<string> Up()
        {
            yield return Action(NitroWeapon, "Nitroglazer Ashen Desert weapon 1/232", 1, 232, 1, 30, 1500, 0, 247, 370, 1);
            yield return Action(NitroMortar, "Nitroglazer Ashen Desert mortar 289/1", 289, 1, 5, 40, 12000, 0, 356, 555, 1);
            yield return Creature(Nitroglazer, "Nitroglazer - Ashen Desert, the Boneyard (1264)", NitroglazerClass, Level, HitPoints,
                NitroglazerName, NitroWeapon, NitroMortar);
            yield return Stat(Nitroglazer, HitPoints, Armor);

            yield return Action(MachinaWeapon, "Hominis Machina Ashen Desert weapon 1/97", 1, 97, 1, 20, 800, 0, 247, 370, 1);
            yield return Creature(Machina, "Hominis Machina Mk XI - Ashen Desert, the Boneyard (1264)", MachinaClass, Level, HitPoints,
                MachinaName, MachinaWeapon, MachinaRevive);
            yield return Stat(Machina, HitPoints, Armor);
            yield return "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                         $"SELECT {Machina}, slot_id, Class_id, color FROM creature_appearance WHERE id = {MachinaLook};";

            foreach (var (pool, at) in NitroglazerPools)
                yield return Pool(pool, at.X, at.Y, at.Z, MapContextId, Nitroglazer, PerPool, PerPool, RespawnTenths, PoolRadius);
            foreach (var (pool, at) in MachinaPools)
                yield return Pool(pool, at.X, at.Y, at.Z, MapContextId, Machina, PerPool, PerPool, RespawnTenths, PoolRadius);
        }

        public static IEnumerable<string> Down()
        {
            var pools = string.Join(", ", NitroglazerPools.Concat(MachinaPools).Select(pool => pool.Pool.ToString(CultureInfo.InvariantCulture)));
            yield return $"DELETE FROM spawnpool WHERE id IN ({pools});";
            yield return $"DELETE FROM creature_appearance WHERE id = {Machina};";
            yield return $"DELETE FROM creature_stat WHERE id IN ({Nitroglazer}, {Machina});";
            yield return $"DELETE FROM creature WHERE id IN ({Nitroglazer}, {Machina});";
            yield return $"DELETE FROM creature_action WHERE id IN ({NitroWeapon}, {MachinaWeapon}, {NitroMortar});";
        }
    }
}
