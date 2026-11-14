using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// The Crucible set 8b creatures, for Larai Zupa Madias' Iron Giants (1213) and The Big Kahuna (1216) at Khulago Base, the
    /// Bane base south of Prometheus Outpost: Bane Juggernauts and Maxxum Ugoretz. Who's In Charge Here? (1215) uses only an
    /// object (CrucibleWhosInChargeHereV1).
    ///
    /// Ids (the Crucible block 770000-770999; sets 1-8a used 770000-770040):
    ///  - 770041 the Juggernaut (creature, creature_stat) and its two attacks (creature_action 770041, 770042), in pools
    ///    770041-770043;
    ///  - 770042 Maxxum Ugoretz (creature, creature_stat), in pool 770044.
    ///
    /// Sourced:
    ///  - Names (client, 1.0): physicalentityclassnamelanguage 7722 "Juggernaut" (entityclass 7722 Vehicle_Bane_Juggernaut);
    ///    creaturenamelanguage 9317 "Maxxum Ugoretz". Client 1213 objective "Destroy 6 Juggernauts", counter 9664
    ///    "Juggernauts Destroyed"; 1216 objective "Kill Maxxum Ugoretz." 9803/9804.
    ///  - The Juggernauts at Khulago Base: TaRapedia Iron Giants (0.5); the Juggernauts by the Khulago trench and tunnel
    ///    (TaRapedia, the route of 1899, 0.5).
    ///  - Ugoretz "roaming in front of and to the west of Khulago Base, /loc approx 340 x -130" (TaRapedia Maxxum Ugoretz,
    ///    2008-06-12, 0.5); "patrolling the northwest area of Kahlugo Base" (TaRapedia The Big Kahuna walkthrough, 0.5); one
    ///    of the six Thrax Officers of the Crucible Assassin title (TaRapedia, 0.5).
    ///  - World DB: the Devil's Den Juggernaut 567038 (class 7722, level 20, 2488 hit points, armour 121; attacks 567170 1/180
    ///    1-60 m 1500 ms 72-108, 567171 428/1 1-40 m 8000 ms 162-231), the Devil's Den Thrax (level 20, 1115 hit points,
    ///    armour 81), Khulago Base's Thrax Soldier Mark IX 531018 (level 42, 7500, armour 200), Executioner Derge 520013.
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - The Juggernaut a new row of class 7722, name 0 so that the client shows its class name "Juggernaut" (the mission's
    ///    word; the client's ranked names 10668 Raider, 10676 Slayer, 10688 Controller and 10757 Aggressor have no level band
    ///    in any source), level 42 as Khulago's Thrax. 16700 hit points and armour 300: 567038 to the Devil's Den Thrax as
    ///    this one to 531018. Its two attacks 567170 and 567171 with the damage scaled by the same Thrax ratio (480-720,
    ///    1080-1540), damage_type 0 so that the client's weapon table decides.
    ///  - Three point pools (radius 0) of two Juggernauts each (six to destroy, 120 s respawn) on the open ground in front
    ///    of Khulago Base: J1 (383, 174.41, -185), J2 (440, 175.5, -190), J3 (310, 175.18, -230). Each 7 m or more from a
    ///    wall, no navmesh lava within 80 m (fix job F asks 20 m; probe 2026-11-07), walked to from 1212's north trench marker with no lava, 38 m and more from every upstream
    ///    Thrax pool, 600 m and more from every map link (probe 2026-11-07).
    ///  - Ugoretz a copy of Derge's row (class 10504 Bane_Thrax_Soldier_Rifle_Boss, his six attacks), as Preceptor Molitor:
    ///    level 43, 10905 hit points and armour 308 (the Crucible's level 43 Thrax 531020). One point pool (radius 0) at
    ///    TaRapedia's /loc (340, 165.12, -130) on the navmesh, 50 s respawn (Derge's), alone and standing: the patrol is not
    ///    built. 5.6 m from a wall, no navmesh lava within 80 m (probe 2026-11-07), walked to from 1212's north trench marker with no lava.
    ///
    /// Plain SQL, valid on SQLite and MySQL; Down takes out only these rows.
    /// </summary>
    public static class CruciblePrometheusOutpostNpcsV1
    {
        public const uint MapContextId = CrucibleMissionDataV1.MapContextId;

        /// <summary>The Juggernaut (a new row of the Juggernauts' class) and its two attacks.</summary>
        public const uint Juggernaut = 770041, JuggernautClass = 7722, JuggernautName = 0, JuggernautLevel = 42;
        public const uint JuggernautHitPoints = 16700, JuggernautArmor = 300, JuggernautsPerPool = 2, JuggernautRespawnTenths = 1200;
        public const uint WeaponAttack = 770041, WeaponAction = 1, WeaponArg = 180, WeaponCooldown = 1500, WeaponDamageMin = 480, WeaponDamageMax = 720;
        public const double WeaponReachMin = 1, WeaponReachMax = 60;
        public const uint CannonAttack = 770042, CannonAction = 428, CannonArg = 1, CannonCooldown = 8000, CannonDamageMin = 1080, CannonDamageMax = 1540;
        public const double CannonReachMin = 1, CannonReachMax = 40;
        /// <summary>damage_type 0: the client's weapon table gives the attack its kind (CreatureAttacks.DamageTypeOf).</summary>
        public const uint WeaponTableDamage = 0;

        /// <summary>Maxxum Ugoretz: a copy of Executioner Derge 520013.</summary>
        public const uint Ugoretz = 770042, UgoretzName = 9317, UgoretzLevel = 43, UgoretzHitPoints = 10905, UgoretzArmor = 308;
        public const uint Derge = CrucibleMinefieldAndSalvageNpcsV1.Derge;
        public const uint UgoretzPool = 770044, UgoretzRespawnTenths = 500;
        public static readonly (double X, double Y, double Z) UgoretzAt = (340, 165.12, -130);

        /// <summary>A point pool's radius.</summary>
        public const uint PointRadius = 0;

        public const uint NextFreeId = 770045;

        /// <summary>The Juggernaut pools in front of Khulago Base.</summary>
        public static readonly (uint Pool, (double X, double Y, double Z) At)[] JuggernautPools =
        {
            (770041, (383, 174.41, -185)), (770042, (440, 175.5, -190)), (770043, (310, 175.18, -230))
        };

        public static IEnumerable<string> Up()
        {
            yield return Action(WeaponAttack, "Juggernaut Crucible weapon 1/180", WeaponAction, WeaponArg, WeaponReachMin, WeaponReachMax,
                WeaponCooldown, 0, WeaponDamageMin, WeaponDamageMax, WeaponTableDamage);
            yield return Action(CannonAttack, "Juggernaut Crucible cannon 428/1", CannonAction, CannonArg, CannonReachMin, CannonReachMax,
                CannonCooldown, 0, CannonDamageMin, CannonDamageMax, WeaponTableDamage);
            yield return Creature(Juggernaut, "Juggernaut - Crucible, Khulago Base (1213)", JuggernautClass, JuggernautLevel,
                JuggernautHitPoints, JuggernautName, WeaponAttack, CannonAttack);
            yield return Stat(Juggernaut, JuggernautHitPoints, JuggernautArmor);

            yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                         "action1, action2, action3, action4, action5, action6, action7, action8) " +
                         $"SELECT {Ugoretz}, 'Maxxum Ugoretz - Crucible, west of Khulago Base (1216)', class_id, faction, " +
                         $"{UgoretzLevel}, {UgoretzHitPoints}, {UgoretzName}, run_speed, walk_speed, action1, action2, action3, " +
                         $"action4, action5, action6, action7, action8 FROM creature WHERE id = {Derge};";
            yield return Stat(Ugoretz, UgoretzHitPoints, UgoretzArmor);

            foreach (var (pool, at) in JuggernautPools)
                yield return Pool(pool, at.X, at.Y, at.Z, MapContextId, Juggernaut, JuggernautsPerPool, JuggernautsPerPool,
                    JuggernautRespawnTenths, PointRadius);
            yield return Pool(UgoretzPool, UgoretzAt.X, UgoretzAt.Y, UgoretzAt.Z, MapContextId, Ugoretz, 1, 1, UgoretzRespawnTenths, PointRadius);
        }

        public static IEnumerable<string> Down()
        {
            var pools = string.Join(", ", JuggernautPools.Select(pool => pool.Pool).Append(UgoretzPool)
                .Select(pool => pool.ToString(CultureInfo.InvariantCulture)));
            yield return $"DELETE FROM spawnpool WHERE id IN ({pools});";
            yield return $"DELETE FROM creature_stat WHERE id IN ({Juggernaut}, {Ugoretz});";
            yield return $"DELETE FROM creature WHERE id IN ({Juggernaut}, {Ugoretz});";
            yield return $"DELETE FROM creature_action WHERE id IN ({WeaponAttack}, {CannonAttack});";
        }
    }
}
