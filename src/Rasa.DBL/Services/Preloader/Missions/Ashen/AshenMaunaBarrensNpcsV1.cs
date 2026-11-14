using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// The Ashen Desert set 2 NPCs and creatures, Ligo Ashen Desert (map 1734), for Lieutenant Simon Kuang's Angry Angry Atta
    /// (1242), Harmful Mutations (1243) and Chemical Oversight (1244), and Captain Sera Karter's Eyes on the Ground (1245):
    /// Kuang, Doctor Jung Kwon and Karter new at Shadow's Edge Post; the Atta the server had on no Ashen Desert pool (Ashen
    /// Atta Soldiers, Ashen Atta Workers and Mutated Atta Soldiers) in six pools in the Mauna Barrens; and Thrax Overseer Exing's
    /// pool moved off the navmesh island it stood on.
    ///
    /// Ids (the Ashen block 790000-790999, AshenLeadInNpcsV1; set 2 790040-790079):
    ///  - 790040 Kuang, 790041 Kwon, 790042 Karter (creature, creature_appearance, creature_stat, npc_package, spawn pool);
    ///  - 790043 the Ashen Atta Soldier, 790044 the Ashen Atta Worker, 790045 the Mutated Atta Soldier (creature, creature_stat);
    ///  - 790046-790051 their pools.
    ///
    /// Sourced:
    ///  - Names (client creaturenamelanguage, 1.0): 9291 "Lieutenant Simon Kuang", 9298 "Doctor Jung Kwon", 9281 "Captain
    ///    Sera Karter", 9318 "Ashen Atta Soldier", 9319 "Ashen Atta Worker", 9320 "Mutated Atta Soldier". The client gives
    ///    the four missions no objective conversation: Kuang, Kwon and Karter have server-only packages (their ids).
    ///  - Places: Kuang, Kwon and Karter at "Shadow's Edge Post" (TaRapedia's mission pages and the post's mission list, 0.5;
    ///    no coordinates); Kwon is female ("she's", the client's 1243 offer); the Atta in the Mauna Barrens (the client's 1242,
    ///    1243 and 1245 logs; World DB region Mauna Barrens (-469, 302, -136) r190). Exing: World DB pool 520007 (-281, 340.4,
    ///    -252) (Ellatha's -281, -252 is the same map value; TaRapedia 1244 "-252.9, -241.6", west of the Sheitan Highway and
    ///    south of Mauna Barrens).
    ///  - The Atta rows: the World DB's Atta at the zone's levels, class 7081 Creature_Atta_Soldier_Standard and 7078
    ///    Creature_Atta_Harvester_Standard (client entityclass; the Atta species flag 82 on both, creature_class_flag, so
    ///    Targets of Opportunity 1580's "50 Atta" counts them): Atta Soldier - Ashen Desert 531015 (L40, 8409 hit points, armour
    ///    285; Avernus Outpost), Atta Harvester - Thunderhead 531099 (L40, 6307, 190), Atta Soldier - Thunderhead 531098 (L41,
    ///    9170, 292).
    ///
    /// Upstream data fixed here (NOTES-upstream-bugs): Exing's pool 520007 stands on a navmesh island on the mesa top west of
    /// the Sheitan Highway; no walk reaches it from any waypoint (probe of 2026-11-09). Moved 14 m south-west, down the mesa's
    /// slope to the nearest ground a player walks to from the Stonewall Ridge and Shadow's Edge Post waypoints.
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Kuang a copy of Mason Linier's row and look (AFS class 3846), Kwon and Karter of Ida Hayner's (the female AFS class
    ///    3848), as set 1. Kuang and Kwon level 40 (no source: the missions' level), Karter 48 (TaRapedia's Captain Sera Karter
    ///    (Avernus Outpost), "the same contact"). 4500 hit points and armour 300, friendly, standing still, rotation 0.
    ///  - The Ashen Atta Soldier a copy of 531015 with the client name 9318; the Ashen Atta Worker a copy of 531099 (the World
    ///    DB's Atta harvester at level 40, the zone band's floor; the Ashen 531016 is 39) with 9319 (the client's "Worker" for the
    ///    harvester class: 1287's "Ashen Atta Harvesters", MS10 10.4); the Mutated Atta Soldier a copy of 531098 (level 41) with
    ///    9320. No look rows (the World DB's Atta have none).
    ///  - Pools: six of three creatures (two of each kind), radius 0 (PoolRadius), 120 s respawn (the Ashen pools' 1200 tenths), in the
    ///    open south basin of the Mauna Barrens, 40 m and more apart, 65 m and more from the Barrens' Fithik and Treemite pools,
    ///    each the navmesh ground at its centre, 30 m from a wall, the ground 7 m round it on the navmesh, walked to from the Shadow's
    ///    Edge Post and White Oasis Post waypoints (probe of 2026-11-09).
    ///  - Places of the NPCs, by the statics of the client map: Kuang at the post's west gate under the "Mauna Barrens"
    ///    holosign (-228.8, 296.5, -736.5), the way to his Atta; Kwon by the hospital (-93.3, 296, -825.4); Karter by the
    ///    dropship transport pad (-130, 294.5, -748), where new arrivals come in ("You're Captain Karter's problem", TaRapedia Get
    ///    Out and Stay Out). Each on the client's floor and the server navmesh, 2.5 m or more from a wall, nothing over the head,
    ///    seen from 16 of 16 places 2 m round it and from the end of the walk to it from the Shadow's Edge Post waypoint, 8 m or
    ///    more from every waypoint and teleporter, 22 m or more outside every hostile pool's circle (probe of 2026-11-09).
    ///
    /// Plain SQL, valid on SQLite and MySQL (the copies are INSERT ... SELECT); Down takes out only these rows and puts Exing
    /// back where upstream had him.
    /// </summary>
    public static class AshenMaunaBarrensNpcsV1
    {
        public const uint MapContextId = AshenMissionDataV1.MapContextId;
        public const uint HitPoints = AshenLeadInNpcsV1.HitPoints, Armor = AshenLeadInNpcsV1.Armor, Friendly = 1;

        public const uint Kuang = 790040, KuangName = 9291, KuangPackage = 790040, KuangLevel = 40;
        public const uint Kwon = 790041, KwonName = 9298, KwonPackage = 790041, KwonLevel = 40;
        public const uint Karter = 790042, KarterName = 9281, KarterPackage = 790042, KarterLevel = 48;

        public static readonly (double X, double Y, double Z) KuangAt = (-205, 297.54, -736);
        public static readonly (double X, double Y, double Z) KwonAt = (-89, 296.78, -822);
        public static readonly (double X, double Y, double Z) KarterAt = (-112, 296.19, -748);

        /// <summary>The Atta: id, name, the World DB row it copies, class.</summary>
        public const uint AttaSoldier = 790043, AttaSoldierName = 9318, AttaSoldierCopy = 531015;
        public const uint AttaWorker = 790044, AttaWorkerName = 9319, AttaWorkerCopy = 531099;
        public const uint MutatedSoldier = 790045, MutatedSoldierName = 9320, MutatedSoldierCopy = 531098;
        public const uint SoldierClass = 7081, WorkerClass = 7078;
        public const uint AttaFlag = 82;                // CreatureFlag Atta (creature_class_flag; TargetsOfOpportunitySeed Flag.Atta)

        /// <summary>
        /// Radius 0: the three stand on rings round the pool's point (SpawnPoolManager.SpawnPoint). With a radius the navmesh's
        /// random point comes from any polygon the circle touches, and the basin's polygons are large: radius 8 put them up to
        /// 60 m off (probe of 2026-11-09, 4000 draws a pool).
        /// </summary>
        public const uint PoolCreatures = 3, RespawnTenths = 1200, PoolRadius = 0;

        /// <summary>The Atta pools in the Mauna Barrens: pool, creature, centre.</summary>
        public static readonly (uint Pool, uint Creature, (double X, double Y, double Z) At)[] AttaPools =
        {
            (790046, AttaSoldier, (-493, 295.61, -240)),
            (790047, AttaSoldier, (-429, 300.18, -240)),
            (790048, AttaWorker, (-461, 298.32, -216)),
            (790049, AttaWorker, (-461, 297.3, -264)),
            (790050, MutatedSoldier, (-533, 302.79, -256)),
            (790051, MutatedSoldier, (-405, 300.97, -272))
        };

        public const uint NextFreeId = 790052;

        /// <summary>Thrax Overseer Exing's pool (World DB), the place before and the place now.</summary>
        public const uint Exing = 520007;
        public static readonly (double X, double Y, double Z) ExingBefore = (-281.0, 340.3999938964844, -252.0);
        public static readonly (double X, double Y, double Z) ExingAt = (-289, 332.98, -264);

        /// <summary>The new NPCs: id, comment, the row they copy, name, level, package, place.</summary>
        public static readonly (uint Id, string Comment, uint Copy, uint Name, uint Level, uint Package, (double X, double Y, double Z) At)[] New =
        {
            (Kuang, "Lieutenant Simon Kuang - Ashen Desert, Shadow's Edge Post (1242)", AshenLeadInNpcsV1.MasonLinier, KuangName, KuangLevel,
                KuangPackage, KuangAt),
            (Kwon, "Doctor Jung Kwon - Ashen Desert, Shadow's Edge Post (1243)", AshenLeadInNpcsV1.IdaHayner, KwonName, KwonLevel, KwonPackage, KwonAt),
            (Karter, "Captain Sera Karter - Ashen Desert, Shadow's Edge Post (1245)", AshenLeadInNpcsV1.IdaHayner, KarterName, KarterLevel,
                KarterPackage, KarterAt)
        };

        /// <summary>The new Atta: id, comment, the row they copy, name.</summary>
        public static readonly (uint Id, string Comment, uint Copy, uint Name)[] Atta =
        {
            (AttaSoldier, "Ashen Atta Soldier - Ashen Desert, Mauna Barrens (1242)", AttaSoldierCopy, AttaSoldierName),
            (AttaWorker, "Ashen Atta Worker - Ashen Desert, Mauna Barrens (1242)", AttaWorkerCopy, AttaWorkerName),
            (MutatedSoldier, "Mutated Atta Soldier - Ashen Desert, Mauna Barrens (1243)", MutatedSoldierCopy, MutatedSoldierName)
        };

        /// <summary>Where an NPC of set 2 stands.</summary>
        public static (double X, double Y, double Z) At(uint pool) => New.Single(npc => npc.Id == pool).At;

        public static IEnumerable<string> Up()
        {
            foreach (var (id, comment, copy, name, level, package, at) in New)
            {
                yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                             "action1, action2, action3, action4, action5, action6, action7, action8) " +
                             $"SELECT {id}, {S(comment)}, class_id, {Friendly}, {level}, {HitPoints}, {name}, 0, 0, " +
                             $"action1, action2, action3, action4, action5, action6, action7, action8 FROM creature WHERE id = {copy};";
                yield return "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                             $"SELECT {id}, slot_id, Class_id, color FROM creature_appearance WHERE id = {copy};";
                yield return Stat(id, HitPoints, Armor);
                yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({id}, {package}, {S(PackageComment(comment))});";
                yield return Pool(id, at.X, at.Y, at.Z, MapContextId, id, 1, 1, 20, 0);
            }

            foreach (var (id, comment, copy, name) in Atta)
            {
                yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                             "action1, action2, action3, action4, action5, action6, action7, action8) " +
                             $"SELECT {id}, {S(comment)}, class_id, faction, level, max_hp, {name}, run_speed, walk_speed, " +
                             $"action1, action2, action3, action4, action5, action6, action7, action8 FROM creature WHERE id = {copy};";
                yield return "INSERT INTO creature_stat (id, body, mind, spirit, health, armor) " +
                             $"SELECT {id}, body, mind, spirit, health, armor FROM creature_stat WHERE id = {copy};";
            }
            foreach (var (pool, creature, at) in AttaPools)
                yield return Pool(pool, at.X, at.Y, at.Z, MapContextId, creature, PoolCreatures, PoolCreatures, RespawnTenths, PoolRadius);

            yield return Place(Exing, ExingAt);
        }

        public static IEnumerable<string> Down()
        {
            // Only while Exing still stands where set 2 put him: a later move of his pool is not undone.
            yield return Place(Exing, ExingBefore).TrimEnd(';') +
                         $" AND ABS(pos_x - {N(ExingAt.X)}) < 0.01 AND ABS(pos_y - {N(ExingAt.Y)}) < 0.01 AND ABS(pos_z - {N(ExingAt.Z)}) < 0.01;";
            var pools = string.Join(", ", AttaPools.Select(pool => pool.Pool.ToString(CultureInfo.InvariantCulture)));
            var atta = string.Join(", ", Atta.Select(row => row.Id.ToString(CultureInfo.InvariantCulture)));
            yield return $"DELETE FROM spawnpool WHERE id IN ({pools});";
            yield return $"DELETE FROM creature_stat WHERE id IN ({atta});";
            yield return $"DELETE FROM creature WHERE id IN ({atta});";
            foreach (var (id, _, _, _, _, _, _) in New.Reverse())
            {
                yield return $"DELETE FROM spawnpool WHERE id = {id};";
                yield return $"DELETE FROM npc_package WHERE id = {id};";
                yield return $"DELETE FROM creature_stat WHERE id = {id};";
                yield return $"DELETE FROM creature_appearance WHERE id = {id};";
                yield return $"DELETE FROM creature WHERE id = {id};";
            }
        }

        private static string Place(uint pool, (double X, double Y, double Z) at) =>
            $"UPDATE spawnpool SET pos_x = {N(at.X)}, pos_y = {N(at.Y)}, pos_z = {N(at.Z)} WHERE id = {pool};";

        /// <summary>npc_package.comment is varchar(50) on MySQL: the name and the mission ("Lieutenant Simon Kuang (1242)").</summary>
        private static string PackageComment(string comment) => comment.Substring(0, comment.IndexOf(" - ")) +
                                                                 comment.Substring(comment.LastIndexOf(" ("));

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string S(string value) => "'" + value.Replace("'", "''") + "'";
    }
}
