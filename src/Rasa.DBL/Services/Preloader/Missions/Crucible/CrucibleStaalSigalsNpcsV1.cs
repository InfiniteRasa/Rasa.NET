using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// The Crucible set 5 NPCs and creatures, Ligo Crucible (map 1993), for Architects Of Destruction (1202), Out Of Sight
    /// (1208) and Irreconcilable Differences (1185): the new Viddia Sigal Nema, Top Khana Sigal Pico and Trista in Staal; two
    /// more Reconstructor Bot pools by the Chaukas Robotics Facility (1202); Bane Shield Drones on the Crucible (1208); and
    /// Preceptor Molitor with his pack north-east of the Awol Camp (1185).
    ///
    /// Ids (the Crucible block 770000-770999, CrucibleOutpostIntrepidNpcsV1; sets 1-4 used 770000-770018):
    ///  - 770019 Nema, 770020 Pico, 770021 Trista (creature, creature_appearance, creature_stat, npc_package, spawn pool);
    ///  - 770022 the Shield Drone (creature, creature_stat, and its attack: creature_action 770022), in pools 770024-770026;
    ///  - 770023 Preceptor Molitor (creature, creature_stat), in pool 770029 with his pack;
    ///  - pools 770027-770028: Reconstructor Bots (set 4's 770015).
    ///
    /// Sourced:
    ///  - Names (client creaturenamelanguage, 1.0): 9069 "Viddia Sigal Nema", 9063 "Top Khana Sigal Pico", 9072 "Trista", 9172
    ///    "Preceptor Molitor"; the Shield Drone ranks 10694 "Shield Drone Controller" (World DB rows at levels 31-34) and 10766
    ///    "Shield Drone Aggressor" (36-44). Client entityclass 7233 Bane_Shield_Drone; its attack 396/1 (World DB, every zone's
    ///    drone) and its heal aura 45 (245/1).
    ///  - Places: Nema "Staal Infirmary", Pico and Trista "Staal Detention Center" (TaRapedia 1202, 1208, 1185, 0.5); Trista
    ///    "standing next to Lohen" (TaRapedia Trista, level 37, female, 0.5). Molitor "Northeast of AWOL Camp", "patrols on
    ///    the road near spawnpoint", loc -846.0, 266.2, 376.4 (TaRapedia Preceptor Molitor; Ellatha "Check Northeast of AWOL
    ///    Camp", 0.5); "He travels with a pack of his kind" (client 9078). Reconstructor Bots "in the vicinity of the Chaukas
    ///    Robotics Facility" (client 9427). Shield Drones: "Hunt Shield Drones", "Bane Shield Drones" (client 9503, 9504), no
    ///    place.
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Nema a copy of Donal's row and look (Brann class 7776), Pico of Vitto's (as Munit), Trista of Lohen's: the server has
    ///    no row of the female Brann class 7775 to copy, so Trista looks male (to-fix row 39). Levels 35 (the level of 1202
    ///    and 1208) and 37 (Trista, TaRapedia); 4500 hit points and armour 300 as the other new NPCs; server-only packages
    ///    (their ids: the client gives them no objective conversation), as Shandy; friendly, standing still.
    ///  - Each 4 m from the Grupa of her or his syndicate: Nema by Donal on the Viddia Quarter floor by the Staal Infirmary
    ///    waypoint, Pico by Munit on the lower corridor under the Top Khana Quarter, Trista by Lohen in the Kappa Grupa's office
    ///    (reached by the office teleporters 909/910 or the north top entrance, NOTES-crucible-staal-access.md).
    ///  - The Shield Drone a new row of class 7233 at level 35 (the level of 1208), name 10766, 5473 hit points and armour 248
    ///    (between the Marshes drone, 34: 5000 / 240, and the Plains drone, 36: 5946 / 255), the drone attack 396/1 at
    ///    120-358 (between 109-327 and 130-389), damage_type 0 so that the client's weapon table decides, and the drones' heal
    ///    aura 45. Three pools of three (radius 6, 120 s) on the approach north of Khulago Base, the Bane base of the Crucible.
    ///  - Molitor a copy of Executioner Derge's row 520013 (class 10504 Bane_Thrax_Soldier_Rifle_Boss, NOTES-mob-skins.md
    ///    ESTIMATE; his six attacks: 20261011000000_Wire_missing_abilities gives him action5 and action6), level 43, one over his pack, with the 10905 hit points and armour 308 of the Crucible's
    ///    level 43 Thrax (531020). His pack: three Crucible Thrax Soldiers 531018 and one Thrax Grenadier 531019 (upstream's
    ///    Crucible Thrax pools hold three and one), in one pool of radius 10, 50 s respawn (Derge's), standing (no patrol),
    ///    on the navmesh 11 m from TaRapedia's x and z (its y 266.2 has no floor: the road there is at y 286.8).
    ///  - The two new Reconstructor pools (three each, radius 6, 120 s) east and south-east of set 4's, 64 m and more from
    ///    the facility's door.
    ///  - Every place on the server navmesh and a client floor; the NPCs at least 2 m from a wall, nothing over the head,
    ///    seen from 16 of 16 places 2 m round, walked to from the lower office teleporter 909 and the Staal Infirmary
    ///    waypoint (Nema), from 909 and Munit (Pico), from the upper teleporter 910 and Lohen (Trista), with no walk over a
    ///    Staal floor hole; the pools walked to from the Labbna Quarter (Reconstructors), Prometheus Outpost (drones) and Awol
    ///    Camp (Molitor) waypoints; every hostile pool 150 m or more from every friendly NPC (probe 2026-11-07).
    ///
    /// Plain SQL, valid on SQLite and MySQL; Down takes out only these rows.
    /// </summary>
    public static class CrucibleStaalSigalsNpcsV1
    {
        public const uint MapContextId = CrucibleMissionDataV1.MapContextId;
        public const uint HitPoints = CrucibleOutpostIntrepidNpcsV1.HitPoints, Armor = CrucibleOutpostIntrepidNpcsV1.Armor;
        public const uint Friendly = 1;

        public const uint Nema = 770019, NemaName = 9069, NemaPackage = 770019, NemaLevel = 35;
        public const uint Pico = 770020, PicoName = 9063, PicoPackage = 770020, PicoLevel = 35;
        public const uint Trista = 770021, TristaName = 9072, TristaPackage = 770021, TristaLevel = 37;
        public static readonly (double X, double Y, double Z) NemaAt = (-219.5, 195.62, 474);
        public static readonly (double X, double Y, double Z) PicoAt = (-216, 180.68, 231.5);
        public static readonly (double X, double Y, double Z) TristaAt = (-211.5, 244.89, 325.5);

        /// <summary>The Shield Drone (a new row of the drones' class) and its attack (creature_action 770022).</summary>
        public const uint ShieldDrone = 770022, ShieldDroneClass = 7233, ShieldDroneName = 10766, ShieldDroneLevel = 35;
        public const uint ShieldDroneHitPoints = 5473, ShieldDroneArmor = 248, DronesPerPool = 3;
        public const uint DroneAttack = 770022, DroneAction = 396, DroneArg = 1, DroneCooldown = 1000, DroneHealAura = 45;
        public const uint DroneDamageMin = 120, DroneDamageMax = 358;
        public const double DroneReachMin = 0.5, DroneReachMax = 3;
        /// <summary>damage_type 0: the client's weapon table gives the attack its kind (CreatureAttacks.DamageTypeOf).</summary>
        public const uint WeaponTableDamage = 0;

        /// <summary>Preceptor Molitor: a copy of Executioner Derge 520013, and his pack's upstream Crucible Thrax.</summary>
        public const uint Molitor = 770023, MolitorName = 9172, MolitorLevel = 43, MolitorHitPoints = 10905, MolitorArmor = 308;
        public const uint Derge = CrucibleMinefieldAndSalvageNpcsV1.Derge, ThraxSoldier = 531018, ThraxGrenadier = 531019;
        public const uint MolitorPool = 770029, PackSoldiers = 3, PackGrenadiers = 1, MolitorRespawnTenths = 500, MolitorPoolRadius = 10;
        public static readonly (double X, double Y, double Z) MolitorAt = (-856, 286.83, 371);

        public const uint PoolRadius = 6, RespawnTenths = 1200;
        public const uint Reconstructor = CrucibleMinefieldAndSalvageNpcsV1.Reconstructor;
        public const uint ReconstructorsPerPool = CrucibleMinefieldAndSalvageNpcsV1.ReconstructorsPerPool;

        public const uint NextFreeId = 770030;

        /// <summary>The Shield Drone pools on the approach north of Khulago Base.</summary>
        public static readonly (uint Pool, (double X, double Y, double Z) At)[] DronePools =
        {
            (770024, (415, 170.08, 110)), (770025, (490, 173.48, 65)), (770026, (535, 172.92, 80))
        };

        /// <summary>The two more Reconstructor Bot pools by the Chaukas Robotics Facility (map link 98).</summary>
        public static readonly (uint Pool, (double X, double Y, double Z) At)[] ReconstructorPools =
        {
            (770027, (-490, 249.72, 480)), (770028, (-460, 251.31, 570))
        };

        /// <summary>The new NPCs: id, comment, the row they copy, name, level, package, place.</summary>
        public static readonly (uint Id, string Comment, uint Copy, uint Name, uint Level, uint Package, (double X, double Y, double Z) At)[] New =
        {
            (Nema, "Viddia Sigal Nema - Staal Infirmary (1202)", CrucibleOutpostIntrepidNpcsV1.Donal, NemaName, NemaLevel, NemaPackage, NemaAt),
            (Pico, "Top Khana Sigal Pico - Staal, Top Khana Quarter (1208)", CrucibleOutpostIntrepidNpcsV1.Vitto, PicoName, PicoLevel, PicoPackage, PicoAt),
            (Trista, "Trista - Staal, the Kappa Grupa's office (1185)", CrucibleGrupaChiefsNpcsV1.Lohen, TristaName, TristaLevel, TristaPackage, TristaAt)
        };

        /// <summary>Where an NPC of set 5 stands.</summary>
        public static (double X, double Y, double Z) At(uint npc) => New.Single(entry => entry.Id == npc).At;

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

            yield return Action(DroneAttack, "Shield Drone Crucible shield_drone_attack", DroneAction, DroneArg, DroneReachMin, DroneReachMax,
                DroneCooldown, 0, DroneDamageMin, DroneDamageMax, WeaponTableDamage);
            yield return Creature(ShieldDrone, "Shield Drone - Crucible, north of Khulago Base (1208)", ShieldDroneClass, ShieldDroneLevel,
                ShieldDroneHitPoints, ShieldDroneName, DroneAttack, DroneHealAura);
            yield return Stat(ShieldDrone, ShieldDroneHitPoints, ShieldDroneArmor);

            yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                         "action1, action2, action3, action4, action5, action6, action7, action8) " +
                         $"SELECT {Molitor}, 'Preceptor Molitor - Crucible, north-east of the Awol Camp (1185)', class_id, faction, " +
                         $"{MolitorLevel}, {MolitorHitPoints}, {MolitorName}, run_speed, walk_speed, action1, action2, action3, " +
                         $"action4, action5, action6, action7, action8 FROM creature WHERE id = {Derge};";
            yield return Stat(Molitor, MolitorHitPoints, MolitorArmor);

            foreach (var (pool, at) in DronePools)
                yield return Pool(pool, at.X, at.Y, at.Z, MapContextId, ShieldDrone, DronesPerPool, DronesPerPool, RespawnTenths, PoolRadius);
            foreach (var (pool, at) in ReconstructorPools)
                yield return Pool(pool, at.X, at.Y, at.Z, MapContextId, Reconstructor, ReconstructorsPerPool, ReconstructorsPerPool,
                    RespawnTenths, PoolRadius);
            yield return "INSERT INTO spawnpool (id, mode, anim_type, respown_time, pos_x, pos_y, pos_z, rotation, map_context_id, " +
                         "creature_1_Id, creature_1_min_count, creature_1_max_count, creature_2_Id, creature_2_min_count, creature_2_max_count, " +
                         "creature_3_Id, creature_3_min_count, creature_3_max_count, creature_4_Id, creature_4_min_count, creature_4_max_count, " +
                         "creature_5_Id, creature_5_min_count, creature_5_max_count, creature_6_Id, creature_6_min_count, creature_6_max_count, radius) " +
                         $"VALUES ({MolitorPool}, 0, 0, {MolitorRespawnTenths}, {N(MolitorAt.X)}, {N(MolitorAt.Y)}, {N(MolitorAt.Z)}, 0, {MapContextId}, " +
                         $"{Molitor}, 1, 1, {ThraxSoldier}, {PackSoldiers}, {PackSoldiers}, {ThraxGrenadier}, {PackGrenadiers}, {PackGrenadiers}, " +
                         $"0, 0, 0, 0, 0, 0, 0, 0, 0, {MolitorPoolRadius});";
        }

        public static IEnumerable<string> Down()
        {
            var pools = string.Join(", ", DronePools.Concat(ReconstructorPools).Select(pool => pool.Pool).Append(MolitorPool)
                .Select(pool => pool.ToString(CultureInfo.InvariantCulture)));
            yield return $"DELETE FROM spawnpool WHERE id IN ({pools});";
            yield return $"DELETE FROM creature_stat WHERE id IN ({ShieldDrone}, {Molitor});";
            yield return $"DELETE FROM creature WHERE id IN ({ShieldDrone}, {Molitor});";
            yield return $"DELETE FROM creature_action WHERE id = {DroneAttack};";
            foreach (var (id, _, _, _, _, _, _) in New.Reverse())
            {
                yield return $"DELETE FROM spawnpool WHERE id = {id};";
                yield return $"DELETE FROM npc_package WHERE id = {id};";
                yield return $"DELETE FROM creature_stat WHERE id = {id};";
                yield return $"DELETE FROM creature_appearance WHERE id = {id};";
                yield return $"DELETE FROM creature WHERE id = {id};";
            }
        }

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string S(string value) => "'" + value.Replace("'", "''") + "'";
    }
}
