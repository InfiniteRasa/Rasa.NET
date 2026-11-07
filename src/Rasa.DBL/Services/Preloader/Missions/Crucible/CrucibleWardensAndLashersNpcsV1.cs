using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// The Crucible set 3 NPCs and creatures, Ligo Crucible (map 1993), for Game Wardens (1147), Earning Respect (1170),
    /// Business Proposal (1229) and Whiplash (1157): the new Medical Technician Halprin (1157) at Outpost Intrepid, and the
    /// two creature families the server had on no Crucible pool: Warden Bots near the Chaukas Robotics Facility (1147's
    /// parts) and Lashers north-east of the Awol Camp (1157's venom glands).
    ///
    /// Ids (the Crucible block 770000-770999, CrucibleOutpostIntrepidNpcsV1; sets 1 and 2 used 770000-770005):
    ///  - 770006 Halprin (creature, creature_appearance, creature_stat, npc_package, spawn pool);
    ///  - 770007 the Warden Bot (creature, creature_stat), in pools 770009-770011;
    ///  - 770008 the Lasher (creature, creature_stat, and its attack: creature_action 770008), in pools 770012-770013.
    ///
    /// Sourced:
    ///  - Names (client creaturenamelanguage, 1.0): 9114 "Medical Technician Halprin"; the Warden Bot ranks 8042-8053
    ///    ("Warden Bot Watchdog" ... "Governor"), of which the World DB has 8046 Jailor at level 38 (531044, Incline) and 8047
    ///    Claviger at 41 and 43 (540001 Thunderhead, 531023 the Chaukas Robotics Facility). No client name is "Lasher": the
    ///    client draws the class's own name (physicalentityclassnamelanguage 7477 "Lasher").
    ///  - Classes (client entityclass, 1.0): 6961 Warden Bot, 7477 Creature_Lasher (creature flag 86, Lasher: World DB
    ///    creature_class_flag, so Targets of Opportunity 1589 counts them). The Lasher's weapon 7476 Weapon_Creature_Lasher
    ///    attacks with action 174 argument 28 (client weaponclass), 2100 ms between attacks (client actionArguments 174,28).
    ///  - Places: Halprin "Outpost Intrepid, Medical", 995, 144, 108, level 38 (TaRapedia NPC page, 0.5). The Warden Bots:
    ///    "hostile bots patrolling the area near the Warden Bot Facility" (client 1147 log); the facility's door is map link
    ///    98 to the Chaukas Robotics Facility (-528.6, 249.4, 581.6, World DB). The Lashers: Defiler Sedge walks the trench
    ///    north-east of the Awol Camp, "possibly fighting the lashers by there", near the Seeds logos (-737, 298, 389)
    ///    (TaRapedia Defiler Sedge and Designer Genes, 0.3).
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Halprin a copy of Captain DeLisi's row and look (AFS class 3846), as Shandy; level 38 (TaRapedia); 4500 hit points
    ///    and armour 300; a server-only package (his id), as Shandy; friendly, standing still.
    ///  - The Warden Bot a copy of the Jailor row 531044 (class 6961, its laser and shock attacks, WorldCreaturePreloader, and its death
    ///    blast 60002, Wire_creature_bombs) at level 35, the
    ///    level of 1147, with the rank one under the Jailor, 8045 "Warden Bot Screw" (the World DB has Jailor 38, Claviger
    ///    41: one rank in about three levels); 4970 hit points and armour 247, the Jailor-to-Claviger rise taken back three
    ///    levels (7071 at 38, 9170 at 41; armour 270 at 38, 308 at 43).
    ///  - The Lasher: a new row of class 7477 (no Lasher row is on any map) at level 40, the level of 1157, with 8470 hit
    ///    points and armour 285 (the Warden Bot line at 40), name 0; one melee attack, 174/28, reach 0.5-4 m, damage
    ///    557-779 (the Crucible Flaregasher's melee, 599-837 at level 43, taken to 40), damage_type 0 so that the client's
    ///    weapon table gives its kind: Virulent (CreatureWeaponDamage 174/28).
    ///  - Pools: three of three Warden Bots 35-54 m round the facility's door, two of three Lashers 11-20 m from the probe's
    ///    centre between the Seeds logos and Molitor's road; 120 s respawn (the Crucible pools' 1200 tenths), radius 6. Every
    ///    place (Halprin and the pools' centres) is the nearest point on the server navmesh and the client floor, at least 2 m
    ///    (pools 3 m) from a wall, nothing over the head, seen from 16/16 places 2 m round (one pool 15/16), walked to from
    ///    the area's waypoint (Halprin: Outpost Intrepid; the pools: the Labbna Quarter and the Awol Camp), probe of
    ///    2026-11-07. The pools stand 120 m or more from every friendly NPC and 35 m or more from the door's map link.
    ///    Halprin stands 3 m from the hospital's spawn point and 30 m from the nearest NPC.
    ///
    /// Plain SQL, valid on SQLite and MySQL; Down takes out only these rows.
    /// </summary>
    public static class CrucibleWardensAndLashersNpcsV1
    {
        public const uint MapContextId = CrucibleMissionDataV1.MapContextId;
        public const uint HitPoints = CrucibleOutpostIntrepidNpcsV1.HitPoints, Armor = CrucibleOutpostIntrepidNpcsV1.Armor;
        public const uint Friendly = 1;

        public const uint Halprin = 770006, HalprinName = 9114, HalprinPackage = 770006, HalprinLevel = 38;
        public static readonly (double X, double Y, double Z) HalprinAt = (989, 144.92, 108);

        /// <summary>The Warden Bot: a copy of the Jailor 531044, one rank under it.</summary>
        public const uint WardenBot = 770007, WardenBotName = 8045, WardenBotClass = 6961, Jailor = 531044;
        public const uint WardenBotLevel = 35, WardenBotHitPoints = 4970, WardenBotArmor = 247;

        /// <summary>The Lasher and its one attack (creature_action 770008).</summary>
        public const uint Lasher = 770008, LasherClass = 7477, LasherLevel = 40, LasherHitPoints = 8470, LasherArmor = 285;
        public const uint LasherAttack = 770008, MeleeAction = 174, LasherMelee = 28, LasherCooldown = 2100;
        /// <summary>damage_type 0: the client's weapon table gives 174/28 its kind, Virulent (CreatureWeaponDamage, CreatureAttacks.DamageTypeOf).</summary>
        public const uint LasherDamageMin = 557, LasherDamageMax = 779, WeaponTableDamage = 0;
        public const double LasherReachMin = 0.5, LasherReachMax = 4;

        public const uint NextFreeId = 770014;

        public const uint PoolCreatures = 3, RespawnTenths = 1200, PoolRadius = 6;

        /// <summary>The Warden Bot pools round the Chaukas Robotics Facility's door (map link 98).</summary>
        public static readonly (uint Pool, (double X, double Y, double Z) At)[] WardenPools =
        {
            (770009, (-528.6, 249.55, 546.6)), (770010, (-493.6, 250.21, 581.6)), (770011, (-578.6, 249.3, 561.6))
        };

        /// <summary>The Lasher nest north-east of the Awol Camp.</summary>
        public static readonly (uint Pool, (double X, double Y, double Z) At)[] LasherPools =
        {
            (770012, (-785, 284.29, 390)), (770013, (-790, 283.89, 420))
        };

        /// <summary>The new NPCs: id, comment, the row they copy, name, level, package, place.</summary>
        public static readonly (uint Id, string Comment, uint Copy, uint Name, uint Level, uint Package, (double X, double Y, double Z) At)[] New =
        {
            (Halprin, "Medical Technician Halprin - Outpost Intrepid (1157)", CrucibleOutpostIntrepidNpcsV1.DeLisi, HalprinName,
                HalprinLevel, HalprinPackage, HalprinAt)
        };

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
                yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({id}, {package}, {S(comment)});";
                yield return Pool(id, at.X, at.Y, at.Z, MapContextId, id, 1, 1, 20, 0);
            }

            yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                         "action1, action2, action3, action4, action5, action6, action7, action8) " +
                         $"SELECT {WardenBot}, 'Warden Bot Screw - Crucible, Chaukas Robotics Facility door (1147)', class_id, faction, " +
                         $"{WardenBotLevel}, {WardenBotHitPoints}, {WardenBotName}, run_speed, walk_speed, action1, action2, action3, " +
                         $"action4, action5, action6, action7, action8 FROM creature WHERE id = {Jailor};";
            yield return Stat(WardenBot, WardenBotHitPoints, WardenBotArmor);

            yield return Action(LasherAttack, "Lasher Crucible lasher_melee", MeleeAction, LasherMelee, LasherReachMin, LasherReachMax,
                LasherCooldown, 0, LasherDamageMin, LasherDamageMax, WeaponTableDamage);
            yield return Creature(Lasher, "Lasher - Crucible, north-east of the Awol Camp (1157)", LasherClass, LasherLevel,
                LasherHitPoints, 0, LasherAttack);
            yield return Stat(Lasher, LasherHitPoints, LasherArmor);

            foreach (var (pool, at) in WardenPools)
                yield return Pool(pool, at.X, at.Y, at.Z, MapContextId, WardenBot, PoolCreatures, PoolCreatures, RespawnTenths, PoolRadius);
            foreach (var (pool, at) in LasherPools)
                yield return Pool(pool, at.X, at.Y, at.Z, MapContextId, Lasher, PoolCreatures, PoolCreatures, RespawnTenths, PoolRadius);
        }

        public static IEnumerable<string> Down()
        {
            var pools = string.Join(", ", WardenPools.Concat(LasherPools).Select(pool => pool.Pool.ToString(CultureInfo.InvariantCulture)));
            yield return $"DELETE FROM spawnpool WHERE id IN ({pools});";
            yield return $"DELETE FROM creature_stat WHERE id IN ({WardenBot}, {Lasher});";
            yield return $"DELETE FROM creature WHERE id IN ({WardenBot}, {Lasher});";
            yield return $"DELETE FROM creature_action WHERE id = {LasherAttack};";
            foreach (var (id, _, _, _, _, _, _) in New.Reverse())
            {
                yield return $"DELETE FROM spawnpool WHERE id = {id};";
                yield return $"DELETE FROM npc_package WHERE id = {id};";
                yield return $"DELETE FROM creature_stat WHERE id = {id};";
                yield return $"DELETE FROM creature_appearance WHERE id = {id};";
                yield return $"DELETE FROM creature WHERE id = {id};";
            }
        }

        private static string S(string value) => "'" + value.Replace("'", "''") + "'";
    }
}
