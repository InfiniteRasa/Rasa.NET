using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Rasa.Structures.World;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// The Crucible set 7 NPCs and creatures, for Heads Will Roll (1206, Ligo Crucible 1993) and the Staal Junkyard missions
    /// (squad instance 2138): Careful Negotiation (1519), Your Part of the Bargain (1529) and Not My Junk (1538). The World DB
    /// has no row of Cirkin, the three Thrax officers, Kalamit, Lamontix or the Ultra Reconstructor (to-fix row 51).
    ///
    /// Ids (the Crucible block 770000-770999; sets 1-6 used 770000-770029):
    ///  - 770030 Cirkin (creature, creature_appearance, creature_stat, npc_package, spawn pool), in the Junkyard;
    ///  - 770031 Archfiend Krubb, 770032 Coercer Mungrel, 770033 Defiler Sedge (creature, creature_stat); the NPC squad
    ///    770031 (npc_squad_type, npc_squad_member, npc_squad_path, its points, npc_squad_spawn) that takes them and a Shield
    ///    Drone 770022 on their beat;
    ///  - 770034 Kalamit (creature, creature_stat, spawn pool) on the Crucible;
    ///  - 770035 Lamontix (creature, creature_stat, spawn pool) in the Junkyard;
    ///  - 770036 the Junkyard's Thrax Technician (creature, creature_stat), in pools 770036 and 770037;
    ///  - 770038 the Ultra Reconstructor (creature, creature_stat, spawn pool) in the Junkyard.
    ///
    /// Sourced:
    ///  - Names (client creaturenamelanguage, 1.0): 9643 "Cirkin", 9175 "Archfiend Krubb", 9176 "Coercer Mungrel", 9177
    ///    "Defiler Sedge", 9720 "Kalamit", 9641 "Lamontix", 9640 "Ultra Reconstructor". Cirkin's package 1742 (client
    ///    objectiveconversation 1538,1). The Crucible Thrax Technician 531020 (World DB, client name 8066).
    ///  - Cirkin: "The Lambda named Cirkin", a Brann who runs the Junkyard (client 13549, 13864); TR MMO DB "Cirkin (Quest
    ///    NPC)", level 42 (0.5). Krubb "looks like Preceptor Molitor"; Mungrel "a Thrax Technician boss"; Sedge "a Thrax Grenadier
    ///    boss"; "They travel together, and they're usually protected by a Shield Drone" (client 9489); they walk "in the trench
    ///    near Molitor" (TaRapedia boss pages, 0.5); locs -839.5, 285.4, 576.3; -731, 466; -764, 276, 493 (TaRapedia 1206) and
    ///    "-796,289,528. Its a small rock bridge, 3 Thrax Bosses spawn around the bridge" (TaR-hist rev 27891, 0.3). Kalamit "one
    ///    hell of a Lasher that we've seen wandering near the Junkyard" (client 13549). Lamontix "directly East" of Cirkin (client
    ///    13943), "around 339.1,-70.5", "up a slight rise above the locator mark" (TaRapedia 1538, 0.5). The Technicians "set up
    ///    shop in southeast part of junkyard" (client 13658). The Ultra Reconstructor "near the upper centre of the map"
    ///    (TaRapedia 1529, 0.5).
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Cirkin a copy of Lohen's row and look (Brann class 7776), level 42, 4500 hit points and armour 300 as the other new
    ///    NPCs, friendly, standing still, on the Junkyard floor 11 m from TR MMO DB's spot (181, 241, -222): that spot is off the
    ///    server navmesh (probe 2026-11-07).
    ///  - Krubb a copy of Executioner Derge 520013 (class 10504, Molitor's look), Mungrel of Davinx 520037 (class 10502, the
    ///    Thrax Technician boss), Sedge of Orax 520061 (class 21882, the Thrax Grenadier boss), each with its row's attacks, at
    ///    level 45 (the level of 1206), 12000 hit points and armour 330. One NPC squad: Krubb leads, Mungrel, Sedge and one Shield
    ///    Drone 770022 (set 5) in a wedge 3 m apart on a 30 m tether; the beat the navmesh corners down the trench from
    ///    TaRapedia's first loc by the rock bridge to its third, there and back, 10 s at each end; sent again 300 s after the
    ///    last of it falls (as MarshesEastAndNorthNpcsV1's caravan).
    ///  - Kalamit a new row of the Lasher boss class 10331 with set 3's Lasher attack 770008, level 42, 12000 hit points and
    ///    armour 300; one in a pool of radius 10 south-west of the Labbna Quarter, 263 m from the Junkyard door, 120 s respawn.
    ///  - Lamontix a copy of Derge at level 42, 12000 hit points, armour 300, on the rise east of the lockbox; the Technician a
    ///    copy of 531020 (its attacks) at level 40 with the Ashen Desert level 40 Technician's 8409 hit points and armour 285,
    ///    three in each of two pools (radius 6) in the south-east; the Ultra Reconstructor a new row of the Reconstructor boss
    ///    class 10314 with set 4's Reconstructor attack 770015, level 42, 12000 hit points, armour 300, one in the north centre.
    ///    The Junkyard pools' own respawn is 90 s (upstream's Junkyard Thrax pools 531134/531135). In a squad's copy a pool
    ///    comes back only with the copy (SquadInstanceConfig.RespawnMinutes, 45 min), but Lamontix and the Ultra Reconstructor
    ///    come back on their 90 s: 1538's and 1529's scenes name their pools public-spawn actors (SpawnPoolManager.ReturnsOnOwnTime).
    ///  - Every place on the server navmesh and walked to: the Junkyard ones from its arrival (29.69, 241.22, -336.85), Kalamit
    ///    from the Labbna Quarter waypoint, the beat from the Awol Camp waypoint; Cirkin 2 m or more from a wall, 180 m or more
    ///    from the Junkyard's door circle and 170 m or more from every hostile pool's circle (probe 2026-11-07).
    ///
    /// Plain SQL, valid on SQLite and MySQL; Down takes out only these rows.
    /// </summary>
    public static class CrucibleStaalJunkyardNpcsV1
    {
        public const uint Crucible = CrucibleMissionDataV1.MapContextId;
        public const uint Junkyard = 2138;              // adv_arieki_ligo_staaljunkyard, a squad instance (SquadInstancePolicies)
        public const uint HitPoints = CrucibleOutpostIntrepidNpcsV1.HitPoints, Armor = CrucibleOutpostIntrepidNpcsV1.Armor;
        public const uint Friendly = 1;

        /// <summary>The Junkyard's arrival (map link 96's destination) and its door back (map link 124, radius 6).</summary>
        public static readonly (double X, double Y, double Z) ArrivalAt = (29.6931, 241.2155, -336.8526);
        /// <summary>The Junkyard door on the Crucible (map link 96, radius 6).</summary>
        public static readonly (double X, double Y, double Z) DoorAt = (-492.1312, 236.9487, -149.0594);
        /// <summary>The bridge road to the door on the Crucible, 28 m outside the door's circle (a marker for the way in).</summary>
        public static readonly (double X, double Y, double Z) BridgeAt = (-492, 237.88, -115);

        public const uint Cirkin = 770030, CirkinName = 9643, CirkinPackage = 1742, CirkinLevel = 42;
        public static readonly (double X, double Y, double Z) CirkinAt = (187, 240.31, -233);

        /// <summary>The three Thrax officers (1206).</summary>
        public const uint Krubb = 770031, KrubbName = 9175, Mungrel = 770032, MungrelName = 9176, Sedge = 770033, SedgeName = 9177;
        public const uint OfficerLevel = 45, OfficerHitPoints = 12000, OfficerArmor = 330;
        public const uint Derge = CrucibleMinefieldAndSalvageNpcsV1.Derge, Davinx = 520037, Orax = 520061;
        public const uint ShieldDrone = CrucibleStaalSigalsNpcsV1.ShieldDrone;

        /// <summary>The officers' squad type, beat and spawn share Krubb's id.</summary>
        public const uint Squad = Krubb;
        public const string SquadName = "Krubb, Mungrel and Sedge", Route = "Krubb, Mungrel and Sedge, the trench by the rock bridge";
        /// <summary>A wedge, 3 m apart; its own creatures' aggro range; no one further than 30 m from Krubb; no bystander.</summary>
        public const byte Formation = 0, LeaderBystander = 0;
        public const double Spacing = 3, EngageRange = 0, Tether = 30;
        /// <summary>There and back.</summary>
        public const byte Mode = 1;
        public const uint EndPauseMs = 10000, RespawnSeconds = 300;

        /// <summary>
        /// The beat: TaRapedia's first loc by the rock bridge, then the corners of the navmesh's walk down the trench under the
        /// bridge to its third loc (probe 2026-11-07).
        /// </summary>
        public static readonly (double X, double Y, double Z)[] Beat =
        {
            (-839.5, 285.68, 576.3), (-844, 286.48, 558.8), (-838.8, 285.28, 558.8), (-832.8, 282.48, 556.4),
            (-825.2, 279.88, 550.4), (-798, 277.88, 527.2), (-764, 276.77, 493)
        };

        /// <summary>The start, facing the first leg (south; NpcSquads' yaw, AbilityManager.YawTowards).</summary>
        public const double SpawnRotation = 0.252;

        /// <summary>The rock bridge over the trench (TaR-hist rev 27891): the marker of the three officers.</summary>
        public static readonly (double X, double Y, double Z) RockBridgeAt = (-796, 289.9, 528);

        /// <summary>Kalamit (1519), on the Crucible.</summary>
        public const uint Kalamit = 770034, KalamitName = 9720, KalamitClass = 10331, KalamitLevel = 42;
        public const uint BossHitPoints = 12000, BossArmor = 300;
        public const uint LasherAttack = 770008;
        public const uint KalamitPool = 770034, KalamitPoolRadius = 10, KalamitRespawnSeconds = 120;
        public static readonly (double X, double Y, double Z) KalamitAt = (-486, 249.88, 120);

        /// <summary>Lamontix (1538), in the Junkyard.</summary>
        public const uint Lamontix = 770035, LamontixName = 9641, LamontixLevel = 42;
        public static readonly (double X, double Y, double Z) LamontixAt = (350, 265.42, -52);

        /// <summary>The Junkyard's Thrax Technicians (1529), a copy of the Crucible's 531020.</summary>
        public const uint Technician = 770036, CrucibleTechnician = 531020, TechnicianLevel = 40;
        public const uint TechnicianHitPoints = 8409, TechnicianArmor = 285, TechniciansPerPool = 3, TechnicianPoolRadius = 6;
        public static readonly (uint Pool, (double X, double Y, double Z) At)[] TechnicianPools =
        {
            (770036, (330, 243.46, -160)), (770037, (306, 257.43, -106))
        };

        /// <summary>The Ultra Reconstructor (1529).</summary>
        public const uint UltraReconstructor = 770038, UltraReconstructorName = 9640, UltraReconstructorClass = 10314;
        public const uint UltraReconstructorLevel = 42, ReconstructorAttack = 770015;
        public static readonly (double X, double Y, double Z) UltraReconstructorAt = (-14, 335.14, 244);

        /// <summary>
        /// The Junkyard pools' own respawn in spawnpool.respown_time's tenths: 90 s, as upstream's Junkyard Thrax pools 531134/531135. In
        /// the squad's copy only the named public-spawn pools (Lamontix, the Ultra Reconstructor) keep it; the Technicians come back with the copy.
        /// </summary>
        public const uint JunkyardRespawnTenths = 900;
        public const uint NextFreeId = 770039;

        /// <summary>Where an NPC or a creature's pool of set 7 stands.</summary>
        public static (double X, double Y, double Z) At(uint pool) => pool switch
        {
            Cirkin => CirkinAt,
            KalamitPool => KalamitAt,
            Lamontix => LamontixAt,
            UltraReconstructor => UltraReconstructorAt,
            _ => TechnicianPools.Single(entry => entry.Pool == pool).At
        };

        public static IEnumerable<string> Up()
        {
            yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                         "action1, action2, action3, action4, action5, action6, action7, action8) " +
                         $"SELECT {Cirkin}, 'Cirkin - Staal Junkyard (1519, 1529, 1538)', class_id, {Friendly}, {CirkinLevel}, {HitPoints}, " +
                         $"{CirkinName}, 0, 0, action1, action2, action3, action4, action5, action6, action7, action8 " +
                         $"FROM creature WHERE id = {CrucibleGrupaChiefsNpcsV1.Lohen};";
            yield return "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                         $"SELECT {Cirkin}, slot_id, Class_id, color FROM creature_appearance WHERE id = {CrucibleGrupaChiefsNpcsV1.Lohen};";
            yield return Stat(Cirkin, HitPoints, Armor);
            yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({Cirkin}, {CirkinPackage}, 'Cirkin - Staal Junkyard');";
            yield return Pool(Cirkin, CirkinAt.X, CirkinAt.Y, CirkinAt.Z, Junkyard, Cirkin, 1, 1, 20, 0);

            foreach (var (id, comment, copy, name, level, health, armor) in new[]
            {
                (Krubb, "Archfiend Krubb - Crucible, the trench by the rock bridge (1206)", Derge, KrubbName, OfficerLevel, OfficerHitPoints, OfficerArmor),
                (Mungrel, "Coercer Mungrel - Crucible, the trench by the rock bridge (1206)", Davinx, MungrelName, OfficerLevel, OfficerHitPoints, OfficerArmor),
                (Sedge, "Defiler Sedge - Crucible, the trench by the rock bridge (1206)", Orax, SedgeName, OfficerLevel, OfficerHitPoints, OfficerArmor),
                (Lamontix, "Lamontix - Staal Junkyard, east of Cirkin (1538)", Derge, LamontixName, LamontixLevel, BossHitPoints, BossArmor),
                (Technician, "Thrax Technician - Staal Junkyard, south-east (1529)", CrucibleTechnician, 8066U, TechnicianLevel, TechnicianHitPoints,
                    TechnicianArmor)
            })
            {
                yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                             "action1, action2, action3, action4, action5, action6, action7, action8) " +
                             $"SELECT {id}, {S(comment)}, class_id, faction, {level}, {health}, {name}, run_speed, walk_speed, " +
                             $"action1, action2, action3, action4, action5, action6, action7, action8 FROM creature WHERE id = {copy};";
                yield return Stat(id, health, armor);
            }
            yield return Creature(Kalamit, "Kalamit - Crucible, south-west of the Labbna Quarter (1519)", KalamitClass, KalamitLevel, BossHitPoints,
                KalamitName, LasherAttack);
            yield return Stat(Kalamit, BossHitPoints, BossArmor);
            yield return Creature(UltraReconstructor, "Ultra Reconstructor - Staal Junkyard, north centre (1529)", UltraReconstructorClass,
                UltraReconstructorLevel, BossHitPoints, UltraReconstructorName, ReconstructorAttack);
            yield return Stat(UltraReconstructor, BossHitPoints, BossArmor);

            yield return PoolSeconds(KalamitPool, KalamitAt.X, KalamitAt.Y, KalamitAt.Z, Crucible, Kalamit, 1, 1, KalamitRespawnSeconds,
                KalamitPoolRadius);
            yield return Pool(Lamontix, LamontixAt.X, LamontixAt.Y, LamontixAt.Z, Junkyard, Lamontix, 1, 1, JunkyardRespawnTenths, 0);
            foreach (var (pool, at) in TechnicianPools)
                yield return Pool(pool, at.X, at.Y, at.Z, Junkyard, Technician, TechniciansPerPool, TechniciansPerPool, JunkyardRespawnTenths,
                    TechnicianPoolRadius);
            yield return Pool(UltraReconstructor, UltraReconstructorAt.X, UltraReconstructorAt.Y, UltraReconstructorAt.Z, Junkyard,
                UltraReconstructor, 1, 1, JunkyardRespawnTenths, 0);

            yield return $"INSERT INTO {NpcSquadTypeEntry.TableName} (id, name, formation, spacing, engage_range, tether, leader_bystander) " +
                $"VALUES ({Squad}, {S(SquadName)}, {Formation}, {N(Spacing)}, {N(EngageRange)}, {N(Tether)}, {LeaderBystander});";
            var slot = 0;
            foreach (var member in new[] { Krubb, Mungrel, Sedge, ShieldDrone })
                yield return $"INSERT INTO {NpcSquadMemberEntry.TableName} (type_id, slot, creature_id, min_count, max_count) " +
                    $"VALUES ({Squad}, {slot++}, {member}, 1, 1);";
            yield return $"INSERT INTO {NpcSquadPathEntry.TableName} (id, map_context_id, name, mode) " +
                $"VALUES ({Squad}, {Crucible}, {S(Route)}, {Mode});";
            for (var step = 0; step < Beat.Length; step++)
                yield return $"INSERT INTO {NpcSquadPathPointEntry.TableName} (path_id, step, pos_x, pos_y, pos_z, pause_ms, run, engage_range) " +
                    $"VALUES ({Squad}, {step}, {N(Beat[step].X)}, {N(Beat[step].Y)}, {N(Beat[step].Z)}, {PauseAt(step)}, 0, 0);";
            var start = Beat[0];
            yield return $"INSERT INTO {NpcSquadSpawnEntry.TableName} (id, map_context_id, type_id, path_id, pos_x, pos_y, pos_z, rotation, " +
                "respawn_seconds, enabled, comment, arrival) " +
                $"VALUES ({Squad}, {Crucible}, {Squad}, {Squad}, {N(start.X)}, {N(start.Y)}, {N(start.Z)}, {N(SpawnRotation)}, " +
                $"{RespawnSeconds}, 1, {S(SquadName)}, 0);";
        }

        public static IEnumerable<string> Down()
        {
            yield return $"DELETE FROM {NpcSquadSpawnEntry.TableName} WHERE id = {Squad};";
            yield return $"DELETE FROM {NpcSquadPathPointEntry.TableName} WHERE path_id = {Squad};";
            yield return $"DELETE FROM {NpcSquadPathEntry.TableName} WHERE id = {Squad};";
            yield return $"DELETE FROM {NpcSquadMemberEntry.TableName} WHERE type_id = {Squad};";
            yield return $"DELETE FROM {NpcSquadTypeEntry.TableName} WHERE id = {Squad};";
            var pools = string.Join(", ", new[] { Cirkin, KalamitPool, Lamontix, UltraReconstructor }
                .Concat(TechnicianPools.Select(pool => pool.Pool)).Select(pool => pool.ToString(CultureInfo.InvariantCulture)));
            yield return $"DELETE FROM spawnpool WHERE id IN ({pools});";
            var creatures = string.Join(", ", new[] { Cirkin, Krubb, Mungrel, Sedge, Kalamit, Lamontix, Technician, UltraReconstructor }
                .Select(id => id.ToString(CultureInfo.InvariantCulture)));
            yield return $"DELETE FROM npc_package WHERE id = {Cirkin};";
            yield return $"DELETE FROM creature_stat WHERE id IN ({creatures});";
            yield return $"DELETE FROM creature_appearance WHERE id = {Cirkin};";
            yield return $"DELETE FROM creature WHERE id IN ({creatures});";
        }

        /// <summary>10 s at each end of the beat, none between.</summary>
        public static uint PauseAt(int step) => step == 0 || step == Beat.Length - 1 ? EndPauseMs : 0;

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string S(string value) => "'" + value.Replace("'", "''") + "'";
    }
}
