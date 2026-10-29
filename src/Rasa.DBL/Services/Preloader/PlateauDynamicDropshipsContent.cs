using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Valverde Plateau's control points as retail had them: an AFS garrison at each point the AFS
    /// holds, flown in by AFS dropship; timed Bane assaults by dropship on each point the AFS holds,
    /// the points' timers apart so the zone is not attacked everywhere at once; an AFS relief ship
    /// when an assault starts. Northeast and Northwest AFS hold at the start; Southeast and
    /// Southwest Bane get their AFS garrison and a Bane counter-attack after a capture.
    ///
    /// Sources: dynamic_spawns_dropships research (TaRapedia Control Point and Wilderness L.Z., Ten
    /// Ton Hammer guides, rgtr patch notes 2007-09-24, zerias blog 2008-02-29, planettr and IGN
    /// boards, videos e8TRtOqRsRQ, 4_ofxpJ_rtY, FpevZX2qIT8, 00UG2o6ot74). Retail gives no timers or
    /// counts for this zone: the numbers here are ESTIMATE from those sources:
    ///  - the attackers are "2 to 4 levels over the Zone average" (zerias): the Plateau's Bane are
    ///    27-28, so the waves are the level 30-31 Thrax (the Pools' rows) and a level 33 commander;
    ///  - "10 attackers for every 1 AFS (NPC) defender ... all at once" (IGN Vault / planettr):
    ///    about 30-35 Bane in three waves of two ships against a garrison of six and a mech;
    ///  - Thrax with Caretakers and Shield Drones, Kael at the Plateau's points (TTH);
    ///  - "the end of an assault [is] friendly NPC units dropping in and helping to wipe up
    ///    remaining Bane forces" (zerias): the AFS relief ship comes after the last wave.
    /// The AFS troops have the Landing Zone garrison's strength against its Thrax, at this level.
    /// </summary>
    public static class PlateauDynamicDropshipsContent
    {
        public const uint MapId = 1497;
        public const uint NortheastAfs = 12, NorthwestAfs = 13, SoutheastBane = 14, SouthwestBane = 15;

        // Level 30-31 Thrax (the Pools' rows), the Plateau's Kael, the Pools' level 33 commander.
        public const uint ThraxSoldier = 531089, ThraxGrenadier = 531090, ThraxTechnician = 531091,
            Caretaker = 531092, ShieldDrone = 531094, ThraxCommander = 531093, Kael = 531088;

        public const uint Rifleman = 692001, MachineGunner = 692002, Mech = 692003;
        public const uint RifleShot = 692001, MachineGunBurst = 692002, MechMinigun = 692003;

        public readonly struct Troop
        {
            public uint Creature { get; }
            public int Min { get; }
            public int Max { get; }
            public Troop(uint creature, int min, int max) { Creature = creature; Min = min; Max = max; }
        }

        public sealed class Assault
        {
            public uint MapId { get; init; } = PlateauDynamicDropshipsContent.MapId;
            public uint PointId { get; init; }
            /// <summary>From the server start, or from the AFS taking the point, to the first assault.</summary>
            public int FirstDelaySeconds { get; init; }
            public int PeriodSeconds { get; init; }
            public int WaveSeconds { get; init; }
            public int LifetimeSeconds { get; init; }
            public LandingZoneAssaultContent.Drop[] Drops { get; init; }
            /// <summary>Per wave, per ship: what the ship brings.</summary>
            public Troop[][][] Waves { get; init; }
            /// <summary>Comes with one ship of the last wave and takes the point if the defence falls.</summary>
            public uint Boss { get; init; }
            /// <summary>From the assault's start to the AFS relief ship.</summary>
            public int ReliefSeconds { get; init; } = 480;
            public LandingZoneAssaultContent.Drop ReliefPad { get; init; }
            public Vector3 ReliefPost { get; init; }
            public Troop[] Relief { get; init; }
            /// <summary>While the Bane hold the point: one AFS ship with this squad now and then (AfsSorties).</summary>
            public Troop[] Sortie { get; init; } = SortieSquad;
            public int SortieFirstDelaySeconds { get; init; }
            public int SortiePeriodSeconds { get; init; } = 1800;
            public int SortieLifetimeSeconds { get; init; } = 600;
        }

        // ESTIMATE: retail "3-4 waves", "usually 2 ships", "about 4-8 per ship"; Thrax infantry
        // first, caretakers, technicians and shield drones behind them, a Kael and the commander
        // in the last wave. 9-10, 10-12 and 11-13 Bane: 30-35 with the commander.
        private static readonly Troop[][][] Waves =
        {
            new[] { new[] { new Troop(ThraxSoldier, 3, 4), new Troop(ThraxGrenadier, 1, 1) },
                    new[] { new Troop(ThraxSoldier, 3, 3), new Troop(ThraxGrenadier, 1, 1) } },
            new[] { new[] { new Troop(ThraxSoldier, 2, 3), new Troop(ThraxGrenadier, 1, 1), new Troop(Caretaker, 1, 1), new Troop(ShieldDrone, 1, 1) },
                    new[] { new Troop(ThraxSoldier, 2, 3), new Troop(ThraxGrenadier, 1, 1), new Troop(ThraxTechnician, 1, 1) } },
            new[] { new[] { new Troop(ThraxSoldier, 3, 3), new Troop(Kael, 1, 1), new Troop(Caretaker, 1, 1), new Troop(ShieldDrone, 0, 1) },
                    new[] { new Troop(ThraxSoldier, 2, 3), new Troop(ThraxGrenadier, 1, 1), new Troop(ThraxTechnician, 1, 1), new Troop(ShieldDrone, 1, 1) } }
        };

        private static readonly Troop[] Relief = { new(Rifleman, 3, 3), new(MachineGunner, 1, 1) };

        // "A single AFS Mech and a fire team of 3-4 flunkies" (IGN Vault / planettr, 2007-12).
        // Every 30 min (ESTIMATE), each point its own time.
        private static readonly Troop[] SortieSquad = { new(Mech, 1, 1), new(Rifleman, 3, 4) };

        private static LandingZoneAssaultContent.Drop Toward(Vector3 at, Vector3 point) =>
            new(at, Math.Atan2(-(point.X - at.X), -(point.Z - at.Z)));

        private static readonly Vector3 NortheastAt = new(115.311f, 370.844f, 418.15f), NorthwestAt = new(-384.718f, 375.768f, 430.858f),
            SoutheastAt = new(92.025f, 370.841f, -132.706f), SouthwestAt = new(-328.112f, 375.702f, -105.556f);

        // Period 45 min per point (ESTIMATE: before December 2007 a point saw about one attack in
        // 1-2 hours (planettr); patch 2007-12-11 made Plateau's "more frequent"; Landing Zone has 60). The first delays put the two AFS points about 22 min
        // apart; a captured Bane point is counter-attacked after 10 min (TaRapedia: "after a delay").
        public static readonly Assault[] Assaults =
        {
            new()
            {
                PointId = NortheastAfs, FirstDelaySeconds = 900, SortieFirstDelaySeconds = 1200, PeriodSeconds = 2700, WaveSeconds = 180, LifetimeSeconds = 900,
                // Outside the west gate (client ArchHumOutpostFenceGateV02 at (78, 370, 426)).
                Drops = new[] { Toward(new(31.9f, 370.7f, 440.6f), NortheastAt), Toward(new(23.6f, 371.1f, 401.2f), NortheastAt) },
                Waves = Waves, Boss = ThraxCommander, Relief = Relief,
                ReliefPad = Toward(new(104f, 369.2f, 418f), NortheastAt), ReliefPost = new(90f, 370f, 418f)
            },
            new()
            {
                PointId = NorthwestAfs, FirstDelaySeconds = 2250, SortieFirstDelaySeconds = 300, PeriodSeconds = 2700, WaveSeconds = 180, LifetimeSeconds = 900,
                // Outside the north gate (client gate at (-377, 376, 467)).
                Drops = new[] { Toward(new(-370.1f, 375.0f, 499.3f), NorthwestAt), Toward(new(-417.6f, 376.3f, 492.8f), NorthwestAt) },
                Waves = Waves, Boss = ThraxCommander, Relief = Relief,
                // The point's dropship pad (client ArchHumDropshipLandingpadV02, patch 2008-06-23).
                ReliefPad = Toward(new(-416.0f, 375.8f, 399.0f), NorthwestAt), ReliefPost = new(-385f, 375.1f, 455f)
            },
            new()
            {
                PointId = SoutheastBane, FirstDelaySeconds = 600, SortieFirstDelaySeconds = 450, PeriodSeconds = 2700, WaveSeconds = 180, LifetimeSeconds = 900,
                // Outside the west entrance (client ArchBaneOutpostWallMinorEntranceV01 at (54.7, 371.2, -128)).
                Drops = new[] { Toward(new(22.6f, 373.7f, -123.9f), SoutheastAt), Toward(new(29.8f, 373.7f, -100.7f), SoutheastAt) },
                Waves = Waves, Boss = ThraxCommander, Relief = Relief,
                ReliefPad = Toward(new(84f, 370.3f, -132f), SoutheastAt), ReliefPost = new(66f, 370.3f, -128f)
            },
            new()
            {
                PointId = SouthwestBane, FirstDelaySeconds = 600, SortieFirstDelaySeconds = 1350, PeriodSeconds = 2700, WaveSeconds = 180, LifetimeSeconds = 900,
                // Outside the east entrance (client entrance at (-294.7, 375.2, -108)).
                Drops = new[] { Toward(new(-268.3f, 376.9f, -109.9f), SouthwestAt), Toward(new(-282.0f, 374.9f, -70.4f), SouthwestAt) },
                Waves = Waves, Boss = ThraxCommander, Relief = Relief,
                ReliefPad = Toward(new(-318f, 374.2f, -105f), SouthwestAt), ReliefPost = new(-303f, 374.4f, -106f)
            }
        };

        /// <summary>The AFS garrison pools: id, point, what, where. All come and come back by AFS dropship.</summary>
        public static readonly (uint Id, uint Point, Troop Troop, Vector3 At, int RespawnTenths)[] Garrison =
        {
            (692001, NortheastAfs, new(Rifleman, 3, 3), new(90f, 370f, 418f), 3000),
            (692002, NortheastAfs, new(Rifleman, 2, 2), new(124f, 369.4f, 418f), 3000),
            (692003, NortheastAfs, new(MachineGunner, 1, 1), new(110f, 369.2f, 400f), 3000),
            (692004, NortheastAfs, new(Mech, 1, 1), new(100f, 369.2f, 432f), 6000),
            (692011, NorthwestAfs, new(Rifleman, 3, 3), new(-385f, 375.1f, 452f), 3000),
            (692012, NorthwestAfs, new(Rifleman, 2, 2), new(-388f, 374.9f, 408f), 3000),
            (692013, NorthwestAfs, new(MachineGunner, 1, 1), new(-365f, 375.1f, 440f), 3000),
            (692014, NorthwestAfs, new(Mech, 1, 1), new(-372f, 375.1f, 425f), 6000),
            (692021, SoutheastBane, new(Rifleman, 3, 3), new(80f, 370.3f, -128f), 3000),
            (692022, SoutheastBane, new(Rifleman, 1, 1), new(98f, 370.2f, -140f), 3000),
            (692023, SoutheastBane, new(MachineGunner, 1, 1), new(98f, 370.2f, -124f), 3000),
            (692031, SouthwestBane, new(Rifleman, 3, 3), new(-312f, 374.3f, -104f), 3000),
            (692032, SouthwestBane, new(Rifleman, 1, 1), new(-330f, 374.3f, -115f), 3000),
            (692033, SouthwestBane, new(MachineGunner, 1, 1), new(-330f, 374.3f, -96f), 3000)
        };

        public static IEnumerable<string> Up()
        {
            // The Landing Zone garrison against its Thrax private (rifleman 650 HP / 450, armour
            // 120 / 80, damage per second x1.44), here against the Plateau Thrax Soldier (2045 HP,
            // armour 125, 87-131 every 1.3 s); the mech as Landing Zone's against its boss (x2),
            // here against the Thrax Commander (6187 HP, armour 280).
            yield return "INSERT INTO creature_action (id,description,action_id,action_arg_id,range_min,range_max,cooldown,windup,min_damage,max_damage,damage_type) VALUES " +
                "(692001,'Plateau AFS rifle',1,134,1,35,800,0,78,116,1)," +
                "(692002,'Plateau AFS machine gun',149,1,1,30,500,0,48,78,1)," +
                "(692003,'Plateau AFS mech minigun',1,224,1,30,400,0,48,72,1);";
            yield return "INSERT INTO creature (id,comment,class_id,faction,level,max_hp,name_id,run_speed,walk_speed,action1,action2,action3,action4,action5,action6,action7,action8) VALUES " +
                "(692001,'Plateau control point AFS Rifleman',29423,1,31,2950,8717,7,2,692001,0,0,0,0,0,0,0)," +
                "(692002,'Plateau control point AFS Machine Gunner',29433,1,31,2950,8719,7,2,692002,0,0,0,0,0,0,0)," +
                "(692003,'Plateau control point AFS Mech Defender',10235,1,33,12400,10736,9,5,692003,0,0,0,0,0,0,0);";
            yield return "INSERT INTO creature_stat (id,body,mind,spirit,health,armor) VALUES " +
                "(692001,15,15,15,2950,190),(692002,15,15,15,2950,190),(692003,30,30,30,12400,400);";
            yield return "INSERT INTO creature_appearance (id,slot_id,class_id,color) VALUES (692001,13,27220,1),(692002,13,20535,1);";
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
            yield return "DELETE FROM control_point_link WHERE kind=2 AND object_id BETWEEN 692001 AND 692099;";
            yield return "DELETE FROM spawnpool WHERE id BETWEEN 692001 AND 692099;";
            yield return "DELETE FROM creature_appearance WHERE id BETWEEN 692001 AND 692003;";
            yield return "DELETE FROM creature_stat WHERE id BETWEEN 692001 AND 692003;";
            yield return "DELETE FROM creature WHERE id BETWEEN 692001 AND 692003;";
            yield return "DELETE FROM creature_action WHERE id BETWEEN 692001 AND 692003;";
        }

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    }
}
