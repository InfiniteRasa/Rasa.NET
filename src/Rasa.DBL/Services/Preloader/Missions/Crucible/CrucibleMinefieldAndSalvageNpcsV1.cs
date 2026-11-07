using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// The Crucible set 4 creatures, Ligo Crucible (map 1993), for Minesweeper (1232), Funeral Derge (1241), Salvage Duty
    /// (1203) and More Salvage Duty (1204): the Bane Smart Mines in the trench west of the Fort Intrepid ruins, the
    /// Reconstructor Bots by the Chaukas Robotics Facility, and upstream's Executioner Derge moved from the Fort Intrepid
    /// control point flag onto the ramparts.
    ///
    /// Ids (the Crucible block 770000-770999, CrucibleOutpostIntrepidNpcsV1; sets 1-3 used 770000-770013):
    ///  - 770014 the Smart Mine (creature, creature_stat; its two actions creature_action 770014 and 770016), in pools
    ///    770014-770016;
    ///  - 770015 the Reconstructor Bot (creature, creature_stat; its attack creature_action 770015), in pools 770017-770018.
    ///
    /// Sourced:
    ///  - Client 1232 texts 9808/9809 (1.0): "Bane Smart Mines near the trenches due west of the Fort Intrepid ruins"; "there's
    ///    no way to reach Staal without passing through the area"; "They stay buried in the sand until they detect a lifeform
    ///    that isn't Bane, then they pop straight up in the air and detonate. When one pops up, you'll have a few seconds to
    ///    shoot it". TaRapedia Smart Mine redirects to Necromite ("often referenced as 'Bane Smart Mines'"; they follow the
    ///    player, slow, a small virulent DOT); TaRapedia Minesweeper walkthrough: "The mines don't actually explode and they
    ///    do very little damage" (0.5). The client has no "Smart Mine" name or class: creaturenamelanguage 433 "Necromite",
    ///    classes 6029 and 7528 (physicalentityclassnamelanguage "Necromite"). The World DB has one Necromite, the Thrax's
    ///    summon 570003 (class 7528, weapon 1/109 and necromite_self_destruct 489, CreatureBombs).
    ///  - Client 1203/1204 texts 9435/9455 (1.0): the parts come from "Reconstructor and Warden Bots". Client entityclass 6960
    ///    Creature_Prison_Bot_Reconstructor (physicalentityclassnamelanguage "Reconstructor Bot"), its weapon 7189
    ///    Weapon_Creature_Arieki_Bot_Reconstructor attacks with action 1 argument 152 (client weaponclass), 1500 ms, 40 m
    ///    (client actionArguments 1,152). Names 8030-8041 "Reconstructor Bot Drudge" ... "Architect" (the Warden Bot ranks'
    ///    order). No Reconstructor row is on any map (World DB). 1202 (set 5): Reconstructor Bots "in the vicinity of the
    ///    Chaukas Robotics Facility" (client).
    ///  - Derge: upstream row 520013 (class 10504, level 47, BossCreaturePreloader) in pool 520013 at the Fort Intrepid control
    ///    point flag (757.1, 171.1, 549.1, BossSpawnpoolPreloader), on the same place as the Thrax Commander 520014 and 27 m
    ///    from the control point's hospital. TaRapedia Funeral Derge and Executioner Derge: he patrols the ramparts, reached by
    ///    the tower stairs, "not the ruins grounds"; Ellatha: "Patrols around Fort Intrepid ruins walls, use stairs to get up
    ///    top"; IGN / patch 1.4: "he spawns at 859 188 476 which is on top of a very tall wall" (0.5).
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - The Smart Mine a copy of the Necromite 570003 (class 7528, name 433) at level 40 (the level of 1232) with 1500 hit
    ///    points and armour 150 (a mine "shot out of the air"); its weapon 1/109 and its self-destruct 489 (the client's
    ///    "detonate") at about a quarter of the Necromite's damage (TaRapedia "very little damage"): 80-120 and 150-300, damage_type 0
    ///    so that the client decides the kind (CreatureWeaponDamage 1/109 Physical; the ability data for 489). No corpse
    ///    explosion. It stands on the sand: no new code buries it (MawLurk buries only the Maw classes; see the triage
    ///    "Mechanics check").
    ///  - The Reconstructor Bot a new row of class 6960 at level 35 (the level of 1147, the Warden Bot Screw's), name 8033
    ///    "Reconstructor Bot Laborer" (the rank of the Screw, 8045, in the Reconstructor list), 4970 hit points and armour
    ///    247 (the Screw's), one attack 1/152, reach 1-40 m, 1500 ms, damage 77-115 (the Screw's laser per second, 1.5 s),
    ///    damage_type 0 so that the client's weapon table gives its kind.
    ///  - Pools: three of four mines (radius 6) in the trench on the road west from the Fort Intrepid waypoint (95-180 m out),
    ///    two of three Reconstructor Bots (radius 6) west and south of the Warden Bots, 83 m and more from the facility's door;
    ///    120 s respawn. Derge on the rampart at the wall spawn (859, 188.68, 476), walked to from the Fort Intrepid waypoint
    ///    up the stairs. Every place on the server navmesh and a client floor, with a complete path from the area's waypoint
    ///    (probe 2026-11-07).
    ///
    /// Plain SQL, valid on SQLite and MySQL; Down takes out only these rows and puts Derge back.
    /// </summary>
    public static class CrucibleMinefieldAndSalvageNpcsV1
    {
        public const uint MapContextId = CrucibleMissionDataV1.MapContextId;
        public const uint PoolRadius = 6, RespawnTenths = 1200;

        /// <summary>The Smart Mine: a copy of the Necromite (Thrax's) 570003, and its two actions.</summary>
        public const uint SmartMine = 770014, SmartMineClass = 7528, SmartMineName = 433, Necromite = 570003;
        public const uint SmartMineLevel = 40, SmartMineHitPoints = 1500, SmartMineArmor = 150, MinesPerPool = 4;
        public const uint MineWeapon = 770014, MineSelfDestruct = 770016;

        /// <summary>The Reconstructor Bot and its attack.</summary>
        public const uint Reconstructor = 770015, ReconstructorClass = 6960, ReconstructorName = 8033;
        public const uint ReconstructorLevel = 35, ReconstructorHitPoints = 4970, ReconstructorArmor = 247, ReconstructorsPerPool = 3;
        public const uint ReconstructorAttack = 770015, ReconstructorAction = 1, ReconstructorArg = 152, ReconstructorCooldown = 1500;
        public const uint ReconstructorDamageMin = 77, ReconstructorDamageMax = 115;
        public const double ReconstructorReachMin = 1, ReconstructorReachMax = 40;
        /// <summary>damage_type 0: the client's weapon table gives the attack its kind (CreatureAttacks.DamageTypeOf).</summary>
        public const uint WeaponTableDamage = 0;

        /// <summary>Executioner Derge (upstream 520013) and his pool.</summary>
        public const uint Derge = 520013, DergePool = 520013;
        public static readonly (double X, double Y, double Z) DergeBefore = (757.0999755859375, 171.10000610351562, 549.0999755859375);
        public static readonly (double X, double Y, double Z) DergeAt = (859, 188.68, 476);

        public const uint NextFreeId = 770019;

        /// <summary>The Smart Mine pools in the trench west of the Fort Intrepid waypoint.</summary>
        public static readonly (uint Pool, (double X, double Y, double Z) At)[] MinePools =
        {
            (770014, (635, 168.45, 557)), (770015, (585, 168.43, 577)), (770016, (545, 168.46, 587))
        };

        /// <summary>The Reconstructor Bot pools by the Chaukas Robotics Facility (map link 98).</summary>
        public static readonly (uint Pool, (double X, double Y, double Z) At)[] ReconstructorPools =
        {
            (770017, (-617, 249.68, 570)), (770018, (-545, 250.53, 486))
        };

        public static IEnumerable<string> Up()
        {
            yield return Action(MineWeapon, "Smart Mine Crucible weapon 1/109", 1, 109, 0.5, 3, 3000, 0, 80, 120, WeaponTableDamage);
            yield return Action(MineSelfDestruct, "Smart Mine Crucible necromite_self_destruct", 489, 1, 0, 5, 1000, 800, 150, 300, WeaponTableDamage);
            yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                         "action1, action2, action3, action4, action5, action6, action7, action8) " +
                         $"SELECT {SmartMine}, 'Bane Smart Mine - Crucible, west of the Fort Intrepid ruins (1232)', class_id, faction, " +
                         $"{SmartMineLevel}, {SmartMineHitPoints}, {SmartMineName}, run_speed, walk_speed, {MineWeapon}, {MineSelfDestruct}, " +
                         $"0, 0, 0, 0, 0, 0 FROM creature WHERE id = {Necromite};";
            yield return "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                         $"SELECT {SmartMine}, slot_id, Class_id, color FROM creature_appearance WHERE id = {Necromite};";
            yield return Stat(SmartMine, SmartMineHitPoints, SmartMineArmor);

            yield return Action(ReconstructorAttack, "Reconstructor Bot Crucible weapon 1/152", ReconstructorAction, ReconstructorArg,
                ReconstructorReachMin, ReconstructorReachMax, ReconstructorCooldown, 0, ReconstructorDamageMin, ReconstructorDamageMax,
                WeaponTableDamage);
            yield return Creature(Reconstructor, "Reconstructor Bot Laborer - Crucible, Chaukas Robotics Facility (1203, 1204)",
                ReconstructorClass, ReconstructorLevel, ReconstructorHitPoints, ReconstructorName, ReconstructorAttack);
            yield return Stat(Reconstructor, ReconstructorHitPoints, ReconstructorArmor);

            foreach (var (pool, at) in MinePools)
                yield return Pool(pool, at.X, at.Y, at.Z, MapContextId, SmartMine, MinesPerPool, MinesPerPool, RespawnTenths, PoolRadius);
            foreach (var (pool, at) in ReconstructorPools)
                yield return Pool(pool, at.X, at.Y, at.Z, MapContextId, Reconstructor, ReconstructorsPerPool, ReconstructorsPerPool,
                    RespawnTenths, PoolRadius);
            yield return Place(DergePool, DergeAt);
        }

        public static IEnumerable<string> Down()
        {
            yield return Place(DergePool, DergeBefore);
            var pools = string.Join(", ", MinePools.Concat(ReconstructorPools).Select(pool => pool.Pool.ToString(CultureInfo.InvariantCulture)));
            yield return $"DELETE FROM spawnpool WHERE id IN ({pools});";
            yield return $"DELETE FROM creature_stat WHERE id IN ({SmartMine}, {Reconstructor});";
            yield return $"DELETE FROM creature_appearance WHERE id = {SmartMine};";
            yield return $"DELETE FROM creature WHERE id IN ({SmartMine}, {Reconstructor});";
            yield return $"DELETE FROM creature_action WHERE id IN ({MineWeapon}, {ReconstructorAttack}, {MineSelfDestruct});";
        }

        private static string Place(uint pool, (double X, double Y, double Z) at) =>
            $"UPDATE spawnpool SET pos_x = {N(at.X)}, pos_y = {N(at.Y)}, pos_z = {N(at.Z)} WHERE id = {pool};";

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    }
}
