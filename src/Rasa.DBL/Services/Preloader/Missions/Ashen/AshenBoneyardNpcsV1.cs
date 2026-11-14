using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// The Ashen Desert set 4 NPCs and creatures, Ligo Ashen Desert (map 1734), the Boneyard and Boneyard Outpost, for Lieutenant
    /// Jan Blood's Xanxless Job (1262), Captain Akio Yamamoto's Fallen Soldiers (1263) and Lieutenant Gabriel Infanger's Stalking
    /// the Stalkers (1272): Infanger new at Boneyard Outpost; Xanx pools in the Boneyard, where the World DB has none; the Desert
    /// Stalker, new, in two pools north-west of the Boneyard's middle.
    ///
    /// Ids (the Ashen block 790000-790999, AshenLeadInNpcsV1; set 4 790120-790159):
    ///  - 790120 Infanger (creature, creature_appearance, creature_stat, npc_package, spawn pool);
    ///  - 790121 the Desert Stalker (creature, creature_stat);
    ///  - 790122-790124 the Xanx pools; 790125, 790126 the Desert Stalker pools.
    ///
    /// Sourced:
    ///  - Names (client creaturenamelanguage, 1.0): 9304 "Lieutenant Gabriel Infanger", 9321 "Class XI Desert Stalker", 471 "Xanx
    ///    Burrower" (World DB 540020's), 9305 "Lieutenant Jan Blood" (World DB 510013's). The client gives the three missions no
    ///    objective conversation: Infanger has a server-only package (his id), Blood none (he gives and takes 1262 by his id).
    ///  - Places: Infanger at "Boneyard Outpost" (TaRapedia Stalking the Stalkers, 0.5; no coordinates); Blood stays at the World
    ///    DB's place (682.4, 289.56, -516.4), the Boneyard Outpost region centre (World DB map_region r77). The Xanx "out there
    ///    ... in the Boneyard" (the client's 1262 log and offer; World DB region The Boneyard (579, 283, -291) r200). The Stalker
    ///    "just northwest of the boneyard at 595, -354" (TaRapedia Stalking the Stalkers); "Travel into the Boneyard and destroy a
    ///    Desert Stalker" (the client's 1272 log).
    ///  - The World DB rows used: Xanx - Ashen Desert 540020 (class 7510 Creature_Xanx_Standard, L39, the Xanx of Maji Field and
    ///    Indra Pass); Stalker Overlord - Plains 520073 (class 3781 Vehicle_Bane_Stalker, L40, 4500 hit points, armour 400, its
    ///    attacks), the World DB's Stalker in the zone band.
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Infanger a copy of Mason Linier's row and look (AFS class 3846), as set 1's Ivan; level 40 (no source: the missions'
    ///    band); 4500 hit points and armour 300, friendly, standing still, rotation 0. His place: the outpost's open ground west of
    ///    Yamamoto's tent, 13 m from Blood, on the client's floor and the server navmesh, 3.5 m from a wall, one floor in the
    ///    column (probe of 2026-11-09).
    ///  - The Desert Stalker a copy of 520073 (its level 40, the zone band's floor) with the client name 9321.
    ///  - Pools of three Xanx (540020), radius 0 (rings, as set 2's Atta), 120 s respawn, in the open middle and north of the
    ///    Boneyard, 40 m and more apart; two pools of one Stalker, radius 0, 120 s respawn, 17.5 m and 18.9 m from TaRapedia's spot
    ///    (its column is two floors: the Boneyard's at y 281.5 and Ashoka's lower level at y 248.7 under it), each where the
    ///    client has one floor in the column. Each pool's point the navmesh ground at its centre, 25 m or more from a wall,
    ///    walked to from the Ashoka Settlement waypoint (probe of 2026-11-09).
    ///
    /// Plain SQL, valid on SQLite and MySQL (the copies are INSERT ... SELECT); Down takes out only these rows. No upstream row
    /// changes.
    /// </summary>
    public static class AshenBoneyardNpcsV1
    {
        public const uint MapContextId = AshenMissionDataV1.MapContextId;
        public const uint HitPoints = AshenLeadInNpcsV1.HitPoints, Armor = AshenLeadInNpcsV1.Armor, Friendly = 1;

        /// <summary>Lieutenant Jan Blood (World DB 510013, Boneyard Outpost), the giver of 1262: used where he stands.</summary>
        public const uint Blood = 510013;

        public const uint Infanger = 790120, InfangerName = 9304, InfangerPackage = 790120, InfangerLevel = 40;
        public static readonly (double X, double Y, double Z) InfangerAt = (676, 289.65, -505);

        /// <summary>The Xanx of the Boneyard (World DB): Xanx - Ashen Desert, "Xanx Burrower".</summary>
        public const uint Xanx = 540020;

        /// <summary>The Desert Stalker: id, name, the row it copies.</summary>
        public const uint Stalker = 790121, StalkerName = 9321, StalkerCopy = 520073;

        /// <summary>Radius 0: rings round the pool's point (SpawnPoolManager.SpawnPoint), as set 2's Atta pools.</summary>
        public const uint XanxPerPool = 3, RespawnTenths = 1200, PoolRadius = 0;

        /// <summary>The Xanx pools: pool, centre.</summary>
        public static readonly (uint Pool, (double X, double Y, double Z) At)[] XanxPools =
        {
            (790122, (560, 283.41, -250)),
            (790123, (600, 282.49, -230)),
            (790124, (640, 282.85, -250))
        };

        /// <summary>The Desert Stalker pools, one each: pool, centre.</summary>
        public static readonly (uint Pool, (double X, double Y, double Z) At)[] StalkerPools =
        {
            (790125, (580, 282.85, -345)),
            (790126, (585, 283.58, -370))
        };

        public const uint NextFreeId = 790127;

        public static IEnumerable<string> Up()
        {
            yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                         "action1, action2, action3, action4, action5, action6, action7, action8) " +
                         $"SELECT {Infanger}, {S("Lieutenant Gabriel Infanger - Ashen Desert, Boneyard Outpost (1272)")}, class_id, {Friendly}, " +
                         $"{InfangerLevel}, {HitPoints}, {InfangerName}, 0, 0, " +
                         $"action1, action2, action3, action4, action5, action6, action7, action8 FROM creature WHERE id = {AshenLeadInNpcsV1.MasonLinier};";
            yield return "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                         $"SELECT {Infanger}, slot_id, Class_id, color FROM creature_appearance WHERE id = {AshenLeadInNpcsV1.MasonLinier};";
            yield return Stat(Infanger, HitPoints, Armor);
            yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({Infanger}, {InfangerPackage}, {S("Lieutenant Gabriel Infanger (1272)")});";
            yield return Pool(Infanger, InfangerAt.X, InfangerAt.Y, InfangerAt.Z, MapContextId, Infanger, 1, 1, 20, 0);

            yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                         "action1, action2, action3, action4, action5, action6, action7, action8) " +
                         $"SELECT {Stalker}, {S("Desert Stalker - Ashen Desert, the Boneyard (1272)")}, class_id, faction, level, max_hp, {StalkerName}, " +
                         $"run_speed, walk_speed, action1, action2, action3, action4, action5, action6, action7, action8 FROM creature WHERE id = {StalkerCopy};";
            yield return "INSERT INTO creature_stat (id, body, mind, spirit, health, armor) " +
                         $"SELECT {Stalker}, body, mind, spirit, health, armor FROM creature_stat WHERE id = {StalkerCopy};";

            foreach (var (pool, at) in XanxPools)
                yield return Pool(pool, at.X, at.Y, at.Z, MapContextId, Xanx, XanxPerPool, XanxPerPool, RespawnTenths, PoolRadius);
            foreach (var (pool, at) in StalkerPools)
                yield return Pool(pool, at.X, at.Y, at.Z, MapContextId, Stalker, 1, 1, RespawnTenths, PoolRadius);
        }

        public static IEnumerable<string> Down()
        {
            var pools = string.Join(", ", XanxPools.Select(pool => pool.Pool).Concat(StalkerPools.Select(pool => pool.Pool))
                .Select(id => id.ToString(CultureInfo.InvariantCulture)));
            yield return $"DELETE FROM spawnpool WHERE id IN ({pools});";
            yield return $"DELETE FROM creature_stat WHERE id = {Stalker};";
            yield return $"DELETE FROM creature WHERE id = {Stalker};";
            yield return $"DELETE FROM spawnpool WHERE id = {Infanger};";
            yield return $"DELETE FROM npc_package WHERE id = {Infanger};";
            yield return $"DELETE FROM creature_stat WHERE id = {Infanger};";
            yield return $"DELETE FROM creature_appearance WHERE id = {Infanger};";
            yield return $"DELETE FROM creature WHERE id = {Infanger};";
        }

        private static string S(string value) => "'" + value.Replace("'", "''") + "'";
    }
}
