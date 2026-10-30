using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using static Rasa.Services.Preloader.DynamicDropshipsContent;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Valverde Pools' control points as the Plateau's (<see cref="PlateauDynamicDropshipsContent"/>):
    /// an AFS garrison by AFS dropship at a point the AFS hold, timed Bane assaults by dropship on it,
    /// an AFS relief ship; AFS sorties against a point the Bane hold, which cannot take it. Retread
    /// Outpost holds at the start; Bane Comm Center and Bane Guard Station get their AFS garrison and
    /// a Bane counter-attack after a capture.
    ///
    /// Sources (dynamic_spawns_dropships research, plateau-and-pools.md):
    ///  - Retread Outpost: "this control point is frequently overrun by Bane. We aren't talking a few
    ///    squads of Bane, I mean dozens literally" (Ten Ton Hammer, Valverde Pools overview, 2008-02);
    ///    "Pools Retread CP now has bane boss defender(s)/attacker(s)" (patch 2007-12-11);
    ///  - Bane Comm Center, Bane Guard Station: "while the AFS attacks, we've yet to see the AI overrun
    ///    one" (Ten Ton Hammer): a sortie cannot take them (AfsSorties).
    /// Retail gives no timers or counts for this zone either: the numbers are ESTIMATE, as the
    /// Plateau's. The attackers are "2 to 4 levels over the Zone average" (zerias): the Pools' Bane
    /// are 30-31, so the waves are the level 33-34 Thrax (the Marshes' rows) and a level 36 commander.
    /// </summary>
    public static class PoolsDynamicDropshipsContent
    {
        public const uint MapId = 1304;
        public const uint RetreadOutpost = 7, BaneCommCenter = 8, BaneGuardStation = 9;

        // Level 33-34 Thrax and the level 36 commander (the Marshes' rows). The Pools have no Kael.
        public const uint ThraxSoldier = 531045, ThraxGrenadier = 531046, ThraxTechnician = 531047,
            Caretaker = 531048, ThraxCommander = 531049;

        public const uint Rifleman = 692101, MachineGunner = 692102, Mech = 692103;

        // As the Plateau's waves (no Shield Drones, see there), a Soldier in the Kael's place: 28-33
        // Bane with the commander.
        private static readonly Troop[][][] Waves =
        {
            new[] { new[] { new Troop(ThraxSoldier, 3, 4), new Troop(ThraxGrenadier, 1, 1) },
                    new[] { new Troop(ThraxSoldier, 3, 3), new Troop(ThraxGrenadier, 1, 1) } },
            new[] { new[] { new Troop(ThraxSoldier, 2, 3), new Troop(ThraxGrenadier, 2, 2), new Troop(Caretaker, 1, 1) },
                    new[] { new Troop(ThraxSoldier, 2, 3), new Troop(ThraxGrenadier, 1, 1), new Troop(ThraxTechnician, 1, 1) } },
            new[] { new[] { new Troop(ThraxSoldier, 4, 4), new Troop(Caretaker, 1, 1), new Troop(ThraxGrenadier, 0, 1) },
                    new[] { new Troop(ThraxSoldier, 2, 3), new Troop(ThraxGrenadier, 2, 2), new Troop(ThraxTechnician, 1, 1) } }
        };

        private static readonly Troop[] Relief = { new(Rifleman, 3, 3), new(MachineGunner, 1, 1) };
        private static readonly Troop[] SortieSquad = { new(Mech, 1, 1), new(Rifleman, 3, 4) };

        private static LandingZoneAssaultContent.Drop Toward(Vector3 at, Vector3 point) =>
            new(at, Math.Atan2(-(point.X - at.X), -(point.Z - at.Z)));

        private static readonly Vector3 RetreadAt = new(201.46f, 867.5f, 332.048f), CommCenterAt = new(-460.235f, 680.438f, -404.708f),
            GuardStationAt = new(558.634f, 670.116f, -104.631f);

        // Period 30 min (ESTIMATE: Retread Outpost "frequently overrun"; the Plateau's 45). Retread
        // Outpost is the zone's only AFS point at the start; a captured Bane point is counter-attacked
        // after 10 min, as on the Plateau.
        public static readonly Assault[] Assaults =
        {
            new()
            {
                MapId = PoolsDynamicDropshipsContent.MapId, PointId = RetreadOutpost, FirstDelaySeconds = 900, SortieFirstDelaySeconds = 300,
                PeriodSeconds = 1800, WaveSeconds = 180, LifetimeSeconds = 900,
                // Outside the north-west gate (client ArchHumOutpostFenceGateV01 at (166.5, 864, 376.5)).
                Drops = new[] { Toward(new(153.3f, 864.8f, 389.5f), RetreadAt), Toward(new(175.8f, 864.9f, 402.5f), RetreadAt) },
                Waves = Waves, Boss = ThraxCommander, Relief = Relief, Sortie = SortieSquad,
                // No pad at the point (client map): the yard.
                ReliefPad = Toward(new(201.5f, 867.6f, 344f), RetreadAt), ReliefPost = new(185.4f, 865.7f, 351.2f)
            },
            new()
            {
                MapId = PoolsDynamicDropshipsContent.MapId, PointId = BaneCommCenter, FirstDelaySeconds = 600, SortieFirstDelaySeconds = 450,
                PeriodSeconds = 1800, WaveSeconds = 180, LifetimeSeconds = 900,
                // Over the bridge outside the north entrance (client ArchBaneOutpostWallMinorEntranceV01
                // at (-448.5, 679.8, -376.8), ArchBaneOutpostBridgeV01 at (-438.9, 677.3, -359.1)).
                Drops = new[] { Toward(new(-436.1f, 679.0f, -345.2f), CommCenterAt), Toward(new(-418.5f, 678.3f, -354.9f), CommCenterAt) },
                Waves = Waves, Boss = ThraxCommander, Relief = Relief, Sortie = SortieSquad,
                ReliefPad = Toward(new(-460.2f, 680.3f, -392.7f), CommCenterAt), ReliefPost = new(-444.2f, 679.8f, -385.6f)
            },
            new()
            {
                MapId = PoolsDynamicDropshipsContent.MapId, PointId = BaneGuardStation, FirstDelaySeconds = 600, SortieFirstDelaySeconds = 1350,
                PeriodSeconds = 1800, WaveSeconds = 180, LifetimeSeconds = 900,
                // Outside the west entrance (client entrance at (536, 670, -116)).
                Drops = new[] { Toward(new(511.0f, 672.3f, -132.1f), GuardStationAt), Toward(new(504.5f, 669.6f, -95.1f), GuardStationAt) },
                Waves = Waves, Boss = ThraxCommander, Relief = Relief, Sortie = SortieSquad,
                ReliefPad = Toward(new(558.6f, 670.0f, -92.6f), GuardStationAt), ReliefPost = new(538.9f, 669.8f, -108.1f)
            }
        };

        /// <summary>The AFS garrison pools: id, point, what, where. All come and come back by AFS dropship.</summary>
        public static readonly (uint Id, uint Point, Troop Troop, Vector3 At, int RespawnTenths)[] Garrison =
        {
            // Each point: 10 riflemen, 2 machine gunners, 4 mechs, as a captured Plateau Bane point.
            // Retread Outpost has one AFS turret (551149), not the Plateau AFS points' four or six,
            // so it gets the bigger garrison too (ESTIMATE).
            (692101, RetreadOutpost, new(Rifleman, 3, 3), new(188.6f, 865.8f, 347.4f), 3000),
            (692102, RetreadOutpost, new(Rifleman, 3, 3), new(214.3f, 868.3f, 347.4f), 3000),
            (692103, RetreadOutpost, new(Rifleman, 2, 2), new(201.5f, 867.6f, 344f), 3000),
            (692104, RetreadOutpost, new(Rifleman, 2, 2), new(192.9f, 865.6f, 355.5f), 3000),
            (692105, RetreadOutpost, new(MachineGunner, 1, 1), new(184.1f, 866.5f, 342f), 3000),
            (692106, RetreadOutpost, new(MachineGunner, 1, 1), new(210.6f, 867.1f, 355f), 3000),
            (692107, RetreadOutpost, new(Mech, 1, 1), new(181.8f, 867.4f, 335.5f), 6000),
            (692108, RetreadOutpost, new(Mech, 1, 1), new(221.2f, 865f, 335.5f), 6000),
            (692109, RetreadOutpost, new(Mech, 1, 1), new(201.5f, 871.6f, 312f), 6000),
            (692110, RetreadOutpost, new(Mech, 1, 1), new(199.6f, 868.6f, 352f), 6000),
            (692111, BaneCommCenter, new(Rifleman, 3, 3), new(-447.4f, 679.8f, -389.4f), 3000),
            (692112, BaneCommCenter, new(Rifleman, 3, 3), new(-473.1f, 679.9f, -420f), 3000),
            (692113, BaneCommCenter, new(Rifleman, 2, 2), new(-460.2f, 679.9f, -384.7f), 3000),
            (692114, BaneCommCenter, new(Rifleman, 2, 2), new(-440.5f, 680.1f, -408.2f), 3000),
            (692115, BaneCommCenter, new(MachineGunner, 1, 1), new(-453.4f, 679.8f, -385.9f), 3000),
            (692116, BaneCommCenter, new(MachineGunner, 1, 1), new(-477.6f, 679.8f, -394.7f), 3000),
            (692117, BaneCommCenter, new(Mech, 1, 1), new(-460.2f, 679.9f, -424.7f), 6000),
            (692118, BaneCommCenter, new(Mech, 1, 1), new(-442.9f, 680f, -414.7f), 6000),
            (692119, BaneCommCenter, new(Mech, 1, 1), new(-473.1f, 680f, -389.4f), 6000),
            (692120, BaneCommCenter, new(Mech, 1, 1), new(-449.8f, 680.1f, -398.7f), 6000),
            (692121, BaneGuardStation, new(Rifleman, 3, 3), new(545.8f, 669.7f, -120f), 3000),
            (692122, BaneGuardStation, new(Rifleman, 3, 3), new(571.5f, 669.8f, -89.3f), 3000),
            (692123, BaneGuardStation, new(Rifleman, 2, 2), new(541.3f, 669.7f, -114.6f), 3000),
            (692124, BaneGuardStation, new(Rifleman, 2, 2), new(546.4f, 669.7f, -89.7f), 3000),
            (692125, BaneGuardStation, new(MachineGunner, 1, 1), new(551.8f, 669.8f, -123.4f), 3000),
            (692126, BaneGuardStation, new(MachineGunner, 1, 1), new(576f, 669.8f, -94.6f), 3000),
            (692127, BaneGuardStation, new(Mech, 1, 1), new(558.6f, 669.9f, -124.6f), 6000),
            (692128, BaneGuardStation, new(Mech, 1, 1), new(575.3f, 670f, -114.6f), 6000),
            (692129, BaneGuardStation, new(Mech, 1, 1), new(558.6f, 669.8f, -84.6f), 6000),
            (692130, BaneGuardStation, new(Mech, 1, 1), new(542.9f, 670f, -107.4f), 6000)
        };

        public static IEnumerable<string> Up()
        {
            // Balance (ESTIMATE): the Plateau's AFS against the Pools' level 30-31 Thrax, times the
            // Marshes' rows over the Pools' (health 3439 / 2652 and damage 147-220 / 113-170: 1.3
            // each); armour as the Thrax's (155 / 140). So that a point no player defends holds off
            // two assaults and falls in the third, as on the Plateau.
            yield return "INSERT INTO creature_action (id,description,action_id,action_arg_id,range_min,range_max,cooldown,windup,min_damage,max_damage,damage_type) VALUES " +
                "(692101,'Pools AFS rifle',1,134,1,35,800,0,213,317,1)," +
                "(692102,'Pools AFS machine gun',149,1,1,30,500,0,131,213,1)," +
                "(692103,'Pools AFS mech minigun',1,224,1,30,400,0,131,196,1);";
            yield return "INSERT INTO creature (id,comment,class_id,faction,level,max_hp,name_id,run_speed,walk_speed,action1,action2,action3,action4,action5,action6,action7,action8) VALUES " +
                "(692101,'Pools control point AFS Rifleman',29423,1,34,9200,8717,7,2,692101,0,0,0,0,0,0,0)," +
                "(692102,'Pools control point AFS Machine Gunner',29433,1,34,9200,8719,7,2,692102,0,0,0,0,0,0,0)," +
                "(692103,'Pools control point AFS Mech Defender',10235,1,36,38700,10736,9,5,692103,0,0,0,0,0,0,0);";
            yield return "INSERT INTO creature_stat (id,body,mind,spirit,health,armor) VALUES " +
                "(692101,15,15,15,9200,505),(692102,15,15,15,9200,505),(692103,30,30,30,38700,1063);";
            yield return "INSERT INTO creature_appearance (id,slot_id,class_id,color) VALUES (692101,13,27220,1),(692102,13,20535,1);";
            foreach (var pool in Garrison)
                yield return "INSERT INTO spawnpool (id,mode,anim_type,respown_time,pos_x,pos_y,pos_z,rotation,map_context_id," +
                    "creature_1_Id,creature_1_min_count,creature_1_max_count,creature_2_Id,creature_2_min_count,creature_2_max_count," +
                    "creature_3_Id,creature_3_min_count,creature_3_max_count,creature_4_Id,creature_4_min_count,creature_4_max_count," +
                    "creature_5_Id,creature_5_min_count,creature_5_max_count,creature_6_Id,creature_6_min_count,creature_6_max_count,radius) VALUES " +
                    $"({pool.Id},1,2,{pool.RespawnTenths},{N(pool.At.X)},{N(pool.At.Y)},{N(pool.At.Z)},0,{MapId},{pool.Troop.Creature},{pool.Troop.Min},{pool.Troop.Max}," +
                    "0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,3);";
            yield return "INSERT INTO control_point_link (control_point_id,kind,object_id) VALUES " +
                string.Join(",", Garrison.Select(p => $"({p.Point},2,{p.Id})")) + ";";
        }

        public static IEnumerable<string> Down()
        {
            yield return "DELETE FROM control_point_link WHERE kind=2 AND object_id BETWEEN 692101 AND 692199;";
            yield return "DELETE FROM spawnpool WHERE id BETWEEN 692101 AND 692199;";
            yield return "DELETE FROM creature_appearance WHERE id BETWEEN 692101 AND 692103;";
            yield return "DELETE FROM creature_stat WHERE id BETWEEN 692101 AND 692103;";
            yield return "DELETE FROM creature WHERE id BETWEEN 692101 AND 692103;";
            yield return "DELETE FROM creature_action WHERE id BETWEEN 692101 AND 692103;";
        }

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    }
}
