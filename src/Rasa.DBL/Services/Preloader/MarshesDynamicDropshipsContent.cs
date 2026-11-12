using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using static Rasa.Services.Preloader.DynamicDropshipsContent;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Valverde Marshes' control points as the Plateau's (<see cref="PlateauDynamicDropshipsContent"/>)
    /// and the Pools' (<see cref="PoolsDynamicDropshipsContent"/>): an AFS garrison by AFS dropship at a
    /// point the AFS hold, timed Bane assaults by dropship on it, an AFS relief ship; AFS sorties against
    /// a point the Bane hold, which cannot take it. Research Point (CP 10) holds at the start (World DB
    /// control_point default owner 1); Bane Assertion Camp (CP 11, default owner 0) gets its AFS garrison
    /// and a Bane counter-attack after a capture. Falcon Hold is not a control point (the client has only
    /// the CP01/CP02 tokens 26603/26604 for the Marshes).
    ///
    /// Ids: block 692200-692299 (World DB: free; the Plateau's 692001-692099, the Pools' 692101-692199;
    /// not the Marshes quests' 760300-760399). AFS rows 692201-692203 (creature, creature_action,
    /// creature_stat; creature_appearance 692201-692202); garrison pools and their control_point_link
    /// rows 692201-692217.
    ///
    /// Sources:
    ///  - Research Point: "Occupied by level 40+ Bane infantry", almost never in AFS hands (Ten Ton
    ///    Hammer, Valverde Marshes overview, 2008-03/04), while the World DB starts it AFS. Kept AFS at
    ///    the start (DB), and made the zone's hard point (ESTIMATE): assaults every 30 min, a garrison
    ///    without the Pools' extra troops, so a point no player defends falls soon and then stays Bane
    ///    until players retake it;
    ///  - Marshes control point attacks "more frequent" (patch 2007-12-11), no number;
    ///  - the attackers are "2 to 4 levels over the Zone average" (zerias): the Marshes' Bane are 33-34
    ///    (World DB rows 531045-531048), so the waves are the level 36-37 Thrax (the Mires' rows
    ///    531055-531058) and the level 39 Mires Thrax Commander 531059 (with the camp's own Bane pools,
    ///    Ten Ton Hammer's "level 40+" in reach). The Marshes have no Kael row (DB).
    /// Retail gives no timers or counts for this zone: the numbers are ESTIMATE, as the Plateau's.
    /// </summary>
    public static class MarshesDynamicDropshipsContent
    {
        public const uint MapId = 1454;
        public const uint ResearchPoint = 10, BaneAssertionCamp = 11;

        public const uint IdMin = 692200, IdMax = 692299;

        // Level 36-37 Thrax and the level 39 commander (the Mires' rows, World DB). No Kael, no Shield
        // Drones (see the Plateau's waves).
        public const uint ThraxSoldier = 531055, ThraxGrenadier = 531056, ThraxTechnician = 531057,
            Caretaker = 531058, ThraxCommander = 531059;

        public const uint Rifleman = 692201, MachineGunner = 692202, Mech = 692203;

        // The Pools' wave table (ESTIMATE): 28-33 Bane with the commander.
        private static readonly Troop[][][] Waves =
        {
            new[] { new[] { new Troop(ThraxSoldier, 3, 4), new Troop(ThraxGrenadier, 1, 1) },
                    new[] { new Troop(ThraxSoldier, 3, 3), new Troop(ThraxGrenadier, 1, 1) } },
            new[] { new[] { new Troop(ThraxSoldier, 2, 3), new Troop(ThraxGrenadier, 2, 2), new Troop(Caretaker, 1, 1) },
                    new[] { new Troop(ThraxSoldier, 2, 3), new Troop(ThraxGrenadier, 1, 1), new Troop(ThraxTechnician, 1, 1) } },
            new[] { new[] { new Troop(ThraxSoldier, 4, 4), new Troop(Caretaker, 1, 1), new Troop(ThraxGrenadier, 0, 1) },
                    new[] { new Troop(ThraxSoldier, 2, 3), new Troop(ThraxGrenadier, 2, 2), new Troop(ThraxTechnician, 1, 1) } }
        };

        // As the Plateau's and the Pools' (ESTIMATE there): 3 riflemen and a machine gunner; the sortie
        // "a single AFS Mech and a fire team of 3-4 flunkies" (IGN Vault / planettr, 2007-12).
        private static readonly Troop[] Relief = { new(Rifleman, 3, 3), new(MachineGunner, 1, 1) };
        // The fire team one over retail's 3-4, 4 riflemen and a machine gunner (ESTIMATE; live run
        // 2026-11-05, Plateau Northwest: the 4 riflemen died to 1-2 mortars, the mech alone did not
        // finish the garrison): so that a squad alone sometimes clears a weak Bane garrison, and with a
        // player helping usually does; a player then takes the point (AfsSorties holds the garrison down).
        private static readonly Troop[] SortieSquad = { new(Mech, 1, 1), new(Rifleman, 4, 4), new(MachineGunner, 1, 1) };

        private static LandingZoneAssaultContent.Drop Toward(Vector3 at, Vector3 point) =>
            new(at, Math.Atan2(-(point.X - at.X), -(point.Z - at.Z)));

        // The points' positions (World DB control_point).
        private static readonly Vector3 ResearchAt = new(-321.1f, 217.066f, -312.847f), AssertionAt = new(104.669f, 219.084f, -325.566f);

        // Research Point: first assault 10 min after the start, then every 30 min (ESTIMATE: the Pools'
        // "frequently overrun" period, for Ten Ton Hammer's "almost never AFS"). Bane Assertion Camp: a
        // captured camp is counter-attacked after 10 min (as the Plateau, TaRapedia "after a delay"),
        // then every 45 min (ESTIMATE, the Plateau's: no "overrun" source for it). Sorties: 7.5 min after
        // the Bane hold a point (the server start for the camp), then every 30 min (ESTIMATE).
        public static readonly Assault[] Assaults =
        {
            new()
            {
                MapId = MarshesDynamicDropshipsContent.MapId, PointId = ResearchPoint, FirstDelaySeconds = 600, SortieFirstDelaySeconds = 450,
                PeriodSeconds = 1800, WaveSeconds = 90, LifetimeSeconds = 900,
                // West of the outpost, 62 m from its nearest turret (outside the north fence
                // gate, client ArchHumOutpostFenceGateV02 at (-296, 216, -244), the troops landed 16-18 m from
                // the fence and platform turrets and went for them), and outside the east fence gate (the same
                // at (-252, 216, -332)); places ESTIMATE, on the navmesh.
                Drops = new[] { Toward(new(-383.9f, 216.9f, -329.6f), ResearchAt), Toward(new(-232.0f, 214.4f, -332.0f), ResearchAt) },
                Waves = Waves, Boss = ThraxCommander, Relief = Relief, Sortie = SortieSquad,
                // The point's dropship pad (client ArchHumDropshipLandingpadV02 at (-315.5, 216.3, -336.5));
                // the post 10 m north of it (ESTIMATE).
                ReliefPad = Toward(new(-315.5f, 216.5f, -336.5f), ResearchAt), ReliefPost = new(-315.5f, 216.6f, -326.5f)
            },
            new()
            {
                MapId = MarshesDynamicDropshipsContent.MapId, PointId = BaneAssertionCamp, FirstDelaySeconds = 600, SortieFirstDelaySeconds = 450,
                PeriodSeconds = 2700, WaveSeconds = 90, LifetimeSeconds = 900,
                // Outside the south wall (client ArchBaneOutpostWallMinorEntranceV01 at (120.2, 219.3, -344)),
                // west and east of the entrance, clear of Overseer Cast'ka's pool (125, 214, -364); places
                // ESTIMATE, on the navmesh.
                Drops = new[] { Toward(new(95.0f, 218.7f, -380.0f), AssertionAt), Toward(new(150.0f, 217.1f, -380.0f), AssertionAt) },
                Waves = Waves, Boss = ThraxCommander, Relief = Relief, Sortie = SortieSquad,
                // No pad at the camp (client map): the yard by the trenches (85-124, 217.7, -303); the post
                // at the point (ESTIMATE).
                ReliefPad = Toward(new(105.0f, 218.7f, -305.2f), AssertionAt), ReliefPost = new(104.7f, 219.1f, -325.6f)
            }
        };

        /// <summary>The AFS garrison pools: id, point, what, where. All come and come back by AFS dropship.</summary>
        public static readonly (uint Id, uint Point, Troop Troop, Vector3 At, int RespawnTenths)[] Garrison =
        {
            // Research Point: 8 riflemen, 2 machine gunners, 2 mechs, as a Plateau AFS point: it has six
            // AFS turrets (ControlPointTurrets, World DB 551151-551161), as Northwest AFS (ESTIMATE). No more
            // troops than that, so that it falls more often than the Pools' Retread Outpost (Ten Ton Hammer).
            // Places inside the fence, ESTIMATE, on the navmesh.
            (692201, ResearchPoint, new(Rifleman, 3, 3), new(-300f, 216.5f, -280f), 3000),
            (692202, ResearchPoint, new(Rifleman, 3, 3), new(-270f, 216.1f, -320f), 3000),
            (692203, ResearchPoint, new(Rifleman, 2, 2), new(-333.6f, 217.2f, -280f), 3000),
            (692204, ResearchPoint, new(MachineGunner, 1, 1), new(-305f, 216.6f, -300f), 3000),
            (692205, ResearchPoint, new(MachineGunner, 1, 1), new(-338.7f, 217f, -319.3f), 3000),
            (692206, ResearchPoint, new(Mech, 1, 1), new(-285f, 216.3f, -300f), 6000),
            (692207, ResearchPoint, new(Mech, 1, 1), new(-320f, 217f, -289.7f), 6000),
            // A captured Bane Assertion Camp: 10 riflemen, 2 machine gunners, 4 mechs, as a captured
            // Plateau Bane point: no AFS turrets there (ControlPointTurrets has no mounts for CP 11).
            // Places inside the walls, ESTIMATE, on the navmesh.
            (692208, BaneAssertionCamp, new(Rifleman, 3, 3), new(90f, 219.6f, -320f), 3000),
            (692209, BaneAssertionCamp, new(Rifleman, 3, 3), new(118f, 218.9f, -318f), 3000),
            (692210, BaneAssertionCamp, new(Rifleman, 2, 2), new(100f, 218.8f, -335f), 3000),
            (692211, BaneAssertionCamp, new(Rifleman, 2, 2), new(112f, 219.2f, -335f), 3000),
            (692212, BaneAssertionCamp, new(MachineGunner, 1, 1), new(95f, 218.9f, -312f), 3000),
            (692213, BaneAssertionCamp, new(MachineGunner, 1, 1), new(122f, 218.5f, -315f), 3000),
            (692214, BaneAssertionCamp, new(Mech, 1, 1), new(125f, 218.5f, -335f), 6000),
            (692215, BaneAssertionCamp, new(Mech, 1, 1), new(132f, 218.5f, -315f), 6000),
            (692216, BaneAssertionCamp, new(Mech, 1, 1), new(115f, 218.8f, -308f), 6000),
            (692217, BaneAssertionCamp, new(Mech, 1, 1), new(88f, 218.6f, -312f), 6000)
        };

        /// <summary>The migration's rows, each insert only where its key is not there yet (DynamicDropshipsContent.Guarded): safe to run twice.</summary>
        public static IEnumerable<string> Up() => DynamicDropshipsContent.Guarded(Rows());

        private static IEnumerable<string> Rows()
        {
            // Balance (ESTIMATE): the Pools' AFS times the Mires' Thrax over the Marshes' (health 4460 /
            // 3439 and damage 98-130 / 76-100: 1.3 each; World DB); armour times 170 / 155 (1.097). Rifle
            // 213-317 -> 277-412, machine gun 131-213 -> 170-277, minigun 131-196 -> 170-255; health 9200
            // -> 11960, mech 38700 -> 50310; armour 505 -> 554, mech 1063 -> 1166. Levels 37 and 39: the
            // Pools' 34 and 36 plus the 3 levels from the Marshes' Thrax to the Mires'. Looks and names as
            // the Plateau's and the Pools' (classes 29423, 29433, 10235; names 8717, 8719, 10736).
            yield return "INSERT INTO creature_action (id,description,action_id,action_arg_id,range_min,range_max,cooldown,windup,min_damage,max_damage,damage_type) VALUES " +
                "(692201,'Marshes AFS rifle',1,134,1,35,800,0,277,412,1)," +
                "(692202,'Marshes AFS machine gun',149,1,1,30,500,0,170,277,1)," +
                "(692203,'Marshes AFS mech minigun',1,224,1,30,400,0,170,255,1);";
            yield return "INSERT INTO creature (id,comment,class_id,faction,level,max_hp,name_id,run_speed,walk_speed,action1,action2,action3,action4,action5,action6,action7,action8) VALUES " +
                "(692201,'Marshes control point AFS Rifleman',29423,1,37,11960,8717,7,2,692201,0,0,0,0,0,0,0)," +
                "(692202,'Marshes control point AFS Machine Gunner',29433,1,37,11960,8719,7,2,692202,0,0,0,0,0,0,0)," +
                "(692203,'Marshes control point AFS Mech Defender',10235,1,39,50310,10736,9,5,692203,0,0,0,0,0,0,0);";
            yield return "INSERT INTO creature_stat (id,body,mind,spirit,health,armor) VALUES " +
                "(692201,15,15,15,11960,554),(692202,15,15,15,11960,554),(692203,30,30,30,50310,1166);";
            yield return "INSERT INTO creature_appearance (id,slot_id,class_id,color) VALUES (692201,13,27220,1),(692202,13,20535,1);";
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
            yield return $"DELETE FROM control_point_link WHERE kind=2 AND object_id BETWEEN {IdMin} AND {IdMax};";
            yield return $"DELETE FROM spawnpool WHERE id BETWEEN {IdMin} AND {IdMax};";
            yield return "DELETE FROM creature_appearance WHERE id BETWEEN 692201 AND 692203;";
            yield return "DELETE FROM creature_stat WHERE id BETWEEN 692201 AND 692203;";
            yield return "DELETE FROM creature WHERE id BETWEEN 692201 AND 692203;";
            yield return "DELETE FROM creature_action WHERE id BETWEEN 692201 AND 692203;";
        }

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    }
}
